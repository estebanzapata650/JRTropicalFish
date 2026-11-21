"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

/** Server Actions del catálogo. Reciben objetos planos; toda validación real la hace la API. */

type Cuerpo = Record<string, unknown>;

async function ejecutar<T = unknown>(
  ruta: string,
  metodo: "POST" | "PUT" | "DELETE",
  body: unknown,
  porDefecto: string,
): Promise<EstadoFormulario & { datos?: T }> {
  const session = await getSession();
  if (!session) redirect("/login");
  let datos: T;
  try {
    datos = await apiFetch<T>(ruta, { method: metodo, token: session.token, body });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : porDefecto };
  }
  revalidatePath("/catalogos", "layout");
  return { ok: true, datos };
}

export async function guardarProductoAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/catalogos/productos/${id}`, "PUT", datos, "No se pudo guardar el producto.")
    : ejecutar<{ id: string; codigo: string }>("/api/v1/catalogos/productos", "POST", datos, "No se pudo crear el producto.");
}

export async function eliminarProductoAction(id: string) {
  return ejecutar(`/api/v1/catalogos/productos/${id}`, "DELETE", undefined, "No se pudo eliminar el producto.");
}

export async function guardarListaAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/catalogos/precios/listas/${id}`, "PUT", datos, "No se pudo guardar la lista.")
    : ejecutar("/api/v1/catalogos/precios/listas", "POST", datos, "No se pudo crear la lista.");
}

export async function recalcularListaAction(id: string) {
  return ejecutar<{ calculados: number; cambiados: number; sinBase: number }>(
    `/api/v1/catalogos/precios/listas/${id}/recalcular`,
    "POST",
    undefined,
    "No se pudo recalcular la lista.",
  );
}

export async function guardarParametrosPreciosAction(datos: Cuerpo) {
  return ejecutar("/api/v1/catalogos/precios/parametros", "PUT", datos, "No se pudieron guardar los parámetros.");
}

export async function guardarCentroAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/catalogos/centros-acopio/${id}`, "PUT", datos, "No se pudo guardar el centro.")
    : ejecutar("/api/v1/catalogos/centros-acopio", "POST", datos, "No se pudo crear el centro.");
}

export async function eliminarCentroAction(id: string) {
  return ejecutar(`/api/v1/catalogos/centros-acopio/${id}`, "DELETE", undefined, "No se pudo eliminar el centro.");
}

export async function guardarProveedorAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/catalogos/proveedores/${id}`, "PUT", datos, "No se pudo guardar el proveedor.")
    : ejecutar("/api/v1/catalogos/proveedores", "POST", datos, "No se pudo crear el proveedor.");
}

export async function eliminarProveedorAction(id: string) {
  return ejecutar(`/api/v1/catalogos/proveedores/${id}`, "DELETE", undefined, "No se pudo eliminar el proveedor.");
}
