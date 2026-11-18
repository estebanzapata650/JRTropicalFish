import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { PerfilDetalle, PerfilResumen, PermisoCatalogo } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { PerfilesCliente } from "./PerfilesCliente";

export const metadata: Metadata = { title: "Perfiles" };

export default async function PerfilesPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.PERFILES_LEER);

  const [perfiles, catalogo] = await Promise.all([
    apiFetch<PerfilResumen[]>("/api/v1/seguridad/perfiles", { token }),
    apiFetch<PermisoCatalogo[]>("/api/v1/seguridad/perfiles/permisos", { token }),
  ]);

  // El detalle (con sus permisos) se precarga para que abrir la edición sea inmediato. Son pocos
  // perfiles; si algún día fueran muchos, cargarlo al abrir el diálogo.
  const detalles = Object.fromEntries(
    (await Promise.all(perfiles.map((p) => apiFetch<PerfilDetalle>(`/api/v1/seguridad/perfiles/${p.id}`, { token })))).map(
      (d) => [d.id, d],
    ),
  );

  return (
    <PerfilesCliente
      perfiles={perfiles}
      catalogo={catalogo}
      detalles={detalles}
      puedeCrear={permisos.includes(PERMISOS.PERFILES_CREAR)}
      puedeEditar={permisos.includes(PERMISOS.PERFILES_EDITAR)}
      puedeEliminar={permisos.includes(PERMISOS.PERFILES_ELIMINAR)}
    />
  );
}
