import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Recepcion } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { RecepcionesCliente } from "./RecepcionesCliente";

export const metadata: Metadata = { title: "Recepciones" };

export default async function RecepcionesPage({ searchParams }: { searchParams: Promise<{ id?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const recepciones = await apiFetch<Recepcion[]>("/api/v1/compras/recepciones", { token });
  const id = (await searchParams).id;
  return <RecepcionesCliente recepciones={recepciones} abrirId={typeof id === "string" ? id : null} verCostos={permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS)} />;
}
