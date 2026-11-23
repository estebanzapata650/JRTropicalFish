import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { EspecieAunap, ProductoAunap } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { AunapCliente } from "./AunapCliente";

export const metadata: Metadata = { title: "AUNAP" };

/** Especies autorizadas por la AUNAP y el código de cada producto del catálogo. */
export default async function AunapPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  const [especies, productos] = await Promise.all([
    apiFetch<EspecieAunap[]>("/api/v1/exportaciones/aunap/especies", { token }),
    apiFetch<ProductoAunap[]>("/api/v1/exportaciones/aunap/productos", { token }),
  ]);
  return <AunapCliente especies={especies} productos={productos} configurar={permisos.includes(PERMISOS.EXPORTACIONES_CONFIGURAR)} />;
}
