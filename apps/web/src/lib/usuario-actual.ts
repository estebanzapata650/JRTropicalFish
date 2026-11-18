import "server-only";
import { cache } from "react";
import { redirect } from "next/navigation";
import { apiFetch } from "./api";
import type { Permiso, UsuarioActual } from "./permisos";
import { getSession } from "./session";

/**
 * Sesión + identidad del usuario, una sola vez por petición (`cache` de React): el layout y la
 * página la piden y la API se consulta una vez. Si no hay sesión o la API la rechaza (expirada o
 * revocada), se redirige al login.
 */
export const obtenerUsuarioActual = cache(async (): Promise<UsuarioActual & { token: string }> => {
  const session = await getSession();
  if (!session) redirect("/login");

  try {
    const usuario = await apiFetch<UsuarioActual>("/api/v1/auth/yo", { token: session.token });
    return { ...usuario, token: session.token };
  } catch {
    redirect("/login");
  }
});

/** Para páginas: sin el permiso se vuelve al panel (la API igual rechazaría las llamadas). */
export async function exigirPermiso(permiso: Permiso) {
  const usuario = await obtenerUsuarioActual();
  if (!usuario.permisos.includes(permiso)) redirect("/");
  return usuario;
}
