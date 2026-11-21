import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CentroAcopio } from "@/lib/catalogos";
import { PERMISOS } from "@/lib/permisos";
import type { ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { CentrosCliente } from "./CentrosCliente";

export const metadata: Metadata = { title: "Centros de acopio" };

export default async function CentrosAcopioPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.CATALOGOS_LEER);
  const [centros, cuencas] = await Promise.all([
    apiFetch<CentroAcopio[]>("/api/v1/catalogos/centros-acopio", { token }),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/cuencas?soloActivos=true", { token }),
  ]);
  return (
    <CentrosCliente
      centros={centros}
      cuencas={cuencas.map(({ id, nombre }) => ({ id, nombre }))}
      puedeEditar={permisos.includes(PERMISOS.CATALOGOS_EDITAR)}
    />
  );
}
