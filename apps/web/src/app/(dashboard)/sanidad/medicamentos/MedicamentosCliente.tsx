"use client";

import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { BotonIcono, Campo, CampoAncho, Casilla, EncabezadoTabla, Etiqueta, EtiquetaEstado, RejillaCampos, TablaResponsive } from "@/components/campos";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { botonPrimarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { TIPOS_DOSIS, UNIDADES_MEDICAMENTO, textoDosis, type Medicamento } from "@/lib/sanidad";
import { guardarMedicamentoAction } from "../actions";

/**
 * Catálogo de medicamentos con su dosis de referencia y de dónde salió. La dosis de cada acuario se calcula
 * con su litraje; el tiempo de retiro (días sin despachar después del tratamiento) lo fija el veterinario.
 */
export function MedicamentosCliente({ medicamentos, gestionar }: { medicamentos: Medicamento[]; gestionar: boolean }) {
  const [editando, setEditando] = useState<Medicamento | "nuevo" | null>(null);
  const [historial, setHistorial] = useState<Medicamento | null>(null);

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        titulo="Medicamentos"
        descripcion="Dosis de referencia de los reportes del veterinario. Cada reporte vale para el grupo tratado: los protocolos los aprueba el veterinario."
        acciones={
          gestionar ? (
            <button type="button" className={botonPrimarioClass} onClick={() => setEditando("nuevo")}>
              <Plus className="mr-1 size-4" aria-hidden /> Nuevo
            </button>
          ) : null
        }
      />
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr>
            <th className="px-3 py-2 font-medium">Medicamento</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Dosis de referencia</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Ayuno</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Retiro</th>
            <th className="hidden px-3 py-2 font-medium xl:table-cell">Fuente</th>
            <th className="px-3 py-2 font-medium">Estado</th>
            <th className="px-3 py-2">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {medicamentos.map((m) => (
            <tr key={m.id} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-2">
                <span className="font-medium text-texto">{m.nombre}</span>
                <span className="block text-xs text-texto-suave">{m.codigo}</span>
                <span className="block text-xs text-texto-suave md:hidden">{textoDosis(m)}</span>
              </td>
              <td className="hidden px-3 py-2 md:table-cell">
                {textoDosis(m)}
                {m.exposicionMinutos ? <span className="block text-xs text-texto-suave">Baño de {m.exposicionMinutos} min</span> : null}
              </td>
              <td className="hidden px-3 py-2 lg:table-cell">{m.requiereAyuno ? "Sí" : "No"}</td>
              <td className="hidden px-3 py-2 lg:table-cell">{m.diasRetiro ? `${m.diasRetiro} días` : <Etiqueta texto="Sin definir" tono="advertencia" />}</td>
              <td className="hidden max-w-64 px-3 py-2 text-xs text-texto-suave xl:table-cell">{m.fuente ?? "—"}</td>
              <td className="px-3 py-2">
                <EtiquetaEstado activo={m.activo} />
              </td>
              <td className="px-3 py-2 text-right whitespace-nowrap">
                <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${m.nombre}`} onClick={() => setHistorial(m)} />
                {gestionar ? <BotonIcono accion="editar" etiqueta={`Editar ${m.nombre}`} onClick={() => setEditando(m)} /> : null}
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo={editando === "nuevo" ? "Nuevo medicamento" : `Editar ${editando?.nombre ?? ""}`} abierto={editando !== null} onCerrar={() => setEditando(null)} ancho="max-w-3xl">
        {editando ? (
          <FormMedicamento
            key={editando === "nuevo" ? "nuevo" : editando.id}
            medicamento={editando === "nuevo" ? null : editando}
            todos={medicamentos}
            onGuardado={() => setEditando(null)}
          />
        ) : null}
      </Dialogo>
      <Dialogo titulo={`Historial de ${historial?.nombre ?? ""}`} abierto={historial !== null} onCerrar={() => setHistorial(null)}>
        {historial ? <HistorialRemoto ruta={`/api/sanidad/historial/Medicamento/${historial.id}`} etiquetas={{}} /> : null}
      </Dialogo>
    </div>
  );
}

function FormMedicamento({ medicamento, todos, onGuardado }: { medicamento: Medicamento | null; todos: Medicamento[]; onGuardado: () => void }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const m = medicamento;
  const [f, setF] = useState({
    codigo: m?.codigo ?? "",
    nombre: m?.nombre ?? "",
    principioActivo: m?.principioActivo ?? "",
    presentacion: m?.presentacion ?? "",
    unidad: m?.unidad ?? "ml",
    tipoDosis: m?.tipoDosis ?? "por_volumen",
    dosisCantidad: m ? String(m.dosisCantidad) : "",
    dosisMaxima: m?.dosisMaxima ? String(m.dosisMaxima) : "",
    dosisBase: m ? String(m.dosisBase) : "100",
    exposicionMinutos: m?.exposicionMinutos ? String(m.exposicionMinutos) : "",
    requiereAyuno: m?.requiereAyuno ?? true,
    diasRetiro: m ? String(m.diasRetiro) : "0",
    indicaciones: m?.indicaciones ?? "",
    contraindicaciones: m?.contraindicaciones ?? "",
    fuente: m?.fuente ?? "",
    activo: m?.activo ?? true,
    incompatibles: m?.incompatibles ?? [],
  });
  const set = (cambio: Partial<typeof f>) => setF((x) => ({ ...x, ...cambio }));
  const decimal = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));

  const enviar = () =>
    iniciar(async () => {
      setError(null);
      const r = await guardarMedicamentoAction(m?.id ?? null, {
        ...f,
        dosisCantidad: decimal(f.dosisCantidad) ?? 0,
        dosisMaxima: decimal(f.dosisMaxima),
        dosisBase: decimal(f.dosisBase) ?? 0,
        exposicionMinutos: f.exposicionMinutos ? Number(f.exposicionMinutos) : null,
        diasRetiro: Number(f.diasRetiro) || 0,
      });
      if (r.error) setError(r.error);
      else {
        onGuardado();
        router.refresh();
      }
    });

  const baseLabel = f.tipoDosis === "por_alimento" ? "Por cada (g de alimento)" : f.tipoDosis === "fija" ? "Base (no aplica)" : "Por cada (litros)";

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        enviar();
      }}
    >
      <RejillaCampos>
        <Campo label="Código" htmlFor="med-codigo">
          <input id="med-codigo" className={inputClass} value={f.codigo} onChange={(e) => set({ codigo: e.target.value })} maxLength={20} required />
        </Campo>
        <Campo label="Nombre" htmlFor="med-nombre">
          <input id="med-nombre" className={inputClass} value={f.nombre} onChange={(e) => set({ nombre: e.target.value })} maxLength={120} required />
        </Campo>
        <Campo label="Principio activo" htmlFor="med-principio">
          <input id="med-principio" className={inputClass} value={f.principioActivo} onChange={(e) => set({ principioActivo: e.target.value })} maxLength={120} />
        </Campo>
        <Campo label="Presentación" htmlFor="med-presentacion">
          <input id="med-presentacion" className={inputClass} value={f.presentacion} onChange={(e) => set({ presentacion: e.target.value })} maxLength={120} />
        </Campo>
        <Campo label="Cómo se dosifica" htmlFor="med-tipo">
          <select id="med-tipo" className={selectClass} value={f.tipoDosis} onChange={(e) => set({ tipoDosis: e.target.value })}>
            {Object.entries(TIPOS_DOSIS).map(([v, t]) => (
              <option key={v} value={v}>
                {t}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Unidad" htmlFor="med-unidad">
          <select id="med-unidad" className={selectClass} value={f.unidad} onChange={(e) => set({ unidad: e.target.value })}>
            {Object.entries(UNIDADES_MEDICAMENTO).map(([v, t]) => (
              <option key={v} value={v}>
                {t}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Dosis" htmlFor="med-dosis" hint="En un rango, el mínimo">
          <input id="med-dosis" inputMode="decimal" className={inputClass} value={f.dosisCantidad} onChange={(e) => set({ dosisCantidad: e.target.value })} required />
        </Campo>
        <Campo label="Dosis máxima" htmlFor="med-maxima" hint="Solo si es un rango (2,5–5 mg/L)">
          <input id="med-maxima" inputMode="decimal" className={inputClass} value={f.dosisMaxima} onChange={(e) => set({ dosisMaxima: e.target.value })} />
        </Campo>
        <Campo label={baseLabel} htmlFor="med-base">
          <input id="med-base" inputMode="decimal" className={inputClass} value={f.dosisBase} onChange={(e) => set({ dosisBase: e.target.value })} required />
        </Campo>
        <Campo label="Exposición (minutos)" htmlFor="med-exposicion" hint="Baños cortos; vacío = prolongado">
          <input id="med-exposicion" type="number" min={1} className={inputClass} value={f.exposicionMinutos} onChange={(e) => set({ exposicionMinutos: e.target.value })} />
        </Campo>
        <Campo label="Tiempo de retiro (días)" htmlFor="med-retiro" hint="Días sin despachar después del tratamiento">
          <input id="med-retiro" type="number" min={0} max={365} className={inputClass} value={f.diasRetiro} onChange={(e) => set({ diasRetiro: e.target.value })} />
        </Campo>
        <div className="flex flex-col justify-end">
          <Casilla label="No alimentar durante el tratamiento" checked={f.requiereAyuno} onChange={(v) => set({ requiereAyuno: v })} />
          <Casilla label="Activo" checked={f.activo} onChange={(v) => set({ activo: v })} />
        </div>
        <CampoAncho>
          <Campo label="Indicaciones" htmlFor="med-indicaciones">
            <textarea id="med-indicaciones" className={textareaClass} value={f.indicaciones} onChange={(e) => set({ indicaciones: e.target.value })} maxLength={1000} />
          </Campo>
        </CampoAncho>
        <CampoAncho>
          <Campo label="Contraindicaciones" htmlFor="med-contra">
            <textarea id="med-contra" className={textareaClass} value={f.contraindicaciones} onChange={(e) => set({ contraindicaciones: e.target.value })} maxLength={1000} />
          </Campo>
        </CampoAncho>
        <CampoAncho>
          <Campo label="Fuente de la dosis" htmlFor="med-fuente" hint="Reporte del veterinario, ficha del fabricante…">
            <input id="med-fuente" className={inputClass} value={f.fuente} onChange={(e) => set({ fuente: e.target.value })} maxLength={300} />
          </Campo>
        </CampoAncho>
        <CampoAncho>
          <fieldset className="flex flex-col gap-1.5">
            <legend className="text-sm font-medium text-texto">No combinar con</legend>
            <div className="grid grid-cols-1 gap-x-4 sm:grid-cols-2 lg:grid-cols-3">
              {todos
                .filter((x) => x.id !== m?.id)
                .map((x) => (
                  <Casilla
                    key={x.id}
                    label={x.nombre}
                    checked={f.incompatibles.includes(x.id)}
                    onChange={(v) => set({ incompatibles: v ? [...f.incompatibles, x.id] : f.incompatibles.filter((i) => i !== x.id) })}
                  />
                ))}
            </div>
          </fieldset>
        </CampoAncho>
      </RejillaCampos>
      {error ? <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">{error}</p> : null}
      <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
        Guardar
      </button>
    </form>
  );
}
