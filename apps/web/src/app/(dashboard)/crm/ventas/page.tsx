import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Cliente, Comercial, Venta } from "@/lib/crm";
import { hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { VentasCliente } from "./VentasCliente";

export const metadata: Metadata = { title: "Ventas" };

export default async function VentasPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.CRM_LEER);
  const puedeGestionar = permisos.includes(PERMISOS.VENTAS_GESTIONAR);
  const esComercial = permisos.includes(PERMISOS.CRM_VENDER);

  const [ventas, clientes, comerciales] = await Promise.all([
    apiFetch<Venta[]>("/api/v1/crm/ventas", { token }),
    puedeGestionar && permisos.includes(PERMISOS.CLIENTES_LEER)
      ? apiFetch<Cliente[]>("/api/v1/crm/clientes", { token })
      : Promise.resolve([]),
    apiFetch<Comercial[]>("/api/v1/crm/comerciales", { token }),
  ]);

  return (
    <VentasCliente
      ventas={ventas}
      clientes={clientes.filter((c) => c.estado !== "inactivo").map(({ id, codigo, nombreEmpresa }) => ({ id, codigo, nombreEmpresa }))}
      comerciales={comerciales}
      puedeGestionar={puedeGestionar}
      puedeAsignar={permisos.includes(PERMISOS.CRM_VER_TODO) || !esComercial}
      hoy={hoyNegocio()}
    />
  );
}
