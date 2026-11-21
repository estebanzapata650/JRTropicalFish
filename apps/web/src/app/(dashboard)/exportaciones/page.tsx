import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { apiFetch } from "@/lib/api";
import type { Venta } from "@/lib/crm";
import type { Embarque } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { EmbarquesCliente } from "./EmbarquesCliente";

export const metadata: Metadata = { title: "Embarques de exportación" };

/**
 * Embarques de exportación. `?venta=` llega desde Ventas: si la venta ya tiene embarque se abre; si no,
 * se propone crearlo.
 */
export default async function ExportacionesPage({ searchParams }: { searchParams: Promise<{ venta?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  const gestionar = permisos.includes(PERMISOS.EXPORTACIONES_GESTIONAR) && permisos.includes(PERMISOS.CRM_LEER);
  const { venta } = await searchParams;

  if (typeof venta === "string" && /^[0-9a-f-]{36}$/i.test(venta)) {
    const existente = await apiFetch<{ id: string } | undefined>(`/api/v1/exportaciones/embarques/por-venta/${venta}`, { token });
    if (existente?.id) redirect(`/exportaciones/${existente.id}`);
  }

  const [embarques, ventas] = await Promise.all([
    apiFetch<Embarque[]>("/api/v1/exportaciones/embarques", { token }),
    gestionar ? apiFetch<Venta[]>("/api/v1/crm/ventas", { token }) : Promise.resolve([]),
  ]);
  const conEmbarque = new Set(embarques.map((e) => e.ventaId));
  const disponibles = ventas
    .filter((v) => v.tipoVenta === "exportacion" && !conEmbarque.has(v.id))
    .map((v) => ({ id: v.id, codigo: v.codigo, cliente: v.clienteNombre, fecha: v.fechaPedido }));

  return (
    <EmbarquesCliente
      embarques={embarques}
      ventasDisponibles={disponibles}
      ventaInicial={typeof venta === "string" && disponibles.some((v) => v.id === venta) ? venta : null}
      puedeCrear={gestionar}
    />
  );
}
