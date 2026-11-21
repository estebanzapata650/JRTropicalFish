"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { History, Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Etiqueta } from "@/components/campos";
import {
  BORDE_ETAPA,
  ETAPAS,
  ETIQUETAS_HISTORIAL_CRM,
  ORDEN_ETAPAS,
  TIPOS_VENTA,
  esTerminal,
  etapasSiguientes,
  type Comercial,
  type Oportunidad,
} from "@/lib/crm";
import { botonPrimarioClass, casillaClass, inputClass, selectClass } from "@/lib/estilos";
import { diasDesde, dinero, fecha } from "@/lib/formato";
import { cambiarEtapaAction, eliminarOportunidadAction, ganarOportunidadAction, guardarOportunidadAction } from "../actions";
import { Tablero } from "../comun";
import { FormularioGanar, FormularioOportunidad, FormularioPerdida, type ClienteOpcion } from "./DialogosOportunidad";

type EstadoDialogo =
  | { tipo: "crear" }
  | { tipo: "editar"; o: Oportunidad }
  | { tipo: "ganar"; o: Oportunidad }
  | { tipo: "perder"; o: Oportunidad }
  | { tipo: "historial"; o: Oportunidad }
  | { tipo: "eliminar"; o: Oportunidad };

/** Cuántos días se muestran las oportunidades ya cerradas (ganadas o perdidas) en el tablero. */
const DIAS_CERRADAS = 30;

/**
 * Tablero del pipeline. Solo se avanza (o se marca Perdida): las columnas anteriores rechazan la
 * tarjeta al arrastrar y los botones solo ofrecen etapas posteriores. Soltar en «Ganada» abre el
 * diálogo de pago, que crea la venta. Las cerradas no tienen movimiento.
 */
export function PipelineCliente({
  oportunidades,
  clientes,
  comerciales,
  usuarioId,
  puedeVender,
  puedeAsignar,
  hoy,
}: {
  oportunidades: Oportunidad[];
  clientes: ClienteOpcion[];
  comerciales: Comercial[];
  usuarioId: string;
  puedeVender: boolean;
  puedeAsignar: boolean;
  hoy: string;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [comercial, setComercial] = useState("");
  const [tipo, setTipo] = useState("");
  const [verCerradasAntiguas, setVerCerradasAntiguas] = useState(false);
  const [dialogo, setDialogo] = useState<EstadoDialogo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return oportunidades.filter(
      (o) =>
        (!comercial || o.comercialId === comercial) &&
        (!tipo || o.tipoVenta === tipo) &&
        (verCerradasAntiguas || !esTerminal(o.etapa) || diasDesde(o.etapaCambiadaEn) <= DIAS_CERRADAS) &&
        (!texto || [o.clienteNombre, o.codigo, o.clienteCodigo].some((v) => v.toLowerCase().includes(texto))),
    );
  }, [oportunidades, busqueda, comercial, tipo, verCerradasAntiguas]);

  const cerrar = () => {
    setDialogo(null);
    setError(null);
  };

  /** Ejecuta una acción; si falla deja el error en el diálogo o, si no hay diálogo, en el aviso. */
  const correr = (accion: () => Promise<{ error?: string; ok?: boolean }>, exito?: string) =>
    iniciar(async () => {
      const r = await accion();
      if (r.error) {
        if (dialogo) setError(r.error);
        else setAviso(r.error);
        return;
      }
      cerrar();
      setAviso(exito ?? null);
      router.refresh();
    });

  const mover = (o: Oportunidad, destino: string) => {
    setAviso(null);
    if (destino === "ganada") setDialogo({ tipo: "ganar", o });
    else if (destino === "perdida") setDialogo({ tipo: "perder", o });
    else correr(() => cambiarEtapaAction(o.id, o.etapa, destino));
  };

  const totales = (etapa: string) => {
    const lista = visibles.filter((o) => o.etapa === etapa);
    const usd = lista.filter((o) => o.moneda === "USD").reduce((s, o) => s + o.montoEstimado, 0);
    const cop = lista.filter((o) => o.moneda === "COP").reduce((s, o) => s + o.montoEstimado, 0);
    const partes = [usd ? dinero(usd, "USD") : null, cop ? dinero(cop, "COP") : null].filter(Boolean);
    return partes.length ? partes.join(" · ") : null;
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Pipeline comercial"
        descripcion="De la prospección al pago confirmado. Las tarjetas solo avanzan."
        acciones={
          puedeVender ? (
            <button type="button" onClick={() => setDialogo({ tipo: "crear" })} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nueva oportunidad
            </button>
          ) : null
        }
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-[1fr_auto_auto_auto] lg:items-center">
        <label className="relative">
          <span className="sr-only">Buscar oportunidad</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Cliente o código…" className={`${inputClass} pl-9`} />
        </label>
        {comerciales.length > 1 ? (
          <select aria-label="Filtrar por comercial" value={comercial} onChange={(e) => setComercial(e.target.value)} className={selectClass}>
            <option value="">Todos los comerciales</option>
            {comerciales.map((c) => (
              <option key={c.id} value={c.id}>
                {c.id === usuarioId ? `${c.nombre} (yo)` : c.nombre}
              </option>
            ))}
          </select>
        ) : null}
        <select aria-label="Filtrar por tipo de venta" value={tipo} onChange={(e) => setTipo(e.target.value)} className={selectClass}>
          <option value="">Exportación y nacional</option>
          {Object.entries(TIPOS_VENTA).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
        <label className="flex items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={verCerradasAntiguas} onChange={(e) => setVerCerradasAntiguas(e.target.checked)} className={casillaClass} />
          Cerradas de hace más de {DIAS_CERRADAS} días
        </label>
      </div>

      {aviso ? (
        <p role="status" className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          {aviso}
        </p>
      ) : null}

      <Tablero
        columnas={ORDEN_ETAPAS.map((e) => ({ id: e, titulo: ETAPAS[e], borde: BORDE_ETAPA[e], resumen: totales(e) }))}
        items={visibles}
        columnaDe={(o) => o.etapa}
        idDe={(o) => o.id}
        etiquetaDe={(o) => `${o.clienteNombre}, ${o.codigo}`}
        puedeSoltar={(o, destino) => puedeVender && etapasSiguientes(o.etapa).includes(destino)}
        onSoltar={mover}
        deshabilitado={!puedeVender || procesando}
        render={(o) => (
          <TarjetaOportunidad
            o={o}
            puedeVender={puedeVender}
            procesando={procesando}
            onMover={(destino) => mover(o, destino)}
            onEditar={() => setDialogo({ tipo: "editar", o })}
            onHistorial={() => setDialogo({ tipo: "historial", o })}
            onEliminar={() => setDialogo({ tipo: "eliminar", o })}
          />
        )}
      />

      <Dialogo
        titulo={dialogo?.tipo === "editar" ? `Editar ${dialogo.o.codigo}` : "Nueva oportunidad"}
        abierto={dialogo?.tipo === "crear" || dialogo?.tipo === "editar"}
        onCerrar={cerrar}
        ancho="max-w-4xl"
      >
        {dialogo?.tipo === "crear" || dialogo?.tipo === "editar" ? (
          <FormularioOportunidad
            oportunidad={dialogo.tipo === "editar" ? dialogo.o : null}
            clientes={clientes}
            comerciales={comerciales}
            puedeAsignar={puedeAsignar}
            hoy={hoy}
            guardando={procesando}
            error={error}
            onGuardar={(datos) =>
              correr(() => guardarOportunidadAction(dialogo.tipo === "editar" ? dialogo.o.id : null, datos))
            }
            onCancelar={cerrar}
          />
        ) : null}
      </Dialogo>

      <Dialogo titulo={dialogo?.tipo === "ganar" ? `Ganar ${dialogo.o.codigo}` : ""} abierto={dialogo?.tipo === "ganar"} onCerrar={cerrar}>
        {dialogo?.tipo === "ganar" ? (
          <FormularioGanar
            oportunidad={dialogo.o}
            hoy={hoy}
            guardando={procesando}
            error={error}
            onGanar={(datos) =>
              iniciar(async () => {
                const r = await ganarOportunidadAction(dialogo.o.id, datos);
                if (r.error) {
                  setError(r.error);
                  return;
                }
                cerrar();
                setAviso(
                  `¡Ganada! Se creó la venta ${r.datos?.ventaCodigo}` +
                    (r.datos?.trackingCodigo ? ` y el despacho ${r.datos.trackingCodigo} en Cuarentena.` : "."),
                );
                router.refresh();
              })
            }
            onCancelar={cerrar}
          />
        ) : null}
      </Dialogo>

      <Dialogo titulo="Marcar como perdida" abierto={dialogo?.tipo === "perder"} onCerrar={cerrar} ancho="max-w-lg">
        {dialogo?.tipo === "perder" ? (
          <FormularioPerdida
            oportunidad={dialogo.o}
            guardando={procesando}
            error={error}
            onPerder={(motivo) => correr(() => cambiarEtapaAction(dialogo.o.id, dialogo.o.etapa, "perdida", motivo))}
            onCancelar={cerrar}
          />
        ) : null}
      </Dialogo>

      <Dialogo titulo={dialogo?.tipo === "historial" ? `Historial de ${dialogo.o.codigo}` : ""} abierto={dialogo?.tipo === "historial"} onCerrar={cerrar}>
        {dialogo?.tipo === "historial" ? (
          <HistorialRemoto ruta={`/api/crm/oportunidades/${dialogo.o.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CRM} />
        ) : null}
      </Dialogo>

      <DialogoConfirmacion
        titulo="Eliminar oportunidad"
        mensaje={
          dialogo?.tipo === "eliminar" ? (
            <>
              ¿Eliminar <strong>{dialogo.o.codigo}</strong> de {dialogo.o.clienteNombre}? Úsalo solo para registros creados por error;
              si el cliente no compró, márcala como Perdida.
            </>
          ) : null
        }
        textoConfirmar="Eliminar"
        peligro
        abierto={dialogo?.tipo === "eliminar"}
        procesando={procesando}
        error={error}
        onConfirmar={() => dialogo?.tipo === "eliminar" && correr(() => eliminarOportunidadAction(dialogo.o.id))}
        onCerrar={cerrar}
      />
    </div>
  );
}

function TarjetaOportunidad({
  o,
  puedeVender,
  procesando,
  onMover,
  onEditar,
  onHistorial,
  onEliminar,
}: {
  o: Oportunidad;
  puedeVender: boolean;
  procesando: boolean;
  onMover: (destino: string) => void;
  onEditar: () => void;
  onHistorial: () => void;
  onEliminar: () => void;
}) {
  const siguientes = etapasSiguientes(o.etapa);
  const proxima = siguientes.find((e) => e !== "perdida");
  const dias = diasDesde(o.etapaCambiadaEn);

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-start justify-between gap-2">
        <div className="min-w-0">
          <Link href={`/crm/clientes/${o.clienteId}`} className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
            {o.clienteNombre}
          </Link>
          <p className="text-xs text-texto-suave">
            {o.codigo} · {o.tipoVenta === "exportacion" ? "Exportación" : "Nacional"}
          </p>
        </div>
        <p className="shrink-0 text-sm font-semibold tabular-nums text-texto">{dinero(o.montoEstimado, o.moneda)}</p>
      </div>
      <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-texto-suave">
        <span>{o.cajasEstimadas} cajas</span>
        <span>{o.comercialNombre}</span>
        {esTerminal(o.etapa) ? (
          <span>{o.etapa === "ganada" ? `Pago ${fecha(o.fechaPagoConfirmado)}` : `Cerrada ${fecha(o.etapaCambiadaEn.slice(0, 10))}`}</span>
        ) : (
          <span className={dias > 14 ? "font-medium text-advertencia-texto" : ""}>{dias} d en la etapa</span>
        )}
      </div>
      {o.etapa === "perdida" && o.motivoPerdida ? <p className="text-xs text-peligro">Motivo: {o.motivoPerdida}</p> : null}
      {o.etapa === "ganada" && o.ventaId ? <Etiqueta texto="Venta creada" tono="exito" /> : null}

      <div className="flex flex-wrap items-center gap-1 border-t border-borde pt-2">
        {puedeVender && proxima ? (
          <button
            type="button"
            disabled={procesando}
            onClick={() => onMover(proxima)}
            className="inline-flex h-9 items-center rounded-md bg-acento px-3 text-xs font-medium text-acento-texto hover:opacity-90 disabled:opacity-60"
            aria-label={`Avanzar ${o.codigo} a ${ETAPAS[proxima]}`}
          >
            {proxima === "ganada" ? "Ganar" : "Avanzar"} ▸
          </button>
        ) : null}
        {puedeVender && siguientes.length > 2 ? (
          <select
            aria-label={`Mover ${o.codigo} a otra etapa`}
            value=""
            disabled={procesando}
            onChange={(e) => e.target.value && onMover(e.target.value)}
            className="h-9 rounded-md border border-borde bg-superficie px-2 text-xs text-texto"
          >
            <option value="">Mover a…</option>
            {siguientes.map((e) => (
              <option key={e} value={e}>
                {ETAPAS[e]}
              </option>
            ))}
          </select>
        ) : puedeVender && siguientes.includes("perdida") ? (
          <button
            type="button"
            disabled={procesando}
            onClick={() => onMover("perdida")}
            className="inline-flex h-9 items-center rounded-md border border-borde px-3 text-xs font-medium text-peligro hover:bg-acento-suave"
          >
            Perdida
          </button>
        ) : null}
        <div className="ml-auto flex">
          <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${o.codigo}`} onClick={onHistorial} />
          {puedeVender ? <BotonIcono accion="editar" etiqueta={`Editar ${o.codigo}`} onClick={onEditar} /> : null}
          {puedeVender && o.etapa !== "ganada" ? (
            <BotonIcono accion="eliminar" etiqueta={`Eliminar ${o.codigo}`} onClick={onEliminar} />
          ) : null}
        </div>
      </div>
    </div>
  );
}
