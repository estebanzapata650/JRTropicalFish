import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CentroAcopio, ListaPrecios, ParametrosPrecios, Producto } from "@/lib/catalogos";
import { PERMISOS } from "@/lib/permisos";
import type { ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ProductosCliente } from "./ProductosCliente";

export const metadata: Metadata = { title: "Productos" };

export default async function ProductosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.CATALOGOS_LEER);
  const verPrecios = permisos.includes(PERMISOS.PRECIOS_LEER);

  const [productos, listas, parametros, grupos, centros] = await Promise.all([
    apiFetch<Producto[]>("/api/v1/catalogos/productos", { token }),
    verPrecios ? apiFetch<ListaPrecios[]>("/api/v1/catalogos/precios/listas", { token }) : Promise.resolve([]),
    verPrecios ? apiFetch<ParametrosPrecios>("/api/v1/catalogos/precios/parametros", { token }) : Promise.resolve(null),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/grupos-producto?soloActivos=true", { token }),
    apiFetch<CentroAcopio[]>("/api/v1/catalogos/centros-acopio", { token }),
  ]);

  return (
    <ProductosCliente
      productos={productos}
      listas={listas}
      parametros={parametros}
      grupos={grupos.map(({ id, codigo, nombre }) => ({ id, codigo, nombre }))}
      centros={centros.map(({ id, nombre, activo }) => ({ id, nombre, activo }))}
      puedeEditar={permisos.includes(PERMISOS.CATALOGOS_EDITAR)}
      puedeEditarPrecios={permisos.includes(PERMISOS.PRECIOS_EDITAR)}
    />
  );
}
