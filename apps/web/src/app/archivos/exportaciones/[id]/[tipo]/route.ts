import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

type Contexto = { params: Promise<{ id: string; tipo: string }> };

const TIPOS = new Set(["preinvoice", "flete", "orden_cargue", "empaque", "factura", "nota_credito", "balance"]);

/**
 * PDF de un documento del embarque. Streaming del cuerpo de la API (no por el proxy `api/[...path]`, que lee
 * texto y corrompería el PDF). Por defecto se abre en el navegador; con `?descargar=1` se descarga con el
 * nombre que pone la API ("Invoice 12187 Kamihata….pdf").
 */
export async function GET(request: NextRequest, { params }: Contexto) {
  const { id, tipo } = await params;
  if (!TIPOS.has(tipo)) return new NextResponse(null, { status: 404 });
  const session = await getSession();
  if (!session) return new NextResponse(null, { status: 401 });

  const response = await fetch(`${API_URL}/api/v1/exportaciones/embarques/${encodeURIComponent(id)}/documentos/${tipo}`, {
    headers: { Authorization: `Bearer ${session.token}` },
    cache: "no-store",
  });
  if (!response.ok || !response.body) return new NextResponse(null, { status: response.status === 200 ? 502 : response.status });

  const disposicion = response.headers.get("content-disposition") ?? `attachment; filename="${tipo}.pdf"`;
  return new NextResponse(response.body, {
    status: 200,
    headers: {
      "Content-Type": "application/pdf",
      "Content-Disposition": request.nextUrl.searchParams.has("descargar") ? disposicion : disposicion.replace(/^attachment/, "inline"),
      "Cache-Control": "private, no-store",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
