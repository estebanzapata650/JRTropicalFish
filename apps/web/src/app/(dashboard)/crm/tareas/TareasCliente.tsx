"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, CampoAncho, Etiqueta, RejillaCampos } from "@/components/campos";
import {
  ESTADOS_TAREA,
  ETIQUETAS_HISTORIAL_CRM,
  ORDEN_ESTADOS_TAREA,
  PRIORIDADES,
  TIPOS_ACCION,
  tonoPrioridad,
  type Comercial,
  type Tarea,
} from "@/lib/crm";
import { botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { fecha } from "@/lib/formato";
import { cambiarEstadoTareaAction, eliminarTareaAction, guardarTareaAction } from "../actions";
import { Tablero } from "@/components/Tablero";
import { SelectMapa } from "../comun";
import type { ClienteOpcion } from "../pipeline/DialogosOportunidad";

type OportunidadOpcion = { id: string; codigo: string; clienteId: string };

const BORDE_ESTADO: Record<string, string> = {
  pendiente: "border-t-info-texto",
  en_curso: "border-t-acento",
  esperando_cliente: "border-t-advertencia-texto",
  completada: "border-t-exito-texto",
};

function textoDias(t: Tarea) {
  if (t.estado === "completada") return `Completada ${fecha(t.completadaEn?.slice(0, 10))}`;
  if (t.diasRestantes < 0) return `Vencida hace ${-t.diasRestantes} d`;
  if (t.diasRestantes === 0) return "Vence hoy";
  return `Faltan ${t.diasRestantes} d`;
}

/**
 * Checklist diario de los comerciales: Por hacer, En curso, Esperando cliente y Completada. Una
 * tarea vencida y no completada se pinta en rojo. Se mueve arrastrando (escritorio) o con botones.
 */
export function TareasCliente({
  tareas,
  comerciales,
  clientes,
  oportunidades,
  usuarioId,
  puedeGestionar,
  puedeAsignar,
  hoy,
}: {
  tareas: Tarea[];
  comerciales: Comercial[];
  clientes: ClienteOpcion[];
  oportunidades: OportunidadOpcion[];
  usuarioId: string;
  puedeGestionar: boolean;
  puedeAsignar: boolean;
  hoy: string;
}) {
  const router = useRouter();
  const [comercial, setComercial] = useState("");
  const [prioridad, setPrioridad] = useState("");
  const [soloVencidas, setSoloVencidas] = useState(false);
  const [ocultarCompletadas, setOcultarCompletadas] = useState(false);
  const [editando, setEditando] = useState<Tarea | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<Tarea | null>(null);
  const [eliminando, setEliminando] = useState<Tarea | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(
    () =>
      tareas.filter(
        (t) =>
          (!comercial || t.comercialId === comercial) &&
          (!prioridad || t.prioridad === prioridad) &&
          (!soloVencidas || t.vencida) &&
          (!ocultarCompletadas || t.estado !== "completada"),
      ),
    [tareas, comercial, prioridad, soloVencidas, ocultarCompletadas],
  );
  const vencidas = tareas.filter((t) => t.vencida).length;

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setEliminando(null);
    setError(null);
  };

  const mover = (t: Tarea, estado: string) =>
    iniciar(async () => {
      const r = await cambiarEstadoTareaAction(t.id, t.estado, estado);
      setAviso(r.error ?? null);
      if (!r.error) router.refresh();
    });

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      const r = await guardarTareaAction(editando?.id ?? null, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Tareas diarias"
        descripcion="Micro-acciones con fecha límite por cliente u oportunidad."
        acciones={
          puedeGestionar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nueva tarea
            </button>
          ) : null
        }
      />

      <div className="flex flex-col gap-3 md:flex-row md:flex-wrap md:items-center">
        {comerciales.length > 1 ? (
          <select aria-label="Filtrar por comercial" value={comercial} onChange={(e) => setComercial(e.target.value)} className={`${selectClass} md:w-56`}>
            <option value="">Todos los comerciales</option>
            {comerciales.map((c) => (
              <option key={c.id} value={c.id}>
                {c.id === usuarioId ? "Mis tareas" : c.nombre}
              </option>
            ))}
          </select>
        ) : null}
        <select aria-label="Filtrar por prioridad" value={prioridad} onChange={(e) => setPrioridad(e.target.value)} className={`${selectClass} md:w-44`}>
          <option value="">Toda prioridad</option>
          {Object.entries(PRIORIDADES).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
        <button
          type="button"
          aria-pressed={soloVencidas}
          onClick={() => setSoloVencidas((v) => !v)}
          className={`inline-flex h-11 items-center justify-center gap-2 rounded-full border px-4 text-sm ${
            soloVencidas ? "border-peligro bg-peligro/15 font-medium text-peligro" : "border-borde text-peligro hover:bg-acento-suave"
          }`}
        >
          Vencidas <span className="tabular-nums">{vencidas}</span>
        </button>
        <label className="flex items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={ocultarCompletadas} onChange={(e) => setOcultarCompletadas(e.target.checked)} className={casillaClass} />
          Ocultar completadas
        </label>
      </div>

      {aviso ? <p role="status" className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">{aviso}</p> : null}

      <Tablero
        columnas={ORDEN_ESTADOS_TAREA.map((e) => ({ id: e, titulo: ESTADOS_TAREA[e], borde: BORDE_ESTADO[e] }))}
        items={visibles}
        columnaDe={(t) => t.estado}
        idDe={(t) => t.id}
        etiquetaDe={(t) => `${t.accion}, ${t.clienteNombre}${t.vencida ? ", vencida" : ""}`}
        puedeSoltar={() => puedeGestionar}
        onSoltar={mover}
        deshabilitado={!puedeGestionar || procesando}
        resaltar={(t) => t.vencida}
        render={(t) => (
          <div className="flex flex-col gap-2">
            <div className="flex items-start justify-between gap-2">
              <p className="text-sm font-medium text-texto">{t.accion}</p>
              <Etiqueta texto={PRIORIDADES[t.prioridad]} tono={tonoPrioridad(t.prioridad)} />
            </div>
            <p className="text-xs text-texto-suave">
              <Link href={`/crm/clientes/${t.clienteId}`} className="hover:text-acento hover:underline">
                {t.clienteNombre}
              </Link>
              {t.oportunidadCodigo ? ` · ${t.oportunidadCodigo}` : ""} · {TIPOS_ACCION[t.tipoAccion]}
            </p>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs">
              <span className={t.vencida ? "font-semibold text-peligro" : t.diasRestantes <= 1 && t.estado !== "completada" ? "font-medium text-advertencia-texto" : "text-texto-suave"}>
                {textoDias(t)}
              </span>
              <span className="text-texto-suave">Vence {fecha(t.fechaVencimiento)}</span>
              <span className="text-texto-suave">{t.comercialNombre}</span>
            </div>
            <div className="flex flex-wrap items-center gap-1 border-t border-borde pt-2">
              {puedeGestionar ? (
                <select
                  aria-label={`Mover la tarea ${t.accion} a otro estado`}
                  value=""
                  disabled={procesando}
                  onChange={(e) => e.target.value && mover(t, e.target.value)}
                  className="h-9 rounded-md border border-borde bg-superficie px-2 text-xs text-texto"
                >
                  <option value="">Mover a…</option>
                  {ORDEN_ESTADOS_TAREA.filter((e) => e !== t.estado).map((e) => (
                    <option key={e} value={e}>
                      {ESTADOS_TAREA[e]}
                    </option>
                  ))}
                </select>
              ) : null}
              {puedeGestionar && t.estado !== "completada" ? (
                <button
                  type="button"
                  disabled={procesando}
                  onClick={() => mover(t, "completada")}
                  className="inline-flex h-9 items-center rounded-md border border-borde px-3 text-xs font-medium text-exito-texto hover:bg-acento-suave"
                  aria-label={`Completar la tarea ${t.accion}`}
                >
                  ✓ Completar
                </button>
              ) : null}
              <div className="ml-auto flex">
                <BotonIcono accion="otra" icono={History} etiqueta={`Historial de la tarea ${t.accion}`} onClick={() => setHistorialDe(t)} />
                {puedeGestionar ? <BotonIcono accion="editar" etiqueta={`Editar la tarea ${t.accion}`} onClick={() => setEditando(t)} /> : null}
                {puedeGestionar ? <BotonIcono accion="eliminar" etiqueta={`Eliminar la tarea ${t.accion}`} onClick={() => setEliminando(t)} /> : null}
              </div>
            </div>
          </div>
        )}
      />

      <Dialogo titulo={editando ? "Editar tarea" : "Nueva tarea"} abierto={creando || editando !== null} onCerrar={cerrar} ancho="max-w-4xl">
        <FormularioTarea
          key={editando?.id ?? "nueva"}
          tarea={editando}
          clientes={clientes}
          oportunidades={oportunidades}
          comerciales={comerciales}
          puedeAsignar={puedeAsignar}
          hoy={hoy}
          guardando={procesando}
          error={error}
          onGuardar={guardar}
          onCancelar={cerrar}
        />
      </Dialogo>
      <Dialogo titulo="Historial de la tarea" abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? <HistorialRemoto ruta={`/api/crm/tareas/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CRM} /> : null}
      </Dialogo>
      <DialogoConfirmacion
        titulo="Eliminar tarea"
        mensaje={eliminando ? <>¿Eliminar la tarea «{eliminando.accion}»?</> : null}
        textoConfirmar="Eliminar"
        peligro
        abierto={eliminando !== null}
        procesando={procesando}
        error={error}
        onConfirmar={() =>
          eliminando &&
          iniciar(async () => {
            const r = await eliminarTareaAction(eliminando.id);
            if (r.error) {
              setError(r.error);
              return;
            }
            cerrar();
            router.refresh();
          })
        }
        onCerrar={cerrar}
      />
    </div>
  );
}

function FormularioTarea({
  tarea,
  clientes,
  oportunidades,
  comerciales,
  puedeAsignar,
  hoy,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  tarea: Tarea | null;
  clientes: ClienteOpcion[];
  oportunidades: OportunidadOpcion[];
  comerciales: Comercial[];
  puedeAsignar: boolean;
  hoy: string;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [clienteId, setClienteId] = useState(tarea?.clienteId ?? "");
  const [oportunidadId, setOportunidadId] = useState(tarea?.oportunidadId ?? "");
  const [accion, setAccion] = useState(tarea?.accion ?? "");
  const [tipoAccion, setTipoAccion] = useState(tarea?.tipoAccion ?? "prospeccion");
  const [prioridad, setPrioridad] = useState(tarea?.prioridad ?? "media");
  const [estado, setEstado] = useState(tarea?.estado ?? "pendiente");
  const [comercialId, setComercialId] = useState(tarea?.comercialId ?? "");
  const [asignacion, setAsignacion] = useState(tarea?.fechaAsignacion ?? hoy);
  const [vencimiento, setVencimiento] = useState(tarea?.fechaVencimiento ?? "");
  const [notas, setNotas] = useState(tarea?.notas ?? "");

  const opcionesCliente =
    tarea && !clientes.some((c) => c.id === tarea.clienteId)
      ? [...clientes, { id: tarea.clienteId, codigo: tarea.clienteCodigo, nombreEmpresa: tarea.clienteNombre }]
      : clientes;
  const delCliente = oportunidades.filter((o) => o.clienteId === clienteId);
  const opcionesOportunidad =
    tarea?.oportunidadId && tarea.clienteId === clienteId && !delCliente.some((o) => o.id === tarea.oportunidadId)
      ? [...delCliente, { id: tarea.oportunidadId, codigo: tarea.oportunidadCodigo ?? "", clienteId }]
      : delCliente;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          clienteId,
          oportunidadId: oportunidadId || null,
          accion,
          tipoAccion,
          estado,
          prioridad,
          comercialId: puedeAsignar && comercialId ? comercialId : null,
          fechaAsignacion: asignacion || null,
          fechaVencimiento: vencimiento || null,
          notas,
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <CampoAncho>
          <Campo label="Acción comercial" htmlFor="accion">
            <input id="accion" value={accion} onChange={(e) => setAccion(e.target.value)} maxLength={300} placeholder="Ej. Enviar stocklist con fotos de Altums" className={inputClass} />
          </Campo>
        </CampoAncho>
        <Campo label="Cliente" htmlFor="clienteTarea">
          <select
            id="clienteTarea"
            value={clienteId}
            onChange={(e) => {
              setClienteId(e.target.value);
              setOportunidadId("");
            }}
            className={selectClass}
          >
            <option value="">Selecciona…</option>
            {opcionesCliente.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombreEmpresa} ({c.codigo})
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Oportunidad (opcional)" htmlFor="oportunidadTarea">
          <select id="oportunidadTarea" value={oportunidadId} onChange={(e) => setOportunidadId(e.target.value)} disabled={!clienteId} className={selectClass}>
            <option value="">{delCliente.length ? "Ninguna" : "Sin oportunidades abiertas"}</option>
            {opcionesOportunidad.map((o) => (
              <option key={o.id} value={o.id}>
                {o.codigo}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Tipo de acción" htmlFor="tipoAccion">
          <SelectMapa id="tipoAccion" valor={tipoAccion} onChange={setTipoAccion} mapa={TIPOS_ACCION} />
        </Campo>
        <Campo label="Prioridad" htmlFor="prioridad">
          <SelectMapa id="prioridad" valor={prioridad} onChange={setPrioridad} mapa={PRIORIDADES} />
        </Campo>
        <Campo label="Estado" htmlFor="estadoTarea">
          <SelectMapa id="estadoTarea" valor={estado} onChange={setEstado} mapa={ESTADOS_TAREA} />
        </Campo>
        {puedeAsignar ? (
          <Campo label="Comercial asignado" htmlFor="comercialTarea">
            <select id="comercialTarea" value={comercialId} onChange={(e) => setComercialId(e.target.value)} className={selectClass}>
              <option value="">Yo</option>
              {comerciales.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </Campo>
        ) : null}
        <Campo label="Fecha de asignación" htmlFor="asignacion">
          <input id="asignacion" type="date" value={asignacion} onChange={(e) => setAsignacion(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Fecha de vencimiento" htmlFor="vencimiento">
          <input id="vencimiento" type="date" min={asignacion || undefined} value={vencimiento} onChange={(e) => setVencimiento(e.target.value)} className={inputClass} />
        </Campo>
        <CampoAncho>
          <Campo label="Notas de seguimiento (opcional)" htmlFor="notasTarea">
            <textarea id="notasTarea" value={notas} onChange={(e) => setNotas(e.target.value)} maxLength={4000} className={textareaClass} />
          </Campo>
        </CampoAncho>
      </RejillaCampos>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
