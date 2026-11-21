"use client";

import { useState } from "react";
import Link from "next/link";
import { Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { piezas, type Recepcion, type RecepcionLinea } from "@/lib/inventario";
import { inputClass } from "@/lib/estilos";
import { fecha, moneda } from "@/lib/formato";
import { useDetalle } from "../useDetalle";

type RecepcionDetalle = { recepcion: Recepcion; lineas: RecepcionLinea[] };

/** Paso 8: cada llegada de mercancía y los lotes que creó. */
export function RecepcionesCliente({ recepciones, abrirId, verCostos }: { recepciones: Recepcion[]; abrirId: string | null; verCostos: boolean }) {
  const [verId, setVerId] = useState<string | null>(abrirId);
  const [busqueda, setBusqueda] = useState("");
  const t = busqueda.trim().toLowerCase();
  const visibles = recepciones.filter((r) => !t || [r.codigo, r.pedidoCodigo, r.proveedorNombre].some((v) => v.toLowerCase().includes(t)));

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Recepciones" descripcion="Llegadas de los pedidos a proveedor: lo recibido, enfermo y muerto, y los lotes creados." />
      <label className="relative sm:max-w-md">
        <span className="sr-only">Buscar recepción</span>
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
        <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Código, pedido o proveedor…" className={`${inputClass} pl-9`} />
      </label>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Recepción</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Fecha · registró</th>
            <th className="px-4 py-3 text-right font-medium">Recibidos</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Enf. · muertos</th>
            {verCostos ? <th className="hidden px-4 py-3 text-right font-medium lg:table-cell">Costo</th> : null}
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((r) => (
            <tr key={r.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <button type="button" onClick={() => setVerId(r.id)} className="text-left font-medium whitespace-nowrap text-acento hover:underline">
                  {r.codigo}
                </button>
                <p className="text-sm text-texto">
                  {r.pedidoCodigo} · {r.proveedorNombre}
                </p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">
                {fecha(r.fecha)}
                <p className="text-xs text-texto-suave">{r.usuarioNombre ?? "—"}</p>
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-texto">{piezas(r.unidades)}</td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">
                {piezas(r.enfermas)} · {piezas(r.muertas)}
              </td>
              {verCostos ? <td className="hidden px-4 py-3 text-right tabular-nums text-texto lg:table-cell">{moneda(r.costoTotal)}</td> : null}
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-4 py-8 text-center text-texto-suave">
                {recepciones.length === 0 ? "Aún no hay recepciones. Se registran desde el pedido a proveedor." : "Ninguna recepción con esa búsqueda."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo="Recepción" abierto={verId !== null} onCerrar={() => setVerId(null)} ancho="max-w-4xl">
        {verId ? <DetalleRecepcion ruta={`/api/compras/recepciones/${verId}`} verCostos={verCostos} /> : null}
      </Dialogo>
    </div>
  );
}

function DetalleRecepcion({ ruta, verCostos }: { ruta: string; verCostos: boolean }) {
  const { cargando, datos: d, error } = useDetalle<RecepcionDetalle>(ruta);
  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (error || !d) return <p className="text-sm text-peligro">{error ?? "No se encontró."}</p>;
  const r = d.recepcion;
  return (
    <div className="flex flex-col gap-4">
      <div>
        <p className="text-lg font-semibold text-texto">
          {r.codigo} · {r.proveedorNombre}
        </p>
        <p className="text-sm text-texto-suave">
          {fecha(r.fecha)} · pedido{" "}
          <Link href={`/compras/pedidos?id=${r.pedidoProveedorId}`} className="text-acento hover:underline">
            {r.pedidoCodigo}
          </Link>{" "}
          · {r.usuarioNombre ?? "—"}
        </p>
      </div>
      {r.observaciones ? <p className="text-sm whitespace-pre-line text-texto">{r.observaciones}</p> : null}
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie · lote</th>
            <th className="px-3 py-2 text-right font-medium">Llegó</th>
            <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Enf. · muertos</th>
            {verCostos ? <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Costo total</th> : null}
          </tr>
        </EncabezadoTabla>
        <tbody>
          {d.lineas.map((l) => (
            <tr key={l.id} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                <span className="text-texto italic">{l.nombreCientifico}</span>
                <p>
                  <Link href={`/inventario/lotes/${l.loteId}`} className="text-xs text-acento hover:underline">
                    {l.loteCodigo}
                  </Link>
                </p>
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">{piezas(l.recibida)}</td>
              <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">
                {piezas(l.enferma)} · {piezas(l.muerta)}
              </td>
              {verCostos ? <td className="hidden px-3 py-2 text-right tabular-nums text-texto md:table-cell">{moneda(l.costoTotal)}</td> : null}
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
      {verCostos ? (
        <p className="text-right text-sm text-texto-suave">
          Total: <strong className="text-texto tabular-nums">{moneda(r.costoTotal)}</strong>
        </p>
      ) : null}
    </div>
  );
}
