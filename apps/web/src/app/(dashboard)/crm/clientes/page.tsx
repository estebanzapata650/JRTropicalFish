import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Cliente } from "@/lib/crm";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ClientesCliente } from "./ClientesCliente";

export const metadata: Metadata = { title: "Clientes" };

export default async function ClientesPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.CLIENTES_LEER);
  const clientes = await apiFetch<Cliente[]>("/api/v1/crm/clientes", { token });
  return <ClientesCliente clientes={clientes} puedeEditar={permisos.includes(PERMISOS.CLIENTES_EDITAR)} />;
}
