"use client";

import { useState, type ReactNode } from "react";

export type ColumnaTablero = { id: string; titulo: string; borde: string; resumen?: ReactNode };

/**
 * Tablero Kanban. En escritorio se arrastra con el drag & drop nativo del navegador; en móvil (donde
 * el arrastre nativo no existe) cada tarjeta trae sus botones de cambio rápido, que pinta `render`.
 *
 * Una columna solo acepta la tarjeta si `puedeSoltar` lo permite: así el pipeline rechaza
 * visualmente soltar en una etapa anterior. La API vuelve a validar todo.
 */
export function Tablero<T>({
  columnas,
  items,
  columnaDe,
  idDe,
  etiquetaDe,
  render,
  puedeSoltar,
  onSoltar,
  deshabilitado = false,
  resaltar,
}: {
  columnas: ColumnaTablero[];
  items: T[];
  columnaDe: (item: T) => string;
  idDe: (item: T) => string;
  etiquetaDe: (item: T) => string;
  render: (item: T) => ReactNode;
  puedeSoltar: (item: T, destino: string) => boolean;
  onSoltar: (item: T, destino: string) => void;
  deshabilitado?: boolean;
  /** Tarjetas en rojo (semáforo): tareas vencidas, despachos con DOA alto. */
  resaltar?: (item: T) => boolean;
}) {
  const [arrastrando, setArrastrando] = useState<T | null>(null);
  const [sobre, setSobre] = useState<string | null>(null);

  const terminar = () => {
    setArrastrando(null);
    setSobre(null);
  };

  return (
    // En móvil cada columna ocupa casi todo el ancho y se recorre deslizando (scroll-snap).
    <div className="-mx-4 flex snap-x snap-mandatory scroll-px-4 gap-3 overflow-x-auto px-4 pb-2 md:mx-0 md:scroll-px-0 md:px-0">
      {columnas.map((columna) => {
        const propios = items.filter((i) => columnaDe(i) === columna.id);
        const acepta = arrastrando !== null && columnaDe(arrastrando) !== columna.id && puedeSoltar(arrastrando, columna.id);
        const rechaza = arrastrando !== null && columnaDe(arrastrando) !== columna.id && !acepta;
        return (
          <section
            key={columna.id}
            aria-label={columna.titulo}
            onDragOver={(e) => {
              if (!acepta) return;
              e.preventDefault();
              e.dataTransfer.dropEffect = "move";
              if (sobre !== columna.id) setSobre(columna.id);
            }}
            onDragLeave={() => sobre === columna.id && setSobre(null)}
            onDrop={(e) => {
              e.preventDefault();
              if (arrastrando && acepta) onSoltar(arrastrando, columna.id);
              terminar();
            }}
            className={`flex w-[85vw] max-w-80 shrink-0 snap-start flex-col gap-2 rounded-md border border-t-4 bg-fondo-sutil p-2 transition-opacity sm:w-72 ${
              columna.borde
            } ${sobre === columna.id ? "border-acento ring-2 ring-acento" : "border-borde"} ${rechaza ? "opacity-50" : ""}`}
          >
            <header className="flex items-baseline justify-between gap-2 px-1 pt-1">
              <h2 className="text-sm font-semibold text-texto">{columna.titulo}</h2>
              <span className="text-xs text-texto-suave tabular-nums">{propios.length}</span>
            </header>
            {columna.resumen ? <div className="px-1 text-xs text-texto-suave">{columna.resumen}</div> : null}
            <ul className="flex min-h-24 flex-col gap-2">
              {propios.map((item) => (
                <li
                  key={idDe(item)}
                  draggable={!deshabilitado}
                  aria-label={etiquetaDe(item)}
                  onDragStart={(e) => {
                    e.dataTransfer.effectAllowed = "move";
                    e.dataTransfer.setData("text/plain", idDe(item));
                    setArrastrando(item);
                  }}
                  onDragEnd={terminar}
                  className={`rounded-md border p-3 shadow-sm ${
                    resaltar?.(item) ? "border-peligro bg-peligro/10" : "border-borde bg-superficie"
                  } ${
                    deshabilitado ? "" : "md:cursor-grab"
                  } ${arrastrando && idDe(arrastrando) === idDe(item) ? "opacity-60" : ""}`}
                >
                  {render(item)}
                </li>
              ))}
              {propios.length === 0 ? <li className="px-1 py-6 text-center text-xs text-texto-tenue">Sin tarjetas</li> : null}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
