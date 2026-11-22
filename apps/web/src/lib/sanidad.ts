// Tipos y etiquetas del módulo de sanidad (espejo de Application/Sanidad/SanidadModels.cs).

export type Medicamento = {
  id: string;
  codigo: string;
  nombre: string;
  principioActivo: string | null;
  presentacion: string | null;
  unidad: string;
  tipoDosis: string;
  dosisCantidad: number;
  dosisMaxima: number | null;
  dosisBase: number;
  exposicionMinutos: number | null;
  requiereAyuno: boolean;
  diasRetiro: number;
  indicaciones: string | null;
  contraindicaciones: string | null;
  fuente: string | null;
  activo: boolean;
  incompatibles: string[];
};

export type Alimento = { id: string; codigo: string; nombre: string; tipo: string; unidad: string; activo: boolean };

export type Dieta = {
  id: string;
  grupoId: string | null;
  grupoNombre: string | null;
  productoId: string | null;
  productoCodigo: string | null;
  productoNombre: string | null;
  alimentoId: string;
  alimentoNombre: string;
  unidad: string;
  vecesDia: number;
  cantidadPor100: number;
  nota: string | null;
};

export type FichaResumen = {
  productoId: string;
  productoCodigo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  grupoId: string;
  grupoNombre: string | null;
  tieneFicha: boolean;
  phMin: number | null;
  phMax: number | null;
  temperaturaMin: number | null;
  temperaturaMax: number | null;
  ayunoLlegadaHoras: number;
  sensibilidadQuimica: string | null;
};

export type FichaTecnica = {
  productoId: string;
  procedencia: string | null;
  caracteristicas: string | null;
  dimorfismo: string | null;
  phMin: number | null;
  phMax: number | null;
  ghMin: number | null;
  ghMax: number | null;
  temperaturaMin: number | null;
  temperaturaMax: number | null;
  etologia: string | null;
  empaque: string | null;
  sensibilidadQuimica: string | null;
  ayunoLlegadaHoras: number;
};

export type Protocolo = {
  id: string;
  codigo: string;
  nombre: string;
  descripcion: string | null;
  estado: string;
  productoId: string | null;
  productoCodigo: string | null;
  grupoId: string | null;
  grupoNombre: string | null;
  centroAcopioId: string | null;
  centroNombre: string | null;
  cuencaId: string | null;
  cuencaNombre: string | null;
  diasCuarentena: number | null;
  ayunoInicialHoras: number;
  aprobadoPor: string | null;
  aprobadoEn: string | null;
  pasos: number;
};

export type PasoProtocolo = {
  id: string;
  dia: number;
  orden: number;
  accion: string;
  medicamentoId: string | null;
  medicamentoNombre: string | null;
  porcentajeAgua: number | null;
  nota: string | null;
};

export type Tratamiento = {
  id: string;
  codigo: string;
  acuarioId: string;
  acuarioCodigo: string;
  protocoloId: string | null;
  protocoloNombre: string | null;
  motivo: string;
  diagnostico: string | null;
  litros: number | null;
  fechaInicio: string;
  fechaFin: string;
  fechaLiberacion: string;
  estado: string;
  nota: string | null;
  aplicaciones: number;
  registradas: number;
  creadoPor: string | null;
  creadoEn: string;
  tieneReporte: boolean;
};

export type Aplicacion = {
  id: string;
  tratamientoId: string;
  tratamientoCodigo: string;
  acuarioId: string;
  acuarioCodigo: string;
  fecha: string;
  dia: number;
  diasTotal: number;
  orden: number;
  accion: string;
  medicamentoId: string | null;
  medicamentoNombre: string | null;
  porcentajeAgua: number | null;
  cantidadCalculada: number | null;
  cantidadAplicada: number | null;
  unidad: string | null;
  estado: string;
  nota: string | null;
  usuarioNombre: string | null;
  registradoEn: string | null;
};

export type Especie = {
  loteId: string | null;
  loteCodigo: string | null;
  productoId: string;
  productoCodigo: string;
  productoNombre: string;
  condicion: string;
  cantidad: number;
};

export type Tarjeta = {
  clave: string;
  columna: string;
  acuarioId: string | null;
  titulo: string;
  sector: string | null;
  litros: number | null;
  responsable: string | null;
  peces: number;
  especies: Especie[];
  etiquetas: string[];
  tratamiento: {
    id: string;
    codigo: string;
    nombre: string;
    estado: string;
    dia: number;
    diasTotal: number;
    fechaLiberacion: string;
    pendientesHoy: number;
    requiereAyuno: boolean;
    alimentoMedicado: boolean;
  } | null;
  vuelo: {
    pedidoId: string;
    pedidoCodigo: string;
    clienteNombre: string;
    embarqueId: string | null;
    embarqueCodigo: string | null;
    salida: string;
    sinHora: boolean;
    horasRestantes: number;
    reservados: number;
  } | null;
  llegada: string | null;
  fechaEstimada: string | null;
  detalle: string | null;
};

export type TableroSanidad = { horasAyunoVuelo: number; tarjetas: Tarjeta[] };

export type Ronda = {
  acuarioId: string;
  acuarioCodigo: string;
  peces: number;
  alimentar: boolean;
  motivos: string[];
  detalleMotivo: string | null;
  alimentoMedicado: boolean;
  raciones: { alimentoId: string | null; alimento: string; unidad: string; vecesDia: number; cantidadToma: number; productoCodigo: string }[];
  sinDieta: boolean;
  tomasHoy: number;
  ultimaToma: string | null;
};

export type Llegada = {
  loteId: string;
  loteCodigo: string;
  productoCodigo: string;
  productoNombre: string;
  cantidad: number;
  acuarioId: string | null;
  acuarioCodigo: string | null;
  llegada: string;
  porUbicar: boolean;
  protocoloSugeridoId: string | null;
  protocoloSugerido: string | null;
  tieneTratamiento: boolean;
};

export type Salida = {
  pedidoId: string;
  pedidoCodigo: string;
  clienteNombre: string;
  embarqueId: string | null;
  embarqueCodigo: string | null;
  salida: string | null;
  destino: string | null;
  acuarios: { acuarioId: string; acuarioCodigo: string; loteId: string; productoCodigo: string; cantidad: number }[];
};

export type MiDia = {
  hoy: string;
  horasAyunoVuelo: number;
  llegadas: Llegada[];
  medicacion: Aplicacion[];
  alimentacion: Ronda[];
  salidas: Salida[];
  avisos: string[];
};

export type Reserva = {
  id: string;
  lineaId: string;
  productoId: string;
  productoCodigo: string;
  loteId: string;
  loteCodigo: string;
  acuarioId: string;
  acuarioCodigo: string;
  cantidad: number;
  creadoEn: string;
};

export const COLUMNAS_SANIDAD: Record<string, string> = {
  por_llegar: "Por llegar / preparar tanque",
  aclimatacion: "Recién llegado / aclimatación",
  cuarentena: "Cuarentena / medicación",
  estable: "Estable / listo para venta",
  ayuno_vuelo: "Ayuno pre-vuelo",
};

export const ORDEN_COLUMNAS = ["por_llegar", "aclimatacion", "cuarentena", "estable", "ayuno_vuelo"];

export const BORDE_COLUMNA: Record<string, string> = {
  por_llegar: "border-t-neutro-texto",
  aclimatacion: "border-t-info-texto",
  cuarentena: "border-t-advertencia-texto",
  estable: "border-t-exito-texto",
  ayuno_vuelo: "border-t-peligro",
};

export const TIPOS_DOSIS: Record<string, string> = {
  por_volumen: "Por litros de agua",
  por_alimento: "Por gramos de alimento",
  fija: "Fija por acuario",
};

export const UNIDADES_MEDICAMENTO: Record<string, string> = { ml: "mL", g: "g", mg: "mg", gotas: "gotas", tableta: "tableta" };

export const UNIDADES_ALIMENTO: Record<string, string> = { g: "g", ml: "mL", cubo: "cubos" };

export const TIPOS_ALIMENTO: Record<string, string> = { seco: "Seco", vivo: "Vivo", congelado: "Congelado", papilla: "Papilla" };

export const ACCIONES: Record<string, string> = {
  medicar: "Medicar",
  cambio_agua: "Cambio de agua",
  ayuno: "Ayuno",
  observacion: "Observación",
};

export const ESTADOS_PROTOCOLO: Record<string, string> = { borrador: "Borrador", aprobado: "Aprobado", inactivo: "Inactivo" };

export const ESTADOS_TRATAMIENTO: Record<string, string> = { activo: "Activo", terminado: "Terminado", cancelado: "Cancelado" };

export const MOTIVOS_TRATAMIENTO: Record<string, string> = {
  profilaxis_llegada: "Profilaxis de llegada",
  prescripcion: "Prescripción veterinaria",
  otro: "Otro",
};

export const MOTIVOS_AYUNO: Record<string, string> = {
  vuelo: "Vuelo en menos de 48 h",
  tratamiento: "Tratamiento médico",
  llegada: "Llegada reciente / estrés",
};

/** Número con coma decimal y sin ceros sobrantes (0,48). */
export const num = (valor: number | null | undefined, decimales = 2) =>
  valor === null || valor === undefined ? "—" : valor.toLocaleString("es-CO", { maximumFractionDigits: decimales });

export const unidad = (u: string | null) => (u ? (UNIDADES_MEDICAMENTO[u] ?? UNIDADES_ALIMENTO[u] ?? u) : "");

/** "0,6 mL por cada 100 L" */
export function textoDosis(m: Pick<Medicamento, "tipoDosis" | "dosisCantidad" | "dosisMaxima" | "dosisBase" | "unidad">) {
  const cantidad = m.dosisMaxima ? `${num(m.dosisCantidad, 4)}–${num(m.dosisMaxima, 4)}` : num(m.dosisCantidad, 4);
  if (m.tipoDosis === "fija") return `${cantidad} ${unidad(m.unidad)} por acuario`;
  const base = m.tipoDosis === "por_alimento" ? `${num(m.dosisBase)} g de alimento` : m.dosisBase === 1 ? "litro" : `${num(m.dosisBase)} L`;
  return `${cantidad} ${unidad(m.unidad)} por ${m.dosisBase === 1 && m.tipoDosis === "por_volumen" ? "" : "cada "}${base}`;
}

export const fechaCorta = (iso: string | null) =>
  iso ? new Date(iso.length === 10 ? `${iso}T12:00:00` : iso).toLocaleDateString("es-CO", { day: "2-digit", month: "short" }) : "—";

export const horaCorta = (iso: string | null) =>
  iso ? new Date(iso).toLocaleString("es-CO", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit", timeZone: "America/Bogota" }) : "—";
