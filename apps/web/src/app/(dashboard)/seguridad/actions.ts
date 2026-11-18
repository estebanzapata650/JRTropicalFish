"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

async function token() {
  const session = await getSession();
  if (!session) redirect("/login");
  return session.token;
}

/**
 * Llama a la API y traduce el resultado. Un 401 significa que la sesión fue revocada (por ejemplo,
 * el administrador cambió los permisos del propio perfil): se vuelve al login.
 */
async function ejecutar(
  ruta: string,
  metodo: "POST" | "PUT" | "DELETE",
  body: unknown,
  porDefecto: string,
  revalidar: string,
): Promise<EstadoFormulario> {
  try {
    await apiFetch(ruta, { method: metodo, token: await token(), body });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : porDefecto };
  }
  revalidatePath(revalidar);
  return { ok: true };
}

// --- Usuarios ---

export async function guardarUsuarioAction(id: string | null, formData: FormData): Promise<EstadoFormulario> {
  const password = String(formData.get("password") ?? "");
  const confirmacion = String(formData.get("confirmacion") ?? "");
  if (password !== confirmacion) {
    return { error: "La contraseña y su confirmación no coinciden." };
  }

  const datos = {
    nombre: String(formData.get("nombre") ?? "").trim(),
    email: String(formData.get("email") ?? "").trim(),
    perfilId: String(formData.get("perfilId") ?? ""),
  };

  return id
    ? ejecutar(
        `/api/v1/seguridad/usuarios/${id}`,
        "PUT",
        // Al editar, la contraseña vacía significa "no cambiarla".
        { ...datos, password: password === "" ? null : password },
        "No se pudo guardar el usuario.",
        "/seguridad/usuarios",
      )
    : ejecutar("/api/v1/seguridad/usuarios", "POST", { ...datos, password }, "No se pudo crear el usuario.", "/seguridad/usuarios");
}

export async function cambiarEstadoUsuarioAction(id: string, activo: boolean): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/seguridad/usuarios/${id}/estado`,
    "PUT",
    { activo },
    "No se pudo cambiar el estado del usuario.",
    "/seguridad/usuarios",
  );
}

export async function desbloquearUsuarioAction(id: string): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/seguridad/usuarios/${id}/desbloquear`,
    "POST",
    undefined,
    "No se pudo desbloquear el usuario.",
    "/seguridad/usuarios",
  );
}

// --- Perfiles ---

export async function guardarPerfilAction(id: string | null, formData: FormData): Promise<EstadoFormulario> {
  const body = {
    nombre: String(formData.get("nombre") ?? "").trim(),
    descripcion: String(formData.get("descripcion") ?? "").trim() || null,
    activo: formData.get("activo") === "on",
    permisos: formData.getAll("permisos").map(String),
  };

  return ejecutar(
    id ? `/api/v1/seguridad/perfiles/${id}` : "/api/v1/seguridad/perfiles",
    id ? "PUT" : "POST",
    body,
    "No se pudo guardar el perfil.",
    "/seguridad/perfiles",
  );
}

export async function eliminarPerfilAction(id: string): Promise<EstadoFormulario> {
  return ejecutar(`/api/v1/seguridad/perfiles/${id}`, "DELETE", undefined, "No se pudo eliminar el perfil.", "/seguridad/perfiles");
}
