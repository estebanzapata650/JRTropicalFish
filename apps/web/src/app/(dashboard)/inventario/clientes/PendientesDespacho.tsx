"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { piezas, type PedidoClientePendiente } from "@/lib/inventario";
import { fecha } from "@/lib/formato";
import { selectClass } from "@/lib/estilos";

/**
 * «Pedidos por cliente sin despachar» de Looker, con lo que faltaba: si hay stock sano en acuarios para
 * despacharlo ya. (El stock es el total de la especie; varios pedidos pueden competir por él.)
 */
export function PendientesDespacho({ lineas }: { lineas: PedidoClientePendiente[] }) {
  const [cliente, setCliente] = useState("");
  const [soloSinStock, setSoloSinStock] = useState(false);
  const clientes = useMemo(() => [...new Map(lineas.map((l) => [l.clienteId, l.clienteNombre])).entries()].sort((a, b) => a[1].localeCompare(b[1])), [lineas]);
  const visibles = lineas.filter((l) => (!cliente || l.clienteId === cliente) && (!soloSinStock || l.stockSano < l.pendiente));

  return (
    <div className="flex flex-col gap-3">
      {lineas.length > 0 ? (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <select aria-label="Cliente" value={cliente} onChange={(e) => setCliente(e.target.value)} className={`${selectClass} sm:w-72`}>
            <option value="">Todos los clientes</option>
            {clientes.map(([id, nombre]) => (
              <option key={id} value={id}>
                {nombre}
              </option>
            ))}
          </select>
          <label className="inline-flex min-h-11 items-center gap-2 text-sm text-texto">
            <input type="checkbox" checked={soloSinStock} onChange={(e) => setSoloSinStock(e.target.checked)} className="size-4 accent-acento" />
            Solo lo que no alcanza con el stock
          </label>
        </div>
      ) : null}
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Pedido · cliente</th>
            <th className="px-3 py-2 text-right font-medium">Pendiente</th>
            <th className="hidden px-3 py-2 font-medium sm:table-cell">Stock</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((l) => (
            <tr key={`${l.pedidoId}-${l.productoId}`} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                <span className="text-texto italic">{l.nombreCientifico}</span>
                <p className="text-xs text-texto-suave md:hidden">
                  {l.clienteNombre} · {l.pedidoCodigo}
                </p>
              </td>
              <td className="hidden px-3 py-2 md:table-cell">
                <Link href={`/crm/pedidos?id=${l.pedidoId}`} className="font-medium text-acento hover:underline">
                  {l.pedidoCodigo}
                </Link>
                <p className="text-xs text-texto-suave">
                  {l.clienteNombre} · {fecha(l.fecha)}
                </p>
              </td>
              <td className="px-3 py-2 text-right tabular-nums">
                <span className="font-medium text-texto">{piezas(l.pendiente)}</span>
                {l.despachada > 0 ? <p className="text-xs text-texto-suave">de {piezas(l.cantidad)}</p> : null}
                <div className="mt-1 sm:hidden">
                  <EstadoStock linea={l} />
                </div>
              </td>
              <td className="hidden px-3 py-2 sm:table-cell">
                <EstadoStock linea={l} />
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-3 py-6 text-center text-texto-suave">
                {lineas.length === 0 ? "No hay pedidos de clientes pendientes por despachar." : "Nada pendiente con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
    </div>
  );
}

function EstadoStock({ linea: l }: { linea: PedidoClientePendiente }) {
  return l.stockSano >= l.pendiente ? (
    <Etiqueta texto="Alcanza" tono="exito" />
  ) : l.stockSano > 0 ? (
    <Etiqueta texto={`Hay ${piezas(l.stockSano)}`} tono="advertencia" />
  ) : (
    <Etiqueta texto="Sin stock" tono="peligro" />
  );
}
