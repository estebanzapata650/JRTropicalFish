"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { ApiError, apiFetch, apiUpload } from "@/lib/api";
import { getSession } from "@/lib/session";
import type { EstadoFormulario } from "@/lib/types";

/**
 * Server Actions de exportaciones. Reciben objetos planos; la API valida y calcula. Un embarque toca la
 * venta, el tracking y el pedido de cliente, así que se revalidan también /crm e /inventario.
 */
async function ejecutar<T = unknown>(ruta: string, metodo: "POST" | "PUT", body: unknown, porDefecto: string): Promise<EstadoFormulario & { datos?: T }> {
  const session = await getSession();
  if (!session) redirect("/login");
  let datos: T;
  try {
    datos = await apiFetch<T>(ruta, { method: metodo, token: session.token, body });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : porDefecto };
  }
  for (const r of ["/exportaciones", "/crm", "/inventario"]) revalidatePath(r, "layout");
  return { ok: true, datos };
}

type Cuerpo = Record<string, unknown>;
const base = "/api/v1/exportaciones";

// --- Embarques ---
export async function crearEmbarqueAction(ventaId: string) {
  return ejecutar<{ id: string; codigo: string }>(`${base}/embarques`, "POST", { ventaId }, "No se pudo crear el embarque.");
}

export async function guardarEmbarqueAction(id: string, datos: Cuerpo) {
  return ejecutar(`${base}/embarques/${id}`, "PUT", datos, "No se pudo guardar el embarque.");
}

export async function guardarLineasEmbarqueAction(id: string, lineas: Cuerpo[]) {
  return ejecutar<{ avisos: string[] }>(`${base}/embarques/${id}/lineas`, "PUT", { lineas }, "No se pudieron guardar las especies.");
}

export async function guardarEmpaqueAction(id: string, filas: Cuerpo[]) {
  return ejecutar(`${base}/embarques/${id}/empaque`, "PUT", { filas }, "No se pudo guardar el empaque.");
}

export async function guardarDoaAction(id: string, filas: Cuerpo[]) {
  return ejecutar(`${base}/embarques/${id}/doa`, "PUT", { filas }, "No se pudo guardar el DOA.");
}

export async function cambiarEstadoEmbarqueAction(id: string, estadoActual: string, estado: string) {
  return ejecutar(`${base}/embarques/${id}/estado`, "PUT", { estadoActual, estado }, "No se pudo cambiar el estado.");
}

export async function facturarEmbarqueAction(id: string) {
  return ejecutar<{ numero: number }>(`${base}/embarques/${id}/facturar`, "POST", null, "No se pudo emitir la factura.");
}

export async function enviarDocumentoAction(id: string, tipo: string, para: string, mensaje: string) {
  return ejecutar<{ para: string; llegoAlDestinatario: boolean; proveedor: string }>(
    `${base}/embarques/${id}/documentos/${tipo}/enviar`,
    "POST",
    { para: para || null, mensaje: mensaje || null },
    "No se pudo enviar el documento.",
  );
}

export async function guardarClienteExportacionAction(clienteId: string, datos: Cuerpo) {
  return ejecutar(`${base}/clientes/${clienteId}`, "PUT", datos, "No se pudieron guardar los datos del cliente.");
}

// --- Configuración ---
export async function guardarEmpresaAction(datos: Cuerpo) {
  return ejecutar(`${base}/configuracion/empresa`, "PUT", datos, "No se pudieron guardar los datos de la empresa.");
}

export async function guardarTarifasAction(datos: Cuerpo) {
  return ejecutar(`${base}/configuracion/tarifas`, "PUT", datos, "No se pudieron guardar las tarifas.");
}

export async function guardarCajaAction(datos: Cuerpo) {
  return ejecutar(`${base}/configuracion/cajas`, "POST", datos, "No se pudo guardar el tipo de caja.");
}

export async function guardarBolsaAction(datos: Cuerpo) {
  return ejecutar(`${base}/configuracion/bolsas`, "POST", datos, "No se pudo guardar el tipo de bolsa.");
}

export async function guardarCargoAction(datos: Cuerpo) {
  return ejecutar(`${base}/configuracion/cargos`, "PUT", datos, "No se pudo guardar el cargo.");
}

/** Sube el logo (multipart, sin fijar Content-Type: lo pone fetch con el boundary). */
export async function subirLogoAction(formData: FormData): Promise<EstadoFormulario> {
  const session = await getSession();
  if (!session) redirect("/login");
  const archivo = formData.get("archivo");
  if (!(archivo instanceof File) || archivo.size === 0) return { error: "Selecciona una imagen PNG o JPG." };
  try {
    const datos = new FormData();
    datos.append("archivo", archivo, archivo.name);
    await apiUpload(`${base}/configuracion/logo`, datos, session.token);
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : "No se pudo subir el logo." };
  }
  revalidatePath("/exportaciones", "layout");
  return { ok: true };
}

// --- AUNAP ---
export async function enlazarProductoAunapAction(productoId: string, especieAunapId: string | null) {
  return ejecutar(`${base}/aunap/productos/${productoId}`, "PUT", { especieAunapId }, "No se pudo enlazar el producto.");
}

export async function enlazarAunapAutomaticoAction() {
  return ejecutar<{ enlazados: number }>(`${base}/aunap/productos/enlazar-automatico`, "POST", null, "No se pudo enlazar.");
}

export async function guardarPermisoAunapAction(embarqueId: string, datos: Cuerpo) {
  return ejecutar(`${base}/embarques/${embarqueId}/aunap/permiso`, "PUT", datos, "No se pudieron guardar los trámites.");
}

export async function guardarEstandarEmpaqueAction(productoId: string, datos: Cuerpo) {
  return ejecutar(`${base}/empaque/estandares/${productoId}`, "PUT", datos, "No se pudo guardar el estándar.");
}

export async function eliminarEstandarEmpaqueAction(productoId: string) {
  const session = await getSession();
  if (!session) redirect("/login");
  try {
    await apiFetch(`${base}/empaque/estandares/${productoId}`, { method: "DELETE", token: session.token });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) redirect("/login");
    return { error: err instanceof ApiError ? err.message : "No se pudo quitar el estándar." };
  }
  revalidatePath("/exportaciones", "layout");
  return { ok: true };
}

export async function guardarParametrosEmpaqueAction(datos: Cuerpo) {
  return ejecutar(`${base}/empaque/costos/parametros`, "PUT", datos, "No se pudieron guardar los costos.");
}

export async function guardarLitrosBolsaAction(tipoBolsaId: string, litrosAgua: number | null) {
  return ejecutar(`${base}/empaque/costos/bolsas/${tipoBolsaId}`, "PUT", { litrosAgua }, "No se pudieron guardar los litros.");
}

export async function guardarCostoAditivoAction(medicamentoId: string, costoCopUnidad: number | null) {
  return ejecutar(`${base}/empaque/costos/aditivos/${medicamentoId}`, "PUT", { costoCopUnidad }, "No se pudo guardar el costo.");
}

export async function generarProformaAction(embarqueId: string) {
  return ejecutar<{ numero: string }>(`${base}/embarques/${embarqueId}/aunap/proforma`, "POST", null, "No se pudo generar la proforma.");
}

export async function guardarCentroAunapAction(centroId: string, municipioAunapId: number | null, excluir: boolean) {
  return ejecutar(`${base}/aunap/centros/${centroId}`, "PUT", { municipioAunapId, excluir }, "No se pudo guardar el origen.");
}

export async function guardarParametrosAunapAction(datos: Cuerpo) {
  return ejecutar(`${base}/aunap/parametros`, "PUT", datos, "No se pudo guardar el encabezado.");
}
