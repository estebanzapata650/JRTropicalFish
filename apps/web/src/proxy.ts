import { NextRequest, NextResponse } from "next/server";

const SESSION_COOKIE = "jr_session";

/** Rutas que no exigen sesión. */
const PUBLICAS = ["/login"];

/**
 * Next.js 16: este archivo reemplaza a `middleware.ts`. Solo comprueba que exista la cookie de
 * sesión; la validez real del token la decide la API (el layout del dashboard redirige al login si
 * `/auth/yo` falla).
 */
export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // El proxy hacia la API decide autenticación por sí mismo.
  if (pathname.startsWith("/api/") || PUBLICAS.includes(pathname)) {
    return NextResponse.next();
  }

  if (!request.cookies.has(SESSION_COOKIE)) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Los archivos estáticos (ruta con extensión) y los internos de Next quedan fuera del proxy.
  matcher: ["/((?!_next/|favicon\\.ico|.*\\.[a-zA-Z0-9]+$).*)"],
};
