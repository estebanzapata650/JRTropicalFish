import { NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

/** Reporte del veterinario (PDF) de un tratamiento, en streaming. */
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const session = await getSession();
  if (!session) return new NextResponse(null, { status: 401 });
  const { id } = await params;
  if (!/^[0-9a-f-]{36}$/i.test(id)) return new NextResponse(null, { status: 404 });

  const response = await fetch(`${API_URL}/api/v1/sanidad/tratamientos/${id}/reporte`, {
    headers: { Authorization: `Bearer ${session.token}` },
    cache: "no-store",
  });
  if (!response.ok || !response.body) return new NextResponse(null, { status: response.status === 200 ? 502 : response.status });

  return new NextResponse(response.body, {
    status: 200,
    headers: {
      "Content-Type": "application/pdf",
      "Content-Disposition": `inline; filename="reporte-${id}.pdf"`,
      "Cache-Control": "private, no-cache",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
