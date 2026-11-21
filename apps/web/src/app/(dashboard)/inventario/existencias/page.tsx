import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Acuario, Existencia } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ExistenciasCliente } from "./ExistenciasCliente";

export const metadata: Metadata = { title: "Existencias" };

export default async function ExistenciasPage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const [existencias, acuarios] = await Promise.all([
    apiFetch<Existencia[]>("/api/v1/inventario/existencias", { token }),
    apiFetch<Acuario[]>("/api/v1/inventario/acuarios", { token }),
  ]);
  const q = (await searchParams).q;

  return (
    <ExistenciasCliente
      existencias={existencias}
      acuarios={acuarios.filter((a) => a.activo).map(({ id, codigo }) => ({ id, codigo }))}
      busquedaInicial={typeof q === "string" ? q : ""}
      puedeOperar={permisos.includes(PERMISOS.INVENTARIO_OPERAR)}
      puedeAjustar={permisos.includes(PERMISOS.INVENTARIO_AJUSTAR)}
    />
  );
}
