/**
 * Clases de Tailwind compartidas por los controles de formulario.
 *
 * Viven aquí y no en `components/campos.tsx` a propósito: ese módulo es `"use client"`, y cuando un
 * Server Component importa una constante de un módulo de cliente recibe una referencia, no el texto.
 * Pasarla directa a `className` funciona, pero interpolarla en una plantilla la convierte en el
 * código fuente de la referencia y la clase queda rota. Desde un módulo normal el problema no existe.
 *
 * Todo color sale de un token del tema; nunca de la paleta fija de Tailwind.
 */
export const inputClass =
  "h-11 w-full rounded-md border border-borde bg-superficie px-3 text-base text-texto outline-none focus:border-acento focus:ring-1 focus:ring-acento";
export const textareaClass =
  "min-h-20 w-full rounded-md border border-borde bg-superficie p-3 text-base text-texto outline-none focus:border-acento focus:ring-1 focus:ring-acento";
export const botonPrimarioClass =
  "inline-flex h-11 items-center justify-center rounded-md bg-acento px-4 text-base font-medium text-acento-texto transition-opacity hover:opacity-90 disabled:opacity-60";
export const botonSecundarioClass =
  "inline-flex h-11 items-center justify-center rounded-md border border-borde px-4 text-sm font-medium text-texto transition-colors hover:bg-acento-suave";
export const botonPeligroClass =
  "inline-flex h-11 items-center justify-center rounded-md bg-peligro px-4 text-base font-medium text-peligro-texto transition-opacity hover:opacity-90 disabled:opacity-60";
export const selectClass =
  "h-11 w-full rounded-md border border-borde bg-superficie px-3 text-base text-texto outline-none focus:border-acento focus:ring-1 focus:ring-acento";
// Los checkboxes nativos ignoran `bg-*`/`text-*`: el único color que respetan es `accent-color`.
// El resto del aspecto (marco y fondo sin marcar) lo resuelve `color-scheme` en globals.css.
export const casillaClass = "size-5 rounded accent-[var(--acento)] focus-visible:outline-acento";
