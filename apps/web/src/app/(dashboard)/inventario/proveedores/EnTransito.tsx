"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { piezas, type CompraPendiente } from "@/lib/inventario";
import { fecha } from "@/lib/formato";
import { selectClass } from "@/lib/estilos";

/** Lo que viene en camino por especie, filtrable por proveedor y especie. Marca lo que ya debió llegar. */
export function EnTransito({ lineas, hoy }: { lineas: CompraPendiente[]; hoy: string }) {
  const [proveedor, setProveedor] = useState("");
  const [especie, setEspecie] = useState("");
  const proveedores = useMemo(() => [...new Map(lineas.map((l) => [l.proveedorId, l.proveedorNombre])).entries()].sort((a, b) => a[1].localeCompare(b[1])), [lineas]);
  const especies = useMemo(
    () => [...new Map(lineas.map((l) => [l.productoId, `${l.productoCodigo} · ${l.nombreCientifico}`])).entries()].sort((a, b) => a[1].localeCompare(b[1])),
    [lineas],
  );
  const visibles = lineas.filter((l) => (!proveedor || l.proveedorId === proveedor) && (!especie || l.productoId === especie));

  return (
    <div className="flex flex-col gap-3">
      {lineas.length > 0 ? (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <select aria-label="Proveedor" value={proveedor} onChange={(e) => setProveedor(e.target.value)} className={selectClass}>
            <option value="">Todos los proveedores</option>
            {proveedores.map(([id, nombre]) => (
              <option key={id} value={id}>
                {nombre}
              </option>
            ))}
          </select>
          <select aria-label="Especie" value={especie} onChange={(e) => setEspecie(e.target.value)} className={selectClass}>
            <option value="">Todas las especies</option>
            {especies.map(([id, nombre]) => (
              <option key={id} value={id}>
                {nombre}
              </option>
            ))}
          </select>
        </div>
      ) : null}
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Pedido · proveedor</th>
            <th className="px-3 py-2 font-medium">Llegada</th>
            <th className="px-3 py-2 text-right font-medium">Faltan</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((l) => {
            const atrasado = !!l.fechaEstimadaLlegada && l.fechaEstimadaLlegada < hoy;
            return (
              <tr key={`${l.pedidoId}-${l.productoId}`} className="border-b border-borde last:border-0">
                <td className="px-3 py-2">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                  <span className="text-texto italic">{l.nombreCientifico}</span>
                  <p className="text-xs text-texto-suave">
                    {l.centroNombre ?? "Sin centro"}
                    <span className="md:hidden"> · {l.proveedorNombre}</span>
                  </p>
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  <Link href={`/compras/pedidos?id=${l.pedidoId}`} className="font-medium text-acento hover:underline">
                    {l.pedidoCodigo}
                  </Link>
                  <p className="text-xs text-texto-suave">{l.proveedorNombre}</p>
                </td>
                <td className="px-3 py-2 whitespace-nowrap">
                  {l.fechaEstimadaLlegada ? <span className="text-texto">{fecha(l.fechaEstimadaLlegada)}</span> : <span className="text-texto-tenue">Sin fecha</span>}
                  {atrasado ? (
                    <p>
                      <Etiqueta texto="Atrasado" tono="advertencia" />
                    </p>
                  ) : null}
                </td>
                <td className="px-3 py-2 text-right tabular-nums">
                  <span className="font-medium text-texto">{piezas(l.pendiente)}</span>
                  {l.recibida > 0 ? <p className="text-xs text-texto-suave">de {piezas(l.cantidad)}</p> : null}
                </td>
              </tr>
            );
          })}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-3 py-6 text-center text-texto-suave">
                {lineas.length === 0 ? "No hay pedidos a proveedor en camino." : "Nada en camino con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
    </div>
  );
}
