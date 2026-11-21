/**
 * Tipos y reglas de presentación del catálogo. Los valores ("salvaje", códigos de lista) son los de
 * `apps/api/src/JRTropicalFish.Domain/Catalogos/Catalogos.cs`. La regla de precios repite
 * `Application/Catalogos/ReglasPrecio.cs` para sugerir precios en el formulario: mantenerlas iguales.
 */

export const TIPOS_PRODUCTO: Record<string, string> = { salvaje: "Salvaje", criadero: "Criadero", especial: "Especial" };

/** Disponibilidades que ya usa el negocio en el stocklist (se puede escribir otra). */
export const DISPONIBILIDADES = [
  "OK",
  "1 Bag",
  "1 - 2 Bags",
  "2 - 3 Bags",
  "3 - 4 Bags",
  "4 - 5 Bags",
  "> 5 Bags",
  "1 - 2 Boxes",
  "Possible get",
  "Not available",
];

export const disponible = (d: string | null) => !!d && d.toLowerCase() !== "not available";

export type CentroRef = { id: string; nombre: string };

export type Producto = {
  id: string;
  codigo: string;
  grupoId: string;
  grupoCodigo: string;
  grupoNombre: string;
  tipo: string;
  nombreCientifico: string;
  nombreComun: string | null;
  nombreEspanol: string | null;
  tallaMinCm: number | null;
  tallaMaxCm: number | null;
  tallaEtiqueta: string | null;
  bolsaMin: number | null;
  bolsaMax: number | null;
  disponibilidad: string | null;
  activo: boolean;
  notas: string | null;
  creadoEn: string;
  centrosAcopio: CentroRef[];
  /** Precio por id de lista (sin las listas de costo si no hay permiso). */
  precios: Record<string, number>;
};

export type ListaPrecios = {
  id: string;
  codigo: string;
  nombre: string;
  moneda: string;
  esCosto: boolean;
  clienteId: string | null;
  clienteNombre: string | null;
  orden: number;
  activo: boolean;
  listaBaseId: string | null;
  listaBaseNombre: string | null;
  factorSalvaje: number | null;
  factorCriadero: number | null;
  factorEspecial: number | null;
  aplicaImpuestos: boolean;
  redondeo: number;
  productos: number;
};

export type ParametrosPrecios = { iva: number; pasarela: number; trmReferencia: number; actualizadoEn: string | null };

export type CentroAcopio = {
  id: string;
  nombre: string;
  cuencaId: string;
  cuencaNombre: string;
  activo: boolean;
  productos: number;
  proveedores: number;
  creadoEn: string;
};

export type Proveedor = {
  id: string;
  nombre: string;
  tipoIdentificacionId: string;
  tipoIdentificacionCodigo: string;
  numeroIdentificacion: string;
  telefono: string | null;
  contacto: string | null;
  email: string | null;
  notas: string | null;
  activo: boolean;
  creadoEn: string;
  centrosAcopio: CentroRef[];
};

const decimal = (v: number, max = 2) => v.toLocaleString("es-CO", { maximumFractionDigits: max });

/** "5 – 7 cm", "12 cm", "small" o "—". */
export function textoTalla(p: Pick<Producto, "tallaMinCm" | "tallaMaxCm" | "tallaEtiqueta">): string {
  if (p.tallaMinCm != null && p.tallaMaxCm != null) {
    return p.tallaMinCm === p.tallaMaxCm ? `${decimal(p.tallaMinCm)} cm` : `${decimal(p.tallaMinCm)} – ${decimal(p.tallaMaxCm)} cm`;
  }
  return p.tallaEtiqueta ?? "—";
}

/** "40 – 50" o "100" peces por bolsa. */
export function textoBolsa(p: Pick<Producto, "bolsaMin" | "bolsaMax">): string {
  if (p.bolsaMin == null) return "—";
  return p.bolsaMax != null && p.bolsaMax !== p.bolsaMin ? `${p.bolsaMin} – ${p.bolsaMax}` : String(p.bolsaMin);
}

export function factorDe(lista: ListaPrecios, tipo: string): number | null {
  return tipo === "salvaje" ? lista.factorSalvaje : tipo === "criadero" ? lista.factorCriadero : lista.factorEspecial;
}

/**
 * Espejo de `ReglasPrecio.Calcular`: base × factor del tipo × [(1 + IVA) × (1 + pasarela)],
 * redondeado al múltiplo de la lista. Los centavos se redondean sobre enteros para evitar los
 * errores de coma flotante (0,95 × 1,1 debe dar 1,05, no 1,04).
 */
export function calcularPrecio(
  base: number | null | undefined,
  tipo: string,
  lista: ListaPrecios,
  parametros: Pick<ParametrosPrecios, "iva" | "pasarela">,
): number | null {
  const factor = factorDe(lista, tipo);
  if (!base || base <= 0 || !factor || factor <= 0) return null;
  let valor = base * factor;
  if (lista.aplicaImpuestos) valor *= (1 + parametros.iva / 100) * (1 + parametros.pasarela / 100);
  const pasos = Number((valor / lista.redondeo).toFixed(6));
  return Number((Math.round(pasos) * lista.redondeo).toFixed(2));
}

/** "Costo de compra × 2 · 1,3 · 2 (salvaje · criadero · especial) + IVA y pasarela · redondeo 50". */
export function textoRegla(lista: ListaPrecios): string {
  if (!lista.listaBaseId) return "Manual";
  const f = [lista.factorSalvaje, lista.factorCriadero, lista.factorEspecial];
  const factores = f.every((x) => x === f[0]) ? `× ${f[0] == null ? "—" : decimal(f[0], 4)}` : `× ${f.map((x) => (x == null ? "—" : decimal(x, 4))).join(" / ")} (salvaje / criadero / especial)`;
  return `${lista.listaBaseNombre} ${factores}${lista.aplicaImpuestos ? " + IVA y pasarela" : ""} · redondeo ${decimal(lista.redondeo)}`;
}

export const ETIQUETAS_HISTORIAL_CATALOGO: Record<string, string> = {
  Codigo: "Código",
  Grupo: "Grupo",
  Tipo: "Tipo",
  NombreCientifico: "Nombre científico",
  NombreComun: "Nombre común",
  NombreEspanol: "Nombre en español",
  TallaMinCm: "Talla mínima (cm)",
  TallaMaxCm: "Talla máxima (cm)",
  TallaEtiqueta: "Talla",
  BolsaMin: "Peces por bolsa (mín.)",
  BolsaMax: "Peces por bolsa (máx.)",
  Disponibilidad: "Disponibilidad",
  Activo: "Activo",
  Notas: "Notas",
  CentrosAcopio: "Centros de acopio",
  Nombre: "Nombre",
  Cuenca: "Cuenca",
  Moneda: "Moneda",
  EsCosto: "Es costo",
  Cliente: "Cliente",
  Orden: "Orden",
  ListaBase: "Lista base",
  FactorSalvaje: "Factor salvaje",
  FactorCriadero: "Factor criadero",
  FactorEspecial: "Factor especial",
  AplicaImpuestos: "IVA y pasarela",
  Redondeo: "Redondeo",
  Recalculo: "Recálculo",
  TipoIdentificacion: "Tipo de identificación",
  NumeroIdentificacion: "Identificación",
  Telefono: "Teléfono",
  Contacto: "Contacto",
  Email: "Correo",
  Iva: "IVA (%)",
  Pasarela: "Pasarela (%)",
  TrmReferencia: "TRM de referencia",
};
