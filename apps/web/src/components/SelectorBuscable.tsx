"use client";

import { useId, useMemo, useRef, useState } from "react";
import { ChevronDown, X } from "lucide-react";
import { inputClass } from "@/lib/estilos";

export type OpcionBuscable = {
  id: string;
  nombre: string;
  detalle?: string | null;
};

const MAXIMO = 50;

/** Sin tildes ni mayúsculas: "Pequeño" y "pequeno" son lo mismo. */
const normalizar = (s: string) =>
  s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase().trim();

/**
 * Coincide si el texto contiene lo escrito, si cada palabra escrita es el comienzo de alguna palabra
 * ("kam tok" → "Kamihata Fish Tokyo") o si lo escrito son las iniciales ("kft").
 */
export function coincide(opcion: OpcionBuscable, consulta: string) {
  const q = normalizar(consulta);
  if (!q) return true;
  const texto = normalizar(`${opcion.nombre} ${opcion.detalle ?? ""}`);
  if (texto.includes(q)) return true;
  const palabras = texto.split(/[^a-z0-9]+/).filter(Boolean);
  const partes = q.split(/\s+/).filter(Boolean);
  if (partes.every((p) => palabras.some((w) => w.startsWith(p)))) return true;
  const iniciales = palabras.map((w) => w[0]).join("");
  return !q.includes(" ") && q.length >= 2 && iniciales.includes(q);
}

/**
 * Lista desplegable con búsqueda: se escribe parte del nombre o las iniciales y la lista se acorta.
 * Flechas para moverse, Enter para elegir, Escape para cerrar. Patrón ARIA combobox.
 *
 * Controlado (`valor` + `onChange`) o, en formularios que se envían por `name` (FormData), no controlado con
 * `valorInicial` + `name`: guarda el id en un campo oculto.
 */
export function SelectorBuscable({
  id,
  opciones,
  valor,
  onChange,
  valorInicial = "",
  name,
  placeholder = "Escribe para buscar…",
  disabled,
  requerido,
  etiqueta,
  vacio,
  className = "",
}: {
  id?: string;
  opciones: OpcionBuscable[];
  valor?: string;
  onChange?: (id: string) => void;
  valorInicial?: string;
  name?: string;
  placeholder?: string;
  disabled?: boolean;
  requerido?: boolean;
  /** `aria-label` cuando no hay un `<label htmlFor>`. */
  etiqueta?: string;
  /** Opción "vacía" con nombre propio ("Todos", "Ninguno", "Yo"): se ofrece al principio de la lista. */
  vacio?: string;
  className?: string;
}) {
  const listaId = useId();
  const [interno, setInterno] = useState(valorInicial);
  const actual = valor ?? interno;
  const todas = useMemo(
    () => (vacio ? [{ id: "", nombre: vacio }, ...opciones] : opciones),
    [opciones, vacio],
  );
  const elegido = todas.find((o) => o.id === actual) ?? null;
  const [texto, setTexto] = useState<string | null>(null);
  const [abierto, setAbierto] = useState(false);
  const [activo, setActivo] = useState(0);
  const contenedor = useRef<HTMLDivElement>(null);

  // Mientras no se escribe, el campo muestra la opción elegida.
  const mostrado =
    texto ?? (elegido && (elegido.id || vacio) ? elegido.nombre : "");
  const coincidentes = useMemo(
    () => todas.filter((o) => coincide(o, texto ?? "")),
    [todas, texto],
  );
  const filtradas = coincidentes.slice(0, MAXIMO);

  const cambiar = (nuevo: string) => {
    if (valor === undefined) setInterno(nuevo);
    onChange?.(nuevo);
  };

  const elegir = (o: OpcionBuscable) => {
    cambiar(o.id);
    setTexto(null);
    setAbierto(false);
  };

  return (
    <div
      ref={contenedor}
      className={`relative ${className}`}
      onBlur={(e) => {
        if (!contenedor.current?.contains(e.relatedTarget as Node | null)) {
          setAbierto(false);
          setTexto(null);
        }
      }}
    >
      {name ? <input type="hidden" name={name} value={actual} /> : null}
      <input
        id={id}
        type="text"
        role="combobox"
        aria-label={etiqueta}
        aria-expanded={abierto}
        aria-controls={listaId}
        aria-autocomplete="list"
        aria-activedescendant={
          abierto && filtradas[activo] ? `${listaId}-${activo}` : undefined
        }
        autoComplete="off"
        required={requerido && !actual}
        disabled={disabled}
        placeholder={placeholder}
        value={mostrado}
        onFocus={(e) => {
          e.target.select();
          setAbierto(true);
        }}
        onChange={(e) => {
          setTexto(e.target.value);
          setActivo(0);
          setAbierto(true);
        }}
        onKeyDown={(e) => {
          if (e.key === "ArrowDown") {
            e.preventDefault();
            setAbierto(true);
            setActivo((a) => Math.min(a + 1, filtradas.length - 1));
          } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setActivo((a) => Math.max(a - 1, 0));
          } else if (e.key === "Enter" && abierto) {
            e.preventDefault();
            if (filtradas[activo]) elegir(filtradas[activo]);
          } else if (e.key === "Escape") {
            setAbierto(false);
            setTexto(null);
          }
        }}
        className={`${inputClass} pr-16`}
      />
      <div className="pointer-events-none absolute inset-y-0 right-2 flex items-center gap-1">
        {actual && !disabled ? (
          <button
            type="button"
            aria-label="Quitar la selección"
            className="pointer-events-auto rounded p-1 text-texto-tenue hover:text-texto"
            onClick={() => {
              cambiar("");
              setTexto("");
              setAbierto(true);
            }}
          >
            <X className="size-4" aria-hidden />
          </button>
        ) : null}
        <ChevronDown className="size-4 text-texto-tenue" aria-hidden />
      </div>
      {abierto && !disabled ? (
        <ul
          id={listaId}
          role="listbox"
          className="absolute z-30 mt-1 max-h-64 w-full min-w-56 overflow-y-auto rounded-md border border-borde bg-superficie py-1 shadow-lg"
        >
          {filtradas.length === 0 ? (
            <li className="px-3 py-2 text-sm text-texto-suave">
              Sin coincidencias.
            </li>
          ) : null}
          {filtradas.map((o, i) => (
            <li
              key={o.id || "vacio"}
              id={`${listaId}-${i}`}
              role="option"
              aria-selected={o.id === actual}
              tabIndex={-1}
              onMouseDown={(e) => e.preventDefault()}
              onClick={() => elegir(o)}
              onMouseEnter={() => setActivo(i)}
              className={`cursor-pointer px-3 py-2.5 text-base ${i === activo ? "bg-acento-suave" : ""} ${o.id === actual ? "font-semibold" : ""} ${o.id ? "text-texto" : "text-texto-suave"}`}
            >
              {o.nombre}
              {o.detalle ? (
                <span className="ml-2 text-sm text-texto-suave">
                  {o.detalle}
                </span>
              ) : null}
            </li>
          ))}
          {coincidentes.length > MAXIMO ? (
            <li className="px-3 py-2 text-xs text-texto-tenue">
              Sigue escribiendo para ver más…
            </li>
          ) : null}
        </ul>
      ) : null}
    </div>
  );
}
