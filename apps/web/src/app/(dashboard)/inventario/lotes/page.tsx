import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Lote } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { LotesCliente } from "./LotesCliente";

export const metadata: Metadata = { title: "Lotes" };

export default async function LotesPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const lotes = await apiFetch<Lote[]>("/api/v1/inventario/lotes", { token });
  return <LotesCliente lotes={lotes} verCostos={permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS)} />;
}
