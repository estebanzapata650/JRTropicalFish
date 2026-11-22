"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch, apiUpload } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

/**
 * Server Actions de sanidad. La API valida y calcula las dosis; aquí solo se reenvía. Tratamientos y reservas
 * tocan lo que ven inventario, pedidos y exportaciones, así que se revalidan también.
 */
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
  for (const r of ["/sanidad", "/inventario", "/crm/pedidos", "/exportaciones"]) revalidatePath(r, "layout");
  return { ok: true, datos };
}

type Cuerpo = Record<string, unknown>;
const base = "/api/v1/sanidad";

export async function guardarMedicamentoAction(id: string | null, datos: Cuerpo) {
  return ejecutar<{ id: string }>(id ? `${base}/medicamentos/${id}` : `${base}/medicamentos`, id ? "PUT" : "POST", datos, "No se pudo guardar el medicamento.");
}

export async function guardarAlimentoAction(id: string | null, datos: Cuerpo) {
  return ejecutar<{ id: string }>(id ? `${base}/alimentos/${id}` : `${base}/alimentos`, id ? "PUT" : "POST", datos, "No se pudo guardar el alimento.");
}

export async function guardarDietaAction(id: string | null, datos: Cuerpo) {
  return ejecutar<{ id: string }>(id ? `${base}/dietas/${id}` : `${base}/dietas`, id ? "PUT" : "POST", datos, "No se pudo guardar la dieta.");
}

export async function eliminarDietaAction(id: string) {
  return ejecutar(`${base}/dietas/${id}`, "DELETE", null, "No se pudo eliminar la dieta.");
}

export async function guardarFichaAction(productoId: string, datos: Cuerpo) {
  return ejecutar(`${base}/fichas/${productoId}`, "PUT", datos, "No se pudo guardar la ficha.");
}

export async function guardarProtocoloAction(id: string | null, datos: Cuerpo) {
  return ejecutar<{ id: string }>(id ? `${base}/protocolos/${id}` : `${base}/protocolos`, id ? "PUT" : "POST", datos, "No se pudo guardar el protocolo.");
}

export async function cambiarEstadoProtocoloAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`${base}/protocolos/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo cambiar el estado del protocolo.");
}

export async function crearTratamientoAction(datos: Cuerpo) {
  return ejecutar<{ id: string; codigo: string; avisos: string[] }>(`${base}/tratamientos`, "POST", datos, "No se pudo iniciar el tratamiento.");
}

export async function cambiarEstadoTratamientoAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`${base}/tratamientos/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo cambiar el estado del tratamiento.");
}

export async function aplicarAction(id: string, cantidad: number | null) {
  return ejecutar(`${base}/aplicaciones/${id}/aplicar`, "PUT", { cantidad, nota: null }, "No se pudo registrar la aplicación.");
}

export async function omitirAction(id: string, nota: string) {
  return ejecutar(`${base}/aplicaciones/${id}/omitir`, "PUT", { cantidad: null, nota }, "No se pudo registrar.");
}

export async function alimentarAction(acuarioId: string, alimentoId: string | null, cantidad: number | null) {
  return ejecutar(`${base}/alimentacion`, "POST", { acuarioId, alimentoId, cantidad }, "No se pudo registrar la alimentación.");
}

export async function fijarReservaAction(lineaId: string, loteId: string, acuarioId: string, cantidad: number) {
  return ejecutar(`${base}/reservas`, "PUT", { lineaId, loteId, acuarioId, cantidad }, "No se pudo guardar la reserva.");
}

export async function guardarParametrosAction(horasAyunoVuelo: number) {
  return ejecutar(`${base}/parametros`, "PUT", { horasAyunoVuelo }, "No se pudieron guardar los parámetros.");
}

export async function asignarResponsableAction(acuarioId: string, responsableId: string | null) {
  return ejecutar(`${base}/acuarios/${acuarioId}/responsable`, "PUT", { responsableId }, "No se pudo asignar el responsable.");
}

/** Ubicar un lote por ubicar (operación de inventario), desde Mi día o el tablero. */
export async function ubicarAction(loteId: string, acuarioId: string, cantidad: number) {
  return ejecutar(
    "/api/v1/inventario/operaciones",
    "POST",
    { operaciones: [{ tipo: "ubicacion", loteId, acuarioOrigenId: null, condicionOrigen: "sano", acuarioDestinoId: acuarioId, condicionDestino: "sano", cantidad, nota: null }] },
    "No se pudo ubicar el lote.",
  );
}

export async function subirReporteAction(id: string, formData: FormData): Promise<EstadoFormulario> {
  const session = await getSession();
  if (!session) redirect("/login");
  const archivo = formData.get("archivo");
  if (!(archivo instanceof File) || archivo.size === 0) return { error: "Selecciona el PDF del reporte." };
  const datos = new FormData();
  datos.append("archivo", archivo);
  try {
    await apiUpload(`${base}/tratamientos/${id}/reporte`, datos, session.token);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : "No se pudo subir el reporte." };
  }
  revalidatePath("/sanidad", "layout");
  return { ok: true };
}
