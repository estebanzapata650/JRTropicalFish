"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState, useTransition } from "react";
import { Eye, FileText, History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { BotonIcono, Campo, CampoAncho, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import {
  ACCIONES,
  ESTADOS_TRATAMIENTO,
  MOTIVOS_TRATAMIENTO,
  fechaCorta,
  horaCorta,
  num,
  unidad,
  type Aplicacion,
  type Medicamento,
  type Protocolo,
  type Tratamiento,
} from "@/lib/sanidad";
import { aplicarAction, cambiarEstadoTratamientoAction, crearTratamientoAction, omitirAction, subirReporteAction } from "../actions";
import { EditorPasos, pasoParaApi, pasoVacio, type PasoEditable } from "../EditorPasos";

type AcuarioOpcion = { id: string; codigo: string; litros: number | null };

const tonoEstado = (e: string) => (e === "activo" ? "advertencia" : e === "terminado" ? "exito" : "neutro");

export function TratamientosCliente({
  tratamientos,
  protocolos,
  medicamentos,
  acuarios,
  abiertoId,
  todos,
  operar,
  gestionar,
}: {
  tratamientos: Tratamiento[];
  protocolos: Protocolo[];
  medicamentos: Medicamento[];
  acuarios: AcuarioOpcion[];
  abiertoId: string | null;
  todos: boolean;
  operar: boolean;
  gestionar: boolean;
}) {
  const router = useRouter();
  const [abierto, setAbierto] = useState<string | null>(abiertoId);
  const [creando, setCreando] = useState(false);

  const cerrar = () => {
    setAbierto(null);
    router.replace(todos ? "/sanidad/tratamientos?todos=1" : "/sanidad/tratamientos", { scroll: false });
  };

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        titulo="Tratamientos"
        descripcion="Medicación y cuarentena por acuario. Mientras dure el tratamiento y su tiempo de retiro, el acuario no se reserva ni se despacha."
        acciones={
          operar ? (
            <button type="button" className={botonPrimarioClass} onClick={() => setCreando(true)}>
              <Plus className="mr-1 size-4" aria-hidden /> Nuevo tratamiento
            </button>
          ) : null
        }
      />
      <div className="flex gap-2">
        <Link href="/sanidad/tratamientos" className={`${botonSecundarioClass} ${todos ? "" : "bg-acento-suave"}`} aria-current={!todos}>
          Vigentes
        </Link>
        <Link href="/sanidad/tratamientos?todos=1" className={`${botonSecundarioClass} ${todos ? "bg-acento-suave" : ""}`} aria-current={todos}>
          Todos
        </Link>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr>
            <th className="px-3 py-2 font-medium">Acuario</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Código</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Protocolo / motivo</th>
            <th className="px-3 py-2 font-medium">Avance</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Libera</th>
            <th className="px-3 py-2 font-medium">Estado</th>
            <th className="px-3 py-2">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {tratamientos.length === 0 ? (
            <tr>
              <td colSpan={7} className="px-3 py-8 text-center text-texto-suave">
                No hay tratamientos {todos ? "" : "vigentes"}.
              </td>
            </tr>
          ) : null}
          {tratamientos.map((t) => (
            <tr key={t.id} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="font-semibold text-texto">{t.acuarioCodigo}</span>
                <span className="block text-xs text-texto-suave md:hidden">{t.codigo}</span>
              </td>
              <td className="hidden px-3 py-2 text-texto-suave md:table-cell">{t.codigo}</td>
              <td className="hidden px-3 py-2 md:table-cell">{t.protocoloNombre ?? MOTIVOS_TRATAMIENTO[t.motivo]}</td>
              <td className="px-3 py-2 tabular-nums">
                {t.registradas}/{t.aplicaciones}
              </td>
              <td className="hidden px-3 py-2 md:table-cell">{fechaCorta(t.fechaLiberacion)}</td>
              <td className="px-3 py-2">
                <Etiqueta texto={ESTADOS_TRATAMIENTO[t.estado]} tono={tonoEstado(t.estado)} />
              </td>
              <td className="px-3 py-2 text-right">
                <BotonIcono accion="otra" icono={Eye} etiqueta={`Ver ${t.codigo} de ${t.acuarioCodigo}`} onClick={() => setAbierto(t.id)} />
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo="Ficha del tratamiento" abierto={abierto !== null} onCerrar={cerrar} ancho="max-w-3xl">
        {abierto ? <FichaTratamiento id={abierto} operar={operar} gestionar={gestionar} /> : null}
      </Dialogo>

      <Dialogo titulo="Nuevo tratamiento" abierto={creando} onCerrar={() => setCreando(false)} ancho="max-w-3xl">
        {creando ? (
          <NuevoTratamiento
            protocolos={protocolos}
            medicamentos={medicamentos}
            acuarios={acuarios}
            gestionar={gestionar}
            onCreado={(id) => {
              setCreando(false);
              setAbierto(id);
              router.refresh();
            }}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

type Detalle = { tratamiento: Tratamiento; aplicaciones: Aplicacion[] };

function FichaTratamiento({ id, operar, gestionar }: { id: string; operar: boolean; gestionar: boolean }) {
  const router = useRouter();
  const [estado, setEstado] = useState<{ id: string; version: number; detalle?: Detalle; error?: string } | null>(null);
  const [version, setVersion] = useState(0);
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{ tipo: "ok" | "error"; texto: string } | null>(null);
  const [historial, setHistorial] = useState(false);
  const [omitir, setOmitir] = useState<string | null>(null);
  const [nota, setNota] = useState("");

  useEffect(() => {
    let vigente = true;
    fetch(`/api/sanidad/tratamientos/${id}`, { cache: "no-store" })
      .then(async (r) => {
        if (!r.ok) throw new Error(((await r.json().catch(() => ({}))) as { error?: string }).error ?? `Error ${r.status}`);
        return (await r.json()) as Detalle;
      })
      .then((detalle) => vigente && setEstado({ id, version, detalle }))
      .catch((e: Error) => vigente && setEstado({ id, version, error: e.message }));
    return () => {
      vigente = false;
    };
  }, [id, version]);

  const correr = (fn: () => Promise<{ error?: string }>, exito: string) =>
    iniciar(async () => {
      const r = await fn();
      setMensaje(r.error ? { tipo: "error", texto: r.error } : { tipo: "ok", texto: exito });
      if (!r.error) {
        setVersion((v) => v + 1);
        router.refresh();
      }
    });

  if (!estado || estado.id !== id || estado.version !== version) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (estado.error || !estado.detalle) return <p className="text-sm text-peligro">{estado.error}</p>;
  const { tratamiento: t, aplicaciones } = estado.detalle;
  const activo = t.estado === "activo";
  const faltan = aplicaciones.filter((a) => a.estado === "pendiente").length;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="text-lg font-semibold text-texto">
            {t.acuarioCodigo} · {t.codigo}
          </p>
          <p className="text-sm text-texto-suave">
            {t.protocoloNombre ?? MOTIVOS_TRATAMIENTO[t.motivo]} · {fechaCorta(t.fechaInicio)} a {fechaCorta(t.fechaFin)} · {t.litros ? `${num(t.litros, 0)} L` : "sin litraje"}
          </p>
        </div>
        <Etiqueta texto={ESTADOS_TRATAMIENTO[t.estado]} tono={tonoEstado(t.estado)} />
      </div>
      <p className="rounded-md bg-info-fondo px-3 py-2 text-sm text-info-texto">
        Se puede reservar y despachar desde el <strong>{fechaCorta(t.fechaLiberacion)}</strong>
        {activo ? " (si se termina a tiempo)" : ""}.
      </p>
      {t.diagnostico ? <p className="text-sm text-texto">Diagnóstico: {t.diagnostico}</p> : null}
      {mensaje ? (
        <p role="status" className={`rounded-md px-3 py-2 text-sm ${mensaje.tipo === "ok" ? "bg-exito-fondo text-exito-texto" : "bg-peligro/15 text-peligro"}`}>
          {mensaje.texto}
        </p>
      ) : null}

      <ul className="flex flex-col gap-2">
        {aplicaciones.map((a) => (
          <li key={a.id} className="flex flex-col gap-2 rounded-md border border-borde p-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex min-w-0 flex-col gap-0.5">
              <p className="text-xs text-texto-suave">
                Día {a.dia} · {fechaCorta(a.fecha)}
              </p>
              <p className="font-medium text-texto">
                {a.accion === "medicar" ? a.medicamentoNombre : ACCIONES[a.accion]}
                {a.porcentajeAgua ? ` ${a.porcentajeAgua} %` : ""}
                {a.accion === "medicar" ? (
                  <span className="ml-2 tabular-nums">{a.cantidadCalculada !== null ? `${num(a.cantidadCalculada)} ${unidad(a.unidad)}` : "dosis sin calcular"}</span>
                ) : null}
              </p>
              {a.nota ? <p className="text-xs text-texto-suave">{a.nota}</p> : null}
              {a.estado !== "pendiente" ? (
                <p className="text-xs text-texto-suave">
                  {a.estado === "aplicada" ? "Aplicado" : "No se hizo"}
                  {a.cantidadAplicada !== null ? ` (${num(a.cantidadAplicada)} ${unidad(a.unidad)})` : ""} · {a.usuarioNombre ?? "—"} · {horaCorta(a.registradoEn)}
                </p>
              ) : null}
            </div>
            {a.estado === "pendiente" && activo && operar ? (
              <div className="flex shrink-0 gap-2">
                <button type="button" disabled={pendiente} className={botonPrimarioClass} onClick={() => correr(() => aplicarAction(a.id, a.cantidadCalculada), "Registrado.")}>
                  {a.accion === "medicar" ? "Aplicado" : "Hecho"}
                </button>
                <button type="button" disabled={pendiente} className={botonSecundarioClass} onClick={() => setOmitir(a.id)}>
                  No se hizo
                </button>
              </div>
            ) : (
              <Etiqueta texto={a.estado === "aplicada" ? "Hecho" : a.estado === "omitida" ? "No se hizo" : "Pendiente"} tono={a.estado === "aplicada" ? "exito" : a.estado === "omitida" ? "neutro" : "advertencia"} />
            )}
          </li>
        ))}
      </ul>

      {omitir ? (
        <form
          className="flex flex-col gap-2 rounded-md border border-borde p-3"
          onSubmit={(e) => {
            e.preventDefault();
            const aplicacion = omitir;
            setOmitir(null);
            correr(() => omitirAction(aplicacion, nota), "Registrado como no hecho.");
            setNota("");
          }}
        >
          <Campo label="¿Por qué no se hizo?" htmlFor="nota-omitir-ficha">
            <textarea id="nota-omitir-ficha" className={textareaClass} value={nota} onChange={(e) => setNota(e.target.value)} required maxLength={500} />
          </Campo>
          <div className="flex gap-2">
            <button type="submit" disabled={!nota.trim() || pendiente} className={botonPrimarioClass}>
              Registrar
            </button>
            <button type="button" className={botonSecundarioClass} onClick={() => setOmitir(null)}>
              Cancelar
            </button>
          </div>
        </form>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {activo && operar ? (
          <button
            type="button"
            disabled={pendiente || faltan > 0}
            title={faltan > 0 ? `Faltan ${faltan} por registrar` : undefined}
            className={botonPrimarioClass}
            onClick={() => correr(() => cambiarEstadoTratamientoAction(t.id, "activo", "terminado"), "Tratamiento terminado.")}
          >
            Terminar tratamiento
          </button>
        ) : null}
        {activo && operar && aplicaciones.every((a) => a.estado !== "aplicada") ? (
          <button type="button" disabled={pendiente} className={botonSecundarioClass} onClick={() => correr(() => cambiarEstadoTratamientoAction(t.id, "activo", "cancelado"), "Tratamiento cancelado.")}>
            Cancelar tratamiento
          </button>
        ) : null}
        {t.tieneReporte ? (
          <a href={`/archivos/sanidad/tratamientos/${t.id}/reporte`} target="_blank" rel="noopener" className={botonSecundarioClass}>
            <FileText className="mr-1 size-4" aria-hidden /> Reporte del veterinario
          </a>
        ) : null}
        <button type="button" className={botonSecundarioClass} onClick={() => setHistorial((v) => !v)}>
          <History className="mr-1 size-4" aria-hidden /> Historial
        </button>
      </div>

      {gestionar ? (
        <form
          action={(formData) => correr(() => subirReporteAction(t.id, formData), "Reporte adjuntado.")}
          className="flex flex-col gap-2 rounded-md border border-dashed border-borde p-3 sm:flex-row sm:items-end"
        >
          <Campo label={t.tieneReporte ? "Reemplazar el reporte del veterinario (PDF)" : "Adjuntar el reporte del veterinario (PDF)"} htmlFor="reporte-pdf">
            <input id="reporte-pdf" name="archivo" type="file" accept="application/pdf" className="text-sm text-texto" required />
          </Campo>
          <button type="submit" disabled={pendiente} className={botonSecundarioClass}>
            Subir
          </button>
        </form>
      ) : null}

      {historial ? <HistorialRemoto ruta={`/api/sanidad/historial/Tratamiento/${t.id}`} etiquetas={{}} /> : null}
    </div>
  );
}

function NuevoTratamiento({
  protocolos,
  medicamentos,
  acuarios,
  gestionar,
  onCreado,
}: {
  protocolos: Protocolo[];
  medicamentos: Medicamento[];
  acuarios: AcuarioOpcion[];
  gestionar: boolean;
  onCreado: (id: string) => void;
}) {
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [avisos, setAvisos] = useState<string[]>([]);
  const [creado, setCreado] = useState<string | null>(null);
  const [acuarioId, setAcuarioId] = useState("");
  const [origen, setOrigen] = useState(protocolos.length > 0 ? protocolos[0].id : "manual");
  const [motivo, setMotivo] = useState("prescripcion");
  const [inicio, setInicio] = useState("");
  const [dias, setDias] = useState("");
  const [diagnostico, setDiagnostico] = useState("");
  const [nota, setNota] = useState("");
  const [pasos, setPasos] = useState<PasoEditable[]>([pasoVacio()]);
  const manual = origen === "manual";
  const acuario = acuarios.find((a) => a.id === acuarioId);

  const enviar = () =>
    iniciar(async () => {
      setError(null);
      const r = await crearTratamientoAction({
        acuarioId,
        protocoloId: manual ? null : origen,
        motivo: manual ? motivo : null,
        diagnostico: diagnostico.trim() || null,
        fechaInicio: inicio || null,
        dias: manual && dias ? Number(dias) : null,
        pasos: manual ? pasos.map(pasoParaApi) : null,
        nota: nota.trim() || null,
      });
      if (r.error) {
        setError(r.error);
        return;
      }
      if (!r.datos?.id) return;
      // Con avisos (sin litraje, incompatibles) el formulario se queda abierto para que se lean.
      if (r.datos.avisos?.length) {
        setAvisos(r.datos.avisos);
        setCreado(r.datos.id);
      } else onCreado(r.datos.id);
    });

  if (creado)
    return (
      <div className="flex flex-col gap-4">
        <p className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">Tratamiento iniciado.</p>
        {avisos.map((a) => (
          <p key={a} className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
            {a}
          </p>
        ))}
        <button type="button" className={botonPrimarioClass} onClick={() => onCreado(creado)}>
          Ver el tratamiento
        </button>
      </div>
    );

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        enviar();
      }}
    >
      <RejillaCampos>
        <Campo label="Acuario" htmlFor="trat-acuario" hint={acuario ? (acuario.litros ? `${num(acuario.litros, 0)} L: base de las dosis` : "Sin litraje: las dosis quedarán sin calcular") : undefined}>
          <select id="trat-acuario" className={selectClass} value={acuarioId} onChange={(e) => setAcuarioId(e.target.value)} required>
            <option value="">Elige…</option>
            {acuarios.map((a) => (
              <option key={a.id} value={a.id}>
                {a.codigo}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Protocolo" htmlFor="trat-origen" hint={manual ? "Prescripción del veterinario: escribe sus pasos." : "Solo aparecen protocolos aprobados."}>
          <select id="trat-origen" className={selectClass} value={origen} onChange={(e) => setOrigen(e.target.value)}>
            {protocolos.map((p) => (
              <option key={p.id} value={p.id}>
                {p.codigo} · {p.nombre}
              </option>
            ))}
            {gestionar ? <option value="manual">Prescripción manual (sin protocolo)</option> : null}
          </select>
        </Campo>
        <Campo label="Inicio" htmlFor="trat-inicio" hint="Vacío = hoy">
          <input id="trat-inicio" type="date" className={inputClass} value={inicio} onChange={(e) => setInicio(e.target.value)} />
        </Campo>
        {manual ? (
          <>
            <Campo label="Motivo" htmlFor="trat-motivo">
              <select id="trat-motivo" className={selectClass} value={motivo} onChange={(e) => setMotivo(e.target.value)}>
                {Object.entries(MOTIVOS_TRATAMIENTO).map(([v, t]) => (
                  <option key={v} value={v}>
                    {t}
                  </option>
                ))}
              </select>
            </Campo>
            <Campo label="Días" htmlFor="trat-dias" hint="Vacío = hasta el último paso">
              <input id="trat-dias" type="number" min={1} max={120} inputMode="numeric" className={inputClass} value={dias} onChange={(e) => setDias(e.target.value)} />
            </Campo>
          </>
        ) : null}
        <CampoAncho>
          <Campo label="Diagnóstico" htmlFor="trat-diagnostico">
            <input id="trat-diagnostico" className={inputClass} value={diagnostico} onChange={(e) => setDiagnostico(e.target.value)} maxLength={1000} placeholder="Ej. Camallanus cotti (reporte AQUAVET)" />
          </Campo>
        </CampoAncho>
        <CampoAncho>
          <Campo label="Nota" htmlFor="trat-nota">
            <textarea id="trat-nota" className={textareaClass} value={nota} onChange={(e) => setNota(e.target.value)} maxLength={1000} />
          </Campo>
        </CampoAncho>
      </RejillaCampos>

      {manual ? (
        <section className="flex flex-col gap-2">
          <h3 className="text-sm font-semibold text-texto">Pasos de la prescripción</h3>
          <EditorPasos pasos={pasos} onChange={setPasos} medicamentos={medicamentos} />
        </section>
      ) : null}

      {error ? <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">{error}</p> : null}
      <button type="submit" disabled={pendiente || !acuarioId} className={botonPrimarioClass}>
        Iniciar tratamiento
      </button>
    </form>
  );
}
