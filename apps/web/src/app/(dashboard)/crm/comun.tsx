"use client";

import type { ReactNode } from "react";
import { MessageCircle, Phone } from "lucide-react";
import { enlaceLlamada, enlaceWhatsapp } from "@/lib/crm";
import { selectClass } from "@/lib/estilos";

/** `<select>` a partir de un mapa valor → etiqueta (los enums de `lib/crm.ts`). */
export function SelectMapa({
  id,
  valor,
  onChange,
  mapa,
  vacio,
  disabled,
}: {
  id: string;
  valor: string;
  onChange: (valor: string) => void;
  mapa: Record<string, string>;
  /** Si se indica, agrega una opción vacía con este texto. */
  vacio?: string;
  disabled?: boolean;
}) {
  return (
    <select id={id} value={valor} onChange={(e) => onChange(e.target.value)} disabled={disabled} className={selectClass}>
      {vacio !== undefined ? <option value="">{vacio}</option> : null}
      {Object.entries(mapa).map(([v, etiqueta]) => (
        <option key={v} value={v}>
          {etiqueta}
        </option>
      ))}
    </select>
  );
}

/** Acciones rápidas de contacto: WhatsApp Web (`wa.me`) y llamada (`tel:`). */
export function BotonesContacto({ telefono, nombre, compacto = false }: { telefono: string | null; nombre: string; compacto?: boolean }) {
  const whatsapp = enlaceWhatsapp(telefono);
  const llamada = enlaceLlamada(telefono);
  if (!whatsapp && !llamada) return null;

  const clase = compacto
    ? "inline-flex size-9 items-center justify-center rounded-md border border-borde text-texto-suave hover:bg-acento-suave hover:text-texto"
    : "inline-flex h-11 items-center justify-center gap-2 rounded-md border border-borde px-4 text-sm font-medium text-texto hover:bg-acento-suave";
  return (
    <div className="flex gap-2">
      {whatsapp ? (
        <a href={whatsapp} target="_blank" rel="noopener noreferrer" className={clase} aria-label={`Escribir por WhatsApp a ${nombre}`} title="WhatsApp">
          <MessageCircle className="size-4" aria-hidden />
          {compacto ? null : "WhatsApp"}
        </a>
      ) : null}
      {llamada ? (
        <a href={llamada} className={clase} aria-label={`Llamar a ${nombre}`} title="Llamar">
          <Phone className="size-4" aria-hidden />
          {compacto ? null : "Llamar"}
        </a>
      ) : null}
    </div>
  );
}

/** Tarjeta de KPI: valor grande, meta y barra de avance opcional. */
export function Kpi({
  titulo,
  valor,
  detalle,
  avance,
  alerta = false,
}: {
  titulo: string;
  valor: string;
  detalle?: ReactNode;
  /** 0–100+; si se indica, dibuja la barra. */
  avance?: number;
  alerta?: boolean;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-2 rounded-md border border-borde bg-superficie p-4">
      <p className="text-sm text-texto-suave">{titulo}</p>
      <p className={`text-2xl font-semibold tabular-nums ${alerta ? "text-peligro" : "text-texto"}`}>{valor}</p>
      {avance !== undefined ? <BarraAvance porcentaje={avance} etiqueta={titulo} /> : null}
      {detalle ? <div className="text-xs text-texto-suave">{detalle}</div> : null}
    </div>
  );
}

/** Barra de avance contra una meta. Llega a verde al 100 %. */
export function BarraAvance({ porcentaje, etiqueta }: { porcentaje: number; etiqueta: string }) {
  const ancho = Math.max(0, Math.min(100, porcentaje));
  return (
    <div
      role="progressbar"
      aria-label={etiqueta}
      aria-valuenow={Math.round(porcentaje)}
      aria-valuemin={0}
      aria-valuemax={100}
      className="h-2 w-full overflow-hidden rounded-full bg-neutro-fondo"
    >
      <div
        className={`h-full rounded-full ${porcentaje >= 100 ? "bg-exito-texto" : "bg-acento"}`}
        style={{ width: `${ancho}%` }}
      />
    </div>
  );
}
