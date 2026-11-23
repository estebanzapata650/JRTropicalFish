#!/usr/bin/env node
/**
 * Carga el listado de especies autorizadas por la AUNAP (Resolución 1924 de 2015, hoja «AUNAP» del Excel
 * «Listado de la Aunap.xlsx») y enlaza los productos cuyo nombre científico es idéntico.
 *
 *   IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run aunap:importar -- "<Listado de la Aunap.xlsx>" [--simular]
 *
 * La hoja trae varios grupos (códigos 1–522, 600–611, 700–739 y 1*–24*), cada uno con su fila de encabezado;
 * esas filas se saltan. Es idempotente: actualiza por código. Requiere el permiso exportaciones:configurar.
 * Los productos que no coinciden por nombre exacto (sinónimos o especies que no están en la resolución) se
 * revisan en Exportaciones → AUNAP.
 */
import { readFileSync } from "node:fs";
import { leerZip } from "./lib/xlsx.mjs";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const ruta = args.find((a) => !a.startsWith("--"));
if (!ruta) {
  console.error('Uso: npm run aunap:importar -- "<Listado de la Aunap.xlsx>" [--simular]');
  process.exit(1);
}

const texto = (s) => s.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, "&");

/** Filas crudas de la hoja (columna → valor): la hoja tiene título y encabezados repetidos, no sirve leerHoja. */
function filasCrudas(zip, nombreHoja) {
  const libro = zip("xl/workbook.xml");
  const hoja = [...libro.matchAll(/<sheet [^>]*name="([^"]+)"[^>]*r:id="([^"]+)"/g)].find((m) => m[1] === nombreHoja);
  if (!hoja) throw new Error(`No hay una hoja llamada «${nombreHoja}».`);
  const rels = zip("xl/_rels/workbook.xml.rels");
  const destino = [...rels.matchAll(/<Relationship [^>]*>/g)].map((m) => m[0]).find((r) => r.includes(`Id="${hoja[2]}"`)).match(/Target="([^"]+)"/)[1];
  const xml = zip(`xl/${destino.replace(/^\/?xl\//, "")}`);
  const compartidas = [...(zip("xl/sharedStrings.xml") ?? "").matchAll(/<si>([\s\S]*?)<\/si>/g)].map((m) =>
    texto([...m[1].matchAll(/<t[^>]*>([\s\S]*?)<\/t>/g)].map((t) => t[1]).join("")),
  );
  return [...xml.matchAll(/<row [^>]*>([\s\S]*?)<\/row>/g)].map((fila) => {
    const celdas = {};
    for (const c of fila[1].matchAll(/<c r="([A-Z]+)\d+"([^>]*?)(?:\/>|>([\s\S]*?)<\/c>)/g)) {
      const tipo = c[2].match(/t="([^"]+)"/)?.[1];
      const v = (c[3] ?? "").match(/<v>([\s\S]*?)<\/v>/)?.[1];
      celdas[c[1]] = (tipo === "s" ? compartidas[Number(v)] : v !== undefined ? texto(v) : "")?.trim() ?? "";
    }
    return celdas;
  });
}

const marcada = (v) => String(v ?? "").trim().toLowerCase() === "x";
const especies = filasCrudas(leerZip(readFileSync(ruta)), "AUNAP")
  .filter((f) => /^\d+\*?$/.test(f.A ?? "") && f.B)
  .map((f) => ({
    codigo: f.A,
    nombreCientifico: f.B.replace(/\s+/g, " "),
    nombreComun: f.C || null,
    amazonia: marcada(f.D),
    orinoquia: marcada(f.E),
    magdalena: marcada(f.F),
    pacifico: marcada(f.G),
    caribe: marcada(f.H),
    orden: f.I || null,
    familia: f.J || null,
  }));

console.log(`${especies.length} especies en el listado.`);
if (simular) {
  console.log(especies.slice(0, 5));
  process.exit(0);
}

const API = process.env.API_URL ?? "http://localhost:5377";
const { IMPORTAR_EMAIL: email, IMPORTAR_PASSWORD: password } = process.env;
if (!email || !password) {
  console.error("Define IMPORTAR_EMAIL e IMPORTAR_PASSWORD (usuario con exportaciones:configurar).");
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

const importacion = await api("POST", "/exportaciones/aunap/especies/importar", { especies });
console.log(`Especies nuevas: ${importacion.nuevas} · actualizadas: ${importacion.actualizadas}`);
const { enlazados } = await api("POST", "/exportaciones/aunap/productos/enlazar-automatico");
const productos = await api("GET", "/exportaciones/aunap/productos");
const sin = productos.filter((p) => p.activo && !p.especieAunapId);
console.log(`Productos enlazados ahora por nombre idéntico: ${enlazados}.`);
console.log(`Productos activos sin código AUNAP: ${sin.length} de ${productos.filter((p) => p.activo).length} (revisar en Exportaciones → AUNAP).`);
