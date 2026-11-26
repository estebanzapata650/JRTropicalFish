/**
 * Tipos y etiquetas de exportaciones (documentos comerciales del despacho). Los valores son los de
 * `apps/api/src/JRTropicalFish.Domain/Exportaciones/EnumsExportaciones.cs`; los cálculos los hace la API
 * (`ReglasEmbarque`) y llegan en `calculos`: la pantalla nunca los recalcula.
 */
import type { TonoEtiqueta } from "@/components/campos";

export const ESTADOS_EMBARQUE: Record<string, string> = {
  borrador: "Borrador",
  preinvoice_enviada: "Preinvoice enviada",
  confirmado: "Confirmado",
  empacado: "Empacado",
  facturado: "Facturado",
  cerrado: "Cerrado",
  cancelado: "Cancelado",
};

export const tonoEmbarque = (e: string): TonoEtiqueta =>
  e === "cerrado" ? "exito" : e === "facturado" ? "info" : e === "cancelado" ? "neutro" : e === "borrador" ? "neutro" : "advertencia";

export const EDITABLES = ["borrador", "preinvoice_enviada", "confirmado", "empacado"];

/** Siguiente paso manual (los otros los pone el sistema: facturado al emitir). */
export const ACCIONES_EMBARQUE: Record<string, { estado: string; texto: string }[]> = {
  borrador: [{ estado: "preinvoice_enviada", texto: "Marcar preinvoice enviada" }],
  preinvoice_enviada: [
    { estado: "confirmado", texto: "Cliente confirmó" },
    { estado: "borrador", texto: "Volver a borrador" },
  ],
  confirmado: [
    { estado: "empacado", texto: "Empaque terminado" },
    { estado: "preinvoice_enviada", texto: "Volver a preinvoice" },
  ],
  empacado: [{ estado: "confirmado", texto: "Reabrir empaque" }],
  facturado: [{ estado: "cerrado", texto: "Cerrar embarque" }],
};

export const FORMATOS_FACTURA: Record<string, string> = {
  jrch: "JRCH (por caja, con empaque)",
  v2: "V2 (por caja, con fracción)",
};

export const DOCUMENTOS: { tipo: string; nombre: string; paraCliente: boolean }[] = [
  { tipo: "preinvoice", nombre: "Preinvoice", paraCliente: true },
  { tipo: "flete", nombre: "Flete estimado", paraCliente: true },
  { tipo: "orden_cargue", nombre: "Orden de cargue", paraCliente: false },
  { tipo: "empaque", nombre: "Lista de empaque", paraCliente: true },
  { tipo: "factura", nombre: "Factura", paraCliente: true },
  { tipo: "nota_credito", nombre: "Nota crédito", paraCliente: true },
  { tipo: "balance", nombre: "Balance", paraCliente: true },
  { tipo: "solicitud_aunap", nombre: "Proforma", paraCliente: false },
  { tipo: "packing_aduana", nombre: "Packing list de aduana", paraCliente: false },
  { tipo: "factura_aduana", nombre: "Custom invoice", paraCliente: false },
];

export const nombreDocumento = (tipo: string) => DOCUMENTOS.find((d) => d.tipo === tipo)?.nombre ?? tipo;

export const usd = (v: number | null | undefined) =>
  v == null ? "—" : `USD ${v.toLocaleString("es-CO", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;

export const cifra = (v: number, decimales = 2) => v.toLocaleString("es-CO", { maximumFractionDigits: decimales });

// --- Configuración ---

export type DatosEmpresa = {
  razonSocial: string;
  nit: string | null;
  direccion: string | null;
  ciudadPais: string | null;
  telefono: string | null;
  email: string | null;
  sitioWeb: string | null;
  banco: string | null;
  swift: string | null;
  cuentaBancaria: string | null;
  beneficiario: string | null;
  direccionBanco: string | null;
  firmanteNombre: string | null;
  firmanteCargo: string | null;
  firmanteTelefono: string | null;
  logoClave: string | null;
  actualizadoEn: string | null;
};

export type Tarifas = {
  tarifaKgJr: number;
  tarifaKgCliente: number;
  fuelKg: number;
  jpFeeKg: number;
  vscKg: number;
  vscMinimo: number;
  dbcPorcentaje: number;
  dbcMinimo: number;
  dfPorcentaje: number;
  dfMinimo: number;
  dueAgentJr: number;
  dueAgentCliente: number;
  dueCarrier: number;
};

export type TipoCaja = {
  id: string;
  nombre: string;
  descripcionFactura: string;
  costoCop: number;
  precioUsd: number;
  kgPorCaja: number;
  pisos: number;
  orden: number;
  activo: boolean;
};

export type TipoBolsa = { id: string; nombre: string; tamano: string | null; porcion: number; costoCop: number; orden: number; activo: boolean };

export type Cargo = { codigo: string; nombre: string; precioUsd: number; orden: number };

export type Configuracion = { empresa: DatosEmpresa; tarifas: Tarifas; cajas: TipoCaja[]; bolsas: TipoBolsa[]; cargos: Cargo[] };

export type ClienteExportacion = {
  clienteId: string;
  direccion: string | null;
  eori: string | null;
  formatoFactura: string;
  listaPreciosId: string | null;
  descuentoPeces: number;
};

// --- Embarque ---

export type CajaCantidad = { tipoCajaId: string; cantidad: number };
export type CargoEmbarque = { codigo: string; nombre: string; cantidad: number; precio: number };

export type Embarque = {
  id: string;
  codigo: string;
  ventaId: string;
  ventaCodigo: string;
  pedidoClienteId: string | null;
  pedidoCodigo: string | null;
  clienteId: string;
  clienteNombre: string;
  comercialId: string;
  comercialNombre: string;
  estado: string;
  listaPreciosId: string | null;
  listaNombre: string | null;
  descuentoPeces: number;
  aerolinea: string | null;
  ruta: string | null;
  awb: string | null;
  destino: string | null;
  salidaVuelo: string | null;
  llegadaVuelo: string | null;
  fechaDespacho: string | null;
  preinvoiceNumero: string | null;
  preinvoiceFecha: string | null;
  formatoFactura: string;
  facturaNumero: number | null;
  facturaFecha: string | null;
  notaCreditoFecha: string | null;
  awbValor: number | null;
  anticipoRecibido: number;
  notas: string | null;
  lineas: number;
  unidades: number;
  cajasEmpacadas: number;
  creadoEn: string;
};

export type LineaEmbarque = {
  id: string;
  orden: number;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  nombreEspanol: string | null;
  talla: string | null;
  cantidad: number;
  precio: number;
  fraccionCaja: number;
  empaque: string | null;
  nota: string | null;
  total: number;
};

export type FilaEmpaque = {
  id: string;
  numeroCaja: number;
  orden: number;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  nombreFactura: string | null;
  tipoBolsaId: string | null;
  tipoBolsaNombre: string | null;
  bolsas: number;
  pecesPorBolsa: number;
  precio: number;
  doa: number;
  doaAdicional: number;
  doaFinal: number;
  nota: string | null;
  cantidad: number;
  total: number;
  muertos: number;
  empaqueTexto: string;
};

export type LineaCargo = { descripcion: string; cantidad: number; precio: number; total: number };

export type Calculos = {
  preinvoice: {
    unidades: number;
    subtotalPeces: number;
    descuento: number;
    cargos: LineaCargo[];
    awbEstimado: number;
    total: number;
    cajasSugeridas: number;
    anticipoPeces: number;
    anticipoAwb: number;
    anticipo: number;
  };
  flete: {
    pesos: { descripcion: string; cajas: number; kgPorCaja: number; kg: number }[];
    kg: number;
    cargoTarifa: number;
    cargoFuel: number;
    cargoJp: number;
    vsc: number;
    dbc: number;
    df: number;
    dueAgent: number;
    dueCarrier: number;
    total: number;
    cargoPeso: number;
  };
  ordenCargue: { orden: number; productoCodigo: string; especie: string; cantidad: number; empaque: string | null; nota: string | null; cajas: string }[];
  factura: { unidades: number; cajas: number; subtotalPeces: number; descuento: number; cargos: LineaCargo[]; awb: number; awbEstimado: boolean; total: number; costoBolsasCop: number };
  notaCredito: {
    lineas: { productoCodigo: string; nombreCientifico: string; nombreComun: string | null; muertos: number; precio: number; total: number }[];
    muertos: number;
    subtotal: number;
    descuento: number;
    total: number;
  };
  balance: { factura: number; notaCredito: number; anticipo: number; saldo: number };
};

export type Envio = {
  id: string;
  tipoDocumento: string;
  para: string;
  asunto: string;
  proveedor: string;
  exitoso: boolean;
  llegoAlDestinatario: boolean;
  error: string | null;
  enviadoPor: string | null;
  creadoEn: string;
};

export type EmbarqueDetalle = {
  embarque: Embarque;
  tarifas: Tarifas;
  cargos: CargoEmbarque[];
  cajasEstimadas: CajaCantidad[];
  cajasReales: CajaCantidad[];
  lineas: LineaEmbarque[];
  empaque: FilaEmpaque[];
  documentos: { id: string; tipo: string; numero: string; generadoPor: string | null; creadoEn: string }[];
  envios: Envio[];
  calculos: Calculos;
  clienteEmail: string | null;
};

// --- AUNAP ---

export type EspecieAunap = {
  id: string;
  codigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  amazonia: boolean;
  orinoquia: boolean;
  magdalena: boolean;
  pacifico: boolean;
  caribe: boolean;
  orden: string | null;
  familia: string | null;
  activo: boolean;
  productos: number;
};

export type ProductoAunap = {
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  grupoNombre: string | null;
  activo: boolean;
  especieAunapId: string | null;
  aunapCodigo: string | null;
  aunapNombreCientifico: string | null;
  aunapNombreComun: string | null;
  aunapFamilia: string | null;
};

export type FilaAunap = {
  especieAunapId: string;
  codigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  familia: string | null;
  productos: string[];
  solicitado: number;
  autorizado: number | null;
  empacado: number;
  estado: "sin_permiso" | "no_autorizada" | "excede" | "ok";
};

/** Trámites del embarque: proforma, VUCE (numero = TM, fecha = aprobación), DEX, factura DIAN y CITES. */
export type Tramites = {
  numero: string | null;
  fecha: string | null;
  notas: string | null;
  actualizadoEn: string;
  vuceFechaSolicitud: string | null;
  proformaNumero: string | null;
  proformaFecha: string | null;
  dexNumero: string | null;
  facturaDian: string | null;
  citesNumero: string | null;
  citesFecha: string | null;
};

export type ResumenAunap = {
  permiso: Tramites | null;
  filas: FilaAunap[];
  sinCodigo: { productoId: string; productoCodigo: string; nombreCientifico: string; cantidad: number }[];
  hayEmpaque: boolean;
  avisos: string[];
};

export const ESTADOS_AUNAP: Record<FilaAunap["estado"], string> = {
  sin_permiso: "Sin VUCE",
  no_autorizada: "No autorizada",
  excede: "Excede",
  ok: "Dentro",
};

/** Cuencas en las que la resolución autoriza la especie. */
export const cuencasAunap = (e: Pick<EspecieAunap, "amazonia" | "orinoquia" | "magdalena" | "pacifico" | "caribe">) =>
  [e.amazonia && "Amazonía", e.orinoquia && "Orinoquía", e.magdalena && "Magdalena", e.pacifico && "Pacífico", e.caribe && "Caribe"].filter(Boolean).join(", ");

// --- Informe mensual AUNAP ---

export type MunicipioAunap = { id: number; departamento: string; municipio: string; cuenca: string };

export type CentroAunap = {
  centroAcopioId: string;
  centroNombre: string;
  cuencaInterna: string;
  municipioAunapId: number | null;
  departamento: string | null;
  municipio: string | null;
  cuenca: string | null;
  /** No se reporta (criadero). */
  excluir: boolean;
};

export type ParametrosAunap = {
  representanteLegal: string | null;
  resolucion: string | null;
  vigenciaDesde: string | null;
  vigenciaHasta: string | null;
  direccion: string | null;
  correo: string | null;
  telefono: string | null;
  actualizadoEn: string | null;
};

export type FilaInformeAunap = {
  embarqueId: string;
  embarqueCodigo: string;
  codigo: string;
  familia: string | null;
  nombreCientifico: string;
  nombreComun: string | null;
  departamento: string | null;
  municipio: string | null;
  cuenca: string | null;
  unidades: number;
  ciudad: string | null;
  pais: string | null;
  fechaExportacion: string;
  dexNumero: string | null;
  vuceNumero: string | null;
  vuceFecha: string | null;
  facturaDian: string | null;
  citesNumero: string | null;
  citesFecha: string | null;
};

export type InformeAunap = {
  anio: number;
  mes: number;
  razonSocial: string;
  parametros: ParametrosAunap;
  filas: FilaInformeAunap[];
  avisos: { embarqueId: string; embarqueCodigo: string; texto: string }[];
  excluidosCriadero: number;
  embarques: number;
};
