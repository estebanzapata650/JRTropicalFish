"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Search } from "lucide-react";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { piezas, type UbicacionAcuario } from "@/lib/inventario";
import { inputClass } from "@/lib/estilos";

const POR_PAGINA = 15;

/** «Ubicaciones – cantidad por acuario» de Looker: qué hay en cada acuario, buscable por acuario o especie. */
export function UbicacionesAcuario({ ubicaciones }: { ubicaciones: UbicacionAcuario[] }) {
  const [busqueda, setBusqueda] = useState("");
  const [mostrar, setMostrar] = useState(POR_PAGINA);
  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return t
      ? ubicaciones.filter(
          (u) => u.acuarioCodigo.toLowerCase().includes(t) || u.especies.some((e) => `${e.productoCodigo} ${e.nombreCientifico}`.toLowerCase().includes(t)),
        )
      : ubicaciones;
  }, [ubicaciones, busqueda]);

  return (
    <div className="flex flex-col gap-3">
      <label className="relative">
        <span className="sr-only">Buscar acuario o especie</span>
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
        <input
          value={busqueda}
          onChange={(e) => {
            setBusqueda(e.target.value);
            setMostrar(POR_PAGINA);
          }}
          placeholder="Acuario o especie…"
          className={`${inputClass} pl-9`}
        />
      </label>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Acuario</th>
            <th className="hidden px-3 py-2 font-medium sm:table-cell">Especies</th>
            <th className="px-3 py-2 text-right font-medium">Peces</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.slice(0, mostrar).map((u) => (
            <tr key={u.acuarioId} className="border-b border-borde last:border-0">
              <td className="px-3 py-2 align-top">
                <Link href={`/inventario/existencias?q=${encodeURIComponent(u.acuarioCodigo)}`} className="font-mono text-acento hover:underline">
                  {u.acuarioCodigo}
                </Link>
                <p className="text-xs text-texto-suave">Sector {u.sector}</p>
                <p className="text-xs text-texto sm:hidden">
                  <span className="italic">{u.especies[0]?.nombreCientifico}</span>
                  {u.especies.length > 1 ? <span className="text-texto-suave"> +{u.especies.length - 1}</span> : null}
                </p>
              </td>
              <td className="hidden max-w-0 px-3 py-2 align-top text-sm text-texto sm:table-cell">
                {u.especies.slice(0, 2).map((e) => (
                  <p key={e.productoCodigo} className="truncate">
                    <span className="mr-1 font-mono text-xs text-texto-suave">{e.productoCodigo}</span>
                    <span className="italic">{e.nombreCientifico}</span>
                  </p>
                ))}
                {u.especies.length > 2 ? <p className="text-xs text-texto-suave">+{u.especies.length - 2} especies más</p> : null}
              </td>
              <td className="px-3 py-2 text-right align-top tabular-nums">
                <span className="font-medium text-texto">{piezas(u.total)}</span>
                {u.enfermos > 0 ? <p className="text-xs text-peligro">{piezas(u.enfermos)} enfermos</p> : null}
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={3} className="px-3 py-6 text-center text-texto-suave">
                {ubicaciones.length === 0 ? "No hay peces en acuarios." : "Ningún acuario con esa búsqueda."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      {mostrar < visibles.length ? (
        <button type="button" onClick={() => setMostrar((m) => m + POR_PAGINA)} className="w-fit text-sm font-medium text-acento hover:underline">
          Mostrar más ({visibles.length - mostrar} acuarios)
        </button>
      ) : null}
    </div>
  );
}
