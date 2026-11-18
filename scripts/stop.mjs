/**
 * Apaga todo lo que levanta `npm run dev`: la API (5377), el sitio web (3100) y los contenedores.
 *
 * Existe porque en Windows matar `npm run dev` no siempre se lleva a los procesos hijos que
 * `next dev` deja escuchando el puerto: hay que matar el árbol por PID (`taskkill /T`).
 */
import { execSync } from "node:child_process";

const PUERTOS = [
  { numero: 3100, nombre: "web (Next.js)" },
  { numero: 5377, nombre: "API (.NET)" },
];

const esWindows = process.platform === "win32";
// `--silencioso` lo usa `predev`: ahí solo interesa liberar puertos que quedaron ocupados de una
// sesión anterior, sin llenar la consola cuando no había nada que apagar.
const silencioso = process.argv.includes("--silencioso");
const log = (mensaje) => {
  if (!silencioso) console.log(mensaje);
};

function pidsEnPuerto(puerto) {
  try {
    const salida = esWindows
      ? execSync(`netstat -ano | findstr ":${puerto} " | findstr LISTENING`, { encoding: "utf8" })
      : execSync(`lsof -ti tcp:${puerto} -s TCP:LISTEN`, { encoding: "utf8" });
    const pids = esWindows
      ? salida.trim().split(/\r?\n/).map((l) => l.trim().split(/\s+/).pop())
      : salida.trim().split(/\r?\n/);
    return [...new Set(pids.filter((p) => p && p !== "0"))];
  } catch {
    return []; // findstr/lsof salen con código 1 cuando no hay coincidencias
  }
}

let algoApagado = false;
for (const { numero, nombre } of PUERTOS) {
  for (const pid of pidsEnPuerto(numero)) {
    try {
      execSync(esWindows ? `taskkill /F /T /PID ${pid}` : `kill -9 ${pid}`, { stdio: "ignore" });
      log(`  liberado ${nombre} — puerto ${numero}, PID ${pid}`);
      algoApagado = true;
    } catch {
      log(`  no se pudo detener el PID ${pid} del puerto ${numero}`);
    }
  }
}
if (!algoApagado) log("  la API y el web ya estaban apagados");

const bajarDocker = !process.argv.includes("--keep-docker");
if (bajarDocker) {
  try {
    execSync("docker compose stop", { stdio: silencioso ? "ignore" : "inherit" });
  } catch {
    log("  no se pudo detener docker compose (¿Docker Desktop apagado?)");
  }
} else {
  log("  contenedores intactos (--keep-docker)");
}

// Confirmación: si algo sigue escuchando, hay que decirlo en vez de dar por hecho el apagado.
const ocupados = PUERTOS.filter((p) => pidsEnPuerto(p.numero).length);
log(
  ocupados.length
    ? `\n⚠ siguen ocupados: ${ocupados.map((p) => p.numero).join(", ")}`
    : "\n✓ puertos 3100 y 5377 libres",
);
