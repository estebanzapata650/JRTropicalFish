import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

/**
 * Logo de la empresa para la vista previa de Configuración. La URL lleva `?v=<actualizadoEn>`, que cambia
 * con cada logo nuevo: por eso se puede guardar en la caché privada del navegador.
 */
export async function GET(request: NextRequest) {
  const session = await getSession();
  if (!session) return new NextResponse(null, { status: 401 });

  const response = await fetch(`${API_URL}/api/v1/exportaciones/configuracion/logo`, {
    headers: { Authorization: `Bearer ${session.token}` },
    cache: "no-store",
  });
  if (!response.ok || !response.body) return new NextResponse(null, { status: response.status === 200 ? 502 : response.status });

  return new NextResponse(response.body, {
    status: 200,
    headers: {
      "Content-Type": response.headers.get("content-type") ?? "application/octet-stream",
      "Cache-Control": request.nextUrl.searchParams.has("v") ? "private, max-age=31536000, immutable" : "private, no-cache",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
