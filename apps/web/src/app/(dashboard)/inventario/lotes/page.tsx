import type { Metadata } from "next";
import { PageHeader } from "@/components/PageHeader";
import { apiFetch } from "@/lib/api";
import type { LoteTrazable } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PestanasTableros } from "../tableros";
import { LotesCliente } from "./LotesCliente";

export const metadata: Metadata = { title: "Inventario · Trazabilidad" };

/** Trazabilidad (Looker: Trazabilidad lotes): cada lote con su recorrido resumido y sus tiempos. */
export default async function LotesPage({ searchParams }: { searchParams: Promise<{ antiguos?: string }> }) {
  const { token } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const lotes = await apiFetch<LoteTrazable[]>("/api/v1/inventario/tableros/trazabilidad", { token });
  const { antiguos } = await searchParams;
  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Inventario" descripcion="El recorrido de cada lote: de qué proveedor llegó, cuánto estuvo en aclimatación y cuarentena, y a qué clientes se envió." />
      <PestanasTableros actual="/inventario/lotes" />
      <LotesCliente lotes={lotes} soloAntiguos={antiguos === "1"} />
    </div>
  );
}
