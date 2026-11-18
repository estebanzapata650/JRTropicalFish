import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

type Contexto = { params: Promise<{ path: string[] }> };

/**
 * Reenvía peticiones de componentes de cliente hacia la API .NET agregando el `Authorization` de
 * la sesión. Así el navegador nunca conoce la URL de la API ni maneja el JWT, y no hace falta CORS.
 *
 * Archivos binarios (PDF, descargas) NO pasan por aquí: tienen route handlers propios que hacen
 * streaming, porque `response.text()` corrompería el contenido.
 */
async function reenviar(request: NextRequest, { params }: Contexto) {
  const { path } = await params;
  const session = await getSession();

  const headers = new Headers();
  headers.set("Content-Type", request.headers.get("content-type") ?? "application/json");
  if (session) headers.set("Authorization", `Bearer ${session.token}`);

  const body = request.method === "GET" || request.method === "HEAD" ? undefined : await request.text();

  const response = await fetch(`${API_URL}/api/v1/${path.join("/")}${request.nextUrl.search}`, {
    method: request.method,
    headers,
    body,
    cache: "no-store",
  });

  return new NextResponse(await response.text(), {
    status: response.status,
    headers: { "Content-Type": response.headers.get("content-type") ?? "application/json" },
  });
}

export const GET = reenviar;
export const POST = reenviar;
export const PUT = reenviar;
export const PATCH = reenviar;
export const DELETE = reenviar;
