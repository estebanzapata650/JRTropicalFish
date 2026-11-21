"use client";

import { useEffect, useSyncExternalStore, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { X } from "lucide-react";

/**
 * Diálogo modal. El contenido tiene su propio scroll (`max-h-[90vh] overflow-y-auto`) para que
 * en móvil nunca se recorte: se desplaza dentro del diálogo, no la página.
 *
 * <p>
 * ⚠️ <b>Se monta en un portal sobre `document.body`, no donde se escribe.</b> Sin eso, un diálogo
 * abierto desde dentro de un `<form>` deja sus campos **dentro de ese formulario**: pulsar Enter
 * en el buscador de un `CampoBuscable` enviaba el formulario que lo contenía —en un formulario grande,
 * guardaba el registro a medio llenar—. El portal saca el diálogo del árbol del formulario y el
 * problema desaparece para todos los que lo usan.
 * </p>
 */
export function Dialogo({
  titulo,
  abierto,
  onCerrar,
  children,
  ancho = "max-w-2xl",
}: {
  titulo: string;
  abierto: boolean;
  onCerrar: () => void;
  children: ReactNode;
  ancho?: string;
}) {
  useEffect(() => {
    if (!abierto) return;
    const alPresionar = (e: KeyboardEvent) => {
      if (e.key === "Escape") onCerrar();
    };
    document.addEventListener("keydown", alPresionar);
    return () => document.removeEventListener("keydown", alPresionar);
  }, [abierto, onCerrar]);

  // El portal necesita `document`, que en el servidor no existe. Un diálogo que nace abierto (p. ej.
  // `?id=` en la URL) no puede pintarse en la hidratación: el HTML del servidor no lo trae y React
  // fallaría (#418). `useSyncExternalStore` da `false` en el servidor y al hidratar, y `true` justo
  // después, sin estado ni efecto (que ESLint prohíbe por los renders en cascada).
  const enCliente = useSyncExternalStore(suscribirNada, () => true, () => false);
  if (!abierto || !enCliente) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-end justify-center sm:items-center">
      {/* Capa de fondo: cierra al tocar, sin nombre accesible propio para no duplicar
          la etiqueta del botón X (que sí es el control anunciado). */}
      <button
        type="button"
        aria-hidden
        tabIndex={-1}
        onClick={onCerrar}
        className="absolute inset-0 bg-black/40"
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={titulo}
        className={`relative flex max-h-[90vh] w-full flex-col overflow-hidden rounded-t-lg bg-superficie shadow-xl sm:rounded-lg ${ancho}`}
      >
        <div className="flex shrink-0 items-center justify-between border-b border-borde px-4 py-3">
          <h2 className="text-base font-semibold text-texto">{titulo}</h2>
          <button
            type="button"
            onClick={onCerrar}
            aria-label="Cerrar"
            className="rounded-md p-1.5 text-texto-suave hover:bg-acento-suave hover:text-texto"
          >
            <X className="size-4" />
          </button>
        </div>
        <div className="overflow-y-auto px-4 py-4">{children}</div>
      </div>
    </div>,
    document.body,
  );
}

const suscribirNada = () => () => {};
