import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Cliente, Comercial, Oportunidad } from "@/lib/crm";
import { hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PipelineCliente } from "./PipelineCliente";

export const metadata: Metadata = { title: "Pipeline comercial" };

export default async function PipelinePage() {
  const { token, permisos, id } = await exigirPermiso(PERMISOS.CRM_LEER);
  const esComercial = permisos.includes(PERMISOS.CRM_VENDER);
  const verTodo = permisos.includes(PERMISOS.CRM_VER_TODO);

  const [oportunidades, clientes, comerciales] = await Promise.all([
    apiFetch<Oportunidad[]>("/api/v1/crm/oportunidades", { token }),
    permisos.includes(PERMISOS.CLIENTES_LEER) ? apiFetch<Cliente[]>("/api/v1/crm/clientes", { token }) : Promise.resolve([]),
    apiFetch<Comercial[]>("/api/v1/crm/comerciales", { token }),
  ]);

  return (
    <PipelineCliente
      oportunidades={oportunidades}
      clientes={clientes.filter((c) => c.estado !== "inactivo").map(({ id, nombreEmpresa, codigo }) => ({ id, nombreEmpresa, codigo }))}
      comerciales={comerciales}
      usuarioId={id}
      puedeVender={esComercial}
      // Quien no es comercial (gerencia, logística) o tiene crm:ver-todo elige el comercial.
      puedeAsignar={verTodo || !esComercial}
      hoy={hoyNegocio()}
    />
  );
}
