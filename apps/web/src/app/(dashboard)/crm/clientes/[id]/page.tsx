import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import type { Cliente, ClienteRelacionados } from "@/lib/crm";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { FichaCliente } from "./FichaCliente";

export const metadata: Metadata = { title: "Ficha de cliente" };

export default async function FichaClientePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const { token, permisos } = await exigirPermiso(PERMISOS.CLIENTES_LEER);
  const verCrm = permisos.includes(PERMISOS.CRM_LEER);

  let cliente: Cliente;
  let relacionados: ClienteRelacionados | null = null;
  try {
    [cliente, relacionados] = await Promise.all([
      apiFetch<Cliente>(`/api/v1/crm/clientes/${encodeURIComponent(id)}`, { token }),
      verCrm ? apiFetch<ClienteRelacionados>(`/api/v1/crm/clientes/${encodeURIComponent(id)}/relacionados`, { token }) : null,
    ]);
  } catch (err) {
    if (err instanceof ApiError && (err.status === 404 || err.status === 400)) notFound();
    throw err;
  }

  return (
    <FichaCliente
      cliente={cliente}
      relacionados={relacionados}
      puedeEditar={permisos.includes(PERMISOS.CLIENTES_EDITAR)}
    />
  );
}
