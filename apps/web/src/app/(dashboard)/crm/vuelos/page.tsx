import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { ParametrosComerciales, Tracking } from "@/lib/crm";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { VuelosCliente } from "./VuelosCliente";

export const metadata: Metadata = { title: "Torre de control de vuelos" };

/** Umbral de alerta DOA si el usuario no puede leer los parámetros comerciales. */
const UMBRAL_DOA_POR_DEFECTO = 3;

export default async function VuelosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.CRM_LEER);
  const [despachos, parametros] = await Promise.all([
    apiFetch<Tracking[]>("/api/v1/crm/tracking", { token }),
    permisos.includes(PERMISOS.METAS_LEER)
      ? apiFetch<ParametrosComerciales>("/api/v1/crm/metricas/parametros", { token })
      : Promise.resolve(null),
  ]);

  return (
    <VuelosCliente
      despachos={despachos}
      umbralDoa={parametros?.umbralDoa ?? UMBRAL_DOA_POR_DEFECTO}
      puedeGestionar={permisos.includes(PERMISOS.TRACKING_GESTIONAR)}
    />
  );
}
