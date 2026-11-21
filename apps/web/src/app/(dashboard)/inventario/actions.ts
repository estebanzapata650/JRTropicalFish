"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

/**
 * Server Actions de inventario, compras y pedidos de cliente. Reciben objetos planos; la API valida.
 * Un movimiento de stock cambia varias pantallas a la vez (existencias, lotes, dashboard, pedidos,
 * la venta del CRM), así que se revalida todo lo relacionado.
 */
async function ejecutar<T = unknown>(
  ruta: string,
  metodo: "POST" | "PUT",
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
  for (const ruta of ["/inventario", "/compras", "/crm"]) revalidatePath(ruta, "layout");
  return { ok: true, datos };
}

type Cuerpo = Record<string, unknown>;

// --- Stock ---
export async function operarStockAction(operaciones: Cuerpo[]) {
  return ejecutar("/api/v1/inventario/operaciones", "POST", { operaciones }, "No se pudo registrar el movimiento.");
}

export async function guardarAcuarioAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/inventario/acuarios/${id}`, "PUT", datos, "No se pudo guardar el acuario.")
    : ejecutar("/api/v1/inventario/acuarios", "POST", datos, "No se pudo crear el acuario.");
}

// --- Compras ---
export async function guardarOfertaAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/compras/ofertas/${id}`, "PUT", datos, "No se pudo guardar la disponibilidad.")
    : ejecutar<{ id: string; codigo: string }>("/api/v1/compras/ofertas", "POST", datos, "No se pudo registrar la disponibilidad.");
}

export async function guardarPedidoProveedorAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/compras/pedidos/${id}`, "PUT", datos, "No se pudo guardar el pedido.")
    : ejecutar<{ id: string; codigo: string }>("/api/v1/compras/pedidos", "POST", datos, "No se pudo crear el pedido.");
}

export async function cambiarEstadoPedidoProveedorAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`/api/v1/compras/pedidos/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo cambiar el estado.");
}

export async function recibirPedidoAction(datos: Cuerpo) {
  return ejecutar<{ id: string; codigo: string; lotes: string[]; avisos: string[] }>(
    "/api/v1/compras/recepciones",
    "POST",
    datos,
    "No se pudo registrar la recepción.",
  );
}

// --- Pedidos de cliente ---
export async function guardarPedidoClienteAction(id: string | null, datos: Cuerpo) {
  return id
    ? ejecutar(`/api/v1/pedidos/${id}`, "PUT", datos, "No se pudo guardar el pedido.")
    : ejecutar<{ id: string; codigo: string }>("/api/v1/pedidos", "POST", datos, "No se pudo crear el pedido.");
}

export async function cambiarEstadoPedidoClienteAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`/api/v1/pedidos/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo cambiar el estado.");
}

export async function despacharPedidoAction(id: string, salidas: Cuerpo[]) {
  return ejecutar<{ estado: string; cumplimiento: number }>(`/api/v1/pedidos/${id}/despachar`, "POST", { salidas }, "No se pudo despachar.");
}
