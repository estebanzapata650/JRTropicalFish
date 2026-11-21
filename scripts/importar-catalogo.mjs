#!/usr/bin/env node
/**
 * Importa el catálogo desde los Excel del negocio:
 *   - «JR MAIN STOCK LIST.xlsx»: hoja Listado (productos y 8 columnas de precio) + hoja
 *     «Listado Productos APP» (tallas numéricas, bolsa estándar y centros de acopio por producto).
 *   - «BASES_TROPICAL FISH.xlsx»: hojas Centros_acopio (con cuenca) y Proveedores.
 *
 *   IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run catalogo:importar -- "<stock list.xlsx>" "<bases.xlsx>" [--simular]
 *
 * Orden: cuencas → centros de acopio → grupos (por prefijo del código) → productos con precios → proveedores.
 * Todo pasa por la API (valida y audita). Es idempotente: lo que ya existe (409) se omite.
 * `--simular` muestra el resultado del mapeo sin llamar a la API.
 */
import { readFileSync } from "node:fs";
import { leerHoja, leerZip } from "./lib/xlsx.mjs";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const [rutaStock, rutaBases] = args.filter((a) => !a.startsWith("--"));
if (!rutaStock || !rutaBases) {
  console.error('Uso: npm run catalogo:importar -- "<JR MAIN STOCK LIST.xlsx>" "<BASES_TROPICAL FISH.xlsx>" [--simular]');
  process.exit(1);
}

// --- Utilidades de mapeo ---
const clave = (s) => String(s ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().replace(/\s+/g, " ").trim();
const texto = (s) => (String(s ?? "").replace(/\s+/g, " ").trim() || null);
const numero = (s) => {
  const n = Number(String(s ?? "").replace(",", "."));
  return Number.isFinite(n) ? n : null;
};
/** Números que Excel guardó como 1.121707658E9 → "1121707658". */
const entero = (s) => {
  const t = String(s ?? "").trim();
  if (!t) return null;
  const n = Number(t);
  return Number.isFinite(n) && /e|\.0+$/i.test(t) ? BigInt(Math.round(n)).toString() : t;
};

/** "5,0 - 7,0" → {min:5,max:7}; "12.0" → {min:12,max:12}; "small" → {etiqueta}. */
function talla(valor) {
  const t = texto(valor);
  if (!t) return {};
  const nums = t.match(/\d+(?:[.,]\d+)?/g)?.map((x) => Number(x.replace(",", ".")));
  if (nums?.length >= 2) return { min: Math.min(nums[0], nums[1]), max: Math.max(nums[0], nums[1]) };
  if (nums?.length === 1) return { min: nums[0], max: nums[0] };
  return { etiqueta: t.toLowerCase().slice(0, 30) };
}

/** "40 ~ 50" → {min:40,max:50}; "100.0" → {min:100,max:100}. */
function bolsa(valor) {
  const nums = String(valor ?? "").match(/\d+(?:\.\d+)?/g)?.map((x) => Math.round(Number(x))).filter((n) => n > 0);
  if (!nums?.length) return {};
  return { min: Math.min(...nums), max: Math.max(...nums) };
}

function disponibilidad(valor) {
  const t = texto(valor);
  if (!t) return null;
  return t.toLowerCase() === "ok" ? "OK" : t.slice(0, 50);
}

const TIPOS = { salvajes: "salvaje", salvaje: "salvaje", criadero: "criadero", especiales: "especial", especial: "especial" };

/** Prefijo del código → nombre del grupo (editable después en Maestros). */
const GRUPOS = {
  AP: "Apistogramas y cíclidos enanos",
  CC: "Cíclidos especiales",
  CF: "Bagres",
  CH: "Carácidos especiales (pirañas)",
  CO: "Corydoras",
  CR: "Crustáceos",
  LO: "Loricáridos (plecos)",
  OT: "Otros",
  PC: "Criadero",
  PT: "Rayas (Potamotrygon)",
  ST: "Estrigatas (pez hacha)",
  TE: "Tetras",
};

/** Columna del Excel → código de la lista de precios (sembradas por la migración FuncionesCatalogos). */
const COLUMNAS_PRECIO = {
  "STANDARD PRICE": "STANDARD",
  "WHOLESALE ASIAN PRICE": "MAYORISTA_ASIA",
  "GLASER PRICE": "GLASER",
  "MINORISTAS DEL EXTERIOR": "MINORISTA_EXTERIOR",
  "COSTO COMPRA": "COSTO",
  "MAYORISTA NACIONAL": "MAYORISTA_NACIONAL",
  "DAVID RAMIREZ": "DAVID_RAMIREZ",
  "AL DETAL NACIONAL": "DETAL_NACIONAL",
};

// --- Lectura de los Excel ---
const stock = leerZip(readFileSync(rutaStock));
const bases = leerZip(readFileSync(rutaBases));
const listado = leerHoja(stock, "Listado").filter((f) => f.COD);
const app = new Map(leerHoja(stock, "Listado Productos APP").filter((f) => f.Cod).map((f) => [f.Cod.trim().toUpperCase(), f]));
const centrosExcel = leerHoja(bases, "Centros_acopio").filter((f) => f["Centro de Acopio"]);
const proveedoresExcel = leerHoja(bases, "Proveedores").filter((f) => f["Nombre tercero"]);

// Productos: el Listado manda (trae precios); la hoja APP completa tallas, bolsa y centros.
const codigosListado = new Set(listado.map((f) => f.COD.trim().toUpperCase()));
const filasProducto = [
  ...listado.map((f) => ({ codigo: f.COD.trim().toUpperCase(), l: f, a: app.get(f.COD.trim().toUpperCase()) })),
  ...[...app.entries()].filter(([c]) => !codigosListado.has(c)).map(([codigo, a]) => ({ codigo, l: null, a })),
];

const productos = filasProducto.map(({ codigo, l, a }) => {
  const prefijo = codigo.replace(/\d+$/, "");
  const tallaApp = { min: numero(a?.["Talla Mínima"]), max: numero(a?.["Talla Máxima"]) };
  const tallaListado = talla(l?.["SIZE (CMS)"]);
  const usarApp = tallaApp.min > 0 || tallaApp.max > 0;
  const bolsaListado = bolsa(l?.["QTY/BAG"]);
  const bolsaApp = bolsa(a?.["Cantidad Empaque Estándar"]);
  const b = bolsaListado.min ? bolsaListado : bolsaApp;
  const cientifico = texto(l?.["SCIENTIFIC NAME"] ?? a?.["Scientific Name"]) ?? texto(l?.["COMMON NAMES"] ?? a?.["Common Name"]);
  const precios = Object.fromEntries(
    Object.entries(COLUMNAS_PRECIO)
      .map(([col, lista]) => [lista, numero(l?.[col])])
      .filter(([, v]) => v > 0),
  );
  return {
    codigo,
    prefijo,
    tipo: TIPOS[clave(l?.TIPO ?? a?.Tipo)] ?? "salvaje",
    nombreCientifico: cientifico,
    nombreComun: texto(l?.["COMMON NAMES"] ?? a?.["Common Name"]),
    nombreEspanol: texto(l?.["SPANISH NAMES"] ?? a?.["Spanish Name"]),
    tallaMinCm: usarApp ? (tallaApp.min || tallaApp.max) : (tallaListado.min ?? null),
    tallaMaxCm: usarApp ? (tallaApp.max || tallaApp.min) : (tallaListado.max ?? null),
    tallaEtiqueta: usarApp ? null : (tallaListado.etiqueta ?? null),
    bolsaMin: b.min ?? null,
    bolsaMax: b.max ?? null,
    disponibilidad: disponibilidad(l?.STATUS),
    centros: [...new Set([a?.["Centro Acopio 1"], a?.["Centro Acopio 2"], a?.["Centro Acopio 3"]].map(texto).filter(Boolean))].slice(0, 3),
    precios,
  };
});

const proveedores = proveedoresExcel.map((f) => {
  // Un "teléfono" sin dígitos suficientes ("No requiere") no es un teléfono: va a las notas.
  const telefonoExcel = entero(f["Teléfono."] ?? f["Teléfono"]);
  const esTelefono = (telefonoExcel?.replace(/\D/g, "").length ?? 0) >= 7;
  return {
    nombre: texto(f["Nombre tercero"]),
    tipo: texto(f["Tipo ID"])?.toUpperCase() ?? "CC",
    numero: entero(f["Identificación"]),
    telefono: esTelefono ? telefonoExcel : null,
    notas: !esTelefono && telefonoExcel ? `Teléfono en el Excel: ${telefonoExcel}` : null,
    contacto: texto(f["Nombres contacto"]),
    centros: [...new Set([f["Centro de Acopio 1"], f["Centro de Acopio 2"], f["Centro de Acopio 3"]].map(texto).filter(Boolean))],
  };
});

// Centros: los de la hoja Centros_acopio más los que solo aparecen en productos o proveedores.
const centros = new Map(centrosExcel.map((f) => [clave(f["Centro de Acopio"]), { nombre: texto(f["Centro de Acopio"]), cuenca: texto(f.Cuenca) }]));
for (const nombre of [...productos.flatMap((p) => p.centros), ...proveedores.flatMap((p) => p.centros)]) {
  if (!centros.has(clave(nombre))) centros.set(clave(nombre), { nombre, cuenca: null });
}

console.log(
  `${productos.length} productos · ${centros.size} centros de acopio · ${proveedores.length} proveedores · ` +
    `${new Set(productos.map((p) => p.prefijo)).size} grupos`,
);

if (simular) {
  console.table(productos.slice(0, 15).map((p) => ({
    codigo: p.codigo, tipo: p.tipo, nombre: p.nombreCientifico?.slice(0, 28), talla: p.tallaEtiqueta ?? `${p.tallaMinCm ?? ""}-${p.tallaMaxCm ?? ""}`,
    bolsa: `${p.bolsaMin ?? ""}-${p.bolsaMax ?? ""}`, disp: p.disponibilidad, centros: p.centros.join(", "), precios: Object.keys(p.precios).length,
  })));
  console.log("Centros sin cuenca en el Excel:", [...centros.values()].filter((c) => !c.cuenca).map((c) => c.nombre).join(", ") || "ninguno");
  console.log("Prefijos sin nombre de grupo:", [...new Set(productos.map((p) => p.prefijo))].filter((p) => !GRUPOS[p]).join(", ") || "ninguno");
  console.log("Productos sin precio Standard:", productos.filter((p) => !p.precios.STANDARD).length);
  process.exit(0);
}

// --- API ---
const API = process.env.API_URL ?? "http://localhost:5377";
const { IMPORTAR_EMAIL: email, IMPORTAR_PASSWORD: password } = process.env;
if (!email || !password) {
  console.error("Define IMPORTAR_EMAIL e IMPORTAR_PASSWORD (usuario con catalogos, precios, proveedores, ver-costos y maestros).");
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
    method: metodo,
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });
  const json = await r.json().catch(() => null);
  if (r.status >= 400 && r.status !== 409) throw new Error(`${metodo} ${ruta}: ${r.status} ${json?.error ?? ""}`);
  return { status: r.status, json };
};

const resumen = {};
const contar = (entidad, status) => {
  resumen[entidad] ??= { creados: 0, existentes: 0, errores: 0 };
  resumen[entidad][status === 201 ? "creados" : status === 409 ? "existentes" : "errores"]++;
};
const intentar = async (entidad, etiqueta, accion) => {
  try {
    const r = await accion();
    contar(entidad, r.status);
    return r;
  } catch (e) {
    contar(entidad, 500);
    console.warn(`  ✗ ${entidad} ${etiqueta}: ${e.message}`);
    return null;
  }
};

// Maestros: se buscan por nombre/código y se crean los que falten.
async function maestro(claveMaestro, codigo, nombre) {
  const lista = (await api("GET", `/maestros/${claveMaestro}`)).json;
  const existente = lista.find((v) => v.codigo === codigo || clave(v.nombre) === clave(nombre));
  if (existente) return existente.id;
  const r = await intentar(claveMaestro, codigo, () => api("POST", `/maestros/${claveMaestro}`, { codigo, nombre, activo: true }));
  return r?.json?.id ?? (await api("GET", `/maestros/${claveMaestro}`)).json.find((v) => v.codigo === codigo)?.id;
}
const codigoDe = (nombre) => clave(nombre).toUpperCase().replace(/[^A-Z0-9]+/g, "_").replace(/^_|_$/g, "").slice(0, 20);

const cuencas = new Map();
for (const c of centros.values()) {
  const nombre = c.cuenca ?? "Sin definir";
  if (!cuencas.has(clave(nombre))) cuencas.set(clave(nombre), await maestro("cuencas", codigoDe(nombre), nombre));
}

const centrosExistentes = new Map((await api("GET", "/catalogos/centros-acopio")).json.map((c) => [clave(c.nombre), c.id]));
for (const c of centros.values()) {
  if (centrosExistentes.has(clave(c.nombre))) {
    contar("centros", 409);
    continue;
  }
  const r = await intentar("centros", c.nombre, () =>
    api("POST", "/catalogos/centros-acopio", { nombre: c.nombre, cuencaId: cuencas.get(clave(c.cuenca ?? "Sin definir")), activo: true }));
  if (r?.json?.id) centrosExistentes.set(clave(c.nombre), r.json.id);
}

const grupos = new Map();
for (const prefijo of new Set(productos.map((p) => p.prefijo))) {
  grupos.set(prefijo, await maestro("grupos-producto", prefijo, GRUPOS[prefijo] ?? `Grupo ${prefijo}`));
}

const listas = new Map((await api("GET", "/catalogos/precios/listas")).json.map((l) => [l.codigo, l.id]));
for (const p of productos) {
  const precios = Object.fromEntries(Object.entries(p.precios).filter(([c]) => listas.has(c)).map(([c, v]) => [listas.get(c), v]));
  await intentar("productos", p.codigo, () =>
    api("POST", "/catalogos/productos", {
      codigo: p.codigo, grupoId: grupos.get(p.prefijo), tipo: p.tipo, nombreCientifico: p.nombreCientifico,
      nombreComun: p.nombreComun, nombreEspanol: p.nombreEspanol, tallaMinCm: p.tallaMinCm, tallaMaxCm: p.tallaMaxCm,
      tallaEtiqueta: p.tallaEtiqueta, bolsaMin: p.bolsaMin, bolsaMax: p.bolsaMax, disponibilidad: p.disponibilidad,
      activo: true, notas: null, centrosAcopio: p.centros.map((c) => centrosExistentes.get(clave(c))).filter(Boolean), precios,
    }));
}

const tiposId = new Map((await api("GET", "/maestros/tipos-identificacion")).json.map((t) => [t.codigo, t.id]));
for (const p of proveedores) {
  await intentar("proveedores", p.nombre, () =>
    api("POST", "/catalogos/proveedores", {
      nombre: p.nombre, tipoIdentificacionId: tiposId.get(p.tipo) ?? tiposId.get("CC"), numeroIdentificacion: p.numero,
      telefono: p.telefono, contacto: p.contacto, email: null, notas: p.notas, activo: true,
      centrosAcopio: p.centros.map((c) => centrosExistentes.get(clave(c))).filter(Boolean),
    }));
}

console.table(resumen);
process.exitCode = Object.values(resumen).some((r) => r.errores > 0) ? 1 : 0;
