import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { ListaPrecios, Producto } from "@/lib/catalogos";
import type { Cliente, Comercial, Venta } from "@/lib/crm";
import { opcionesProducto, type PedidoCliente } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PedidosClienteCliente, type VentaOrigen } from "./PedidosClienteCliente";

/** Lista que pone el precio en el pedido según su moneda (decisión del negocio). */
const LISTAS_PEDIDO: Record<string, string> = { USD: "STANDARD", COP: "MAYORISTA_NACIONAL" };

export const metadata: Metadata = { title: "Pedidos de clientes" };

/**
 * Pedidos de clientes (pasos 4–5 y 10 del Excel). `?venta=` llega desde Ventas: si la venta ya tiene
 * pedido se abre; si no, se abre el formulario para crearlo enlazado a ella. `?id=` abre un pedido.
 */
export default async function PedidosClientePage({ searchParams }: { searchParams: Promise<{ id?: string; venta?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const gestionar = permisos.includes(PERMISOS.PEDIDOS_GESTIONAR);
  const { id, venta } = await searchParams;

  const [pedidos, productos, clientes, comerciales, listas] = await Promise.all([
    apiFetch<PedidoCliente[]>("/api/v1/pedidos", { token }),
    gestionar && permisos.includes(PERMISOS.CATALOGOS_LEER) ? apiFetch<Producto[]>("/api/v1/catalogos/productos", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.CLIENTES_LEER) ? apiFetch<Cliente[]>("/api/v1/crm/clientes", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.CRM_LEER) ? apiFetch<Comercial[]>("/api/v1/crm/comerciales", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.PRECIOS_LEER) ? apiFetch<ListaPrecios[]>("/api/v1/catalogos/precios/listas", { token }) : Promise.resolve([]),
  ]);

  // Cada moneda tiene su lista; la moneda de la lista debe coincidir para no mezclar dólares y pesos.
  const listaPorMoneda = Object.fromEntries(
    listas
      .filter((l) => l.activo && !l.esCosto && LISTAS_PEDIDO[l.moneda] === l.codigo)
      .map((l) => [l.moneda, l.id]),
  );

  // Desde Ventas: abrir el pedido de esa venta o proponer crearlo.
  let abrirId = typeof id === "string" ? id : null;
  let ventaOrigen: VentaOrigen | null = null;
  if (typeof venta === "string" && /^[0-9a-f-]{36}$/i.test(venta)) {
    const existente = await apiFetch<PedidoCliente | undefined>(`/api/v1/pedidos/por-venta/${venta}`, { token });
    if (existente) abrirId = existente.id;
    else if (gestionar && permisos.includes(PERMISOS.CRM_LEER)) {
      const v = await apiFetch<Venta>(`/api/v1/crm/ventas/${venta}`, { token }).catch(() => null);
      if (v) ventaOrigen = { id: v.id, codigo: v.codigo, clienteNombre: v.clienteNombre, comercialNombre: v.comercialNombre, ciudad: v.ciudadEntrega, moneda: v.moneda };
    }
  }

  return (
    <PedidosClienteCliente
      pedidos={pedidos}
      productos={opcionesProducto(productos, listaPorMoneda)}
      clientes={clientes.filter((c) => c.estado !== "inactivo").map((c) => ({ id: c.id, nombre: `${c.codigo} · ${c.nombreEmpresa}`, moneda: c.pais === "Colombia" ? "COP" : "USD" }))}
      comerciales={comerciales.map((c) => ({ id: c.id, nombre: c.nombre }))}
      abrirId={abrirId}
      ventaOrigen={ventaOrigen}
      puedeGestionar={gestionar}
      puedeDespachar={permisos.includes(PERMISOS.PEDIDOS_DESPACHAR)}
      puedeAsignar={permisos.includes(PERMISOS.CRM_VER_TODO) || !permisos.includes(PERMISOS.CRM_VENDER)}
    />
  );
}
