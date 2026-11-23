"use client";

import { useState } from "react";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import { Campo, CampoAncho, RejillaCampos } from "@/components/campos";
import {
  ESTADOS_PAGO,
  ETAPAS,
  ETAPAS_ACTIVAS,
  ORIGENES_CANAL,
  TIPOS_VENTA,
  esTerminal,
  type Comercial,
  type Oportunidad,
} from "@/lib/crm";
import {
  botonPeligroClass,
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  textareaClass,
} from "@/lib/estilos";
import { dinero } from "@/lib/formato";
import { SelectMapa } from "../comun";

export type ClienteOpcion = {
  id: string;
  codigo: string;
  nombreEmpresa: string;
};

function Botones({
  guardando,
  texto,
  peligro = false,
  onCancelar,
}: {
  guardando: boolean;
  texto: string;
  peligro?: boolean;
  onCancelar: () => void;
}) {
  return (
    <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
      <button
        type="button"
        onClick={onCancelar}
        className={botonSecundarioClass}
      >
        Cancelar
      </button>
      <button
        type="submit"
        disabled={guardando}
        className={peligro ? botonPeligroClass : botonPrimarioClass}
      >
        {guardando ? "Guardando…" : texto}
      </button>
    </div>
  );
}

const numero = (texto: string) =>
  texto.trim() === "" ? 0 : Number(texto.replace(",", "."));

/**
 * Crear o editar una oportunidad. La etapa solo se elige al crear (una etapa activa); después se
 * mueve en el tablero. Una oportunidad cerrada solo deja editar las notas.
 */
export function FormularioOportunidad({
  oportunidad,
  clientes,
  comerciales,
  puedeAsignar,
  hoy,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  oportunidad: Oportunidad | null;
  clientes: ClienteOpcion[];
  comerciales: Comercial[];
  puedeAsignar: boolean;
  hoy: string;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const cerrada = oportunidad ? esTerminal(oportunidad.etapa) : false;
  const [clienteId, setClienteId] = useState(oportunidad?.clienteId ?? "");
  const [tipoVenta, setTipoVenta] = useState(
    oportunidad?.tipoVenta ?? "exportacion",
  );
  const [moneda, setMoneda] = useState(oportunidad?.moneda ?? "USD");
  const [monto, setMonto] = useState(
    oportunidad ? String(oportunidad.montoEstimado) : "",
  );
  const [cajas, setCajas] = useState(
    oportunidad ? String(oportunidad.cajasEstimadas) : "",
  );
  const [fechaContacto, setFechaContacto] = useState(
    oportunidad?.fechaContactoInicial ?? hoy,
  );
  const [etapa, setEtapa] = useState("prospeccion");
  const [comercialId, setComercialId] = useState(
    oportunidad?.comercialId ?? "",
  );
  const [notas, setNotas] = useState(oportunidad?.notasSeguimiento ?? "");

  // El cliente actual siempre está entre las opciones aunque ya esté inactivo.
  const opcionesCliente =
    oportunidad && !clientes.some((c) => c.id === oportunidad.clienteId)
      ? [
          ...clientes,
          {
            id: oportunidad.clienteId,
            codigo: oportunidad.clienteCodigo,
            nombreEmpresa: oportunidad.clienteNombre,
          },
        ]
      : clientes;
  const opcionesComercial =
    oportunidad && !comerciales.some((c) => c.id === oportunidad.comercialId)
      ? [
          ...comerciales,
          {
            id: oportunidad.comercialId,
            nombre: oportunidad.comercialNombre,
            email: "",
          },
        ]
      : comerciales;

  const cambiarTipo = (valor: string) => {
    setTipoVenta(valor);
    // Regla de negocio: exportación se negocia en USD y nacional en COP (se puede cambiar).
    setMoneda(valor === "exportacion" ? "USD" : "COP");
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          clienteId,
          tipoVenta,
          comercialId: puedeAsignar && comercialId ? comercialId : null,
          fechaContactoInicial: fechaContacto || null,
          etapa: oportunidad ? null : etapa,
          moneda,
          montoEstimado: numero(monto),
          cajasEstimadas: Math.trunc(numero(cajas)),
          notasSeguimiento: notas,
        });
      }}
      className="flex flex-col gap-6"
    >
      {cerrada ? (
        <p className="rounded-md bg-neutro-fondo px-3 py-2 text-sm text-neutro-texto">
          La oportunidad está cerrada ({ETAPAS[oportunidad!.etapa]}): solo se
          pueden editar las notas de seguimiento.
        </p>
      ) : null}
      <fieldset disabled={cerrada} className="contents">
        <RejillaCampos>
          <CampoAncho>
            <Campo label="Cliente" htmlFor="clienteId">
              <SelectorBuscable
                id="clienteId"
                opciones={opcionesCliente.map((c) => ({
                  id: c.id,
                  nombre: c.nombreEmpresa,
                  detalle: c.codigo,
                }))}
                valor={clienteId}
                onChange={setClienteId}
                placeholder="Escribe el nombre o las iniciales…"
              />
            </Campo>
          </CampoAncho>
          <Campo label="Tipo de venta" htmlFor="tipoVenta">
            <SelectMapa
              id="tipoVenta"
              valor={tipoVenta}
              onChange={cambiarTipo}
              mapa={TIPOS_VENTA}
            />
          </Campo>
          <Campo label="Moneda" htmlFor="moneda">
            <SelectMapa
              id="moneda"
              valor={moneda}
              onChange={setMoneda}
              mapa={{ USD: "USD", COP: "COP" }}
            />
          </Campo>
          <Campo label={`Monto estimado (${moneda})`} htmlFor="monto">
            <input
              id="monto"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={monto}
              onChange={(e) => setMonto(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo label="Cajas estimadas" htmlFor="cajas">
            <input
              id="cajas"
              type="number"
              inputMode="numeric"
              min={0}
              step={1}
              value={cajas}
              onChange={(e) => setCajas(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo label="Fecha de contacto inicial" htmlFor="fechaContacto">
            <input
              id="fechaContacto"
              type="date"
              max={hoy}
              value={fechaContacto}
              onChange={(e) => setFechaContacto(e.target.value)}
              className={inputClass}
            />
          </Campo>
          {oportunidad ? null : (
            <Campo label="Etapa inicial" htmlFor="etapa">
              <SelectMapa
                id="etapa"
                valor={etapa}
                onChange={setEtapa}
                mapa={Object.fromEntries(
                  ETAPAS_ACTIVAS.map((e) => [e, ETAPAS[e]]),
                )}
              />
            </Campo>
          )}
          {puedeAsignar ? (
            <Campo label="Comercial asignado" htmlFor="comercialId">
              <SelectorBuscable
                id="comercialId"
                opciones={opcionesComercial.map((c) => ({
                  id: c.id,
                  nombre: c.nombre,
                }))}
                valor={comercialId}
                onChange={(v) => setComercialId(v)}
                placeholder="Escribe para buscar…"
              />
            </Campo>
          ) : null}
        </RejillaCampos>
      </fieldset>
      <Campo label="Notas de seguimiento (opcional)" htmlFor="notas">
        <textarea
          id="notas"
          value={notas}
          onChange={(e) => setNotas(e.target.value)}
          maxLength={4000}
          className={`${textareaClass} min-h-28`}
        />
      </Campo>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <Botones guardando={guardando} texto="Guardar" onCancelar={onCancelar} />
    </form>
  );
}

/**
 * Ganar: confirma el pago y crea la venta (y el tracking del vuelo si es exportación). La TRM
 * solo se pide si la oportunidad está en USD.
 */
export function FormularioGanar({
  oportunidad,
  hoy,
  guardando,
  error,
  onGanar,
  onCancelar,
}: {
  oportunidad: Oportunidad;
  hoy: string;
  guardando: boolean;
  error: string | null;
  onGanar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const usd = oportunidad.moneda === "USD";
  const [fechaPago, setFechaPago] = useState(hoy);
  const [origen, setOrigen] = useState(
    oportunidad.tipoVenta === "exportacion"
      ? "b2b_directo"
      : "whatsapp_directo",
  );
  const [estadoPago, setEstadoPago] = useState("anticipo");
  const [trm, setTrm] = useState("");
  const valorCop = usd && trm ? oportunidad.montoEstimado * numero(trm) : null;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGanar({
          etapaActual: oportunidad.etapa,
          fechaPagoConfirmado: fechaPago,
          origenCanal: origen,
          estadoPago,
          trm: usd ? numero(trm) || null : null,
        });
      }}
      className="flex flex-col gap-6"
    >
      <div className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">
        Se creará la venta de{" "}
        <strong>{dinero(oportunidad.montoEstimado, oportunidad.moneda)}</strong>{" "}
        ({oportunidad.cajasEstimadas} cajas)
        {oportunidad.tipoVenta === "exportacion"
          ? " y su seguimiento de vuelo en fase Cuarentena."
          : "."}
      </div>
      <RejillaCampos>
        <Campo label="Fecha de pago confirmado" htmlFor="fechaPago">
          <input
            id="fechaPago"
            type="date"
            max={hoy}
            value={fechaPago}
            onChange={(e) => setFechaPago(e.target.value)}
            className={inputClass}
          />
        </Campo>
        <Campo label="Estado de pago" htmlFor="estadoPago">
          <SelectMapa
            id="estadoPago"
            valor={estadoPago}
            onChange={setEstadoPago}
            mapa={ESTADOS_PAGO}
          />
        </Campo>
        <Campo label="Origen / canal" htmlFor="origen">
          <SelectMapa
            id="origen"
            valor={origen}
            onChange={setOrigen}
            mapa={ORIGENES_CANAL}
          />
        </Campo>
        {usd ? (
          <Campo
            label="TRM del día (COP por USD)"
            htmlFor="trm"
            hint={
              valorCop
                ? `Equivale a ${dinero(valorCop, "COP")}`
                : "Fija el valor en pesos de la venta para la meta."
            }
          >
            <input
              id="trm"
              type="number"
              inputMode="decimal"
              min={1000}
              max={20000}
              step="0.01"
              value={trm}
              onChange={(e) => setTrm(e.target.value)}
              className={inputClass}
            />
          </Campo>
        ) : null}
      </RejillaCampos>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <Botones
        guardando={guardando}
        texto="Ganar y crear venta"
        onCancelar={onCancelar}
      />
    </form>
  );
}

export function FormularioPerdida({
  oportunidad,
  guardando,
  error,
  onPerder,
  onCancelar,
}: {
  oportunidad: Oportunidad;
  guardando: boolean;
  error: string | null;
  onPerder: (motivo: string) => void;
  onCancelar: () => void;
}) {
  const [motivo, setMotivo] = useState("");
  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onPerder(motivo);
      }}
      className="flex flex-col gap-4"
    >
      <p className="text-sm text-texto">
        <strong>{oportunidad.clienteNombre}</strong> · {oportunidad.codigo}. Una
        oportunidad perdida queda cerrada y no vuelve a etapas anteriores.
      </p>
      <Campo label="Motivo de la pérdida" htmlFor="motivo">
        <textarea
          id="motivo"
          value={motivo}
          onChange={(e) => setMotivo(e.target.value)}
          maxLength={500}
          className={textareaClass}
        />
      </Campo>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <Botones
        guardando={guardando}
        texto="Marcar como perdida"
        peligro
        onCancelar={onCancelar}
      />
    </form>
  );
}
