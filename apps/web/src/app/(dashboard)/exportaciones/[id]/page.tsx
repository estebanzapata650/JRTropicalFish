import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import type { Producto } from "@/lib/catalogos";
import type { ClienteExportacion, Configuracion, EmbarqueDetalle } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { FichaEmbarque } from "./FichaEmbarque";

export const metadata: Metadata = { title: "Embarque de exportación" };

export default async function EmbarquePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const { token, permisos } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  let detalle: EmbarqueDetalle;
  try {
    detalle = await apiFetch<EmbarqueDetalle>(`/api/v1/exportaciones/embarques/${encodeURIComponent(id)}`, { token });
  } catch (err) {
    if (err instanceof ApiError && (err.status === 404 || err.status === 400)) notFound();
    throw err;
  }
  const gestionar = permisos.includes(PERMISOS.EXPORTACIONES_GESTIONAR);
  const empacar = permisos.includes(PERMISOS.EXPORTACIONES_EMPACAR);

  const [configuracion, productos, listas, clienteExportacion] = await Promise.all([
    apiFetch<Configuracion>("/api/v1/exportaciones/configuracion", { token }),
    (gestionar || empacar) && permisos.includes(PERMISOS.CATALOGOS_LEER) ? apiFetch<Producto[]>("/api/v1/catalogos/productos", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.PRECIOS_LEER)
      ? apiFetch<{ id: string; nombre: string; esCosto: boolean; activo: boolean }[]>("/api/v1/catalogos/precios/listas", { token })
      : Promise.resolve([]),
    apiFetch<ClienteExportacion>(`/api/v1/exportaciones/clientes/${detalle.embarque.clienteId}`, { token }),
  ]);

  return (
    <FichaEmbarque
      d={detalle}
      cajas={configuracion.cajas}
      bolsas={configuracion.bolsas}
      productos={productos.filter((p) => p.activo).map((p) => ({ id: p.id, codigo: p.codigo, nombre: p.nombreCientifico }))}
      listas={listas.filter((l) => !l.esCosto && l.activo).map((l) => ({ id: l.id, nombre: l.nombre }))}
      clienteExportacion={clienteExportacion}
      puedeGestionar={gestionar}
      puedeEmpacar={empacar}
    />
  );
}
