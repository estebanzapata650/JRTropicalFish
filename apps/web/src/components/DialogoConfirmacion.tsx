"use client";

import { botonPeligroClass, botonPrimarioClass, botonSecundarioClass } from "@/lib/estilos";
import { Dialogo } from "./Dialogo";

/**
 * Confirmación de una acción sensible (desactivar, eliminar). Reemplaza a `window.confirm`, que no
 * sigue el tema, no se puede estilizar y en móvil se ve como un error del navegador.
 */
export function DialogoConfirmacion({
  titulo,
  mensaje,
  textoConfirmar,
  peligro = false,
  abierto,
  procesando,
  error,
  onConfirmar,
  onCerrar,
}: {
  titulo: string;
  mensaje: React.ReactNode;
  textoConfirmar: string;
  peligro?: boolean;
  abierto: boolean;
  procesando?: boolean;
  error?: string | null;
  onConfirmar: () => void;
  onCerrar: () => void;
}) {
  return (
    <Dialogo titulo={titulo} abierto={abierto} onCerrar={onCerrar} ancho="max-w-md">
      <div className="flex flex-col gap-4">
        <div className="text-sm text-texto">{mensaje}</div>
        {error ? <p className="text-sm text-peligro">{error}</p> : null}
        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button type="button" onClick={onCerrar} className={botonSecundarioClass}>
            Cancelar
          </button>
          <button
            type="button"
            onClick={onConfirmar}
            disabled={procesando}
            className={peligro ? botonPeligroClass : botonPrimarioClass}
          >
            {procesando ? "Procesando…" : textoConfirmar}
          </button>
        </div>
      </div>
    </Dialogo>
  );
}
