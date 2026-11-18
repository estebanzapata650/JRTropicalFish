import { NextResponse } from "next/server";
import { SESSION_COOKIE } from "@/lib/session";

/**
 * Cerrar sesión es una navegación completa del navegador (form POST + 303), no una Server Action:
 * con la Server Action, un clic en el menú mientras el cierre seguía en curso dejaba dos
 * respuestas solapadas en el router de cliente. Además funciona sin JavaScript.
 */
export async function POST() {
  // Location relativa: el navegador la resuelve contra el host por el que entró (localhost, la IP
  // de red, el dominio de producción).
  const response = new NextResponse(null, { status: 303, headers: { Location: "/login" } });
  response.cookies.delete(SESSION_COOKIE);
  return response;
}
