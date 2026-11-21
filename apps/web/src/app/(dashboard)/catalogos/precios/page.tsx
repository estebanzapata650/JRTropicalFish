import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { ListaPrecios, ParametrosPrecios } from "@/lib/catalogos";
import type { Cliente } from "@/lib/crm";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PreciosCliente } from "./PreciosCliente";

export const metadata: Metadata = { title: "Listas de precios" };

export default async function PreciosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.PRECIOS_LEER);
  const puedeEditar = permisos.includes(PERMISOS.PRECIOS_EDITAR);

  const [listas, parametros, clientes] = await Promise.all([
    apiFetch<ListaPrecios[]>("/api/v1/catalogos/precios/listas", { token }),
    apiFetch<ParametrosPrecios>("/api/v1/catalogos/precios/parametros", { token }),
    puedeEditar && permisos.includes(PERMISOS.CLIENTES_LEER) ? apiFetch<Cliente[]>("/api/v1/crm/clientes", { token }) : Promise.resolve([]),
  ]);

  return (
    <PreciosCliente
      listas={listas}
      parametros={parametros}
      clientes={clientes.map(({ id, nombreEmpresa }) => ({ id, nombre: nombreEmpresa }))}
      puedeEditar={puedeEditar}
      puedeVerCostos={permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS)}
    />
  );
}
