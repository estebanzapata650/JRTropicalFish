"use client";

import { useState } from "react";
import { Campo, CampoAncho, RejillaCampos, Seccion } from "@/components/campos";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import {
  ESTADOS_PAGO,
  ESTADOS_PREPARACION,
  ORIGENES_CANAL,
  TIPOS_VENTA,
  type Comercial,
  type Venta,
} from "@/lib/crm";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  textareaClass,
} from "@/lib/estilos";
import { dinero } from "@/lib/formato";
import { SelectMapa } from "../comun";
import type { ClienteOpcion } from "../pipeline/DialogosOportunidad";

const numero = (texto: string) =>
  texto.trim() === "" ? 0 : Number(texto.replace(",", "."));

/**
 * Venta con formulario dinámico (regla Show_If):
 * - Exportación: aerolínea y guía AWB; moneda USD por defecto; sin dirección de entrega.
 * - Nacional: transportadora, ciudad y dirección de entrega; moneda COP por defecto; sin AWB.
 * Cliente, comercial y tipo solo se eligen al crear (de ellos dependen la oportunidad y el vuelo).
 */
export function FormularioVenta({
  venta,
  clientes,
  comerciales,
  puedeAsignar,
  hoy,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  venta: Venta | null;
  clientes: ClienteOpcion[];
  comerciales: Comercial[];
  puedeAsignar: boolean;
  hoy: string;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [tipoVenta, setTipoVenta] = useState(venta?.tipoVenta ?? "exportacion");
  const [clienteId, setClienteId] = useState(venta?.clienteId ?? "");
  const [comercialId, setComercialId] = useState(venta?.comercialId ?? "");
  const [origen, setOrigen] = useState(venta?.origenCanal ?? "b2b_directo");
  const [fechaPedido, setFechaPedido] = useState(venta?.fechaPedido ?? hoy);
  const [moneda, setMoneda] = useState(venta?.moneda ?? "USD");
  const [total, setTotal] = useState(venta ? String(venta.total) : "");
  const [trm, setTrm] = useState(venta?.trm ? String(venta.trm) : "");
  const [cajas, setCajas] = useState(venta ? String(venta.totalCajas) : "");
  const [transportadora, setTransportadora] = useState(
    venta?.transportadoraAerolinea ?? "",
  );
  const [awb, setAwb] = useState(venta?.noGuiaAwb ?? "");
  const [ciudad, setCiudad] = useState(venta?.ciudadEntrega ?? "");
  const [direccion, setDireccion] = useState(venta?.direccionEntrega ?? "");
  const [estadoPago, setEstadoPago] = useState(
    venta?.estadoPago ?? "pendiente",
  );
  const [preparacion, setPreparacion] = useState(
    venta?.estadoPreparacion ?? "pendiente_picking",
  );
  const [notas, setNotas] = useState(venta?.notas ?? "");

  const exportacion = tipoVenta === "exportacion";
  const usd = moneda === "USD";
  const valorCop = usd ? numero(total) * numero(trm) : numero(total);

  const cambiarTipo = (valor: string) => {
    setTipoVenta(valor);
    setMoneda(valor === "exportacion" ? "USD" : "COP");
    if (valor === "nacional")
      setOrigen((o) => (o === "b2b_directo" ? "whatsapp_directo" : o));
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          clienteId: venta ? null : clienteId || null,
          comercialId: venta || !puedeAsignar ? null : comercialId || null,
          tipoVenta: venta ? null : tipoVenta,
          origenCanal: origen,
          fechaPedido,
          moneda,
          total: numero(total),
          trm: usd ? numero(trm) || null : null,
          totalCajas: Math.trunc(numero(cajas)),
          transportadoraAerolinea: transportadora,
          noGuiaAwb: exportacion ? awb : null,
          estadoPago,
          estadoPreparacion: preparacion,
          ciudadEntrega: ciudad,
          direccionEntrega: exportacion ? null : direccion,
          notas,
        });
      }}
      className="flex flex-col gap-6"
    >
      <Seccion titulo="Pedido">
        <RejillaCampos>
          {venta ? null : (
            <>
              <Campo label="Tipo de venta" htmlFor="tipoVenta">
                <SelectMapa
                  id="tipoVenta"
                  valor={tipoVenta}
                  onChange={cambiarTipo}
                  mapa={TIPOS_VENTA}
                />
              </Campo>
              <Campo label="Cliente" htmlFor="clienteId">
                <SelectorBuscable
                  id="clienteId"
                  opciones={clientes.map((c) => ({
                    id: c.id,
                    nombre: c.nombreEmpresa,
                    detalle: c.codigo,
                  }))}
                  valor={clienteId}
                  onChange={setClienteId}
                  placeholder="Escribe el nombre o las iniciales…"
                />
              </Campo>
              {puedeAsignar ? (
                <Campo label="Comercial" htmlFor="comercialId">
                  <SelectorBuscable
                    id="comercialId"
                    opciones={comerciales.map((c) => ({
                      id: c.id,
                      nombre: c.nombre,
                    }))}
                    valor={comercialId}
                    onChange={(v) => setComercialId(v)}
                    placeholder="Escribe para buscar…"
                  />
                </Campo>
              ) : null}
            </>
          )}
          <Campo label="Origen / canal" htmlFor="origen">
            <SelectMapa
              id="origen"
              valor={origen}
              onChange={setOrigen}
              mapa={ORIGENES_CANAL}
            />
          </Campo>
          <Campo label="Fecha del pedido" htmlFor="fechaPedido">
            <input
              id="fechaPedido"
              type="date"
              value={fechaPedido}
              onChange={(e) => setFechaPedido(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo label="Total de cajas" htmlFor="cajas">
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
        </RejillaCampos>
      </Seccion>

      <Seccion titulo="Valor y pago">
        <RejillaCampos>
          <Campo label="Moneda" htmlFor="moneda">
            <SelectMapa
              id="moneda"
              valor={moneda}
              onChange={setMoneda}
              mapa={{ USD: "USD", COP: "COP" }}
            />
          </Campo>
          <Campo label={`Total (${moneda})`} htmlFor="total">
            <input
              id="total"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={total}
              onChange={(e) => setTotal(e.target.value)}
              className={inputClass}
            />
          </Campo>
          {usd ? (
            <Campo
              label="TRM (COP por USD)"
              htmlFor="trm"
              hint={
                valorCop > 0
                  ? `Equivale a ${dinero(valorCop, "COP")}`
                  : undefined
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
          <Campo label="Estado de pago" htmlFor="estadoPago">
            <SelectMapa
              id="estadoPago"
              valor={estadoPago}
              onChange={setEstadoPago}
              mapa={ESTADOS_PAGO}
            />
          </Campo>
        </RejillaCampos>
      </Seccion>

      <Seccion titulo={exportacion ? "Despacho aéreo" : "Entrega nacional"}>
        <RejillaCampos>
          <Campo label="Preparación" htmlFor="preparacion">
            <SelectMapa
              id="preparacion"
              valor={preparacion}
              onChange={setPreparacion}
              mapa={ESTADOS_PREPARACION}
            />
          </Campo>
          <Campo
            label={
              exportacion ? "Aerolínea (opcional)" : "Transportadora (opcional)"
            }
            htmlFor="transportadora"
          >
            <input
              id="transportadora"
              value={transportadora}
              onChange={(e) => setTransportadora(e.target.value)}
              maxLength={150}
              className={inputClass}
            />
          </Campo>
          {exportacion ? (
            <Campo label="Guía aérea AWB (opcional)" htmlFor="awb">
              <input
                id="awb"
                value={awb}
                onChange={(e) => setAwb(e.target.value)}
                maxLength={50}
                className={`${inputClass} font-mono`}
              />
            </Campo>
          ) : null}
          <Campo
            label={
              exportacion
                ? "Ciudad de destino (opcional)"
                : "Ciudad de entrega (opcional)"
            }
            htmlFor="ciudad"
          >
            <input
              id="ciudad"
              value={ciudad}
              onChange={(e) => setCiudad(e.target.value)}
              maxLength={100}
              className={inputClass}
            />
          </Campo>
          {exportacion ? null : (
            <CampoAncho>
              <Campo
                label="Dirección de entrega (opcional)"
                htmlFor="direccion"
              >
                <input
                  id="direccion"
                  value={direccion}
                  onChange={(e) => setDireccion(e.target.value)}
                  maxLength={250}
                  className={inputClass}
                />
              </Campo>
            </CampoAncho>
          )}
          <CampoAncho>
            <Campo label="Notas (opcional)" htmlFor="notasVenta">
              <textarea
                id="notasVenta"
                value={notas}
                onChange={(e) => setNotas(e.target.value)}
                maxLength={2000}
                className={textareaClass}
              />
            </Campo>
          </CampoAncho>
        </RejillaCampos>
      </Seccion>

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
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
          className={botonPrimarioClass}
        >
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
