import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

type Contexto = { params: Promise<{ id: string }> };

/**
 * Foto de un empleado. Hace streaming del cuerpo de la API (no pasa por el proxy `api/[...path]`,
 * que lee texto y corrompería la imagen). La URL lleva `?v=<fotoVersion>`, que cambia con cada foto
 * nueva: por eso se puede guardar en la caché privada del navegador.
 */
export async function GET(request: NextRequest, { params }: Contexto) {
  const { id } = await params;
  const session = await getSession();
  if (!session) return new NextResponse(null, { status: 401 });

  const response = await fetch(`${API_URL}/api/v1/administracion/empleados/${encodeURIComponent(id)}/foto`, {
    headers: { Authorization: `Bearer ${session.token}` },
    cache: "no-store",
  });

  if (!response.ok || !response.body) return new NextResponse(null, { status: response.status === 200 ? 502 : response.status });

  const versionada = request.nextUrl.searchParams.has("v");
  return new NextResponse(response.body, {
    status: 200,
    headers: {
      "Content-Type": response.headers.get("content-type") ?? "application/octet-stream",
      "Cache-Control": versionada ? "private, max-age=31536000, immutable" : "private, no-cache",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
