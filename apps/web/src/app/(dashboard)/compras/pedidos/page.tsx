import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Proveedor, Producto } from "@/lib/catalogos";
import type { Oferta, PedidoProveedor } from "@/lib/inventario";
import { opcionesProducto } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PedidosProveedorCliente } from "./PedidosProveedorCliente";

export const metadata: Metadata = { title: "Pedidos a proveedor" };

export default async function PedidosProveedorPage({ searchParams }: { searchParams: Promise<{ id?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const gestionar = permisos.includes(PERMISOS.COMPRAS_GESTIONAR);

  const [pedidos, productos, proveedores, ofertas] = await Promise.all([
    apiFetch<PedidoProveedor[]>("/api/v1/compras/pedidos", { token }),
    gestionar && permisos.includes(PERMISOS.CATALOGOS_LEER) ? apiFetch<Producto[]>("/api/v1/catalogos/productos", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.PROVEEDORES_LEER) ? apiFetch<Proveedor[]>("/api/v1/catalogos/proveedores", { token }) : Promise.resolve([]),
    gestionar ? apiFetch<Oferta[]>("/api/v1/compras/ofertas", { token }) : Promise.resolve([]),
  ]);
  const id = (await searchParams).id;

  return (
    <PedidosProveedorCliente
      pedidos={pedidos}
      productos={opcionesProducto(productos)}
      proveedores={proveedores.filter((p) => p.activo).map((p) => ({ id: p.id, nombre: p.nombre }))}
      ofertas={ofertas.filter((o) => o.estado === "abierta").map((o) => ({ id: o.id, codigo: o.codigo, proveedorId: o.proveedorId }))}
      abrirId={typeof id === "string" ? id : null}
      puedeGestionar={gestionar}
      puedeRecibir={permisos.includes(PERMISOS.COMPRAS_RECIBIR)}
      verCostos={permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS)}
    />
  );
}
