import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { UsuarioResumen } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ConfiguracionSanidadCliente, type AcuarioSanidad } from "./ConfiguracionSanidadCliente";

export const metadata: Metadata = { title: "Configuración de sanidad" };

export default async function ConfiguracionSanidadPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_GESTIONAR);
  const [parametros, acuarios, usuarios] = await Promise.all([
    apiFetch<{ horasAyunoVuelo: number }>("/api/v1/sanidad/parametros", { token }),
    apiFetch<AcuarioSanidad[]>("/api/v1/sanidad/acuarios", { token }),
    permisos.includes(PERMISOS.USUARIOS_LEER) ? apiFetch<UsuarioResumen[]>("/api/v1/seguridad/usuarios", { token }) : Promise.resolve([] as UsuarioResumen[]),
  ]);
  return (
    <ConfiguracionSanidadCliente
      horasAyunoVuelo={parametros.horasAyunoVuelo}
      acuarios={acuarios}
      usuarios={usuarios.filter((u) => u.activo).map((u) => ({ id: u.id, nombre: u.nombre }))}
    />
  );
}
