/**
 * Deja el almacén de objetos de desarrollo (Garage) listo para usar: asigna el layout del nodo,
 * importa la clave de desarrollo y crea el bucket con permisos.
 *
 * Es idempotente y lo llama `predev`, así que en una máquina nueva `npm run dev` deja todo andando
 * sin pasos manuales. Garage exige este arranque porque, a diferencia de otros servidores S3, no
 * atiende peticiones hasta que su nodo tiene un rol asignado en el cluster.
 */
import { execFileSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";

const CONTENEDOR = "jrtropicalfish-garage";

function variablesDeEntorno() {
  const valores = { ...process.env };
  if (existsSync(".env")) {
    for (const linea of readFileSync(".env", "utf8").split(/\r?\n/)) {
      const m = /^\s*([A-Z0-9_]+)\s*=\s*(.*)\s*$/.exec(linea);
      if (m && !linea.trimStart().startsWith("#")) valores[m[1]] ??= m[2];
    }
  }
  return valores;
}

/** Ejecuta el CLI de Garage dentro del contenedor. Devuelve stdout, o null si falló. */
function garage(...args) {
  try {
    return execFileSync("docker", ["exec", CONTENEDOR, "/garage", ...args], {
      encoding: "utf8",
      stdio: ["ignore", "pipe", "pipe"],
    });
  } catch {
    return null;
  }
}

const env = variablesDeEntorno();
const acceso = env.S3_ACCESS_KEY;
const secreto = env.S3_SECRET_KEY;
const bucket = env.S3_BUCKET ?? "jr-documentos";

if (!acceso || !secreto) {
  console.log("  almacén S3: faltan S3_ACCESS_KEY/S3_SECRET_KEY en .env, se omite la preparación");
  process.exit(0);
}

// Garage tarda un par de segundos en aceptar conexiones RPC tras arrancar.
let estado = null;
for (let intento = 0; intento < 30 && estado === null; intento++) {
  estado = garage("status");
  if (estado === null) execFileSync("node", ["-e", "setTimeout(()=>{},1000)"]);
}

if (estado === null) {
  console.log(`  almacén S3: ${CONTENEDOR} no responde, se omite la preparación`);
  process.exit(0);
}

let huboCambios = false;

// 1. El nodo necesita un rol antes de atender peticiones S3.
if (estado.includes("NO ROLE ASSIGNED")) {
  const id = estado
    .split(/\r?\n/)
    .map((l) => l.trim().split(/\s+/)[0])
    .find((t) => /^[0-9a-f]{16}$/.test(t));

  if (id) {
    garage("layout", "assign", "-z", "dc1", "-c", "1G", id);
    const version = /Current cluster layout version:\s*(\d+)/.exec(garage("layout", "show") ?? "");
    garage("layout", "apply", "--version", String(Number(version?.[1] ?? 0) + 1));
    huboCambios = true;
  }
}

// 2. Clave con credenciales fijas, para que .env no dependa de lo que genere Garage.
if (!(garage("key", "list") ?? "").includes(acceso)) {
  garage("key", "import", "--yes", acceso, secreto, "-n", "dev");
  huboCambios = true;
}

// 3. Bucket y permisos (allow es idempotente).
if (!(garage("bucket", "list") ?? "").includes(bucket)) {
  garage("bucket", "create", bucket);
  huboCambios = true;
}
garage("bucket", "allow", "--read", "--write", "--owner", bucket, "--key", "dev");

console.log(huboCambios ? `  almacén S3 preparado (bucket ${bucket})` : `  almacén S3 listo (bucket ${bucket})`);
