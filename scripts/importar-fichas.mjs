#!/usr/bin/env node
/**
 * Carga las fichas técnicas del PDF «Ficha Técnica por Especie» (FT.CA.01) en Sanidad → Fichas técnicas.
 *
 *   IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run sanidad:importar-fichas -- "<Ficha Técnica por Especie.pdf>" [--simular] [--sobrescribir]
 *
 * Lee el PDF con `scripts/extraer-palabras-pdf` (necesita el SDK de .NET) y asigna cada ficha a su producto por el
 * código interno, solo si el nombre de la ficha coincide con el del producto (el PDF trae códigos repetidos o
 * equivocados). No toca una ficha que ya exista, salvo con --sobrescribir. Requiere el permiso sanidad:gestionar.
 * Siempre deja en docs/temp/fichas-revision.csv las fichas que no se cargaron, para revisarlas a mano.
 */
import { execFileSync } from "node:child_process";
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { asignarFichas, leerFichas } from "./lib/fichas-pdf.mjs";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const sobrescribir = args.includes("--sobrescribir");
const ruta = args.find((a) => !a.startsWith("--"));
if (!ruta) {
  console.error('Uso: npm run sanidad:importar-fichas -- "<Ficha Técnica por Especie.pdf>" [--simular] [--sobrescribir]');
  process.exit(1);
}

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const temporal = mkdtempSync(join(tmpdir(), "fichas-"));
const salida = join(temporal, "palabras.json");
try {
  execFileSync("dotnet", ["run", "--project", join(raiz, "scripts/extraer-palabras-pdf"), "--", resolve(ruta), salida], { stdio: "inherit" });
} catch {
  console.error("No se pudo leer el PDF (¿está instalado el SDK de .NET?).");
  process.exit(1);
}
const { fichas, problemas } = leerFichas(JSON.parse(readFileSync(salida, "utf8")));
rmSync(temporal, { recursive: true, force: true });
console.log(`${fichas.length} fichas leídas.${problemas.length ? ` Problemas: ${problemas.join("; ")}` : ""}`);

const API = process.env.API_URL ?? "http://localhost:5377";
const { IMPORTAR_EMAIL: email, IMPORTAR_PASSWORD: password } = process.env;
if (!email || !password) {
  console.error("Define IMPORTAR_EMAIL e IMPORTAR_PASSWORD (usuario con sanidad:gestionar).");
  process.exit(1);
}
const login = await fetch(`${API}/api/v1/auth/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email, password }),
});
if (!login.ok) {
  console.error(`No se pudo iniciar sesión (${login.status}).`);
  process.exit(1);
}
const { token } = await login.json();
const api = async (metodo, rutaApi, cuerpo) => {
  const r = await fetch(`${API}/api/v1${rutaApi}`, {
    method: metodo,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });
  const t = await r.text();
  if (!r.ok) throw new Error(`${metodo} ${rutaApi} → ${r.status} ${t}`);
  return t ? JSON.parse(t) : null;
};

const productos = (await api("GET", "/catalogos/productos")).map((p) => ({ id: p.id, codigo: p.codigo, nombreCientifico: p.nombreCientifico }));
const existentes = new Set((await api("GET", "/sanidad/fichas")).filter((f) => f.tieneFicha).map((f) => f.productoId));
const { asignadas, sinProducto, noCoinciden } = asignarFichas(fichas, productos);

let creadas = 0, actualizadas = 0, omitidas = 0;
const errores = [];
for (const { producto, ficha: f } of asignadas) {
  const existe = existentes.has(producto.id);
  if (existe && !sobrescribir) {
    omitidas++;
    continue;
  }
  if (simular) continue;
  try {
    await api("PUT", `/sanidad/fichas/${producto.id}`, {
      procedencia: f.procedencia, caracteristicas: f.caracteristicas, dimorfismo: f.dimorfismo,
      phMin: f.phMin, phMax: f.phMax, ghMin: f.ghMin, ghMax: f.ghMax, temperaturaMin: f.temperaturaMin, temperaturaMax: f.temperaturaMax,
      etologia: f.etologia, empaque: f.empaque, sensibilidadQuimica: null, ayunoLlegadaHoras: 24,
    });
    if (existe) actualizadas++;
    else creadas++;
  } catch (e) {
    errores.push(`${producto.codigo}: ${e.message}`);
  }
}

const csv = (v) => `"${String(v ?? "").replace(/"/g, '""')}"`;
const filas = [
  ["motivo", "codigo", "producto", "ficha del PDF"],
  ...noCoinciden.map((n) => ["El nombre de la ficha no coincide con el producto", n.codigo, n.producto, n.fichas.join(" / ")]),
  ...sinProducto.map((s) => ["El código no existe en el catálogo", s.codigo, "", s.especie]),
  ...errores.map((e) => ["Error al guardar", e.split(":")[0], "", e]),
];
mkdirSync(join(raiz, "docs/temp"), { recursive: true });
writeFileSync(join(raiz, "docs/temp/fichas-revision.csv"), "﻿" + filas.map((f) => f.map(csv).join(";")).join("\r\n"));

console.log(`${simular ? "[simulación] " : ""}Fichas asignadas a productos: ${asignadas.length}.`);
console.log(`Creadas: ${simular ? 0 : creadas} · actualizadas: ${simular ? 0 : actualizadas} · ya existían (no se tocaron): ${omitidas}.`);
console.log(`Para revisar a mano (docs/temp/fichas-revision.csv): ${noCoinciden.length} con nombre distinto al producto, ${sinProducto.length} códigos que no están en el catálogo, ${errores.length} errores.`);
if (errores.length) console.log(errores.join("\n"));
