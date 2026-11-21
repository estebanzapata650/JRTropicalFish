"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { AlertTriangle, History, Plane, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, CampoAncho, RejillaCampos } from "@/components/campos";
import { ETIQUETAS_HISTORIAL_CRM, FASES_TRACKING, ORDEN_FASES, porcentajeDoa, type Tracking } from "@/lib/crm";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import { dinero, fecha } from "@/lib/formato";
import { cambiarFaseAction, guardarTrackingAction } from "../actions";
import { SelectMapa } from "../comun";

const pct = (v: number | null) => (v == null ? "—" : `${v.toLocaleString("es-CO", { maximumFractionDigits: 2 })} %`);
const numero = (texto: string) => (texto.trim() === "" ? 0 : Number(texto.replace(",", ".")));

/**
 * Torre de control: cada despacho de exportación con sus 6 fases logísticas y el control DOA.
 * Un despacho con % DOA por encima del umbral se marca en rojo.
 */
export function VuelosCliente({
  despachos,
  umbralDoa,
  puedeGestionar,
}: {
  despachos: Tracking[];
  umbralDoa: number;
  puedeGestionar: boolean;
}) {
  const router = useRouter();
  const [fase, setFase] = useState("");
  const [soloAlertas, setSoloAlertas] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const [editando, setEditando] = useState<Tracking | null>(null);
  const [historialDe, setHistorialDe] = useState<Tracking | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const enAlerta = (t: Tracking) => t.porcentajeDoa != null && t.porcentajeDoa > umbralDoa;
  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return despachos.filter(
      (t) =>
        (!fase || t.fase === fase) &&
        (!soloAlertas || (t.porcentajeDoa != null && t.porcentajeDoa > umbralDoa)) &&
        (!texto || [t.codigo, t.ventaCodigo, t.clienteNombre, t.noAwb, t.aerolinea, t.nFacturaInvoice].some((x) => x?.toLowerCase().includes(texto))),
    );
  }, [despachos, fase, soloAlertas, busqueda, umbralDoa]);

  const alertas = despachos.filter(enAlerta).length;

  const avanzar = (t: Tracking) => {
    const siguiente = ORDEN_FASES[ORDEN_FASES.indexOf(t.fase) + 1];
    if (!siguiente) return;
    iniciar(async () => {
      const r = await cambiarFaseAction(t.id, t.fase, siguiente);
      setAviso(r.error ?? null);
      if (!r.error) router.refresh();
    });
  };

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      if (!editando) return;
      const r = await guardarTrackingAction(editando.id, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      setEditando(null);
      setError(null);
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Torre de control de vuelos" descripcion={`Despachos de exportación por fase y control DOA (alerta sobre ${pct(umbralDoa)}).`} />

      <div className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 md:mx-0 md:flex-wrap md:px-0">
        <BotonFiltro activo={!fase && !soloAlertas} onClick={() => { setFase(""); setSoloAlertas(false); }}>
          Todos <span className="tabular-nums">{despachos.length}</span>
        </BotonFiltro>
        {ORDEN_FASES.map((f) => (
          <BotonFiltro key={f} activo={fase === f} onClick={() => setFase(fase === f ? "" : f)}>
            {FASES_TRACKING[f]} <span className="tabular-nums">{despachos.filter((t) => t.fase === f).length}</span>
          </BotonFiltro>
        ))}
        <BotonFiltro activo={soloAlertas} peligro onClick={() => setSoloAlertas((v) => !v)}>
          <AlertTriangle className="size-4" aria-hidden /> DOA &gt; {pct(umbralDoa)} <span className="tabular-nums">{alertas}</span>
        </BotonFiltro>
      </div>

      <label className="relative sm:max-w-sm">
        <span className="sr-only">Buscar despacho</span>
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
        <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Cliente, AWB, factura…" className={`${inputClass} pl-9`} />
      </label>

      {aviso ? <p role="status" className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">{aviso}</p> : null}

      {filtrados.length === 0 ? (
        <p className="rounded-md border border-dashed border-borde py-10 text-center text-sm text-texto-suave">
          {despachos.length === 0 ? "Aún no hay despachos. Nacen al ganar una oportunidad de exportación." : "Ningún despacho coincide con el filtro."}
        </p>
      ) : (
        <ul className="grid grid-cols-1 gap-4 xl:grid-cols-2">
          {filtrados.map((t) => {
            const alerta = enAlerta(t);
            const posicion = ORDEN_FASES.indexOf(t.fase);
            const siguiente = ORDEN_FASES[posicion + 1];
            return (
              <li
                key={t.id}
                className={`flex min-w-0 flex-col gap-4 rounded-md border bg-superficie p-4 ${alerta ? "border-peligro ring-1 ring-peligro" : "border-borde"}`}
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <Link href={`/crm/clientes/${t.clienteId}`} className="block truncate font-medium text-texto hover:text-acento hover:underline">
                      {t.clienteNombre}
                    </Link>
                    <p className="text-xs text-texto-suave">
                      {t.codigo} · {t.ventaCodigo} · {t.comercialNombre}
                    </p>
                  </div>
                  <div className={`shrink-0 rounded-md px-2.5 py-1 text-right ${alerta ? "bg-peligro/15 text-peligro" : "bg-neutro-fondo text-neutro-texto"}`}>
                    <p className="text-xs">% DOA</p>
                    <p className="text-lg font-semibold tabular-nums">{pct(t.porcentajeDoa)}</p>
                  </div>
                </div>

                {/* Stepper de fases: en móvil solo puntos y la fase actual; desde md, las etiquetas. */}
                <ol aria-label={`Fase actual: ${FASES_TRACKING[t.fase]}`} className="flex items-center gap-1">
                  {ORDEN_FASES.map((f, i) => (
                    <li key={f} className="flex flex-1 flex-col items-center gap-1" aria-current={i === posicion ? "step" : undefined}>
                      <span
                        className={`h-2 w-full rounded-full ${
                          i < posicion ? "bg-acento" : i === posicion ? (alerta && f === "control_doa" ? "bg-peligro" : "bg-acento") : "bg-neutro-fondo"
                        } ${i === posicion ? "ring-2 ring-acento/40" : ""}`}
                      />
                      <span className={`hidden text-center text-[11px] leading-tight md:block ${i === posicion ? "font-medium text-texto" : "text-texto-tenue"}`}>
                        {FASES_TRACKING[f].replace(/^\d\. /, "")}
                      </span>
                    </li>
                  ))}
                </ol>
                <p className="text-sm font-medium text-texto md:hidden">{FASES_TRACKING[t.fase]}</p>

                <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm sm:grid-cols-4">
                  <Dato titulo="AWB">
                    <span className="font-mono text-xs">{t.noAwb ?? "—"}</span>
                  </Dato>
                  <Dato titulo="Aerolínea">{t.aerolinea ?? "—"}</Dato>
                  <Dato titulo="Despacho">{fecha(t.fechaDespachoVuelo)}</Dato>
                  <Dato titulo="Cajas">{t.cajasReales}</Dato>
                  <Dato titulo="FOB peces">{dinero(t.montoFobUsd, "USD")}</Dato>
                  <Dato titulo="Flete aéreo">{dinero(t.fleteAereoUsd, "USD")}</Dato>
                  <Dato titulo="DOA reportado">
                    <span className={alerta ? "font-medium text-peligro" : ""}>{dinero(t.doaReportadoUsd, "USD")}</span>
                  </Dato>
                  <Dato titulo="Factura / NC">
                    {t.nFacturaInvoice ?? "—"}
                    {t.notaCreditoNo ? ` · ${t.notaCreditoNo}` : ""}
                  </Dato>
                </dl>

                <div className="flex flex-wrap items-center gap-2 border-t border-borde pt-3">
                  {puedeGestionar && siguiente ? (
                    <button
                      type="button"
                      disabled={procesando}
                      onClick={() => avanzar(t)}
                      className="inline-flex h-10 items-center gap-2 rounded-md bg-acento px-3 text-sm font-medium text-acento-texto hover:opacity-90 disabled:opacity-60"
                      aria-label={`Avanzar ${t.codigo} a ${FASES_TRACKING[siguiente]}`}
                    >
                      <Plane className="size-4" aria-hidden /> {FASES_TRACKING[siguiente]}
                    </button>
                  ) : null}
                  <div className="ml-auto flex">
                    <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${t.codigo}`} onClick={() => setHistorialDe(t)} />
                    {puedeGestionar ? <BotonIcono accion="editar" etiqueta={`Editar ${t.codigo}`} onClick={() => setEditando(t)} /> : null}
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      )}

      <Dialogo titulo={editando ? `Despacho ${editando.codigo} · ${editando.clienteNombre}` : ""} abierto={editando !== null} onCerrar={() => { setEditando(null); setError(null); }} ancho="max-w-4xl">
        {editando ? (
          <FormularioTracking
            key={editando.id}
            tracking={editando}
            umbralDoa={umbralDoa}
            guardando={procesando}
            error={error}
            onGuardar={guardar}
            onCancelar={() => { setEditando(null); setError(null); }}
          />
        ) : null}
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.codigo}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? <HistorialRemoto ruta={`/api/crm/tracking/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CRM} /> : null}
      </Dialogo>
    </div>
  );
}

function BotonFiltro({ activo, peligro = false, onClick, children }: { activo: boolean; peligro?: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={activo}
      onClick={onClick}
      className={`inline-flex h-10 shrink-0 items-center gap-1.5 rounded-full border px-3 text-sm whitespace-nowrap transition-colors ${
        activo
          ? peligro
            ? "border-peligro bg-peligro/15 font-medium text-peligro"
            : "border-acento bg-acento font-medium text-acento-texto"
          : peligro
            ? "border-borde text-peligro hover:bg-acento-suave"
            : "border-borde text-texto hover:bg-acento-suave"
      }`}
    >
      {children}
    </button>
  );
}

function Dato({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="min-w-0">
      <dt className="text-xs text-texto-suave">{titulo}</dt>
      <dd className="truncate text-texto tabular-nums">{children}</dd>
    </div>
  );
}

/** Edición del despacho con calculadora de % DOA en vivo (misma fórmula que la base). */
function FormularioTracking({
  tracking,
  umbralDoa,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  tracking: Tracking;
  umbralDoa: number;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [f, setF] = useState({
    nFacturaInvoice: tracking.nFacturaInvoice ?? "",
    noAwb: tracking.noAwb ?? "",
    aerolinea: tracking.aerolinea ?? "",
    fechaDespachoVuelo: tracking.fechaDespachoVuelo ?? "",
    cajasReales: String(tracking.cajasReales),
    fase: tracking.fase,
    montoFobUsd: String(tracking.montoFobUsd),
    fleteAereoUsd: String(tracking.fleteAereoUsd),
    doaReportadoUsd: String(tracking.doaReportadoUsd),
    notaCreditoNo: tracking.notaCreditoNo ?? "",
  });
  const set = (campo: keyof typeof f) => (valor: string) => setF((x) => ({ ...x, [campo]: valor }));
  const doa = porcentajeDoa(numero(f.montoFobUsd), numero(f.doaReportadoUsd));
  const alerta = doa != null && doa > umbralDoa;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          nFacturaInvoice: f.nFacturaInvoice,
          noAwb: f.noAwb,
          aerolinea: f.aerolinea,
          fechaDespachoVuelo: f.fechaDespachoVuelo || null,
          cajasReales: Math.trunc(numero(f.cajasReales)),
          fase: f.fase,
          montoFobUsd: numero(f.montoFobUsd),
          fleteAereoUsd: numero(f.fleteAereoUsd),
          doaReportadoUsd: numero(f.doaReportadoUsd),
          notaCreditoNo: f.notaCreditoNo,
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <Campo label="Fase" htmlFor="fase">
          <SelectMapa id="fase" valor={f.fase} onChange={set("fase")} mapa={FASES_TRACKING} />
        </Campo>
        <Campo label="Factura / invoice" htmlFor="factura">
          <input id="factura" value={f.nFacturaInvoice} onChange={(e) => set("nFacturaInvoice")(e.target.value)} maxLength={50} className={inputClass} />
        </Campo>
        <Campo label="Guía aérea AWB" htmlFor="awbT">
          <input id="awbT" value={f.noAwb} onChange={(e) => set("noAwb")(e.target.value)} maxLength={50} className={`${inputClass} font-mono`} />
        </Campo>
        <Campo label="Aerolínea" htmlFor="aerolinea">
          <input id="aerolinea" value={f.aerolinea} onChange={(e) => set("aerolinea")(e.target.value)} maxLength={150} className={inputClass} />
        </Campo>
        <Campo label="Fecha de despacho del vuelo" htmlFor="fechaVuelo">
          <input id="fechaVuelo" type="date" value={f.fechaDespachoVuelo} onChange={(e) => set("fechaDespachoVuelo")(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Cajas reales" htmlFor="cajasReales">
          <input id="cajasReales" type="number" inputMode="numeric" min={0} step={1} value={f.cajasReales} onChange={(e) => set("cajasReales")(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="FOB peces (USD)" htmlFor="fob">
          <input id="fob" type="number" inputMode="decimal" min={0} step="0.01" value={f.montoFobUsd} onChange={(e) => set("montoFobUsd")(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Flete aéreo (USD)" htmlFor="flete">
          <input id="flete" type="number" inputMode="decimal" min={0} step="0.01" value={f.fleteAereoUsd} onChange={(e) => set("fleteAereoUsd")(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="DOA reportado (USD)" htmlFor="doa">
          <input id="doa" type="number" inputMode="decimal" min={0} step="0.01" value={f.doaReportadoUsd} onChange={(e) => set("doaReportadoUsd")(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="N.º nota crédito" htmlFor="nc">
          <input id="nc" value={f.notaCreditoNo} onChange={(e) => set("notaCreditoNo")(e.target.value)} maxLength={50} className={inputClass} />
        </Campo>
        <CampoAncho>
          <output
            htmlFor="fob doa"
            aria-live="polite"
            className={`flex items-center justify-between rounded-md px-4 py-3 ${alerta ? "bg-peligro/15 text-peligro" : "bg-neutro-fondo text-neutro-texto"}`}
          >
            <span className="text-sm">% DOA = DOA reportado ÷ FOB peces × 100 {alerta ? `· supera el umbral de ${pct(umbralDoa)}` : ""}</span>
            <span className="text-xl font-semibold tabular-nums">{pct(doa)}</span>
          </output>
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
