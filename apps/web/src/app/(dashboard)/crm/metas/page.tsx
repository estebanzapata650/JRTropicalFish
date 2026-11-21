import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { MetaMes, ParametrosComerciales } from "@/lib/crm";
import { hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { MetasCliente } from "./MetasCliente";

export const metadata: Metadata = { title: "Metas comerciales" };

export default async function MetasPage({ searchParams }: { searchParams: Promise<{ mes?: string | string[] }> }) {
  const { token } = await exigirPermiso(PERMISOS.METAS_EDITAR);
  const pedido = (await searchParams).mes;
  const mes = typeof pedido === "string" && /^\d{4}-\d{2}$/.test(pedido) ? pedido : hoyNegocio().slice(0, 7);

  const [metas, parametros] = await Promise.all([
    apiFetch<MetaMes[]>(`/api/v1/crm/metricas/metas?mes=${mes}-01`, { token }),
    apiFetch<ParametrosComerciales>("/api/v1/crm/metricas/parametros", { token }),
  ]);

  return <MetasCliente key={mes} mes={mes} metas={metas} parametros={parametros} />;
}
