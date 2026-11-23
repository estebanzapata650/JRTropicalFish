"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import {
  Ban,
  Clock,
  PlaneTakeoff,
  Search,
  Syringe,
  UserRound,
} from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { Campo, Etiqueta, type TonoEtiqueta } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { Tablero } from "@/components/Tablero";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
} from "@/lib/estilos";
import {
  BORDE_COLUMNA,
  COLUMNAS_SANIDAD,
  ORDEN_COLUMNAS,
  fechaCorta,
  horaCorta,
  num,
  type TableroSanidad,
  type Tarjeta,
} from "@/lib/sanidad";
import {
  cambiarEstadoTratamientoAction,
  crearTratamientoAction,
  ubicarAction,
} from "../actions";

type ProtocoloOpcion = { id: string; nombre: string; dias: number | null };
type AcuarioOpcion = { id: string; codigo: string };
type Accion = { tipo: "iniciar" | "ubicar" | "terminar"; tarjeta: Tarjeta };

function tonoEtiqueta(texto: string): TonoEtiqueta {
  if (texto === "No alimentar" || texto.startsWith("Vuelo")) return "peligro";
  if (
    texto === "Medicar hoy" ||
    texto === "Enfermos" ||
    texto === "Sin litraje" ||
    texto === "Sin hora de vuelo" ||
    texto === "Ubicar"
  )
    return "advertencia";
  if (texto.startsWith("Retiro") || texto === "Alimento medicado")
    return "info";
  return "neutro";
}

/**
 * Tablero de bodega por acuario. La columna sale de los datos (vuelo, tratamiento, llegada); arrastrar solo
 * hace la operación real que corresponde: iniciar un protocolo, ubicar un lote o terminar un tratamiento.
 */
export function TableroSanidadCliente({
  tablero,
  protocolos,
  acuarios,
  operar,
  ubicar,
}: {
  tablero: TableroSanidad;
  protocolos: ProtocoloOpcion[];
  acuarios: AcuarioOpcion[];
  operar: boolean;
  ubicar: boolean;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [busqueda, setBusqueda] = useState("");
  const [accion, setAccion] = useState<Accion | null>(null);
  const [mensaje, setMensaje] = useState<{
    tipo: "ok" | "error";
    texto: string;
  } | null>(null);

  const tarjetas = useMemo(() => {
    const q = busqueda.trim().toLowerCase();
    if (!q) return tablero.tarjetas;
    return tablero.tarjetas.filter(
      (t) =>
        t.titulo.toLowerCase().includes(q) ||
        (t.responsable ?? "").toLowerCase().includes(q) ||
        t.especies.some(
          (e) =>
            e.productoCodigo.toLowerCase().includes(q) ||
            e.productoNombre.toLowerCase().includes(q),
        ),
    );
  }, [busqueda, tablero.tarjetas]);

  // En móvil se abre en lo más urgente que tenga tarjetas: ayuno, cuarentena, aclimatación, estable, por llegar.
  const [columnaElegida, setColumnaElegida] = useState<string | null>(null);
  const columnaMovil =
    columnaElegida ??
    ["ayuno_vuelo", "cuarentena", "aclimatacion", "estable", "por_llegar"].find(
      (c) => tablero.tarjetas.some((t) => t.columna === c),
    ) ??
    "estable";

  const correr = (fn: () => Promise<{ error?: string }>, exito: string) =>
    iniciar(async () => {
      const r = await fn();
      setMensaje(
        r.error
          ? { tipo: "error", texto: r.error }
          : { tipo: "ok", texto: exito },
      );
      if (!r.error) {
        setAccion(null);
        router.refresh();
      }
    });

  const destinos = (
    t: Tarjeta,
  ): { destino: string; tipo: Accion["tipo"] }[] => {
    const lista: { destino: string; tipo: Accion["tipo"] }[] = [];
    const lotePorUbicar = t.clave.startsWith("lote:");
    if (lotePorUbicar && ubicar)
      lista.push({ destino: "estable", tipo: "ubicar" });
    if (
      t.acuarioId &&
      operar &&
      !t.tratamiento &&
      (t.columna === "aclimatacion" || t.columna === "estable") &&
      protocolos.length > 0
    )
      lista.push({ destino: "cuarentena", tipo: "iniciar" });
    if (t.acuarioId && operar && t.tratamiento?.estado === "activo")
      lista.push({ destino: "estable", tipo: "terminar" });
    return lista;
  };

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        titulo="Tablero de acuarios"
        descripcion={`Estado biológico de cada acuario. Ayuno pre-vuelo: ${tablero.horasAyunoVuelo} h antes de la salida.`}
      />
      <div className="relative max-w-md">
        <Search
          className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue"
          aria-hidden
        />
        <input
          type="search"
          aria-label="Buscar acuario, especie o responsable"
          placeholder="Acuario, especie o responsable"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          className={`${inputClass} pl-9`}
        />
      </div>
      {mensaje ? (
        <p
          role="status"
          className={`rounded-md px-3 py-2 text-sm ${mensaje.tipo === "ok" ? "bg-exito-fondo text-exito-texto" : "bg-peligro/15 text-peligro"}`}
        >
          {mensaje.texto}
        </p>
      ) : null}

      {/* Móvil: una columna a la vez, elegida con chips (con las manos mojadas, deslizar entre columnas estorba). */}
      <div className="flex flex-col gap-3 md:hidden">
        <div
          role="tablist"
          aria-label="Estado de los acuarios"
          className="-mx-4 flex gap-2 overflow-x-auto px-4"
        >
          {ORDEN_COLUMNAS.map((c) => {
            const n = tarjetas.filter((t) => t.columna === c).length;
            const activa = columnaMovil === c;
            return (
              <button
                key={c}
                type="button"
                role="tab"
                aria-selected={activa}
                onClick={() => setColumnaElegida(c)}
                className={`inline-flex h-11 shrink-0 items-center gap-2 rounded-full border px-4 text-sm font-medium ${
                  activa
                    ? "border-acento bg-acento text-acento-texto"
                    : "border-borde bg-superficie text-texto"
                }`}
              >
                {COLUMNAS_SANIDAD[c].split(" / ")[0]}
                <span className="tabular-nums">{n}</span>
              </button>
            );
          })}
        </div>
        <p className="text-sm text-texto-suave">
          {COLUMNAS_SANIDAD[columnaMovil]}
        </p>
        <ul className="flex flex-col gap-2">
          {tarjetas
            .filter((t) => t.columna === columnaMovil)
            .map((t) => (
              <li
                key={t.clave}
                className={`rounded-md border p-3 ${t.columna === "ayuno_vuelo" ? "border-peligro bg-peligro/10" : "border-borde bg-superficie"}`}
              >
                <TarjetaAcuario
                  t={t}
                  acciones={destinos(t)}
                  onAccion={(tipo) => setAccion({ tipo, tarjeta: t })}
                />
              </li>
            ))}
          {tarjetas.every((t) => t.columna !== columnaMovil) ? (
            <li className="py-6 text-center text-sm text-texto-tenue">
              Sin acuarios en este estado.
            </li>
          ) : null}
        </ul>
      </div>

      <div className="hidden md:block">
        <Tablero
          columnas={ORDEN_COLUMNAS.map((c) => ({
            id: c,
            titulo: COLUMNAS_SANIDAD[c],
            borde: BORDE_COLUMNA[c],
            resumen: `${num(
              tarjetas
                .filter((t) => t.columna === c)
                .reduce((s, t) => s + t.peces, 0),
              0,
            )} peces`,
          }))}
          items={tarjetas}
          columnaDe={(t) => t.columna}
          idDe={(t) => t.clave}
          etiquetaDe={(t) => `${t.titulo}: ${COLUMNAS_SANIDAD[t.columna]}`}
          deshabilitado={!operar && !ubicar}
          resaltar={(t) => t.columna === "ayuno_vuelo"}
          puedeSoltar={(t, destino) =>
            destinos(t).some((d) => d.destino === destino)
          }
          onSoltar={(t, destino) => {
            const d = destinos(t).find((x) => x.destino === destino);
            if (d) setAccion({ tipo: d.tipo, tarjeta: t });
          }}
          render={(t) => (
            <TarjetaAcuario
              t={t}
              acciones={destinos(t)}
              onAccion={(tipo) => setAccion({ tipo, tarjeta: t })}
            />
          )}
        />
      </div>

      <Dialogo
        titulo={
          accion
            ? `${accion.tipo === "iniciar" ? "Iniciar protocolo en" : accion.tipo === "ubicar" ? "Ubicar" : "Terminar tratamiento de"} ${accion.tarjeta.titulo}`
            : ""
        }
        abierto={accion !== null}
        onCerrar={() => setAccion(null)}
        ancho="max-w-md"
      >
        {accion?.tipo === "iniciar" ? (
          <IniciarForm
            protocolos={protocolos}
            pendiente={pendiente}
            onEnviar={(protocoloId) =>
              correr(
                () =>
                  crearTratamientoAction({
                    acuarioId: accion.tarjeta.acuarioId,
                    protocoloId,
                  }),
                `Tratamiento iniciado en ${accion.tarjeta.titulo}.`,
              )
            }
          />
        ) : null}
        {accion?.tipo === "ubicar" ? (
          <UbicarForm
            acuarios={acuarios}
            pendiente={pendiente}
            onEnviar={(acuarioId) => {
              const lote = accion.tarjeta.especies[0];
              const sanos = accion.tarjeta.especies
                .filter((e) => e.condicion === "sano")
                .reduce((s, e) => s + e.cantidad, 0);
              correr(
                () => ubicarAction(lote.loteId!, acuarioId, sanos),
                `${lote.loteCodigo} ubicado.`,
              );
            }}
          />
        ) : null}
        {accion?.tipo === "terminar" && accion.tarjeta.tratamiento ? (
          <div className="flex flex-col gap-4">
            <p className="text-sm text-texto">
              {accion.tarjeta.tratamiento.codigo} · Día{" "}
              {accion.tarjeta.tratamiento.dia}/
              {accion.tarjeta.tratamiento.diasTotal}. Solo se termina con todas
              las dosis registradas (aplicadas u omitidas). Se puede despachar
              desde el {fechaCorta(accion.tarjeta.tratamiento.fechaLiberacion)}.
            </p>
            <button
              type="button"
              disabled={pendiente}
              className={botonPrimarioClass}
              onClick={() => {
                const t = accion.tarjeta.tratamiento!;
                correr(
                  () =>
                    cambiarEstadoTratamientoAction(t.id, "activo", "terminado"),
                  `${t.codigo} terminado.`,
                );
              }}
            >
              Terminar tratamiento
            </button>
            <Link
              href={`/sanidad/tratamientos?id=${accion.tarjeta.tratamiento.id}`}
              className={botonSecundarioClass}
            >
              Ver dosis del tratamiento
            </Link>
          </div>
        ) : null}
      </Dialogo>
    </div>
  );
}

function TarjetaAcuario({
  t,
  acciones,
  onAccion,
}: {
  t: Tarjeta;
  acciones: { destino: string; tipo: Accion["tipo"] }[];
  onAccion: (tipo: Accion["tipo"]) => void;
}) {
  const visibles = t.especies.slice(0, 3);
  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-baseline justify-between gap-2">
        <p className="text-base font-semibold text-texto">{t.titulo}</p>
        <span className="text-sm tabular-nums text-texto">
          {num(t.peces, 0)}
        </span>
      </div>
      {t.acuarioId ? (
        <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-texto-suave">
          {t.litros ? <span>{num(t.litros, 0)} L</span> : null}
          <span className="inline-flex items-center gap-1">
            <UserRound className="size-3" aria-hidden />{" "}
            {t.responsable ?? "Sin responsable"}
          </span>
        </p>
      ) : t.detalle ? (
        <p className="text-xs text-texto-suave">
          {t.detalle}
          {t.fechaEstimada ? ` · llega ${fechaCorta(t.fechaEstimada)}` : ""}
        </p>
      ) : null}
      <ul className="flex flex-col gap-0.5 text-xs">
        {visibles.map((e) => (
          <li
            key={`${e.loteId ?? ""}${e.productoId}${e.condicion}`}
            className="flex justify-between gap-2"
          >
            <span className="truncate text-texto">
              {e.productoCodigo}{" "}
              <span className="text-texto-suave">{e.productoNombre}</span>
              {e.condicion === "enfermo" ? (
                <span className="text-advertencia-texto"> · enfermos</span>
              ) : null}
            </span>
            <span className="tabular-nums text-texto-suave">
              {num(e.cantidad, 0)}
            </span>
          </li>
        ))}
        {t.especies.length > visibles.length ? (
          <li className="text-texto-tenue">
            +{t.especies.length - visibles.length} más
          </li>
        ) : null}
      </ul>
      {t.tratamiento ? (
        <p className="flex items-center gap-1 text-xs text-texto">
          <Syringe className="size-3" aria-hidden />
          {t.tratamiento.codigo} ·{" "}
          {t.tratamiento.estado === "activo"
            ? `Día ${t.tratamiento.dia}/${t.tratamiento.diasTotal}`
            : `retiro hasta ${fechaCorta(t.tratamiento.fechaLiberacion)}`}
          {t.tratamiento.pendientesHoy > 0
            ? ` · ${t.tratamiento.pendientesHoy} pendiente(s)`
            : ""}
        </p>
      ) : null}
      {t.vuelo ? (
        <p
          className={`flex items-center gap-1 text-xs ${t.columna === "ayuno_vuelo" ? "font-semibold text-peligro" : "text-texto"}`}
        >
          <PlaneTakeoff className="size-3" aria-hidden />
          {t.vuelo.embarqueCodigo ?? t.vuelo.pedidoCodigo} ·{" "}
          {horaCorta(t.vuelo.salida)} ·{" "}
          {t.vuelo.horasRestantes <= 0
            ? "hoy"
            : `faltan ${Math.ceil(t.vuelo.horasRestantes)} h`}
        </p>
      ) : null}
      {t.columna === "aclimatacion" && t.llegada ? (
        <p className="flex items-center gap-1 text-xs text-texto-suave">
          <Clock className="size-3" aria-hidden /> Llegó {horaCorta(t.llegada)}
        </p>
      ) : null}
      {t.columna === "ayuno_vuelo" ? (
        <p className="flex items-center gap-1 text-sm font-bold text-peligro">
          <Ban className="size-4" aria-hidden /> PROHIBIDO ALIMENTAR
        </p>
      ) : null}
      {t.etiquetas.length > 0 ? (
        <div className="flex flex-wrap gap-1">
          {t.etiquetas.map((e) => (
            <Etiqueta key={e} texto={e} tono={tonoEtiqueta(e)} />
          ))}
        </div>
      ) : null}
      {acciones.length > 0 ? (
        <div className="flex flex-wrap gap-2 pt-1">
          {acciones.map((a) => (
            <button
              key={a.tipo}
              type="button"
              onClick={() => onAccion(a.tipo)}
              className={`${botonSecundarioClass} h-10 flex-1 text-xs`}
            >
              {a.tipo === "iniciar"
                ? "Iniciar protocolo"
                : a.tipo === "ubicar"
                  ? "Ubicar"
                  : "Terminar tratamiento"}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function IniciarForm({
  protocolos,
  pendiente,
  onEnviar,
}: {
  protocolos: ProtocoloOpcion[];
  pendiente: boolean;
  onEnviar: (id: string) => void;
}) {
  const [id, setId] = useState(protocolos[0]?.id ?? "");
  return (
    <div className="flex flex-col gap-4">
      <Campo
        label="Protocolo aprobado"
        htmlFor="protocolo-iniciar"
        hint="Las dosis se calculan con el litraje del acuario."
      >
        <SelectorBuscable
          id="protocolo-iniciar"
          opciones={protocolos.map((p) => ({
            id: p.id,
            nombre: p.dias ? `${p.nombre} (${p.dias} días)` : p.nombre,
          }))}
          valor={id}
          onChange={(v) => setId(v)}
        />
      </Campo>
      <button
        type="button"
        disabled={pendiente || !id}
        className={botonPrimarioClass}
        onClick={() => onEnviar(id)}
      >
        Iniciar
      </button>
    </div>
  );
}

function UbicarForm({
  acuarios,
  pendiente,
  onEnviar,
}: {
  acuarios: AcuarioOpcion[];
  pendiente: boolean;
  onEnviar: (id: string) => void;
}) {
  const [id, setId] = useState("");
  return (
    <div className="flex flex-col gap-4">
      <Campo label="Acuario" htmlFor="acuario-ubicar">
        <SelectorBuscable
          id="acuario-ubicar"
          opciones={acuarios.map((a) => ({ id: a.id, nombre: a.codigo }))}
          valor={id}
          onChange={(v) => setId(v)}
          placeholder="Escribe para buscar…"
        />
      </Campo>
      <button
        type="button"
        disabled={pendiente || !id}
        className={botonPrimarioClass}
        onClick={() => onEnviar(id)}
      >
        Ubicar todo el lote
      </button>
    </div>
  );
}
