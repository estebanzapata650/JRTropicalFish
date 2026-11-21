import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Configuracion } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ConfiguracionCliente } from "./ConfiguracionCliente";

export const metadata: Metadata = { title: "Configuración de exportaciones" };

export default async function ConfiguracionExportacionesPage() {
  const { token } = await exigirPermiso(PERMISOS.EXPORTACIONES_CONFIGURAR);
  const configuracion = await apiFetch<Configuracion>("/api/v1/exportaciones/configuracion", { token });
  return <ConfiguracionCliente c={configuracion} />;
}
