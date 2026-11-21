import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { esTerminal, type Cliente, type Comercial, type Oportunidad, type Tarea } from "@/lib/crm";
import { hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { TareasCliente } from "./TareasCliente";

export const metadata: Metadata = { title: "Tareas" };

export default async function TareasPage() {
  const { token, permisos, id } = await exigirPermiso(PERMISOS.CRM_LEER);
  const puedeGestionar = permisos.includes(PERMISOS.CRM_VENDER);

  const [tareas, comerciales, clientes, oportunidades] = await Promise.all([
    apiFetch<Tarea[]>("/api/v1/crm/tareas", { token }),
    apiFetch<Comercial[]>("/api/v1/crm/comerciales", { token }),
    puedeGestionar && permisos.includes(PERMISOS.CLIENTES_LEER)
      ? apiFetch<Cliente[]>("/api/v1/crm/clientes", { token })
      : Promise.resolve([]),
    puedeGestionar ? apiFetch<Oportunidad[]>("/api/v1/crm/oportunidades", { token }) : Promise.resolve([]),
  ]);

  return (
    <TareasCliente
      tareas={tareas}
      comerciales={comerciales}
      clientes={clientes.filter((c) => c.estado !== "inactivo").map(({ id, codigo, nombreEmpresa }) => ({ id, codigo, nombreEmpresa }))}
      // Para vincular una tarea basta con las oportunidades abiertas.
      oportunidades={oportunidades
        .filter((o) => !esTerminal(o.etapa))
        .map(({ id, codigo, clienteId }) => ({ id, codigo, clienteId }))}
      usuarioId={id}
      puedeGestionar={puedeGestionar}
      puedeAsignar={permisos.includes(PERMISOS.CRM_VER_TODO)}
      hoy={hoyNegocio()}
    />
  );
}
