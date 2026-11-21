/**
 * Tipos, etiquetas y reglas de presentación del CRM comercial. Los valores ("prospeccion",
 * "exportacion"…) son los de `apps/api/src/JRTropicalFish.Domain/Crm/EnumsCrm.cs`; aquí vive solo
 * cómo se muestran. Mantener sincronizado.
 */
import type { TonoEtiqueta } from "@/components/campos";

export type Opcion = { valor: string; etiqueta: string };

const opciones = (mapa: Record<string, string>): Opcion[] =>
  Object.entries(mapa).map(([valor, etiqueta]) => ({ valor, etiqueta }));

export const TIPOS_CLIENTE: Record<string, string> = {
  b2b_internacional: "B2B Internacional",
  local_colombia: "Local Colombia",
  ecommerce: "E-commerce",
};

export const LICENCIAS: Record<string, string> = {
  activa: "Activa",
  en_tramite: "En trámite",
  vencida: "Vencida",
  no_aplica: "No aplica",
};

export const ESTADOS_CLIENTE: Record<string, string> = { activo: "Activo", inactivo: "Inactivo", prospecto: "Prospecto" };

export const TIPOS_VENTA: Record<string, string> = { exportacion: "Exportación", nacional: "Nacional (local)" };

export const MONEDAS = ["USD", "COP"] as const;

/** Etapas en orden. Las 4 primeras son activas; Ganada y Perdida, terminales. */
export const ETAPAS: Record<string, string> = {
  prospeccion: "1. Prospección",
  stocklist: "2. Stocklist enviado",
  pre_invoice: "3. Pre-invoice",
  aprobacion_vuelo: "4. Aprobación vuelo",
  ganada: "5. Ganada",
  perdida: "Perdida",
};
export const ORDEN_ETAPAS = Object.keys(ETAPAS);
export const ETAPAS_ACTIVAS = ORDEN_ETAPAS.slice(0, 4);
export const esTerminal = (etapa: string) => etapa === "ganada" || etapa === "perdida";

/**
 * Espejo de `ReglasPipeline.Siguientes`: solo hacia adelante (se puede saltar) o a Perdida. Desde
 * Ganada o Perdida no hay movimiento. La API vuelve a validarlo.
 */
export function etapasSiguientes(actual: string): string[] {
  if (esTerminal(actual)) return [];
  const posicion = ORDEN_ETAPAS.indexOf(actual);
  return ORDEN_ETAPAS.filter((e) => e === "perdida" || ORDEN_ETAPAS.indexOf(e) > posicion);
}

/** Semáforo del pipeline: azul al inicio, naranja cerca del cierre, verde ganada, rojo perdida. */
export function tonoEtapa(etapa: string): TonoEtiqueta {
  if (etapa === "ganada") return "exito";
  if (etapa === "perdida") return "peligro";
  if (etapa === "pre_invoice" || etapa === "aprobacion_vuelo") return "advertencia";
  return "info";
}

/** Borde superior de la columna del tablero, con el mismo semáforo (clases completas para Tailwind). */
export const BORDE_ETAPA: Record<string, string> = {
  prospeccion: "border-t-info-texto",
  stocklist: "border-t-info-texto",
  pre_invoice: "border-t-advertencia-texto",
  aprobacion_vuelo: "border-t-advertencia-texto",
  ganada: "border-t-exito-texto",
  perdida: "border-t-peligro",
};

export const ORIGENES_CANAL: Record<string, string> = {
  b2b_directo: "B2B directo",
  web_ecommerce: "Web e-commerce",
  whatsapp_directo: "WhatsApp directo",
  mostrador_bodega: "Mostrador bodega",
};

export const ESTADOS_PAGO: Record<string, string> = { pendiente: "Pendiente", anticipo: "Anticipo", pagado: "Pagado" };

export const tonoPago = (estado: string): TonoEtiqueta =>
  estado === "pagado" ? "exito" : estado === "anticipo" ? "info" : "advertencia";

export const ESTADOS_PREPARACION: Record<string, string> = {
  pendiente_picking: "1. Pendiente picking",
  en_empaque: "2. En empaque",
  listo_despacho: "3. Listo para despacho",
  entregado: "4. Entregado",
};

export const FASES_TRACKING: Record<string, string> = {
  cuarentena: "1. Cuarentena",
  empaque_o2: "2. Empaque / O₂",
  inspeccion_ica: "3. Inspección ICA",
  vuelo_transito: "4. Vuelo en tránsito",
  arribo: "5. Arribo",
  control_doa: "6. Control DOA",
};
export const ORDEN_FASES = Object.keys(FASES_TRACKING);

export const TIPOS_ACCION: Record<string, string> = {
  prospeccion: "Prospección",
  stocklist_fotos: "Stocklist / fotos",
  pre_invoice: "Pre-invoice",
  cobro_cartera: "Cobro cartera",
  post_venta_doa: "Posventa / DOA",
};

export const ESTADOS_TAREA: Record<string, string> = {
  pendiente: "Por hacer",
  en_curso: "En curso",
  esperando_cliente: "Esperando cliente",
  completada: "Completada",
};
export const ORDEN_ESTADOS_TAREA = Object.keys(ESTADOS_TAREA);

export const PRIORIDADES: Record<string, string> = { alta: "Alta", media: "Media", baja: "Baja" };

export const tonoPrioridad = (p: string): TonoEtiqueta => (p === "alta" ? "peligro" : p === "media" ? "advertencia" : "neutro");

export const listaOpciones = opciones;

/**
 * `https://wa.me/<indicativo + número>`. WhatsApp exige el número sin `+` ni ceros iniciales: el
 * prefijo internacional `00` (como viene en el Excel: `0031-105912173-0`) se quita.
 */
export function enlaceWhatsapp(telefono: string | null | undefined): string | null {
  const digitos = (telefono?.replace(/\D/g, "") ?? "").replace(/^0+/, "");
  return digitos.length >= 7 ? `https://wa.me/${digitos}` : null;
}

export function enlaceLlamada(telefono: string | null | undefined): string | null {
  const limpio = telefono?.replace(/[^\d+]/g, "") ?? "";
  return limpio.replace(/\D/g, "").length >= 7 ? `tel:${limpio}` : null;
}

/** Misma fórmula que la columna generada `porcentaje_doa`. */
export function porcentajeDoa(fob: number, doa: number): number | null {
  return fob > 0 ? Math.round((doa / fob) * 100 * 100) / 100 : null;
}

// --- Tipos de la API ---

export type Comercial = { id: string; nombre: string; email: string };

export type Cliente = {
  id: string;
  codigo: string;
  nombreEmpresa: string;
  contactoPrincipal: string | null;
  email: string | null;
  telefonoWhatsapp: string | null;
  pais: string;
  ciudad: string | null;
  aeropuertoIata: string | null;
  tipoCliente: string;
  licenciaImportacion: string;
  estado: string;
  notas: string | null;
  creadoEn: string;
};

export type Oportunidad = {
  id: string;
  codigo: string;
  clienteId: string;
  clienteCodigo: string;
  clienteNombre: string;
  tipoVenta: string;
  comercialId: string;
  comercialNombre: string;
  fechaContactoInicial: string;
  etapa: string;
  moneda: string;
  montoEstimado: number;
  cajasEstimadas: number;
  fechaPagoConfirmado: string | null;
  motivoPerdida: string | null;
  notasSeguimiento: string | null;
  etapaCambiadaEn: string;
  ventaId: string | null;
  creadoEn: string;
};

export type Venta = {
  id: string;
  codigo: string;
  oportunidadId: string | null;
  oportunidadCodigo: string | null;
  clienteId: string;
  clienteCodigo: string;
  clienteNombre: string;
  comercialId: string;
  comercialNombre: string;
  origenCanal: string;
  fechaPedido: string;
  tipoVenta: string;
  moneda: string;
  total: number;
  trm: number | null;
  totalCop: number;
  totalCajas: number;
  transportadoraAerolinea: string | null;
  noGuiaAwb: string | null;
  estadoPago: string;
  estadoPreparacion: string;
  ciudadEntrega: string | null;
  direccionEntrega: string | null;
  notas: string | null;
  trackingId: string | null;
  creadoEn: string;
};

export type Tracking = {
  id: string;
  codigo: string;
  ventaId: string;
  ventaCodigo: string;
  oportunidadId: string | null;
  oportunidadCodigo: string | null;
  clienteId: string;
  clienteCodigo: string;
  clienteNombre: string;
  comercialId: string;
  comercialNombre: string;
  nFacturaInvoice: string | null;
  noAwb: string | null;
  aerolinea: string | null;
  fechaDespachoVuelo: string | null;
  cajasReales: number;
  fase: string;
  montoFobUsd: number;
  fleteAereoUsd: number;
  doaReportadoUsd: number;
  porcentajeDoa: number | null;
  notaCreditoNo: string | null;
  creadoEn: string;
};

export type Tarea = {
  id: string;
  codigo: string;
  clienteId: string;
  clienteCodigo: string;
  clienteNombre: string;
  oportunidadId: string | null;
  oportunidadCodigo: string | null;
  accion: string;
  tipoAccion: string;
  estado: string;
  prioridad: string;
  comercialId: string;
  comercialNombre: string;
  fechaAsignacion: string;
  fechaVencimiento: string;
  diasRestantes: number;
  notas: string | null;
  completadaEn: string | null;
  creadoEn: string;
  vencida: boolean;
};

export type ParametrosComerciales = {
  metaEmpresaCop: number;
  metaComercialCop: number;
  puntoEquilibrioDespachos: number;
  porcentajeComision: number;
  umbralDoa: number;
  metaClientesNuevos: number;
  actualizadoEn: string | null;
};

export type MetaMes = { comercialId: string; comercialNombre: string; metaId: string | null; metaCop: number | null };

export type Dashboard = {
  mes: string;
  parametros: ParametrosComerciales;
  ventasCop: number;
  doaCop: number;
  fleteCop: number;
  ventaNetaCop: number;
  porcentajeMetaEmpresa: number;
  poolComision: number;
  despachos: number;
  clientesNuevos: number;
  comerciales: {
    comercialId: string;
    nombre: string;
    ventas: number;
    ventasCop: number;
    metaCop: number;
    porcentajeMeta: number;
    comision: number;
  }[];
  embudo: { etapa: string; cantidad: number; montoUsd: number; montoCop: number }[];
  alertasDoa: Tracking[];
  tareasVencidas: Tarea[];
};

export type ClienteRelacionados = {
  oportunidades: Oportunidad[];
  ventas: Venta[];
  vuelos: Tracking[];
  tareas: Tarea[];
};

/** Etiquetas de campos para el historial (los nombres vienen de los records *Auditable de la API). */
export const ETIQUETAS_HISTORIAL_CRM: Record<string, string> = {
  Codigo: "Código",
  NombreEmpresa: "Empresa",
  ContactoPrincipal: "Contacto",
  Email: "Correo",
  TelefonoWhatsapp: "Teléfono / WhatsApp",
  Pais: "País",
  Ciudad: "Ciudad",
  AeropuertoIata: "Aeropuerto (IATA)",
  TipoCliente: "Tipo de cliente",
  LicenciaImportacion: "Licencia de importación",
  Estado: "Estado",
  Notas: "Notas",
  Cliente: "Cliente",
  Comercial: "Comercial",
  TipoVenta: "Tipo de venta",
  FechaContactoInicial: "Contacto inicial",
  Etapa: "Etapa",
  Moneda: "Moneda",
  MontoEstimado: "Monto estimado",
  CajasEstimadas: "Cajas estimadas",
  FechaPagoConfirmado: "Pago confirmado",
  MotivoPerdida: "Motivo de pérdida",
  NotasSeguimiento: "Notas de seguimiento",
  OrigenCanal: "Origen / canal",
  FechaPedido: "Fecha del pedido",
  Total: "Total",
  Trm: "TRM",
  TotalCajas: "Total cajas",
  TransportadoraAerolinea: "Transportadora / aerolínea",
  NoGuiaAwb: "Guía AWB",
  EstadoPago: "Estado de pago",
  EstadoPreparacion: "Preparación",
  CiudadEntrega: "Ciudad de entrega",
  DireccionEntrega: "Dirección de entrega",
  NFacturaInvoice: "Factura / invoice",
  NoAwb: "Guía AWB",
  Aerolinea: "Aerolínea",
  FechaDespachoVuelo: "Fecha de despacho",
  CajasReales: "Cajas reales",
  Fase: "Fase",
  MontoFobUsd: "FOB peces (USD)",
  FleteAereoUsd: "Flete aéreo (USD)",
  DoaReportadoUsd: "DOA reportado (USD)",
  NotaCreditoNo: "Nota crédito",
  Oportunidad: "Oportunidad",
  Accion: "Acción",
  TipoAccion: "Tipo de acción",
  Prioridad: "Prioridad",
  FechaAsignacion: "Asignada",
  FechaVencimiento: "Vence",
  Origen: "Origen",
};
