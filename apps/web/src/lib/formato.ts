/** Formatos de fecha comunes. La zona horaria de negocio es Bogotá, sin importar dónde corra el servidor. */
const ZONA = "America/Bogota";

export function fechaHora(iso: string | null | undefined): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("es-CO", { dateStyle: "medium", timeStyle: "short", timeZone: ZONA });
}

/** "hace 5 min", "hace 3 h", o la fecha si es más antigua que una semana. */
export function haceCuanto(iso: string | null | undefined, ahora: Date = new Date()): string {
  if (!iso) return "Nunca";
  const minutos = Math.round((ahora.getTime() - new Date(iso).getTime()) / 60000);
  if (minutos < 1) return "Hace un momento";
  if (minutos < 60) return `Hace ${minutos} min`;
  const horas = Math.round(minutos / 60);
  if (horas < 24) return `Hace ${horas} h`;
  const dias = Math.round(horas / 24);
  if (dias < 7) return `Hace ${dias} d`;
  return fechaHora(iso);
}

export function estaBloqueado(bloqueadoHasta: string | null, ahora: Date = new Date()): boolean {
  return !!bloqueadoHasta && new Date(bloqueadoHasta).getTime() > ahora.getTime();
}

/** Fecha de calendario (`yyyy-MM-dd`, un DateOnly de la API) sin conversión de zona horaria. */
export function fecha(valor: string | null | undefined): string {
  if (!valor) return "—";
  const [anio, mes, dia] = valor.split("-").map(Number);
  return new Date(Date.UTC(anio, mes - 1, dia)).toLocaleDateString("es-CO", { dateStyle: "medium", timeZone: "UTC" });
}

/** Edad cumplida a la fecha `hoy` (`yyyy-MM-dd`). */
export function edad(nacimiento: string | null | undefined, hoy: string): number | null {
  if (!nacimiento) return null;
  const [an, mn, dn] = nacimiento.split("-").map(Number);
  const [ah, mh, dh] = hoy.split("-").map(Number);
  return ah - an - (mh < mn || (mh === mn && dh < dn) ? 1 : 0);
}

/** "Hoy" de negocio (Bogotá) como `yyyy-MM-dd`. */
export function hoyNegocio(ahora: Date = new Date()): string {
  return ahora.toLocaleDateString("en-CA", { timeZone: ZONA });
}

const formatoPesos = new Intl.NumberFormat("es-CO", { style: "currency", currency: "COP", maximumFractionDigits: 0 });

/** Valor en pesos colombianos, sin decimales. */
export function moneda(valor: number | null | undefined): string {
  return valor == null ? "—" : formatoPesos.format(valor);
}
