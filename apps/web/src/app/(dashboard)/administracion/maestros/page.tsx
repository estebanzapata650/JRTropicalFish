import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { MaestroInfo, ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { MaestrosCliente } from "./MaestrosCliente";

export const metadata: Metadata = { title: "Maestros" };

export default async function MaestrosPage({ searchParams }: { searchParams: Promise<{ tipo?: string | string[] }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.MAESTROS_LEER);
  const { tipo } = await searchParams;

  const maestros = await apiFetch<MaestroInfo[]>("/api/v1/maestros", { token });
  const actual = maestros.find((m) => m.clave === tipo) ?? maestros[0];
  const valores = actual
    ? await apiFetch<ValorMaestro[]>(`/api/v1/maestros/${encodeURIComponent(actual.clave)}`, { token })
    : [];

  return (
    <MaestrosCliente
      key={actual?.clave}
      maestros={maestros}
      actual={actual}
      valores={valores}
      puedeEditar={permisos.includes(PERMISOS.MAESTROS_EDITAR)}
    />
  );
}
