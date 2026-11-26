"use client";

import Link from "next/link";
import { TriangleAlert } from "lucide-react";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { pesos, type CostoEmpaqueEmbarque } from "@/lib/exportaciones";
import { useDetalle } from "../../compras/useDetalle";

/**
 * Costo de empaque del despacho (con el empaque real) y las filas que se salen del estándar de su especie. `version`
 * cambia cuando se guarda el empaque o las cajas, para recalcular.
 */
export function CostoEmpaque({ embarqueId, version }: { embarqueId: string; version: number }) {
  const { cargando, datos, error } = useDetalle<CostoEmpaqueEmbarque>(`/api/exportaciones/empaque/embarques/${embarqueId}`, version);
  if (cargando) return <p className="text-sm text-texto-suave">Calculando el costo de empaque…</p>;
  if (error || !datos) return <p className="text-sm text-peligro">{error ?? "No se pudo calcular el costo de empaque."}</p>;
  if (datos.conceptos.length === 0) return null;

  return (
    <section aria-labelledby="titulo-costo-empaque" className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-3">
      <div className="flex flex-wrap items-baseline justify-between gap-2">
        <h2 id="titulo-costo-empaque" className="text-base font-semibold text-texto">
          Costo de empaque (interno)
        </h2>
        <p className="text-sm text-texto">
          <strong className="tabular-nums">{pesos(datos.total)}</strong>
          {datos.costoPorPez != null ? <span className="text-texto-suave"> · {pesos(datos.costoPorPez)} por pez</span> : null}
        </p>
      </div>

      {datos.fueraDeEstandar.length > 0 ? (
        <div className="flex flex-col gap-1 rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          <p className="flex items-center gap-2 font-medium">
            <TriangleAlert className="size-4 shrink-0" aria-hidden /> Fuera del estándar de empaque ({datos.fueraDeEstandar.length})
          </p>
          <ul className="flex flex-col gap-0.5">
            {datos.fueraDeEstandar.map((a, i) => (
              <li key={i}>
                Caja {a.numeroCaja} · {a.productoCodigo}: {a.texto}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Concepto</th>
            <th className="px-3 py-2 text-right font-medium">Cantidad</th>
            <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Unitario</th>
            <th className="px-3 py-2 text-right font-medium">Total</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {datos.conceptos.map((c) => (
            <tr key={c.concepto} className="border-b border-borde last:border-0">
              <td className="px-3 py-2 text-texto">{c.concepto}</td>
              <td className="px-3 py-2 text-right whitespace-nowrap tabular-nums text-texto">
                {c.cantidad.toLocaleString("es-CO")} <span className="text-xs text-texto-suave">{c.unidad}</span>
              </td>
              <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">{pesos(c.costoUnitario)}</td>
              <td className={`px-3 py-2 text-right tabular-nums ${c.total == null ? "text-advertencia-texto" : "text-texto"}`}>
                {c.total == null ? "Sin costo" : pesos(c.total)}
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      {datos.faltantes.length > 0 ? (
        <p className="text-xs text-texto-suave">
          El total no incluye lo que falta por cargar: {datos.faltantes.join(" · ")}. Se llena en{" "}
          <Link href="/exportaciones/empaque?vista=costos" className="text-acento underline">
            Exportaciones → Empaque → Costos
          </Link>
          .
        </p>
      ) : null}
    </section>
  );
}
