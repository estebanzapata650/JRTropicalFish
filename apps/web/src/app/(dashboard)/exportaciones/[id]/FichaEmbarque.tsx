"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, History, UserCog } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { HistorialRemoto } from "@/components/HistorialCambios";
import {
  Campo,
  CampoAncho,
  Etiqueta,
  RejillaCampos,
} from "@/components/campos";
import {
  ACCIONES_EMBARQUE,
  EDITABLES,
  ESTADOS_EMBARQUE,
  FORMATOS_FACTURA,
  nombreDocumento,
  tonoEmbarque,
  usd,
  type ClienteExportacion,
  type EmbarqueDetalle,
  type TipoBolsa,
  type TipoCaja,
} from "@/lib/exportaciones";
import type { ProductoOpcion } from "@/lib/inventario";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
  textareaClass,
} from "@/lib/estilos";
import { fechaHora } from "@/lib/formato";
import {
  cambiarEstadoEmbarqueAction,
  guardarClienteExportacionAction,
} from "../actions";
import { Aviso, numero } from "./comun";
import { PasoAunap } from "./PasoAunap";
import { PasoEmpaque } from "./PasoEmpaque";
import { PasoBalance, PasoDoa, PasoFactura } from "./PasoFactura";
import { PasoFlete, PasoOrdenCargue } from "./PasoFlete";
import { PasoPreinvoice } from "./PasoPreinvoice";

const PASOS = [
  { clave: "preinvoice", texto: "1 Pedido y preinvoice" },
  { clave: "aunap", texto: "2 AUNAP y aduana" },
  { clave: "flete", texto: "3 Flete" },
  { clave: "cargue", texto: "4 Orden de cargue" },
  { clave: "empaque", texto: "5 Empaque" },
  { clave: "factura", texto: "6 Factura" },
  { clave: "doa", texto: "7 DOA y nota crédito" },
  { clave: "balance", texto: "8 Balance" },
] as const;

type Paso = (typeof PASOS)[number]["clave"];

/** Paso sugerido según el estado: donde el embarque necesita trabajo. */
const pasoInicial = (estado: string): Paso =>
  estado === "confirmado"
    ? "empaque"
    : estado === "empacado"
      ? "factura"
      : estado === "facturado"
        ? "doa"
        : estado === "cerrado"
          ? "balance"
          : "preinvoice";

const ETIQUETAS_HISTORIAL: Record<string, string> = {
  Estado: "Estado",
  Lista: "Lista de precios",
  DescuentoPeces: "Descuento peces (%)",
  Aerolinea: "Aerolínea",
  Ruta: "Ruta",
  Awb: "AWB",
  Destino: "Destino",
  SalidaVuelo: "Salida del vuelo",
  LlegadaVuelo: "Llegada del vuelo",
  FechaDespacho: "Fecha de despacho",
  FormatoFactura: "Formato de factura",
  AwbValor: "Valor AWB",
  AnticipoRecibido: "Anticipo recibido",
  Tarifas: "Tarifas de flete",
  Cargos: "Cargos",
  CajasEstimadas: "Cajas estimadas",
  CajasReales: "Cajas reales",
  Notas: "Notas",
  Especies: "Especies",
  Empaque: "Empaque",
  Factura: "Factura",
  DOA: "DOA (peces)",
};

export function FichaEmbarque({
  d,
  cajas,
  bolsas,
  productos,
  listas,
  clienteExportacion,
  puedeGestionar,
  puedeEmpacar,
}: {
  d: EmbarqueDetalle;
  cajas: TipoCaja[];
  bolsas: TipoBolsa[];
  productos: ProductoOpcion[];
  listas: { id: string; nombre: string }[];
  clienteExportacion: ClienteExportacion;
  puedeGestionar: boolean;
  puedeEmpacar: boolean;
}) {
  const router = useRouter();
  const e = d.embarque;
  const [paso, setPaso] = useState<Paso>(pasoInicial(e.estado));
  const [historial, setHistorial] = useState(false);
  const [cliente, setCliente] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();
  const editable = EDITABLES.includes(e.estado);
  const acciones = (ACCIONES_EMBARQUE[e.estado] ?? []).filter(
    (a) =>
      puedeGestionar ||
      (puedeEmpacar &&
        (a.estado === "empacado" ||
          (e.estado === "empacado" && a.estado === "confirmado"))),
  );

  const cambiarEstado = (nuevo: string) =>
    iniciar(async () => {
      const r = await cambiarEstadoEmbarqueAction(e.id, e.estado, nuevo);
      if (r.error) {
        setError(r.error);
        return;
      }
      setError(null);
      setAviso(`Estado: ${ESTADOS_EMBARQUE[nuevo]}.`);
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <Link
        href="/exportaciones"
        className="inline-flex w-fit items-center gap-1.5 text-sm text-texto-suave hover:text-texto"
      >
        <ArrowLeft className="size-4" aria-hidden /> Embarques
      </Link>

      <header className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
        <div className="flex min-w-0 flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-xl font-semibold text-texto">{e.codigo}</h1>
            <Etiqueta
              texto={ESTADOS_EMBARQUE[e.estado]}
              tono={tonoEmbarque(e.estado)}
            />
          </div>
          <p className="text-texto">{e.clienteNombre}</p>
          <p className="text-sm text-texto-suave">
            Venta{" "}
            <Link href="/crm/ventas" className="text-acento hover:underline">
              {e.ventaCodigo}
            </Link>
            {e.pedidoCodigo ? (
              <>
                {" · pedido "}
                <Link
                  href={`/crm/pedidos?id=${e.pedidoClienteId}`}
                  className="text-acento hover:underline"
                >
                  {e.pedidoCodigo}
                </Link>
              </>
            ) : null}
            {" · "}
            {e.comercialNombre}
            {e.facturaNumero ? ` · factura ${e.facturaNumero}` : ""}
          </p>
          {e.pedidoClienteId && !e.facturaNumero ? (
            <p className="text-xs text-texto-suave">
              Los acuarios que salen se reservan en el{" "}
              <Link
                href={`/crm/pedidos?id=${e.pedidoClienteId}`}
                className="text-acento hover:underline"
              >
                picking del pedido
              </Link>
              : desde{" "}
              {e.salidaVuelo
                ? "48 h antes del vuelo"
                : "las 00:00 del despacho (falta la hora del vuelo)"}{" "}
              quedan en ayuno.
            </p>
          ) : null}
        </div>
        <div className="flex flex-wrap gap-2">
          {acciones.map((a) => (
            <button
              key={a.estado}
              type="button"
              disabled={procesando}
              onClick={() => cambiarEstado(a.estado)}
              className={botonSecundarioClass}
            >
              {a.texto}
            </button>
          ))}
          {puedeGestionar ? (
            <button
              type="button"
              onClick={() => setCliente(true)}
              className={`${botonSecundarioClass} gap-2`}
            >
              <UserCog className="size-4" aria-hidden /> Datos del cliente
            </button>
          ) : null}
          <button
            type="button"
            onClick={() => setHistorial(true)}
            className={`${botonSecundarioClass} gap-2`}
            aria-label={`Historial de ${e.codigo}`}
          >
            <History className="size-4" aria-hidden /> Historial
          </button>
          {puedeGestionar && editable ? (
            <button
              type="button"
              disabled={procesando}
              onClick={() => cambiarEstado("cancelado")}
              className="inline-flex min-h-11 items-center rounded-md px-3 text-sm text-peligro hover:bg-peligro/10"
            >
              Cancelar embarque
            </button>
          ) : null}
        </div>
      </header>
      <Aviso texto={aviso} />
      <Aviso texto={error} error />

      <section
        aria-label="Totales"
        className="grid grid-cols-2 gap-3 md:grid-cols-4"
      >
        <Total
          titulo="Preinvoice"
          valor={usd(d.calculos.preinvoice.total)}
          detalle={`${d.calculos.preinvoice.unidades.toLocaleString("es-CO")} peces pedidos`}
        />
        <Total
          titulo="Factura"
          valor={usd(d.calculos.factura.total)}
          detalle={e.facturaNumero ? `N° ${e.facturaNumero}` : "Borrador"}
        />
        <Total
          titulo="Nota crédito"
          valor={usd(d.calculos.notaCredito.total)}
          detalle={`${d.calculos.notaCredito.muertos.toLocaleString("es-CO")} peces DOA`}
        />
        <Total
          titulo="Saldo"
          valor={usd(d.calculos.balance.saldo)}
          detalle={
            d.calculos.balance.saldo >= 0
              ? "A favor de JR"
              : "A favor del cliente"
          }
        />
      </section>

      <nav
        aria-label="Pasos del embarque"
        className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0"
      >
        <ul className="flex min-w-max gap-1 border-b border-borde">
          {PASOS.map((p) => (
            <li key={p.clave}>
              <button
                type="button"
                onClick={() => setPaso(p.clave)}
                aria-current={paso === p.clave ? "step" : undefined}
                className={`-mb-px inline-flex min-h-11 items-center border-b-2 px-3 text-sm font-medium ${paso === p.clave ? "border-acento text-texto" : "border-transparent text-texto-suave hover:text-texto"}`}
              >
                {p.texto}
              </button>
            </li>
          ))}
        </ul>
      </nav>

      {paso === "preinvoice" ? (
        <PasoPreinvoice
          d={d}
          productos={productos}
          listas={listas}
          cajas={cajas}
          editable={editable}
          puedeGestionar={puedeGestionar}
        />
      ) : null}
      {paso === "aunap" ? (
        <PasoAunap d={d} puedeGestionar={puedeGestionar} />
      ) : null}
      {paso === "flete" ? (
        <PasoFlete
          d={d}
          puede={editable && puedeGestionar}
          puedeEnviar={puedeGestionar}
        />
      ) : null}
      {paso === "cargue" ? <PasoOrdenCargue d={d} /> : null}
      {paso === "empaque" ? (
        <PasoEmpaque
          d={d}
          productos={productos}
          bolsas={bolsas}
          puede={editable && puedeEmpacar}
          puedeEnviar={puedeGestionar}
        />
      ) : null}
      {paso === "factura" ? (
        <PasoFactura
          d={d}
          cajas={cajas}
          editable={editable}
          puedeGestionar={puedeGestionar}
        />
      ) : null}
      {paso === "doa" ? (
        <PasoDoa
          d={d}
          puede={
            puedeGestionar &&
            (e.estado === "facturado" || e.estado === "cerrado")
          }
        />
      ) : null}
      {paso === "balance" ? (
        <PasoBalance d={d} puede={puedeGestionar && e.estado !== "cancelado"} />
      ) : null}

      {d.envios.length > 0 ? (
        <section
          aria-labelledby="titulo-envios"
          className="flex flex-col gap-2"
        >
          <h2 id="titulo-envios" className="text-base font-semibold text-texto">
            Documentos enviados
          </h2>
          <ul className="flex flex-col divide-y divide-borde rounded-md border border-borde text-sm">
            {d.envios.map((x) => (
              <li
                key={x.id}
                className="flex flex-wrap items-baseline justify-between gap-2 px-3 py-2"
              >
                <span className="text-texto">
                  {nombreDocumento(x.tipoDocumento)} → {x.para}
                  {!x.exitoso ? (
                    <span className="ml-1 text-peligro">falló: {x.error}</span>
                  ) : !x.llegoAlDestinatario ? (
                    <span className="ml-1 text-texto-suave">
                      ({x.proveedor}, sin salida real)
                    </span>
                  ) : null}
                </span>
                <span className="text-xs text-texto-suave">
                  {fechaHora(x.creadoEn)} · {x.enviadoPor}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <Dialogo
        titulo={`Historial de ${e.codigo}`}
        abierto={historial}
        onCerrar={() => setHistorial(false)}
      >
        {historial ? (
          <HistorialRemoto
            ruta={`/api/exportaciones/embarques/${e.id}/historial`}
            etiquetas={ETIQUETAS_HISTORIAL}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={`Datos de exportación de ${e.clienteNombre}`}
        abierto={cliente}
        onCerrar={() => setCliente(false)}
        ancho="max-w-3xl"
      >
        {cliente ? (
          <DatosCliente
            info={clienteExportacion}
            listas={listas}
            onGuardado={() => {
              setCliente(false);
              setAviso(
                "Datos del cliente guardados: se usan en sus próximos embarques y en los documentos.",
              );
              router.refresh();
            }}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function Total({
  titulo,
  valor,
  detalle,
}: {
  titulo: string;
  valor: string;
  detalle: string;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1 rounded-md border border-borde bg-superficie p-3">
      <p className="text-sm text-texto-suave">{titulo}</p>
      <p className="truncate text-lg font-semibold text-texto">{valor}</p>
      <p className="text-xs text-texto-suave">{detalle}</p>
    </div>
  );
}

/** Dirección para documentos, EORI, formato de factura, lista y descuento por defecto del cliente. */
function DatosCliente({
  info,
  listas,
  onGuardado,
}: {
  info: ClienteExportacion;
  listas: { id: string; nombre: string }[];
  onGuardado: () => void;
}) {
  const [direccion, setDireccion] = useState(info.direccion ?? "");
  const [eori, setEori] = useState(info.eori ?? "");
  const [formato, setFormato] = useState(info.formatoFactura);
  const [lista, setLista] = useState(info.listaPreciosId ?? "");
  const [descuento, setDescuento] = useState(String(info.descuentoPeces));
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  return (
    <form
      onSubmit={(ev) => {
        ev.preventDefault();
        iniciar(async () => {
          const r = await guardarClienteExportacionAction(info.clienteId, {
            direccion,
            eori,
            formatoFactura: formato,
            listaPreciosId: lista || null,
            descuentoPeces: numero(descuento) ?? 0,
          });
          if (r.error) {
            setError(r.error);
            return;
          }
          onGuardado();
        });
      }}
      className="flex flex-col gap-5"
    >
      <RejillaCampos>
        <CampoAncho>
          <Campo label="Dirección (para los documentos)" htmlFor="dirCli">
            <textarea
              id="dirCli"
              value={direccion}
              onChange={(ev) => setDireccion(ev.target.value)}
              maxLength={300}
              className={textareaClass}
            />
          </Campo>
        </CampoAncho>
        <Campo label="EORI (Unión Europea)" htmlFor="eori">
          <input
            id="eori"
            value={eori}
            onChange={(ev) => setEori(ev.target.value)}
            maxLength={40}
            className={inputClass}
          />
        </Campo>
        <Campo label="Formato de factura" htmlFor="formatoCli">
          <select
            id="formatoCli"
            value={formato}
            onChange={(e) => setFormato(e.target.value)}
            className={selectClass}
          >
            {Object.entries(FORMATOS_FACTURA).map(([v, t]) => (
              <option key={v} value={v}>
                {t}
              </option>
            ))}
          </select>
        </Campo>
        <Campo
          label="Lista de precios"
          htmlFor="listaCli"
          hint="Vacío = STANDARD."
        >
          <SelectorBuscable
            id="listaCli"
            opciones={listas.map((l) => ({ id: l.id, nombre: l.nombre }))}
            valor={lista}
            onChange={(v) => setLista(v)}
            vacio="Por defecto"
          />
        </Campo>
        <Campo label="Descuento sobre peces (%)" htmlFor="descCli">
          <input
            id="descCli"
            type="number"
            inputMode="decimal"
            min={0}
            max={100}
            step="0.01"
            value={descuento}
            onChange={(ev) => setDescuento(ev.target.value)}
            className={inputClass}
          />
        </Campo>
      </RejillaCampos>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex justify-end">
        <button
          type="submit"
          disabled={guardando}
          className={botonPrimarioClass}
        >
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
