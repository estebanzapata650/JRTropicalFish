"use server";

import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { getSession, setSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

type Sesion = { token: string; expiraEn: string };

/**
 * Cambia la contraseña del propio usuario. La API revoca las demás sesiones y devuelve un token
 * nuevo, que se guarda en la cookie para no sacar al usuario de esta.
 */
export async function cambiarPasswordAction(_prev: EstadoFormulario, formData: FormData): Promise<EstadoFormulario> {
  const session = await getSession();
  if (!session) redirect("/login");

  const passwordActual = String(formData.get("passwordActual") ?? "");
  const passwordNueva = String(formData.get("passwordNueva") ?? "");
  const confirmacion = String(formData.get("confirmacion") ?? "");

  if (passwordNueva !== confirmacion) {
    return { error: "La nueva contraseña y su confirmación no coinciden." };
  }

  let nueva: Sesion;
  try {
    nueva = await apiFetch<Sesion>("/api/v1/auth/cambiar-password", {
      method: "POST",
      token: session.token,
      body: { passwordActual, passwordNueva },
    });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : "No se pudo cambiar la contraseña." };
  }

  await setSession(nueva.token, nueva.expiraEn);
  return { ok: true };
}
