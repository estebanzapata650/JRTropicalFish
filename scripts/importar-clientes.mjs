#!/usr/bin/env node
/**
 * Importa la hoja «Clientes» del Excel de control comercial (JR_Control_Comercial_Master) al CRM.
 *
 *   IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run crm:importar-clientes -- "<ruta.xlsx>" [--simular]
 *
 * - Pasa por la API (`POST /api/v1/crm/clientes`), así que aplica las mismas validaciones y deja
 *   auditoría. Los datos reales no quedan en el código ni en migraciones.
 * - Conserva el código original (CLI-047) y la API sube el consecutivo.
 * - Es idempotente: un cliente que ya existe (409) se omite. Se puede correr varias veces.
 * - `--simular` solo muestra cómo quedaría cada fila, sin llamar a la API.
 * - API_URL (por defecto http://localhost:5377) permite apuntar a otro ambiente.
 *
 * Lee el .xlsx sin dependencias: un .xlsx es un ZIP con XML adentro.
 */
import { readFileSync } from "node:fs";
import { inflateRawSync } from "node:zlib";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const ruta = args.find((a) => !a.startsWith("--"));
if (!ruta) {
  console.error('Uso: npm run crm:importar-clientes -- "<ruta.xlsx>" [--simular]');
  process.exit(1);
}

// --- ZIP mínimo (directorio central + deflate) ---
function leerZip(buffer) {
  let fin = buffer.length - 22;
  while (fin >= 0 && buffer.readUInt32LE(fin) !== 0x06054b50) fin--;
  if (fin < 0) throw new Error("El archivo no es un .xlsx válido.");
  const total = buffer.readUInt16LE(fin + 10);
  let p = buffer.readUInt32LE(fin + 16);
  const archivos = new Map();
  for (let i = 0; i < total; i++) {
    const metodo = buffer.readUInt16LE(p + 10);
    const comprimido = buffer.readUInt32LE(p + 20);
    const largoNombre = buffer.readUInt16LE(p + 28);
    const largoExtra = buffer.readUInt16LE(p + 30);
    const largoComentario = buffer.readUInt16LE(p + 32);
    const local = buffer.readUInt32LE(p + 42);
    const nombre = buffer.toString("utf8", p + 46, p + 46 + largoNombre);
    archivos.set(nombre, { metodo, comprimido, local });
    p += 46 + largoNombre + largoExtra + largoComentario;
  }
  return (nombre) => {
    const a = archivos.get(nombre);
    if (!a) return null;
    const inicio = a.local + 30 + buffer.readUInt16LE(a.local + 26) + buffer.readUInt16LE(a.local + 28);
    const datos = buffer.subarray(inicio, inicio + a.comprimido);
    return (a.metodo === 8 ? inflateRawSync(datos) : datos).toString("utf8");
  };
}

const xmlTexto = (s) =>
  s.replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&amp;/g, "&");

function leerHoja(zip, nombreHoja) {
  const libro = zip("xl/workbook.xml");
  const hoja = [...libro.matchAll(/<sheet [^>]*name="([^"]+)"[^>]*r:id="([^"]+)"/g)].find((m) => m[1] === nombreHoja);
  if (!hoja) throw new Error(`No hay una hoja llamada «${nombreHoja}».`);
  const rels = zip("xl/_rels/workbook.xml.rels");
  const destino = [...rels.matchAll(/<Relationship [^>]*>/g)]
    .map((m) => m[0])
    .find((r) => r.includes(`Id="${hoja[2]}"`))
    ?.match(/Target="([^"]+)"/)?.[1];
  const xml = zip(`xl/${destino.replace(/^\/?xl\//, "")}`);

  const compartidas = [...(zip("xl/sharedStrings.xml") ?? "").matchAll(/<si>([\s\S]*?)<\/si>/g)].map((m) =>
    xmlTexto([...m[1].matchAll(/<t[^>]*>([\s\S]*?)<\/t>/g)].map((t) => t[1]).join("")),
  );

  const filas = [];
  for (const fila of xml.matchAll(/<row [^>]*>([\s\S]*?)<\/row>/g)) {
    const celdas = {};
    for (const c of fila[1].matchAll(/<c r="([A-Z]+)\d+"([^>]*?)(?:\/>|>([\s\S]*?)<\/c>)/g)) {
      const [, columna, atributos, contenido = ""] = c;
      const tipo = atributos.match(/t="([^"]+)"/)?.[1];
      const v = contenido.match(/<v>([\s\S]*?)<\/v>/)?.[1];
      let valor = "";
      if (tipo === "s") valor = compartidas[Number(v)] ?? "";
      else if (tipo === "inlineStr") valor = xmlTexto([...contenido.matchAll(/<t[^>]*>([\s\S]*?)<\/t>/g)].map((t) => t[1]).join(""));
      else if (v !== undefined) valor = xmlTexto(v);
      celdas[columna] = valor.trim();
    }
    filas.push(celdas);
  }
  const encabezados = filas.shift() ?? {};
  return filas.map((celdas) => Object.fromEntries(Object.entries(encabezados).map(([col, enc]) => [enc, celdas[col] ?? ""])));
}

// --- Mapeo de valores del Excel a los del CRM ---
const sinTildes = (s) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();
const valorDe = (fila, ...nombres) => {
  for (const n of nombres) {
    const clave = Object.keys(fila).find((k) => sinTildes(k) === sinTildes(n));
    if (clave && fila[clave]) return fila[clave];
  }
  return "";
};

function iata(texto) {
  const t = texto.trim().toUpperCase();
  if (/^[A-Z]{3}$/.test(t)) return t;
  return t.match(/\(([A-Z]{3})\)/)?.[1] ?? null; // "Aeropuerto Internacional de Kansai (KIX)"
}

function licencia(texto) {
  const t = sinTildes(texto);
  if (["si", "activa", "yes"].includes(t)) return "activa";
  if (t.includes("tramite")) return "en_tramite";
  if (t.includes("vencid")) return "vencida";
  return "no_aplica";
}

function estado(texto) {
  const t = sinTildes(texto);
  return t === "inactivo" ? "inactivo" : t === "prospecto" ? "prospecto" : "activo";
}

function tipoCliente(texto, pais) {
  const t = sinTildes(texto);
  if (t.includes("commerce")) return "ecommerce";
  return sinTildes(pais) === "colombia" ? "local_colombia" : "b2b_internacional";
}

function aCliente(fila) {
  const pais = valorDe(fila, "Pais");
  const email = valorDe(fila, "Email");
  return {
    codigo: valorDe(fila, "ID_Cliente") || null,
    nombreEmpresa: valorDe(fila, "Nombre_Empresa"),
    contactoPrincipal: valorDe(fila, "Contacto_Principal") || null,
    email: /^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) ? email : null,
    telefonoWhatsapp: valorDe(fila, "Telefono_WhatsApp", "Telefono_Whatsapp") || null,
    pais: pais || "Sin país",
    ciudad: valorDe(fila, "Ciudad") || null,
    aeropuertoIata: iata(valorDe(fila, "Aeropuerto_IATA", "Ciudad_Aeropuerto_IATA")),
    tipoCliente: tipoCliente(valorDe(fila, "Tipo_Cliente"), pais),
    licenciaImportacion: licencia(valorDe(fila, "Licencia_Importacion_Activa", "Licencia_Importacion")),
    estado: estado(valorDe(fila, "Estado_Cliente", "Estado")),
    notas: valorDe(fila, "Tipo_Cliente") ? `Tipo en el Excel: ${valorDe(fila, "Tipo_Cliente")}` : null,
  };
}

// --- Ejecución ---
const zip = leerZip(readFileSync(ruta));
const clientes = leerHoja(zip, "Clientes").map(aCliente).filter((c) => c.nombreEmpresa);
console.log(`${clientes.length} clientes en la hoja «Clientes».`);

if (simular) {
  console.table(clientes.map((c) => ({ codigo: c.codigo, empresa: c.nombreEmpresa, pais: c.pais, iata: c.aeropuertoIata, tipo: c.tipoCliente, licencia: c.licenciaImportacion })));
  process.exit(0);
}

const API = process.env.API_URL ?? "http://localhost:5377";
const { IMPORTAR_EMAIL: email, IMPORTAR_PASSWORD: password } = process.env;
if (!email || !password) {
  console.error("Define IMPORTAR_EMAIL e IMPORTAR_PASSWORD (un usuario con el permiso clientes:editar).");
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

const resumen = { creados: 0, existentes: 0, errores: 0 };
for (const cliente of clientes) {
  const r = await fetch(`${API}/api/v1/crm/clientes`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(cliente),
  });
  if (r.status === 201) {
    resumen.creados++;
    continue;
  }

  const { error } = await r.json().catch(() => ({}));
  // 409 por el mismo nombre = ya importado. 409 por el código = otro cliente lo ocupa: es un error.
  if (r.status === 409 && !String(error).includes("código")) {
    resumen.existentes++;
  } else {
    resumen.errores++;
    console.warn(`  ✗ ${cliente.codigo ?? ""} ${cliente.nombreEmpresa}: ${error ?? r.status}`);
  }
}
console.log(`Creados: ${resumen.creados} · ya existían: ${resumen.existentes} · con error: ${resumen.errores}`);
process.exitCode = resumen.errores > 0 ? 1 : 0;
