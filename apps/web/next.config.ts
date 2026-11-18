import os from "node:os";
import path from "node:path";
import { fileURLToPath } from "node:url";
import type { NextConfig } from "next";

/**
 * Hosts desde los que se permite abrir el servidor de desarrollo.
 *
 * Desde Next.js 16, entrar al `next dev` por un host distinto de `localhost` (la IP de la red,
 * un celular en la misma wifi…) hace que el servidor responda 403 a todos los chunks de
 * `/_next/static`: la página se pinta (HTML del servidor) pero React nunca hidrata y ningún botón
 * de cliente funciona. Aquí se autorizan automáticamente todas las IPs de esta máquina, y se pueden
 * sumar otros hosts (un dominio local, un túnel) con `ALLOWED_DEV_ORIGINS=a,b,c`.
 * Solo afecta a desarrollo; en producción Next.js ignora esta opción.
 */
function origenesDeDesarrollo(): string[] {
  const propias = Object.values(os.networkInterfaces())
    .flat()
    .filter((i): i is os.NetworkInterfaceInfo => !!i && i.family === "IPv4")
    .map((i) => i.address);
  const extra = (process.env.ALLOWED_DEV_ORIGINS ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
  return [...new Set([...propias, ...extra])];
}

const nextConfig: NextConfig = {
  output: "standalone",
  // Las Server Actions traen un límite de 1 MB por defecto y la subida de documentos falla con un
  // error opaco al superarlo. 12 MB deja margen sobre el máximo de 10 MB que valida la API.
  experimental: { serverActions: { bodySizeLimit: "12mb" } },
  allowedDevOrigins: origenesDeDesarrollo(),
  // La raíz del repositorio tiene su propio package-lock.json (orquestador de `npm run dev`), así
  // que Next.js ya no puede deducir cuál es su directorio raíz y advierte por los dos lockfiles.
  // Se lo decimos explícitamente: es esta carpeta.
  turbopack: { root: path.dirname(fileURLToPath(import.meta.url)) },
};

export default nextConfig;
