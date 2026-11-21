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
 */
import { readFileSync } from "node:fs";
import { leerHoja, leerZip } from "./lib/xlsx.mjs";

const args = process.argv.slice(2);
const simular = args.includes("--simular");
const ruta = args.find((a) => !a.startsWith("--"));
if (!ruta) {
  console.error('Uso: npm run crm:importar-clientes -- "<ruta.xlsx>" [--simular]');
  process.exit(1);
}

// --- Mapeo de valores del Excel a los del CRM ---
const sinTildes = (s) => s.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
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
