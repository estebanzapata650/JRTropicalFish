import { textoTalla, type Producto } from "@/lib/catalogos";
/**
 * Tipos, etiquetas y reglas de presentación de inventario, compras y pedidos de cliente. Los valores
 * son los de `apps/api/src/JRTropicalFish.Domain/Inventario/EnumsInventario.cs`; las transiciones
 * repiten `ReglasPedido` (la API vuelve a validarlas). Mantener sincronizado.
 */
import type { TonoEtiqueta } from "@/components/campos";

export const CONDICIONES: Record<string, string> = { sano: "Sano", enfermo: "Enfermo" };

export const TIPOS_MOVIMIENTO: Record<string, string> = {
  entrada: "Entrada (recepción)",
  saldo_inicial: "Saldo inicial",
  ubicacion: "Ubicación",
  traslado: "Traslado",
  enfermo: "Enfermo",
  aliviado: "Aliviado",
  mortalidad: "Mortalidad",
  despacho: "Despacho",
  ajuste: "Ajuste",
};

export const ESTADOS_OFERTA: Record<string, string> = { abierta: "Abierta", cerrada: "Cerrada" };

export const ESTADOS_PEDIDO_PROVEEDOR: Record<string, string> = {
  borrador: "Borrador",
  enviado: "Enviado",
  en_transito: "En tránsito",
  recibido_parcial: "Recibido en parte",
  recibido: "Recibido",
  cancelado: "Cancelado",
};

export const ESTADOS_PEDIDO_CLIENTE: Record<string, string> = {
  borrador: "Borrador",
  confirmado: "Confirmado",
  en_preparacion: "En preparación",
  despachado: "Despachado",
  cancelado: "Cancelado",
};

/** Acciones manuales de estado (espejo de ReglasPedido) con el texto del botón. */
export const ACCIONES_PEDIDO_PROVEEDOR: Record<string, { estado: string; texto: string }[]> = {
  borrador: [{ estado: "enviado", texto: "Marcar enviado" }, { estado: "cancelado", texto: "Cancelar" }],
  enviado: [{ estado: "en_transito", texto: "En tránsito" }, { estado: "cancelado", texto: "Cancelar" }],
  en_transito: [{ estado: "cancelado", texto: "Cancelar" }],
  recibido_parcial: [{ estado: "recibido", texto: "Cerrar (no llegará el resto)" }],
};

export const ACCIONES_PEDIDO_CLIENTE: Record<string, { estado: string; texto: string }[]> = {
  borrador: [{ estado: "confirmado", texto: "Confirmar" }, { estado: "cancelado", texto: "Cancelar" }],
  confirmado: [
    { estado: "en_preparacion", texto: "Iniciar preparación" },
    { estado: "borrador", texto: "Volver a borrador" },
    { estado: "cancelado", texto: "Cancelar" },
  ],
  en_preparacion: [{ estado: "despachado", texto: "Cerrar con faltantes" }],
};

/** Estados en los que la mercancía viene en camino (se puede recibir). */
export const PEDIDO_PROVEEDOR_RECIBIBLE = ["enviado", "en_transito", "recibido_parcial"];

export const tonoPedidoProveedor = (e: string): TonoEtiqueta =>
  e === "recibido" ? "exito" : e === "cancelado" ? "neutro" : e === "borrador" ? "neutro" : e === "recibido_parcial" ? "advertencia" : "info";

export const tonoPedidoCliente = (e: string): TonoEtiqueta =>
  e === "despachado" ? "exito" : e === "cancelado" ? "neutro" : e === "en_preparacion" ? "advertencia" : e === "confirmado" ? "info" : "neutro";

/** Un lote con más de estos días en bodega se marca (indicador de la app anterior). */
export const DIAS_LOTE_ANTIGUO = 20;

export const pct = (v: number) => `${v.toLocaleString("es-CO", { maximumFractionDigits: 1 })} %`;
export const piezas = (v: number) => v.toLocaleString("es-CO");

// --- Tipos de la API ---

export type Acuario = {
  id: string;
  codigo: string;
  sector: string;
  nivel: number;
  posicion: number;
  capacidadLitros: number | null;
  activo: boolean;
  sanos: number;
  enfermos: number;
  lotes: number;
};

export type Existencia = {
  id: string;
  loteId: string;
  loteCodigo: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  acuarioId: string | null;
  acuarioCodigo: string | null;
  condicion: string;
  cantidad: number;
  fechaIngreso: string;
  diasEnBodega: number;
  proveedorNombre: string | null;
  centroNombre: string | null;
};

export type Lote = {
  id: string;
  codigo: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  proveedorId: string | null;
  proveedorNombre: string | null;
  centroAcopioId: string | null;
  centroNombre: string | null;
  recepcionCodigo: string | null;
  fechaIngreso: string;
  diasEnBodega: number;
  costoUnitario: number | null;
  recibidos: number;
  muertosLlegada: number;
  origen: string;
  sanos: number;
  enfermos: number;
  porUbicar: number;
  despachados: number;
  muertos: number;
  creadoEn: string;
  disponible: number;
  porcentajeMuertos: number;
};

export type Movimiento = {
  id: string;
  tipo: string;
  loteId: string;
  loteCodigo: string;
  productoCodigo: string;
  nombreCientifico: string;
  acuarioOrigenCodigo: string | null;
  condicionOrigen: string | null;
  acuarioDestinoCodigo: string | null;
  condicionDestino: string | null;
  cantidad: number;
  pedidoCodigo: string | null;
  recepcionCodigo: string | null;
  nota: string | null;
  usuarioNombre: string | null;
  fecha: string;
};

export type Oferta = {
  id: string;
  codigo: string;
  proveedorId: string;
  proveedorNombre: string;
  fecha: string;
  fechaEstimadaLlegada: string | null;
  estado: string;
  observaciones: string | null;
  lineas: number;
  unidades: number;
  creadoEn: string;
};

export type OfertaLinea = {
  id: string;
  ofertaId: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  cantidad: number;
};

export type PedidoProveedor = {
  id: string;
  codigo: string;
  proveedorId: string;
  proveedorNombre: string;
  ofertaId: string | null;
  ofertaCodigo: string | null;
  fecha: string;
  fechaEstimadaLlegada: string | null;
  estado: string;
  observaciones: string | null;
  unidades: number;
  recibidas: number;
  costoTotal: number | null;
  creadoEn: string;
  cumplimiento: number;
};

export type PedidoProveedorLinea = {
  id: string;
  pedidoId: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  cantidad: number;
  costoUnitario: number | null;
  observacion: string | null;
  recibida: number;
  pendiente: number;
};

export type Recepcion = {
  id: string;
  codigo: string;
  pedidoProveedorId: string;
  pedidoCodigo: string;
  proveedorNombre: string;
  fecha: string;
  observaciones: string | null;
  usuarioNombre: string | null;
  unidades: number;
  enfermas: number;
  muertas: number;
  costoTotal: number;
  creadoEn: string;
};

export type RecepcionLinea = {
  id: string;
  recepcionId: string;
  pedidoLineaId: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  recibida: number;
  enferma: number;
  muerta: number;
  costoUnitario: number;
  costoTotal: number;
  loteId: string;
  loteCodigo: string;
};

export type PedidoProveedorDetalle = { pedido: PedidoProveedor; lineas: PedidoProveedorLinea[]; recepciones: Recepcion[] };

export type PedidoCliente = {
  id: string;
  codigo: string;
  clienteId: string;
  clienteNombre: string;
  comercialId: string;
  comercialNombre: string;
  ventaId: string | null;
  ventaCodigo: string | null;
  moneda: string | null;
  fecha: string;
  ciudad: string | null;
  estado: string;
  observaciones: string | null;
  unidades: number;
  despachadas: number;
  total: number | null;
  creadoEn: string;
  cumplimiento: number;
};

export type PedidoClienteLinea = {
  id: string;
  pedidoId: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  cantidad: number;
  precioUnitario: number | null;
  observacion: string | null;
  /** Del producto en el catálogo ("5 – 6 cm"); cada talla es un producto. */
  talla: string | null;
  despachada: number;
  pendiente: number;
};

export type PedidoClienteDetalle = { pedido: PedidoCliente; lineas: PedidoClienteLinea[] };

export type InventarioProducto = {
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreEspanol: string | null;
  centroAcopioId: string | null;
  centroNombre: string | null;
  sanos: number;
  porUbicar: number;
  enfermos: number;
  pendienteClientes: number;
  enTransito: number;
  ultimaOfertaProveedor: string | null;
  ultimaOfertaCantidad: number | null;
  ultimaOfertaFecha: string | null;
  faltante: number;
  cubierto: boolean;
};

// --- Tableros (Bodega, Proveedores, Clientes, Trazabilidad) ---

export type UbicacionAcuario = {
  acuarioId: string;
  acuarioCodigo: string;
  sector: string;
  sanos: number;
  enfermos: number;
  total: number;
  especies: { productoCodigo: string; nombreCientifico: string; cantidad: number }[];
};

export type TableroBodega = {
  sanos: number;
  aclimatacion: number;
  enfermos: number;
  acuariosActivos: number;
  acuariosOcupados: number;
  acuariosConEnfermos: number;
  acuariosVacios: number;
  ocupacion: number;
  lotesActivos: number;
  lotesAntiguos: number;
  productos: InventarioProducto[];
  ubicaciones: UbicacionAcuario[];
  existenciasEnfermas: Existencia[];
};

export type CompraPendiente = {
  pedidoId: string;
  pedidoCodigo: string;
  proveedorId: string;
  proveedorNombre: string;
  fecha: string;
  fechaEstimadaLlegada: string | null;
  estado: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  centroNombre: string | null;
  cantidad: number;
  recibida: number;
  pendiente: number;
};

export type CumplimientoCompra = {
  pedidoId: string;
  pedidoCodigo: string;
  proveedorId: string;
  proveedorNombre: string;
  fecha: string;
  estado: string;
  pedidas: number;
  recibidas: number;
  recibidasEnFull: number;
  cumplimiento: number;
};

export type ProveedorResumen = {
  proveedorId: string | null;
  proveedorNombre: string;
  pedidos: number;
  pedidas: number;
  recibidasEnFull: number;
  lotes: number;
  recibidos: number;
  muertosLlegada: number;
  muertosDespues: number;
  cumplimiento: number;
  porcentajeMuertos: number;
};

export type SemanaRecepcion = { semana: string; recibidos: number; muertos: number; porcentajeMuertos: number };

export type TableroProveedores = {
  desde: string;
  hasta: string;
  pedidosEnTransito: number;
  unidadesEnTransito: number;
  centrosEnTransito: number;
  pedidas: number;
  recibidasEnFull: number;
  recibidos: number;
  muertos: number;
  cumplimiento: number;
  porcentajeMuertos: number;
  enTransito: CompraPendiente[];
  proveedores: ProveedorResumen[];
  pedidos: CumplimientoCompra[];
  semanas: SemanaRecepcion[];
};

export type PedidoClientePendiente = {
  pedidoId: string;
  pedidoCodigo: string;
  clienteId: string;
  clienteNombre: string;
  comercialId: string;
  fecha: string;
  estado: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  cantidad: number;
  despachada: number;
  stockSano: number;
  pendiente: number;
};

export type ClienteResumen = {
  clienteId: string;
  clienteNombre: string;
  pedidos: number;
  solicitadas: number;
  despachadasPedidos: number;
  despachadoPeriodo: number;
  cumplimiento: number;
};

export type TableroClientes = {
  desde: string;
  hasta: string;
  pedidosPendientes: number;
  unidadesPendientes: number;
  unidadesDespachadas: number;
  solicitadas: number;
  despachadasPedidos: number;
  cumplimiento: number;
  pendientes: PedidoClientePendiente[];
  clientes: ClienteResumen[];
  especies: { productoId: string; productoCodigo: string; nombreCientifico: string; cantidad: number; clientes: number }[];
};

export type LoteTiempos = {
  loteId: string;
  diasAclimatacion: number;
  aclimatacionEnCurso: boolean;
  diasCuarentena: number;
  cuarentenaEnCurso: boolean;
  episodiosCuarentena: number;
  despachos: number;
  clientes: number;
  primerDespacho: string | null;
  ultimoDespacho: string | null;
};

export type LoteTrazable = { lote: Lote; tiempos: LoteTiempos };

export type Despacho = {
  fecha: string;
  loteId: string;
  loteCodigo: string;
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  clienteId: string;
  clienteNombre: string;
  comercialId: string;
  pedidoId: string;
  pedidoCodigo: string;
  acuarioCodigo: string | null;
  cantidad: number;
};

export type RecorridoLote = { lote: Lote; tiempos: LoteTiempos; existencias: Existencia[]; despachos: Despacho[]; movimientos: Movimiento[] };

/** Duración legible: horas si es menos de un día ("5 h"), un decimal bajo 10 días ("2,5 días"), si no entero. */
export const dias = (v: number) => {
  if (v < 1) return `${Math.max(1, Math.round(v * 24))} h`;
  const n = v < 10 ? Math.round(v * 10) / 10 : Math.round(v);
  return `${n.toLocaleString("es-CO")} ${n === 1 ? "día" : "días"}`;
};

/** Producto para los selectores de líneas: "AP005 · Crenicara Punctulatum". */
export type ProductoOpcion = {
  id: string;
  codigo: string;
  /** "Oscares Tigre Mediano · 5 – 6 cm": el nombre científico se repite entre tallas y no las distingue. */
  nombre: string;
  /** Nombre científico y nombre en inglés, para buscar también por ellos. */
  detalle?: string | null;
  /** Precio de lista por moneda (USD = STANDARD, COP = MAYORISTA_NACIONAL), si se cargaron las listas. */
  precios?: Record<string, number>;
};

/** Productos activos del catálogo como opciones del selector de especies. `listas`: id de la lista de precios de cada moneda. */
export function opcionesProducto(
  productos: Producto[],
  listas: Record<string, string> = {},
): ProductoOpcion[] {
  return productos
    .filter((p) => p.activo)
    .map((p) => {
      const nombre = p.nombreEspanol ?? p.nombreComun ?? p.nombreCientifico;
      const talla = textoTalla(p);
      return {
        id: p.id,
        codigo: p.codigo,
        nombre: talla === "—" ? nombre : `${nombre} · ${talla}`,
        detalle:
          [p.nombreCientifico, p.nombreComun]
            .filter((x) => x && x !== nombre)
            .join(" · ") || null,
        precios: Object.fromEntries(
          Object.entries(listas)
            .filter(([, id]) => p.precios[id] != null)
            .map(([moneda, id]) => [moneda, p.precios[id]]),
        ),
      };
    });
}
