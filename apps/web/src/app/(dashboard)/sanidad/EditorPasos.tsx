"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { Plus } from "lucide-react";
import { BotonIcono } from "@/components/campos";
import { botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import { ACCIONES, textoDosis, type Medicamento } from "@/lib/sanidad";

export type PasoEditable = {
  clave: string;
  dia: number;
  accion: string;
  medicamentoId: string;
  porcentajeAgua: string;
  nota: string;
};

export const pasoVacio = (dia = 1): PasoEditable => ({
  clave: crypto.randomUUID(),
  dia,
  accion: "medicar",
  medicamentoId: "",
  porcentajeAgua: "",
  nota: "",
});

/** Lo que la API espera de cada paso. */
export const pasoParaApi = (p: PasoEditable) => ({
  dia: p.dia,
  accion: p.accion,
  medicamentoId: p.accion === "medicar" ? p.medicamentoId || null : null,
  porcentajeAgua:
    p.accion === "cambio_agua" && p.porcentajeAgua
      ? Number(p.porcentajeAgua)
      : null,
  nota: p.nota.trim() || null,
});

/**
 * Pasos de un protocolo o de una prescripción: día, acción, medicamento (con su dosis de referencia) o
 * porcentaje del cambio de agua. En móvil cada paso es una tarjeta; en escritorio, una fila.
 */
export function EditorPasos({
  pasos,
  onChange,
  medicamentos,
  deshabilitado = false,
}: {
  pasos: PasoEditable[];
  onChange: (pasos: PasoEditable[]) => void;
  medicamentos: Medicamento[];
  deshabilitado?: boolean;
}) {
  const activos = medicamentos.filter((m) => m.activo);
  const cambiar = (clave: string, cambio: Partial<PasoEditable>) =>
    onChange(pasos.map((p) => (p.clave === clave ? { ...p, ...cambio } : p)));

  return (
    <div className="flex flex-col gap-2">
      {pasos.length === 0 ? (
        <p className="text-sm text-texto-suave">Sin pasos.</p>
      ) : null}
      {pasos.map((p, i) => {
        const medicamento = activos.find((m) => m.id === p.medicamentoId);
        return (
          <div
            key={p.clave}
            className="grid grid-cols-[5rem_1fr_auto] items-start gap-2 rounded-md border border-borde p-2 md:grid-cols-[5rem_11rem_1fr_1fr_auto]"
          >
            <label className="flex flex-col gap-1 text-xs text-texto-suave">
              Día
              <input
                type="number"
                min={1}
                max={120}
                inputMode="numeric"
                aria-label={`Día del paso ${i + 1}`}
                className={inputClass}
                value={p.dia}
                disabled={deshabilitado}
                onChange={(e) =>
                  cambiar(p.clave, {
                    dia: Math.max(1, Number(e.target.value) || 1),
                  })
                }
              />
            </label>
            <label className="flex flex-col gap-1 text-xs text-texto-suave">
              Acción
              <select
                aria-label={`Acción del paso ${i + 1}`}
                value={p.accion}
                onChange={(e) => cambiar(p.clave, { accion: e.target.value })}
                className={selectClass}
                disabled={deshabilitado}
              >
                {Object.entries(ACCIONES).map(([v, t]) => (
                  <option key={v} value={v}>
                    {t}
                  </option>
                ))}
              </select>
            </label>
            <div className="col-start-3 row-start-1 self-end md:col-start-5">
              {!deshabilitado ? (
                <BotonIcono
                  accion="eliminar"
                  etiqueta={`Quitar el paso ${i + 1}`}
                  onClick={() =>
                    onChange(pasos.filter((x) => x.clave !== p.clave))
                  }
                />
              ) : null}
            </div>
            <div className="col-span-3 flex flex-col gap-1 md:col-span-1 md:col-start-3 md:row-start-1">
              {p.accion === "medicar" ? (
                <label className="flex flex-col gap-1 text-xs text-texto-suave">
                  Medicamento
                  <SelectorBuscable
                    etiqueta={`Medicamento del paso ${i + 1}`}
                    opciones={activos.map((m) => ({
                      id: m.id,
                      nombre: m.nombre,
                    }))}
                    valor={p.medicamentoId}
                    onChange={(v) => cambiar(p.clave, { medicamentoId: v })}
                    disabled={deshabilitado}
                    placeholder="Escribe para buscar…"
                  />
                  {medicamento ? (
                    <span className="text-xs text-texto-suave">
                      {textoDosis(medicamento)}
                    </span>
                  ) : null}
                </label>
              ) : p.accion === "cambio_agua" ? (
                <label className="flex flex-col gap-1 text-xs text-texto-suave">
                  Porcentaje
                  <input
                    type="number"
                    min={1}
                    max={100}
                    inputMode="numeric"
                    aria-label={`Porcentaje de agua del paso ${i + 1}`}
                    className={inputClass}
                    value={p.porcentajeAgua}
                    disabled={deshabilitado}
                    onChange={(e) =>
                      cambiar(p.clave, { porcentajeAgua: e.target.value })
                    }
                  />
                </label>
              ) : null}
            </div>
            <label className="col-span-3 flex flex-col gap-1 text-xs text-texto-suave md:col-span-1 md:col-start-4 md:row-start-1">
              Nota
              <input
                aria-label={`Nota del paso ${i + 1}`}
                className={inputClass}
                value={p.nota}
                maxLength={500}
                disabled={deshabilitado}
                onChange={(e) => cambiar(p.clave, { nota: e.target.value })}
              />
            </label>
          </div>
        );
      })}
      {!deshabilitado ? (
        <button
          type="button"
          className={`${botonSecundarioClass} self-start`}
          onClick={() =>
            onChange([
              ...pasos,
              pasoVacio(
                pasos.length ? Math.max(...pasos.map((p) => p.dia)) : 1,
              ),
            ])
          }
        >
          <Plus className="mr-1 size-4" aria-hidden /> Agregar paso
        </button>
      ) : null}
    </div>
  );
}
