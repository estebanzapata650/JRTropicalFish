#!/usr/bin/env node
/**
 * Migra el inventario de la app anterior (Excel «BASES_TROPICAL FISH», hecha en AppSheet):
 *   1. Acuarios (hoja «Base acuarios») más los que aparezcan en movimientos y no estén en la base.
 *   2. Saldo inicial: por cada lote LOT-xxxx de «Detalle_lotes», el stock vivo actual por acuario y
 *      condición, calculado de «Movimientos» (Entrada suma, Salida resta; estado «Enfermo» = enfermo).
 *      Los saldos negativos (inconsistencias del Excel) se dejan en 0 y se reportan.
 *      El "acuario" virtual ACLIMATACION de la app anterior es nuestro «por ubicar» (acuario nulo).
 *
 *   IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run inventario:importar -- "<BASES.xlsx>" [--simular]
 *
 * Pasa por la API (valida, audita, deja el kardex con movimientos «saldo_inicial») y es idempotente:
 * acuarios y lotes que ya existen (409) se omiten. Requiere productos, proveedores y centros importados
 * antes (npm run catalogo:importar).
 */
import { readFileSync } from "node:fs";
import { leerHoja, leerZip } from "./lib/xlsx.mjs";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const ruta = args.find((a) => !a.startsWith("--"));
if (!ruta) {
  console.error('Uso: npm run inventario:importar -- "<BASES_TROPICAL FISH.xlsx>" [--simular]');
  process.exit(1);
}

const clave = (s) => String(s ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().replace(/\s+/g, " ").trim();
const numero = (s) => {
  const n = Number(String(s ?? "").replace(",", "."));
  return Number.isFinite(n) ? n : 0;
};
/** Fecha serial de Excel (días desde 1899-12-30) → yyyy-MM-dd. */
const fechaExcel = (s) => {
  const n = numero(s);
  if (!n) return null;
  return new Date(Math.round((n - 25569) * 86400000)).toISOString().slice(0, 10);
};

/** Ubicación virtual de la app anterior para lo recién llegado = nuestro «por ubicar». */
const POR_UBICAR = "ACLIMATACION";
const esPorUbicar = (codigo) => clave(codigo) === clave(POR_UBICAR);

const zip = leerZip(readFileSync(ruta));
const baseAcuariosTodos = leerHoja(zip, "Base acuarios").filter((a) => a["Código"]);
const baseAcuarios = baseAcuariosTodos.filter((a) => !esPorUbicar(a["Código"]));
const aclimatacionExcel = numero(baseAcuariosTodos.find((a) => esPorUbicar(a["Código"]))?.["Cantidad actual"]);
const detalleLotes = leerHoja(zip, "Detalle_lotes").filter((d) => d.ID);
const movimientos = leerHoja(zip, "Movimientos").filter((m) => m.ID_Lote && m.Acuario);

// --- Acuarios ---
const acuarios = new Map(
  baseAcuarios.map((a) => [
    a["Código"].trim().toUpperCase(),
    {
      codigo: a["Código"].trim().toUpperCase(),
      sector: (a.Sector || a["Código"].trim()[0]).toUpperCase(),
      nivel: Math.round(numero(a.Nivel)),
      posicion: Math.round(numero(a["Posición"])),
      capacidadLitros: numero(a["Capacidad en lts"]) || null,
      cantidadExcel: numero(a["Cantidad actual"]),
    },
  ]),
);
// Acuarios citados en movimientos que no están en la base (ej. C101): se deducen del código.
const deducidos = [];
for (const codigo of new Set(movimientos.map((m) => m.Acuario.trim().toUpperCase()))) {
  if (acuarios.has(codigo) || esPorUbicar(codigo)) continue;
  const m = codigo.match(/^([A-Z]+)(\d)(\d+)$/);
  acuarios.set(codigo, { codigo, sector: m?.[1] ?? codigo[0], nivel: Number(m?.[2] ?? 0), posicion: Number(m?.[3] ?? 0), capacidadLitros: null, cantidadExcel: 0 });
  deducidos.push(codigo);
}

// --- Saldo por lote × acuario × condición ---
const neto = new Map();
for (const m of movimientos) {
  const condicion = clave(m.Estado) === "enfermo" ? "enfermo" : "sano";
  const k = `${m.ID_Lote.trim()}|${m.Acuario.trim().toUpperCase()}|${condicion}`;
  const signo = clave(m["Tipo de movimiento"]) === "entrada" ? 1 : -1;
  neto.set(k, (neto.get(k) ?? 0) + signo * Math.round(numero(m.Cantidad)));
}
const negativos = [...neto].filter(([, v]) => v < 0).map(([k, v]) => `${k.replaceAll("|", " · ")}: ${v}`);

const porLote = new Map();
for (const [k, v] of neto) {
  if (v <= 0) continue;
  const [lote, acuario, condicion] = k.split("|");
  (porLote.get(lote) ?? porLote.set(lote, []).get(lote)).push({ acuarioCodigo: esPorUbicar(acuario) ? null : acuario, condicion, cantidad: v });
}

const detalle = new Map(detalleLotes.map((d) => [d.ID.trim(), d]));
const lotes = [...porLote].map(([codigo, existencias]) => {
  const d = detalle.get(codigo) ?? {};
  const stock = existencias.reduce((s, e) => s + e.cantidad, 0);
  const muertos = Math.round(numero(d.Muertos));
  return {
    codigo,
    sku: String(d["Producto (SKU)"] ?? movimientos.find((m) => m.ID_Lote.trim() === codigo)?.Especie ?? "").trim().toUpperCase(),
    proveedor: d.Proveedor ?? null,
    centro: d["Centro de acopio"] ?? null,
    fechaIngreso: fechaExcel(d.Fecha),
    recibidos: Math.max(Math.round(numero(d.Confirmado)), stock + muertos),
    muertos,
    existencias,
  };
});

// Contraste con «Cantidad actual» de la base de acuarios.
const porAcuario = new Map();
for (const l of lotes) for (const e of l.existencias) porAcuario.set(e.acuarioCodigo ?? POR_UBICAR, (porAcuario.get(e.acuarioCodigo ?? POR_UBICAR) ?? 0) + e.cantidad);
const diferencias = [...acuarios.values(), { codigo: POR_UBICAR, cantidadExcel: aclimatacionExcel }]
  .filter((a) => (porAcuario.get(a.codigo) ?? 0) !== a.cantidadExcel)
  .map((a) => `${a.codigo}: Excel ${a.cantidadExcel}, movimientos ${porAcuario.get(a.codigo) ?? 0}`);

console.log(
  `${acuarios.size} acuarios (${deducidos.length} deducidos de movimientos: ${deducidos.join(", ") || "ninguno"}) · ` +
    `${lotes.length} lotes con stock · ${lotes.reduce((s, l) => s + l.existencias.reduce((t, e) => t + e.cantidad, 0), 0)} peces`,
);
if (negativos.length) console.log(`Saldos negativos en el Excel, cargados como 0 (${negativos.length}):\n  ${negativos.join("\n  ")}`);
if (diferencias.length) console.log(`Acuarios cuyo total no coincide con «Cantidad actual» (${diferencias.length}):\n  ${diferencias.join("\n  ")}`);

if (simular) {
  console.table(lotes.slice(0, 12).map((l) => ({ lote: l.codigo, sku: l.sku, fecha: l.fechaIngreso, proveedor: l.proveedor, acuarios: l.existencias.map((e) => `${e.acuarioCodigo ?? "por ubicar"}${e.condicion === "enfermo" ? "(enf)" : ""}:${e.cantidad}`).join(" ") })));
  process.exit(0);
}

// --- API ---
const API = process.env.API_URL ?? "http://localhost:5377";
const { IMPORTAR_EMAIL: email, IMPORTAR_PASSWORD: password } = process.env;
if (!email || !password) {
  console.error("Define IMPORTAR_EMAIL e IMPORTAR_PASSWORD (usuario con inventario:ajustar y catálogos).");
  process.exit(1);
}
const login = await fetch(`${API}/api/v1/auth/login`, {
  method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ email, password }),
});
if (!login.ok) {
  console.error(`No se pudo iniciar sesión (${login.status}).`);
  process.exit(1);
}
const { token } = await login.json();
const api = async (metodo, ruta, cuerpo) => {
  const r = await fetch(`${API}/api/v1${ruta}`, {
    method: metodo, headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });
  return { status: r.status, json: await r.json().catch(() => null) };
};

const resumen = { acuarios: { creados: 0, existentes: 0, errores: 0 }, lotes: { creados: 0, existentes: 0, errores: 0 } };
const contar = (entidad, r, etiqueta) => {
  if (r.status === 201) resumen[entidad].creados++;
  else if (r.status === 409) resumen[entidad].existentes++;
  else {
    resumen[entidad].errores++;
    console.warn(`  ✗ ${entidad} ${etiqueta}: ${r.status} ${r.json?.error ?? ""}`);
  }
};

for (const a of acuarios.values()) {
  contar("acuarios", await api("POST", "/inventario/acuarios", { codigo: a.codigo, sector: a.sector, nivel: a.nivel, posicion: a.posicion, capacidadLitros: a.capacidadLitros, activo: true }), a.codigo);
}

const productos = new Map((await api("GET", "/catalogos/productos")).json.map((p) => [p.codigo.toUpperCase(), p.id]));
const proveedores = new Map((await api("GET", "/catalogos/proveedores")).json.map((p) => [clave(p.nombre), p.id]));
const centros = new Map((await api("GET", "/catalogos/centros-acopio")).json.map((c) => [clave(c.nombre), c.id]));

for (const l of lotes) {
  const productoId = productos.get(l.sku);
  if (!productoId) {
    resumen.lotes.errores++;
    console.warn(`  ✗ lote ${l.codigo}: el producto ${l.sku || "(sin SKU)"} no está en el catálogo`);
    continue;
  }
  contar("lotes", await api("POST", "/inventario/saldo-inicial", {
    codigo: l.codigo, productoId, proveedorId: proveedores.get(clave(l.proveedor)) ?? null, centroAcopioId: centros.get(clave(l.centro)) ?? null,
    fechaIngreso: l.fechaIngreso, costoUnitario: null, recibidos: l.recibidos, muertos: l.muertos, existencias: l.existencias,
  }), l.codigo);
}

console.table(resumen);
process.exitCode = resumen.acuarios.errores + resumen.lotes.errores > 0 ? 1 : 0;
