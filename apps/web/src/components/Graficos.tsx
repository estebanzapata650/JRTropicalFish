import type { ReactNode } from "react";
import { AlertTriangle } from "lucide-react";

/**
 * Gráficos de los tableros, hechos con CSS y los tokens del tema (sin librería). Siguen el método del
 * skill dataviz: barras delgadas (≤ 24 px) con el extremo de dato redondeado y la base recta, un solo
 * tono para comparar magnitudes, texto siempre en tokens de texto (nunca del color de la serie) y cada
 * cifra visible o en la leyenda/tabla: el tooltip (hover y foco) amplía, nunca es el único acceso.
 * No usan estado: sirven en Server Components.
 */

/** Recorta y formatea números de eje: 0 · 500 · 1.000 · 12 mil. */
const compacto = (v: number) =>
  v >= 10_000 ? `${(v / 1000).toLocaleString("es-CO", { maximumFractionDigits: 1 })} mil` : v.toLocaleString("es-CO");

/** Tope «redondo» para el eje: 1, 2 o 5 × 10ⁿ por encima del máximo. */
function topeRedondo(max: number) {
  if (max <= 0) return 1;
  const base = 10 ** Math.floor(Math.log10(max));
  return ([1, 2, 5, 10].map((m) => m * base).find((t) => t >= max) ?? 10 * base);
}

export type FilaBarra = { clave: string; etiqueta: ReactNode; detalle?: ReactNode; valor: number; texto: string };

/**
 * Barras horizontales ordenadas (reemplazan las tortas de Looker): comparar partes entre sí es más exacto
 * sobre una base común que en sectores. Un solo tono; el nombre y la cifra van en texto.
 */
export function BarrasRanking({
  filas,
  vacio,
  etiqueta,
  escalaMinima = 0,
}: {
  filas: FilaBarra[];
  vacio: string;
  etiqueta: string;
  /** Para porcentajes: el ancho total vale al menos esto (un 2,8 % no debe verse como una barra llena). */
  escalaMinima?: number;
}) {
  if (filas.length === 0) return <p className="text-sm text-texto-suave">{vacio}</p>;
  const max = Math.max(...filas.map((f) => f.valor), escalaMinima, 1);
  return (
    <ol aria-label={etiqueta} className="flex flex-col gap-3">
      {filas.map((f) => (
        <li key={f.clave} className="flex flex-col gap-1">
          <div className="flex items-baseline justify-between gap-3 text-sm">
            <span className="min-w-0 truncate text-texto">{f.etiqueta}</span>
            <span className="shrink-0 font-medium tabular-nums text-texto">{f.texto}</span>
          </div>
          <div className="h-3 w-full" aria-hidden>
            <div className="h-full rounded-r bg-acento transition-opacity hover:opacity-80" style={{ width: `${Math.max((f.valor / max) * 100, f.valor > 0 ? 1 : 0)}%` }} />
          </div>
          {f.detalle ? <p className="text-xs text-texto-suave">{f.detalle}</p> : null}
        </li>
      ))}
    </ol>
  );
}

/** Nivel de un cumplimiento: ≥ 95 % bien, ≥ 80 % atención, menos es bajo. */
export const nivelCumplimiento = (p: number) => (p >= 95 ? "bien" : p >= 80 ? "atencion" : "bajo");

/**
 * Lista de medidores: una fila por proveedor/cliente con su % contra el 100 %. La pista es un tono claro
 * de la misma familia; el relleno sube de severidad y, cuando es bajo, lleva icono y texto (no solo color).
 */
export function ListaMedidores({
  filas,
  vacio,
  etiqueta,
}: {
  filas: { clave: string; etiqueta: ReactNode; detalle?: ReactNode; porcentaje: number }[];
  vacio: string;
  etiqueta: string;
}) {
  if (filas.length === 0) return <p className="text-sm text-texto-suave">{vacio}</p>;
  return (
    <ul aria-label={etiqueta} className="flex flex-col gap-3">
      {filas.map((f) => {
        const nivel = nivelCumplimiento(f.porcentaje);
        return (
          <li key={f.clave} className="flex flex-col gap-1">
            <div className="flex items-baseline justify-between gap-3 text-sm">
              <span className="min-w-0 truncate text-texto">{f.etiqueta}</span>
              <span className={`inline-flex shrink-0 items-center gap-1 font-medium tabular-nums ${nivel === "bajo" ? "text-peligro" : "text-texto"}`}>
                {nivel === "bajo" ? <AlertTriangle className="size-3.5" aria-label="Bajo" /> : null}
                {f.porcentaje.toLocaleString("es-CO", { maximumFractionDigits: 1 })} %
              </span>
            </div>
            <div
              role="meter"
              aria-label={typeof f.etiqueta === "string" ? f.etiqueta : etiqueta}
              aria-valuenow={Math.round(f.porcentaje)}
              aria-valuemin={0}
              aria-valuemax={100}
              className="h-2 w-full overflow-hidden rounded-full bg-acento-suave"
            >
              <div
                className={`h-full rounded-full ${nivel === "bien" ? "bg-acento" : nivel === "atencion" ? "bg-serie-enfermo" : "bg-peligro"}`}
                style={{ width: `${Math.min(100, Math.max(0, f.porcentaje))}%` }}
              />
            </div>
            {f.detalle ? <p className="text-xs text-texto-suave">{f.detalle}</p> : null}
          </li>
        );
      })}
    </ul>
  );
}

/**
 * Columnas en el tiempo (una serie): base recta, tapa redondeada, ≤ 24 px de ancho, eje con valores
 * redondos y rejilla tenue. Cada columna es enfocable y muestra su detalle al pasar o enfocar; el mismo
 * dato está en la tabla que acompaña al gráfico.
 */
export function Columnas({
  puntos,
  etiqueta,
  formato = compacto,
}: {
  puntos: { clave: string; eje: string; valor: number; tooltip: string }[];
  etiqueta: string;
  formato?: (v: number) => string;
}) {
  const tope = topeRedondo(Math.max(...puntos.map((p) => p.valor), 0));
  const marcas = [tope, tope / 2, 0];
  // En móvil, una de cada dos etiquetas del eje X si hay muchas.
  const saltar = puntos.length > 8;
  return (
    <figure aria-label={etiqueta} className="flex flex-col gap-2">
      <div className="grid grid-cols-[auto_1fr] gap-2">
        <div className="flex h-40 flex-col justify-between text-right text-xs tabular-nums text-texto-tenue" aria-hidden>
          {marcas.map((m) => (
            <span key={m} className="-translate-y-1/2 first:translate-y-0 last:translate-y-0">
              {formato(m)}
            </span>
          ))}
        </div>
        <div className="relative h-40">
          {marcas.map((m) => (
            <div key={m} className="absolute inset-x-0 border-t border-borde" style={{ bottom: `${(m / tope) * 100}%` }} aria-hidden />
          ))}
          <ul className="absolute inset-0 flex items-end justify-around gap-0.5">
            {puntos.map((p) => (
              <li key={p.clave} className="group relative flex h-full flex-1 items-end justify-center">
                <div
                  tabIndex={0}
                  aria-label={p.tooltip}
                  className="w-full max-w-6 rounded-t bg-acento outline-none transition-opacity group-hover:opacity-80 focus-visible:ring-2 focus-visible:ring-acento focus-visible:ring-offset-2 focus-visible:ring-offset-superficie"
                  style={{ height: `${Math.max((p.valor / tope) * 100, p.valor > 0 ? 1 : 0)}%`, minHeight: p.valor > 0 ? 2 : 0 }}
                />
                <span
                  role="tooltip"
                  className="pointer-events-none absolute bottom-full left-1/2 z-10 mb-1 hidden -translate-x-1/2 rounded-md border border-borde bg-superficie px-2 py-1 text-xs whitespace-nowrap text-texto shadow-md group-focus-within:block group-hover:block"
                >
                  {p.tooltip}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>
      <div className="grid grid-cols-[auto_1fr] gap-2" aria-hidden>
        <span className="invisible text-xs">{formato(tope)}</span>
        <div className="flex justify-around gap-0.5">
          {puntos.map((p, i) => (
            <span key={p.clave} className={`flex-1 text-center text-[11px] text-texto-tenue ${saltar && i % 2 ? "invisible sm:visible" : ""}`}>
              {p.eje}
            </span>
          ))}
        </div>
      </div>
    </figure>
  );
}

export type ParteLote = { clave: string; etiqueta: string; valor: number; color: string };

/**
 * Barra segmentada delgada: en qué quedó cada pez de un lote (en acuario, aclimatación, enfermo, enviado,
 * muerto). Sin texto encima: las cifras van en la leyenda de al lado o en la fila de la tabla.
 */
export function BarraSegmentada({ partes, etiqueta, gruesa = false }: { partes: ParteLote[]; etiqueta: string; gruesa?: boolean }) {
  const total = partes.reduce((s, p) => s + p.valor, 0);
  const visibles = partes.filter((p) => p.valor > 0);
  return (
    <div
      role="img"
      aria-label={`${etiqueta}: ${visibles.map((p) => `${p.etiqueta} ${p.valor.toLocaleString("es-CO")}`).join(", ") || "sin datos"}`}
      className={`flex w-full gap-0.5 ${gruesa ? "h-2" : "h-1.5"}`}
    >
      {total === 0 ? <div className="h-full w-full rounded-full bg-neutro-fondo" /> : null}
      {visibles.map((p) => (
        <div
          key={p.clave}
          title={`${p.etiqueta}: ${p.valor.toLocaleString("es-CO")}`}
          className={`h-full rounded-full ${p.color}`}
          style={{ width: `${(p.valor / total) * 100}%`, minWidth: 4 }}
        />
      ))}
    </div>
  );
}

/** Leyenda de la barra segmentada con la cifra y el % de cada parte (es su vista de tabla). */
export function LeyendaLote({ partes, base }: { partes: ParteLote[]; base: number }) {
  return (
    <ul className="grid grid-cols-2 gap-x-4 gap-y-2 sm:grid-cols-3 lg:grid-cols-5">
      {partes.map((p) => (
        <li key={p.clave} className="flex items-start gap-2 text-sm">
          <span className={`mt-1.5 size-2.5 shrink-0 rounded-sm ${p.color}`} aria-hidden />
          <span className="flex flex-col">
            <span className="text-texto-suave">{p.etiqueta}</span>
            <span className="font-semibold text-texto tabular-nums">
              {p.valor.toLocaleString("es-CO")}
              {base > 0 ? (
                <span className="ml-1 text-xs font-normal text-texto-suave">
                  {((p.valor / base) * 100).toLocaleString("es-CO", { maximumFractionDigits: 1 })} %
                </span>
              ) : null}
            </span>
          </span>
        </li>
      ))}
    </ul>
  );
}

/** Partes de un lote en el orden validado de la paleta. */
export const partesLote = (l: { sanos: number; porUbicar: number; enfermos: number; despachados: number; muertos: number }): ParteLote[] => [
  { clave: "sano", etiqueta: "En acuarios", valor: l.sanos, color: "bg-serie-sano" },
  { clave: "aclimatacion", etiqueta: "Aclimatación", valor: l.porUbicar, color: "bg-serie-aclimatacion" },
  { clave: "enfermo", etiqueta: "Enfermos", valor: l.enfermos, color: "bg-serie-enfermo" },
  { clave: "despachado", etiqueta: "Enviados", valor: l.despachados, color: "bg-serie-despachado" },
  { clave: "muerto", etiqueta: "Muertos", valor: l.muertos, color: "bg-serie-muerto" },
];
