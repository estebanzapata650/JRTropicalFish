import { NextResponse } from "next/server";
import { API_URL } from "@/lib/api";
import { getSession } from "@/lib/session";

type Contexto = { params: Promise<{ anio: string; mes: string }> };

/** Excel del informe mensual AUNAP. Streaming del cuerpo de la API (el proxy `api/[...path]` lee texto y lo corrompería). */
export async function GET(_request: Request, { params }: Contexto) {
  const { anio, mes } = await params;
  if (!/^\d{4}$/.test(anio) || !/^\d{1,2}$/.test(mes)) return new NextResponse(null, { status: 404 });
  const session = await getSession();
  if (!session) return new NextResponse(null, { status: 401 });

  const response = await fetch(`${API_URL}/api/v1/exportaciones/aunap/informe/${anio}/${mes}/excel`, {
    headers: { Authorization: `Bearer ${session.token}` },
    cache: "no-store",
  });
  if (!response.ok || !response.body) return new NextResponse(null, { status: response.status === 200 ? 502 : response.status });

  return new NextResponse(response.body, {
    status: 200,
    headers: {
      "Content-Type": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      "Content-Disposition": response.headers.get("content-disposition") ?? `attachment; filename="Informe AUNAP ${anio}-${mes}.xlsx"`,
      "Cache-Control": "private, no-store",
      "X-Content-Type-Options": "nosniff",
    },
  });
}
