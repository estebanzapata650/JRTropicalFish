"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Search } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { DIAS_LOTE_ANTIGUO, pct, piezas, type Lote } from "@/lib/inventario";
import { casillaClass, inputClass } from "@/lib/estilos";
import { fecha, moneda } from "@/lib/formato";

/**
 * Trazabilidad por lote (indicador 4 de la app anterior): proveedor, lo que llegó, lo que queda,
 * lo despachado, lo muerto y el % de muertos. Los de más de 20 días con stock se marcan.
 */
export function LotesCliente({ lotes, verCostos }: { lotes: Lote[]; verCostos: boolean }) {
  const [busqueda, setBusqueda] = useState("");
  const [conStock, setConStock] = useState(true);
  const [antiguos, setAntiguos] = useState(false);

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return lotes.filter(
      (l) =>
        (!conStock || l.disponible > 0) &&
        (!antiguos || (l.disponible > 0 && l.diasEnBodega > DIAS_LOTE_ANTIGUO)) &&
        (!t || [l.codigo, l.productoCodigo, l.nombreCientifico, l.nombreEspanol, l.proveedorNombre, l.centroNombre].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [lotes, busqueda, conStock, antiguos]);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Lotes" descripcion="Cada llegada de una especie es un lote: de dónde vino, cuánto queda y qué pasó con él." />
      <div className="flex flex-col gap-3 md:flex-row md:items-center">
        <label className="relative md:flex-1">
          <span className="sr-only">Buscar lote</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Lote, especie, proveedor o centro…" className={`${inputClass} pl-9`} />
        </label>
        <label className="flex items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={conStock} onChange={(e) => setConStock(e.target.checked)} className={casillaClass} />
          Solo con stock
        </label>
        <label className="flex items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={antiguos} onChange={(e) => setAntiguos(e.target.checked)} className={casillaClass} />
          Más de {DIAS_LOTE_ANTIGUO} días
        </label>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Lote · especie</th>
            <th className="hidden px-4 py-3 font-medium lg:table-cell">Proveedor · centro</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Ingreso</th>
            <th className="hidden px-4 py-3 text-right font-medium xl:table-cell">Recibidos</th>
            <th className="px-4 py-3 text-right font-medium">Queda</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Despachados</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">% muertos</th>
            {verCostos ? <th className="hidden px-4 py-3 text-right font-medium xl:table-cell">Costo u.</th> : null}
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((l) => (
            <tr key={l.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <Link href={`/inventario/lotes/${l.id}`} className="font-medium text-acento hover:underline">
                  {l.codigo}
                </Link>
                <p className="text-sm text-texto">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                  <span className="italic">{l.nombreCientifico}</span>
                </p>
              </td>
              <td className="hidden px-4 py-3 text-texto lg:table-cell">
                {l.proveedorNombre ?? "—"}
                <p className="text-xs text-texto-suave">{l.centroNombre}</p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap md:table-cell">
                <span className="text-texto">{fecha(l.fechaIngreso)}</span>
                <p className={`text-xs ${l.diasEnBodega > DIAS_LOTE_ANTIGUO && l.disponible > 0 ? "font-medium text-advertencia-texto" : "text-texto-suave"}`}>
                  {l.diasEnBodega} días{l.origen === "saldo_inicial" ? " · migrado" : ""}
                </p>
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto xl:table-cell">{piezas(l.recibidos)}</td>
              <td className="px-4 py-3 text-right tabular-nums">
                <span className="font-medium text-texto">{piezas(l.disponible)}</span>
                <div className="mt-1 flex flex-wrap justify-end gap-1">
                  {l.porUbicar > 0 ? <Etiqueta texto={`${piezas(l.porUbicar)} por ubicar`} tono="advertencia" /> : null}
                  {l.enfermos > 0 ? <Etiqueta texto={`${piezas(l.enfermos)} enf.`} tono="peligro" /> : null}
                </div>
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">{piezas(l.despachados)}</td>
              <td className={`hidden px-4 py-3 text-right tabular-nums sm:table-cell ${l.porcentajeMuertos > 5 ? "font-medium text-peligro" : "text-texto"}`}>
                {pct(l.porcentajeMuertos)}
              </td>
              {verCostos ? <td className="hidden px-4 py-3 text-right tabular-nums text-texto xl:table-cell">{moneda(l.costoUnitario)}</td> : null}
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={8} className="px-4 py-8 text-center text-texto-suave">
                No hay lotes con ese filtro.
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <p className="text-sm text-texto-suave">
        {visibles.length} lotes · {piezas(visibles.reduce((s, l) => s + l.disponible, 0))} peces
      </p>
    </div>
  );
}
