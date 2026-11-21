"use client";

import { useState, type ReactNode } from "react";
import { Pencil, Trash2, type LucideIcon } from "lucide-react";
import { casillaClass } from "@/lib/estilos";

const labelClass = "text-sm font-medium text-texto";

/**
 * Rejilla de formulario: una columna en móvil y hasta tres en pantallas anchas, para no
 * desperdiciar el ancho del escritorio ni obligar a un scroll interminable.
 */
export function RejillaCampos({ children }: { children: ReactNode }) {
  return <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">{children}</div>;
}

/** Envuelve un campo que debe ocupar todo el ancho de la rejilla (textos largos, listas). */
export function CampoAncho({ children }: { children: ReactNode }) {
  return <div className="md:col-span-2 xl:col-span-3">{children}</div>;
}

export function Campo({
  label,
  htmlFor,
  hint,
  children,
}: {
  label: string;
  htmlFor?: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <label htmlFor={htmlFor} className={labelClass}>
        {label}
      </label>
      {children}
      {hint ? <p className="text-xs text-texto-suave">{hint}</p> : null}
    </div>
  );
}

export function Casilla({
  name,
  label,
  defaultChecked,
  checked,
  onChange,
}: {
  name?: string;
  label: string;
  defaultChecked?: boolean;
  checked?: boolean;
  onChange?: (valor: boolean) => void;
}) {
  return (
    <label className="flex items-center gap-2.5 py-1">
      <input
        type="checkbox"
        name={name}
        defaultChecked={defaultChecked}
        checked={checked}
        onChange={onChange ? (e) => onChange(e.target.checked) : undefined}
        className={casillaClass}
      />
      <span className="text-sm text-texto">{label}</span>
    </label>
  );
}

/**
 * Sección de formulario. En móvil se pliega para no dejar una pantalla interminable; en escritorio
 * está siempre abierta, porque ahí el espacio sobra.
 *
 * El plegado se resuelve con CSS (`hidden md:flex`), no leyendo el viewport en un efecto: así no
 * hay render extra tras montar ni desajuste entre el HTML del servidor y el del cliente.
 */
export function Seccion({ titulo, children }: { titulo: string; children: ReactNode }) {
  const [abiertaEnMovil, setAbiertaEnMovil] = useState(false);

  return (
    <section className="rounded-md border border-borde bg-superficie">
      <button
        type="button"
        onClick={() => setAbiertaEnMovil((v) => !v)}
        aria-expanded={abiertaEnMovil}
        className="flex w-full items-center justify-between px-4 py-3 text-left text-sm font-semibold text-texto md:cursor-default"
      >
        {titulo}
        <span aria-hidden className="text-texto-tenue md:hidden">
          {abiertaEnMovil ? "−" : "+"}
        </span>
      </button>
      <div
        className={`flex-col gap-4 border-t border-borde px-4 py-4 md:flex ${
          abiertaEnMovil ? "flex" : "hidden"
        }`}
      >
        {children}
      </div>
    </section>
  );
}

/**
 * Tabla con scroll horizontal contenido, para que nunca desborde la página.
 *
 * `compactaEnMovil`: sin ancho mínimo bajo `md`. Úsalo cuando las columnas visibles en móvil (las
 * demás con `hidden md:table-cell`) caben en ~390 px; así el estado y las acciones no quedan
 * escondidos detrás del scroll.
 */
export function TablaResponsive({ children, compactaEnMovil = false }: { children: ReactNode; compactaEnMovil?: boolean }) {
  return (
    <div className="w-full overflow-x-auto rounded-md border border-borde bg-superficie">
      <table className={`w-full text-left text-sm ${compactaEnMovil ? "md:min-w-[40rem]" : "min-w-[40rem]"}`}>{children}</table>
    </div>
  );
}

export function EncabezadoTabla({ children }: { children: ReactNode }) {
  return <thead className="border-b border-borde bg-superficie-alterna">{children}</thead>;
}

/** Tonos de la etiqueta. Salen de tokens del tema, nunca de la paleta fija de Tailwind. */
const tonos = {
  exito: "bg-exito-fondo text-exito-texto",
  advertencia: "bg-advertencia-fondo text-advertencia-texto",
  peligro: "bg-peligro/15 text-peligro",
  neutro: "bg-neutro-fondo text-neutro-texto",
  info: "bg-info-fondo text-info-texto",
} as const;

export type TonoEtiqueta = keyof typeof tonos;

export function Etiqueta({ texto, tono }: { texto: string; tono: TonoEtiqueta }) {
  return (
    <span className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${tonos[tono]}`}>
      {texto}
    </span>
  );
}

export function EtiquetaEstado({ activo }: { activo: boolean }) {
  return <Etiqueta texto={activo ? "Activo" : "Inactivo"} tono={activo ? "exito" : "neutro"} />;
}

/**
 * Acciones de fila como iconos. Llevan `aria-label` y `title`: el icono solo no comunica
 * la acción a un lector de pantalla ni en hover.
 */
export function BotonIcono({
  accion,
  icono,
  etiqueta,
  onClick,
  disabled,
}: {
  /** "editar" y "eliminar" traen su icono; para cualquier otra acción se pasa `icono`. */
  accion: "editar" | "eliminar" | "otra";
  icono?: LucideIcon;
  etiqueta: string;
  onClick: () => void;
  disabled?: boolean;
}) {
  const Icono = accion === "editar" ? Pencil : accion === "eliminar" ? Trash2 : (icono ?? Pencil);
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      title={etiqueta}
      aria-label={etiqueta}
      className={`rounded-md p-2 transition-colors disabled:opacity-40 ${
        accion === "eliminar"
          ? "text-peligro hover:bg-acento-suave"
          : "text-texto-suave hover:bg-acento-suave hover:text-texto"
      }`}
    >
      <Icono className="size-4" aria-hidden />
    </button>
  );
}
