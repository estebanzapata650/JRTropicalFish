import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Acuario } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { AcuariosCliente } from "./AcuariosCliente";

export const metadata: Metadata = { title: "Acuarios" };

export default async function AcuariosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const acuarios = await apiFetch<Acuario[]>("/api/v1/inventario/acuarios", { token });
  return <AcuariosCliente acuarios={acuarios} puedeEditar={permisos.includes(PERMISOS.INVENTARIO_AJUSTAR)} />;
}
