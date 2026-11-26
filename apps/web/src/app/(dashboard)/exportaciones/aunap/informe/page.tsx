import type { Metadata } from "next";
import Link from "next/link";
import { Download, TriangleAlert } from "lucide-react";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { apiFetch } from "@/lib/api";
import { botonPrimarioClass, botonSecundarioClass } from "@/lib/estilos";
import type { InformeAunap } from "@/lib/exportaciones";
import { fecha, hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { SelectorMes } from "./SelectorMes";

export const metadata: Metadata = { title: "Informe AUNAP" };

const MESES = ["Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio", "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"];

/**
 * Informe mensual AUNAP (Formato Informe Mensual de la DTIV): lo que salió de bodega en las exportaciones del mes, por
 * especie y municipio de origen, con la VUCE, el DEX, la factura DIAN y el CITES. `?mes=2026-10`; por defecto, el mes
 * anterior (el que se reporta).
 */
export default async function InformeAunapPage({ searchParams }: { searchParams: Promise<{ mes?: string }> }) {
  const { token } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  const { mes } = await searchParams;
  const [anio, numero] = periodo(mes);
  const informe = await apiFetch<InformeAunap>(`/api/v1/exportaciones/aunap/informe/${anio}/${numero}`, { token });
  const total = informe.filas.reduce((s, f) => s + f.unidades, 0);
  const valor = `${anio}-${String(numero).padStart(2, "0")}`;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Informe mensual AUNAP"
        descripcion="Lo que de verdad salió de bodega en las exportaciones del mes, por especie y municipio de origen."
        acciones={
          <Link href="/exportaciones/aunap/origenes" className={botonSecundarioClass}>
            Orígenes y encabezado
          </Link>
        }
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <SelectorMes valor={valor} />
        {informe.filas.length > 0 ? (
          <a href={`/archivos/exportaciones/aunap/informe/${anio}/${numero}`} className={`${botonPrimarioClass} gap-2`}>
            <Download className="size-4" aria-hidden /> Descargar Excel
          </a>
        ) : null}
      </div>

      <p className="text-sm text-texto-suave">
        {MESES[numero - 1]} {anio}: {informe.embarques} {informe.embarques === 1 ? "embarque" : "embarques"} ·{" "}
        {total.toLocaleString("es-CO")} peces declarados
        {informe.excluidosCriadero > 0 ? ` · ${informe.excluidosCriadero.toLocaleString("es-CO")} de criadero no se reportan` : ""}.
      </p>

      {informe.avisos.length > 0 ? (
        <section aria-labelledby="avisos" className="flex flex-col gap-2 rounded-md bg-advertencia-fondo p-3 text-sm text-advertencia-texto">
          <h2 id="avisos" className="flex items-center gap-2 font-semibold">
            <TriangleAlert className="size-4 shrink-0" aria-hidden /> Revisa antes de enviar ({informe.avisos.length})
          </h2>
          <ul className="flex flex-col gap-1">
            {informe.avisos.map((a, i) => (
              <li key={i}>
                <Link href={`/exportaciones/${a.embarqueId}`} className="font-medium underline">
                  {a.embarqueCodigo}
                </Link>
                : {a.texto}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Origen</th>
            <th className="px-3 py-2 text-right font-medium">Unidades</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Destino</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Exportación</th>
            <th className="hidden px-3 py-2 font-medium xl:table-cell">VUCE · DEX · factura</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {informe.filas.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-3 py-8 text-center text-texto-suave">
                No hubo despachos de exportación en este mes.
              </td>
            </tr>
          ) : null}
          {informe.filas.map((f, i) => (
            <tr key={`${f.embarqueId}-${f.codigo}-${f.municipio}-${i}`} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-2">
                <span className="font-mono text-xs text-texto-suave">{f.codigo}</span> <span className="text-texto italic">{f.nombreCientifico}</span>
                <span className="block text-xs text-texto-suave">
                  {f.nombreComun ?? ""}
                  {f.familia ? ` · ${f.familia}` : ""}
                </span>
                <span className="block text-xs text-texto-suave md:hidden">
                  {f.municipio ? `${f.municipio} (${f.departamento}) · ${f.cuenca}` : "Sin origen"} · {fecha(f.fechaExportacion)}
                </span>
              </td>
              <td className="hidden px-3 py-2 md:table-cell">
                {f.municipio ? (
                  <>
                    <span className="text-texto">{f.municipio}</span>
                    <span className="block text-xs text-texto-suave">
                      {f.departamento} · {f.cuenca}
                    </span>
                  </>
                ) : (
                  <span className="text-advertencia-texto">Sin origen</span>
                )}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">{f.unidades.toLocaleString("es-CO")}</td>
              <td className="hidden px-3 py-2 lg:table-cell">
                <span className="text-texto">{f.ciudad ?? "—"}</span>
                <span className="block text-xs text-texto-suave">{f.pais}</span>
              </td>
              <td className="hidden px-3 py-2 whitespace-nowrap md:table-cell">
                <span className="text-texto">{fecha(f.fechaExportacion)}</span>
                <Link href={`/exportaciones/${f.embarqueId}`} className="block text-xs text-acento hover:underline">
                  {f.embarqueCodigo}
                </Link>
              </td>
              <td className="hidden px-3 py-2 text-xs text-texto-suave xl:table-cell">
                <span className="block">{f.vuceNumero ?? "Sin VUCE"}{f.vuceFecha ? ` · ${fecha(f.vuceFecha)}` : ""}</span>
                <span className="block">DEX {f.dexNumero ?? "—"} · {f.facturaDian ?? "sin factura"}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
    </div>
  );
}

/** "2026-10" → [2026, 10]; sin valor o inválido, el mes anterior al de hoy (Bogotá). */
function periodo(valor: string | undefined): [number, number] {
  const m = /^(\d{4})-(\d{2})$/.exec(valor ?? "");
  if (m && +m[2] >= 1 && +m[2] <= 12) return [+m[1], +m[2]];
  const [a, mes] = hoyNegocio().split("-").map(Number);
  return mes === 1 ? [a - 1, 12] : [a, mes - 1];
}
