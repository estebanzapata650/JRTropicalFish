"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

/**
 * Server Actions del CRM. Reciben objetos planos ya armados por el cliente (no FormData): los
 * formularios del CRM tienen campos condicionales y numéricos, y así el tipo viaja tal cual.
 * Toda validación real la hace la API; aquí solo se reenvía y se traduce el error.
 */

type Cuerpo = Record<string, unknown>;

async function token() {
  const session = await getSession();
  if (!session) redirect("/login");
  return session.token;
}

async function ejecutar<T = unknown>(
  ruta: string,
  metodo: "POST" | "PUT" | "DELETE",
  body: unknown,
  porDefecto: string,
): Promise<EstadoFormulario & { datos?: T }> {
  let datos: T;
  try {
    datos = await apiFetch<T>(ruta, { method: metodo, token: await token(), body });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : porDefecto };
  }
  // Todo el CRM comparte datos (una venta aparece en la ficha del cliente y en el dashboard).
  revalidatePath("/crm", "layout");
  return { ok: true, datos };
}

// --- Clientes ---

export async function guardarClienteAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/crm/clientes/${id}`, "PUT", datos, "No se pudo guardar el cliente.")
    : ejecutar<{ id: string; codigo: string }>("/api/v1/crm/clientes", "POST", datos, "No se pudo crear el cliente.");
}

export async function eliminarClienteAction(id: string) {
  return ejecutar(`/api/v1/crm/clientes/${id}`, "DELETE", undefined, "No se pudo eliminar el cliente.");
}

// --- Oportunidades ---

export async function guardarOportunidadAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/crm/oportunidades/${id}`, "PUT", datos, "No se pudo guardar la oportunidad.")
    : ejecutar("/api/v1/crm/oportunidades", "POST", datos, "No se pudo crear la oportunidad.");
}

export async function cambiarEtapaAction(id: string, etapaActual: string, etapa: string, motivoPerdida?: string) {
  return ejecutar(
    `/api/v1/crm/oportunidades/${id}/etapa`,
    "PUT",
    { etapaActual, etapa, motivoPerdida: motivoPerdida ?? null },
    "No se pudo cambiar la etapa.",
  );
}

export async function ganarOportunidadAction(id: string, datos: Cuerpo) {
  return ejecutar<{ ventaId: string; ventaCodigo: string; trackingId: string | null; trackingCodigo: string | null }>(
    `/api/v1/crm/oportunidades/${id}/ganar`,
    "POST",
    datos,
    "No se pudo ganar la oportunidad.",
  );
}

export async function eliminarOportunidadAction(id: string) {
  return ejecutar(`/api/v1/crm/oportunidades/${id}`, "DELETE", undefined, "No se pudo eliminar la oportunidad.");
}

// --- Ventas y vuelos ---

export async function guardarVentaAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/crm/ventas/${id}`, "PUT", datos, "No se pudo guardar la venta.")
    : ejecutar("/api/v1/crm/ventas", "POST", datos, "No se pudo registrar la venta.");
}

export async function guardarTrackingAction(id: string, datos: Cuerpo) {
  return ejecutar(`/api/v1/crm/tracking/${id}`, "PUT", datos, "No se pudo guardar el despacho.");
}

export async function cambiarFaseAction(id: string, faseActual: string, fase: string) {
  return ejecutar(`/api/v1/crm/tracking/${id}/fase`, "PUT", { faseActual, fase }, "No se pudo cambiar la fase.");
}

// --- Tareas ---

export async function guardarTareaAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/crm/tareas/${id}`, "PUT", datos, "No se pudo guardar la tarea.")
    : ejecutar("/api/v1/crm/tareas", "POST", datos, "No se pudo crear la tarea.");
}

export async function cambiarEstadoTareaAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`/api/v1/crm/tareas/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo mover la tarea.");
}

export async function eliminarTareaAction(id: string) {
  return ejecutar(`/api/v1/crm/tareas/${id}`, "DELETE", undefined, "No se pudo eliminar la tarea.");
}

// --- Metas ---

export async function guardarMetasAction(mes: string, metas: { comercialId: string; metaCop: number }[]) {
  return ejecutar("/api/v1/crm/metricas/metas", "PUT", { mes, metas }, "No se pudieron guardar las metas.");
}

export async function guardarParametrosAction(datos: Cuerpo) {
  return ejecutar("/api/v1/crm/metricas/parametros", "PUT", datos, "No se pudieron guardar los parámetros.");
}
