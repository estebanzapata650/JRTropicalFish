import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Proveedor } from "@/lib/catalogos";
import type { InventarioProducto } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { NecesidadesCliente } from "./NecesidadesCliente";

export const metadata: Metadata = { title: "Necesidades de compra" };

export default async function NecesidadesPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const gestionar = permisos.includes(PERMISOS.COMPRAS_GESTIONAR) && permisos.includes(PERMISOS.PROVEEDORES_LEER);
  const [necesidades, proveedores] = await Promise.all([
    apiFetch<InventarioProducto[]>("/api/v1/inventario/necesidades", { token }),
    gestionar ? apiFetch<Proveedor[]>("/api/v1/catalogos/proveedores", { token }) : Promise.resolve([]),
  ]);

  return (
    <NecesidadesCliente
      necesidades={necesidades}
      proveedores={proveedores.filter((p) => p.activo).map((p) => ({ id: p.id, nombre: p.nombre }))}
      puedeGestionar={gestionar}
    />
  );
}
