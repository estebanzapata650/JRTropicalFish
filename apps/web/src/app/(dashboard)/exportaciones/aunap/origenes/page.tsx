import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CentroAunap, MunicipioAunap, ParametrosAunap } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { OrigenesCliente } from "./OrigenesCliente";

export const metadata: Metadata = { title: "Orígenes AUNAP" };

/** Municipio de origen de cada centro de acopio y encabezado del informe mensual AUNAP. */
export default async function OrigenesAunapPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  const [centros, municipios, parametros] = await Promise.all([
    apiFetch<CentroAunap[]>("/api/v1/exportaciones/aunap/centros", { token }),
    apiFetch<MunicipioAunap[]>("/api/v1/exportaciones/aunap/municipios", { token }),
    apiFetch<ParametrosAunap>("/api/v1/exportaciones/aunap/parametros", { token }),
  ]);
  return (
    <OrigenesCliente
      centros={centros}
      municipios={municipios}
      parametros={parametros}
      configurar={permisos.includes(PERMISOS.EXPORTACIONES_CONFIGURAR)}
    />
  );
}
