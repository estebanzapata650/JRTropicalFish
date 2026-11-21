"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch, apiUpload } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

const RUTA_EMPLEADOS = "/administracion/empleados";
const RUTA_MAESTROS = "/administracion/maestros";
const TAMANO_MAXIMO_FOTO = 10 * 1024 * 1024;

async function token() {
  const session = await getSession();
  if (!session) redirect("/login");
  return session.token;
}

/** Traduce un error de la API. Un 401 es una sesión revocada: se vuelve al login. */
function traducir(err: unknown, porDefecto: string): EstadoFormulario {
  if (err instanceof ApiError && err.status === 401) redirect("/login");
  return { error: err instanceof ApiError ? err.message : porDefecto };
}

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
    return traducir(err, porDefecto);
  }
  revalidatePath(revalidar);
  return { ok: true };
}

const texto = (formData: FormData, campo: string) => String(formData.get(campo) ?? "").trim() || null;

// --- Maestros ---

export async function guardarValorMaestroAction(
  clave: string,
  id: string | null,
  formData: FormData,
): Promise<EstadoFormulario> {
  const body = {
    codigo: texto(formData, "codigo") ?? "",
    nombre: texto(formData, "nombre") ?? "",
    activo: formData.get("activo") === "on",
  };
  const base = `/api/v1/maestros/${encodeURIComponent(clave)}`;
  return ejecutar(id ? `${base}/${id}` : base, id ? "PUT" : "POST", body, "No se pudo guardar el valor.", RUTA_MAESTROS);
}

export async function cambiarEstadoValorMaestroAction(clave: string, id: string, activo: boolean): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/maestros/${encodeURIComponent(clave)}/${id}/estado`,
    "PUT",
    { activo },
    "No se pudo cambiar el estado.",
    RUTA_MAESTROS,
  );
}

export async function eliminarValorMaestroAction(clave: string, id: string): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/maestros/${encodeURIComponent(clave)}/${id}`,
    "DELETE",
    undefined,
    "No se pudo eliminar el valor.",
    RUTA_MAESTROS,
  );
}

// --- Empleados ---

/**
 * Crea o actualiza el empleado y, si viene, sube la foto (o la quita). Son dos llamadas: si la foto
 * falla, el empleado ya quedó guardado y se avisa para que solo se reintente la foto.
 */
export async function guardarEmpleadoAction(
  id: string | null,
  puedeVerSalario: boolean,
  formData: FormData,
): Promise<EstadoFormulario & { id?: string }> {
  const salario = texto(formData, "salario");
  const body = {
    tipoIdentificacionId: texto(formData, "tipoIdentificacionId"),
    numeroIdentificacion: texto(formData, "numeroIdentificacion") ?? "",
    nombres: texto(formData, "nombres") ?? "",
    apellidos: texto(formData, "apellidos") ?? "",
    genero: texto(formData, "genero"),
    fechaNacimiento: texto(formData, "fechaNacimiento"),
    telefono: texto(formData, "telefono"),
    email: texto(formData, "email"),
    direccion: texto(formData, "direccion"),
    ciudad: texto(formData, "ciudad"),
    cargoId: texto(formData, "cargoId"),
    areaId: texto(formData, "areaId"),
    tipoContratoId: texto(formData, "tipoContratoId"),
    fechaIngreso: texto(formData, "fechaIngreso"),
    fechaRetiro: texto(formData, "fechaRetiro"),
    // Sin el permiso no se envía: la API lo rechazaría y conserva el salario guardado.
    salario: puedeVerSalario && salario !== null ? Number(salario) : null,
  };

  if (!body.tipoIdentificacionId || !body.cargoId || !body.tipoContratoId) {
    return { error: "Completa el tipo de identificación, el cargo y el tipo de contrato." };
  }
  if (!body.fechaIngreso) return { error: "La fecha de ingreso es obligatoria." };
  if (body.salario !== null && !Number.isFinite(body.salario)) return { error: "El salario no es válido." };

  const foto = formData.get("foto");
  const hayFoto = foto instanceof File && foto.size > 0;
  if (hayFoto && foto.size > TAMANO_MAXIMO_FOTO) return { error: "La foto no puede pesar más de 10 MB." };

  const sesion = await token();
  let empleadoId = id;
  try {
    if (id) {
      await apiFetch(`/api/v1/administracion/empleados/${id}`, { method: "PUT", token: sesion, body });
    } else {
      const creado = await apiFetch<{ id: string }>("/api/v1/administracion/empleados", { method: "POST", token: sesion, body });
      empleadoId = creado.id;
    }
  } catch (err) {
    return traducir(err, "No se pudo guardar el empleado.");
  }

  revalidatePath(RUTA_EMPLEADOS);

  try {
    if (hayFoto) {
      const datos = new FormData();
      datos.append("archivo", foto, foto.name);
      await apiUpload(`/api/v1/administracion/empleados/${empleadoId}/foto`, datos, sesion);
    } else if (id && formData.get("quitarFoto") === "on") {
      await apiFetch(`/api/v1/administracion/empleados/${id}/foto`, { method: "DELETE", token: sesion });
    }
  } catch (err) {
    const { error } = traducir(err, "No se pudo guardar la foto.");
    return { id: empleadoId ?? undefined, error: `El empleado se guardó, pero la foto no: ${error}` };
  }

  return { ok: true, id: empleadoId ?? undefined };
}

export async function cambiarEstadoEmpleadoAction(id: string, activo: boolean): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/administracion/empleados/${id}/estado`,
    "PUT",
    { activo },
    "No se pudo cambiar el estado del empleado.",
    RUTA_EMPLEADOS,
  );
}

export async function eliminarEmpleadoAction(id: string): Promise<EstadoFormulario> {
  return ejecutar(
    `/api/v1/administracion/empleados/${id}`,
    "DELETE",
    undefined,
    "No se pudo eliminar el empleado.",
    RUTA_EMPLEADOS,
  );
}
