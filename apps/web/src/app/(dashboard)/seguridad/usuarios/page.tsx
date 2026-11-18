import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { PerfilResumen, UsuarioResumen } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { UsuariosCliente } from "./UsuariosCliente";

export const metadata: Metadata = { title: "Usuarios" };

export default async function UsuariosPage() {
  const usuario = await exigirPermiso(PERMISOS.USUARIOS_LEER);
  const { token, permisos } = usuario;

  // La lista de perfiles alimenta el selector del formulario. Sin perfiles:leer la API la negaría,
  // así que no se pide y el formulario avisa qué permiso falta.
  const [usuarios, perfiles] = await Promise.all([
    apiFetch<UsuarioResumen[]>("/api/v1/seguridad/usuarios", { token }),
    permisos.includes(PERMISOS.PERFILES_LEER)
      ? apiFetch<PerfilResumen[]>("/api/v1/seguridad/perfiles", { token })
      : Promise.resolve<PerfilResumen[]>([]),
  ]);

  return (
    <UsuariosCliente
      usuarios={usuarios}
      perfiles={perfiles.filter((p) => p.activo).map((p) => ({ id: p.id, nombre: p.nombre }))}
      usuarioActualId={usuario.id}
      puedeCrear={permisos.includes(PERMISOS.USUARIOS_CREAR)}
      puedeEditar={permisos.includes(PERMISOS.USUARIOS_EDITAR)}
      puedeCambiarEstado={permisos.includes(PERMISOS.USUARIOS_ELIMINAR)}
      ahoraIso={new Date().toISOString()}
    />
  );
}
