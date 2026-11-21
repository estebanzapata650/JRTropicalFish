import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { ApiError, apiFetch } from "@/lib/api";
import { TIPOS_MOVIMIENTO, pct, piezas, type Existencia, type Lote, type Movimiento } from "@/lib/inventario";
import { fecha, fechaHora, moneda } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";

export const metadata: Metadata = { title: "Trazabilidad de lote" };

/** Ficha del lote: datos de origen, dónde está hoy y su kardex (todos los movimientos, inmutables). */
export default async function LotePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  let datos: { lote: Lote; existencias: Existencia[]; movimientos: Movimiento[] };
  try {
    datos = await apiFetch(`/api/v1/inventario/lotes/${encodeURIComponent(id)}`, { token });
  } catch (err) {
    if (err instanceof ApiError && (err.status === 404 || err.status === 400)) notFound();
    throw err;
  }
  const { lote: l, existencias, movimientos } = datos;
  const verCostos = permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS);

  const lugar = (codigo: string | null, condicion: string | null) =>
    condicion ? `${codigo ?? "por ubicar"}${condicion === "enfermo" ? " (enfermo)" : ""}` : "—";

  return (
    <div className="flex flex-col gap-6">
      <Link href="/inventario/lotes" className="inline-flex w-fit items-center gap-1.5 text-sm text-texto-suave hover:text-texto">
        <ArrowLeft className="size-4" aria-hidden /> Lotes
      </Link>
      <header className="flex flex-col gap-1">
        <p className="text-xs font-medium text-texto-suave">
          {l.codigo} · {l.origen === "saldo_inicial" ? "Saldo inicial migrado" : `Recepción ${l.recepcionCodigo ?? ""}`}
        </p>
        <h1 className="text-xl font-semibold text-texto">
          <span className="mr-2 font-mono text-base text-texto-suave">{l.productoCodigo}</span>
          <span className="italic">{l.nombreCientifico}</span>
        </h1>
        <p className="text-sm text-texto-suave">
          {l.nombreEspanol} · {l.proveedorNombre ?? "Sin proveedor"}
          {l.centroNombre ? ` · ${l.centroNombre}` : ""} · ingresó el {fecha(l.fechaIngreso)} ({l.diasEnBodega} días)
        </p>
      </header>

      <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        <Dato titulo="Recibidos" valor={piezas(l.recibidos)} />
        <Dato titulo="Sanos ubicados" valor={piezas(l.sanos)} />
        <Dato titulo="Por ubicar" valor={piezas(l.porUbicar)} />
        <Dato titulo="Enfermos" valor={piezas(l.enfermos)} peligro={l.enfermos > 0} />
        <Dato titulo="Despachados" valor={piezas(l.despachados)} />
        <Dato titulo="Muertos" valor={`${piezas(l.muertos)} · ${pct(l.porcentajeMuertos)}`} peligro={l.porcentajeMuertos > 5} />
        {verCostos ? <Dato titulo="Costo unitario" valor={moneda(l.costoUnitario)} /> : null}
      </dl>

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Dónde está hoy</h2>
        {existencias.length === 0 ? (
          <p className="text-sm text-texto-suave">El lote ya no tiene stock.</p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {existencias.map((e) => (
              <li key={e.id} className={`rounded-md border px-3 py-2 text-sm ${e.condicion === "enfermo" ? "border-peligro bg-peligro/10" : "border-borde bg-superficie"}`}>
                <span className="font-mono text-texto">{e.acuarioCodigo ?? "Por ubicar"}</span>
                <span className="ml-2 font-medium tabular-nums text-texto">{piezas(e.cantidad)}</span>
                {e.condicion === "enfermo" ? <span className="ml-1 text-xs text-peligro">enfermos</span> : null}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Kardex ({movimientos.length})</h2>
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-4 py-3 font-medium">Fecha · tipo</th>
              <th className="hidden px-4 py-3 font-medium sm:table-cell">De → a</th>
              <th className="px-4 py-3 text-right font-medium">Cantidad</th>
              <th className="hidden px-4 py-3 font-medium md:table-cell">Referencia · usuario</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {movimientos.map((m) => (
              <tr key={m.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <Etiqueta texto={TIPOS_MOVIMIENTO[m.tipo] ?? m.tipo} tono={m.tipo === "mortalidad" || m.tipo === "enfermo" ? "peligro" : m.tipo === "despacho" ? "exito" : "neutro"} />
                  <p className="mt-1 text-xs text-texto-suave">{fechaHora(m.fecha)}</p>
                  <p className="text-xs text-texto-suave sm:hidden">
                    {lugar(m.acuarioOrigenCodigo, m.condicionOrigen)} → {lugar(m.acuarioDestinoCodigo, m.condicionDestino)}
                  </p>
                </td>
                <td className="hidden px-4 py-3 text-texto sm:table-cell">
                  {lugar(m.acuarioOrigenCodigo, m.condicionOrigen)} → {lugar(m.acuarioDestinoCodigo, m.condicionDestino)}
                </td>
                <td className="px-4 py-3 text-right font-medium tabular-nums text-texto">{piezas(m.cantidad)}</td>
                <td className="hidden px-4 py-3 text-texto md:table-cell">
                  {m.pedidoCodigo ?? m.recepcionCodigo ?? m.nota ?? "—"}
                  <p className="text-xs text-texto-suave">{m.usuarioNombre}</p>
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      </section>
    </div>
  );
}

function Dato({ titulo, valor, peligro = false }: { titulo: string; valor: string; peligro?: boolean }) {
  return (
    <div className="flex flex-col gap-0.5 rounded-md border border-borde bg-superficie p-3">
      <dt className="text-xs text-texto-suave">{titulo}</dt>
      <dd className={`text-lg font-semibold tabular-nums ${peligro ? "text-peligro" : "text-texto"}`}>{valor}</dd>
    </div>
  );
}
