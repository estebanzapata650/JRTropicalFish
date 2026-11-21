import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Producto, Proveedor } from "@/lib/catalogos";
import type { Oferta } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { OfertasCliente } from "./OfertasCliente";

export const metadata: Metadata = { title: "Disponibilidad de proveedores" };

export default async function OfertasPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const gestionar = permisos.includes(PERMISOS.COMPRAS_GESTIONAR);
  const [ofertas, productos, proveedores] = await Promise.all([
    apiFetch<Oferta[]>("/api/v1/compras/ofertas", { token }),
    gestionar && permisos.includes(PERMISOS.CATALOGOS_LEER) ? apiFetch<Producto[]>("/api/v1/catalogos/productos", { token }) : Promise.resolve([]),
    gestionar && permisos.includes(PERMISOS.PROVEEDORES_LEER) ? apiFetch<Proveedor[]>("/api/v1/catalogos/proveedores", { token }) : Promise.resolve([]),
  ]);

  return (
    <OfertasCliente
      ofertas={ofertas}
      productos={productos.filter((p) => p.activo).map((p) => ({ id: p.id, codigo: p.codigo, nombre: p.nombreCientifico }))}
      proveedores={proveedores.filter((p) => p.activo).map((p) => ({ id: p.id, nombre: p.nombre }))}
      puedeGestionar={gestionar}
    />
  );
}
