import type { Metadata } from "next";
import Link from "next/link";
import { AlertTriangle, Clock } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { apiFetch } from "@/lib/api";
import { ETAPAS, ORDEN_ETAPAS, type Dashboard } from "@/lib/crm";
import { dinero, fecha, moneda, monedaCorta } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { BarraAvance, Kpi } from "./comun";
import { SelectorMes } from "./SelectorMes";

export const metadata: Metadata = { title: "Dashboard comercial" };

const pct = (v: number) => `${v.toLocaleString("es-CO", { maximumFractionDigits: 1 })} %`;

/** `?mes=2026-11` → `2026-11-01`; cualquier otra cosa, el mes actual (lo decide la API). */
function mesDeConsulta(valor: string | string[] | undefined): string | null {
  return typeof valor === "string" && /^\d{4}-\d{2}$/.test(valor) ? `${valor}-01` : null;
}

export default async function DashboardCrmPage({ searchParams }: { searchParams: Promise<{ mes?: string | string[] }> }) {
  const { token } = await exigirPermiso(PERMISOS.METAS_LEER);
  const mes = mesDeConsulta((await searchParams).mes);
  const d = await apiFetch<Dashboard>(`/api/v1/crm/metricas/dashboard${mes ? `?mes=${mes}` : ""}`, { token });
  const p = d.parametros;

  const porEtapa = new Map(d.embudo.map((e) => [e.etapa, e]));
  const maximo = Math.max(1, ...d.embudo.map((e) => e.cantidad));
  const ganadas = porEtapa.get("ganada")?.cantidad ?? 0;
  const perdidas = porEtapa.get("perdida")?.cantidad ?? 0;
  const tasaCierre = ganadas + perdidas > 0 ? (ganadas / (ganadas + perdidas)) * 100 : null;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Dashboard comercial"
        descripcion="Ventas del mes contra la meta, embudo, alertas DOA y tareas vencidas."
        acciones={<SelectorMes valor={d.mes.slice(0, 7)} />}
      />

      <section aria-label="Indicadores del mes" className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Kpi
          titulo="Ventas del mes"
          valor={monedaCorta(d.ventasCop)}
          avance={d.porcentajeMetaEmpresa}
          detalle={`${pct(d.porcentajeMetaEmpresa)} de la meta de ${monedaCorta(p.metaEmpresaCop)}`}
        />
        <Kpi
          titulo="Despachos de exportación"
          valor={`${d.despachos} / ${p.puntoEquilibrioDespachos}`}
          avance={p.puntoEquilibrioDespachos > 0 ? (d.despachos / p.puntoEquilibrioDespachos) * 100 : 0}
          detalle={
            d.despachos >= p.puntoEquilibrioDespachos
              ? "Punto de equilibrio superado"
              : `Faltan ${p.puntoEquilibrioDespachos - d.despachos} para el punto de equilibrio`
          }
        />
        <Kpi
          titulo="Clientes nuevos"
          valor={`${d.clientesNuevos} / ${p.metaClientesNuevos}`}
          avance={p.metaClientesNuevos > 0 ? (d.clientesNuevos / p.metaClientesNuevos) * 100 : 0}
          detalle="Clientes con su primera venta este mes"
        />
        <Kpi
          titulo="Pool de comisiones"
          valor={monedaCorta(d.poolComision)}
          detalle={
            <>
              {pct(p.porcentajeComision)} de la venta neta {moneda(d.ventaNetaCop)}
              {d.fleteCop > 0 || d.doaCop > 0 ? ` (descontados flete ${moneda(d.fleteCop)} y DOA ${moneda(d.doaCop)})` : ""}
            </>
          }
        />
      </section>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <section aria-labelledby="titulo-comerciales" className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
          <h2 id="titulo-comerciales" className="text-base font-semibold text-texto">
            Avance por comercial
          </h2>
          {d.comerciales.length === 0 ? (
            <p className="text-sm text-texto-suave">Sin comerciales con meta ni ventas este mes.</p>
          ) : (
            <ul className="flex flex-col gap-4">
              {d.comerciales.map((c) => (
                <li key={c.comercialId} className="flex flex-col gap-1.5" title={`${c.nombre}: ${moneda(c.ventasCop)} de ${moneda(c.metaCop)}`}>
                  <div className="flex flex-wrap items-baseline justify-between gap-x-3">
                    <span className="text-sm font-medium text-texto">{c.nombre}</span>
                    <span className="text-sm tabular-nums text-texto">
                      {monedaCorta(c.ventasCop)} <span className="text-texto-suave">/ {monedaCorta(c.metaCop)} · {pct(c.porcentajeMeta)}</span>
                    </span>
                  </div>
                  <BarraAvance porcentaje={c.porcentajeMeta} etiqueta={`Avance de ${c.nombre}`} />
                  <p className="text-xs text-texto-suave">
                    {c.ventas} {c.ventas === 1 ? "venta" : "ventas"} · comisión estimada {moneda(c.comision)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section aria-labelledby="titulo-embudo" className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <h2 id="titulo-embudo" className="text-base font-semibold text-texto">
              Embudo comercial
            </h2>
            <p className="text-xs text-texto-suave">
              Activas hoy · cerradas en el mes{tasaCierre != null ? ` · tasa de cierre ${pct(tasaCierre)}` : ""}
            </p>
          </div>
          <table className="w-full text-sm">
            <caption className="sr-only">Oportunidades por etapa</caption>
            <thead className="sr-only">
              <tr>
                <th>Etapa</th>
                <th>Cantidad</th>
                <th>Valor</th>
              </tr>
            </thead>
            <tbody>
              {ORDEN_ETAPAS.map((etapa) => {
                const fila = porEtapa.get(etapa);
                const cantidad = fila?.cantidad ?? 0;
                const valor = [fila?.montoUsd ? dinero(fila.montoUsd, "USD") : null, fila?.montoCop ? dinero(fila.montoCop, "COP") : null]
                  .filter(Boolean)
                  .join(" · ");
                return (
                  <tr key={etapa} title={`${ETAPAS[etapa]}: ${cantidad}${valor ? ` · ${valor}` : ""}`}>
                    <th scope="row" className="w-36 py-1.5 pr-3 text-left font-normal whitespace-nowrap text-texto-suave">
                      {ETAPAS[etapa]}
                    </th>
                    <td className="py-1.5">
                      <div className="flex items-center gap-2">
                        <div className="h-5 min-w-0 flex-1">
                          <div
                            className={`h-full rounded-r ${etapa === "ganada" ? "bg-exito-texto" : etapa === "perdida" ? "bg-peligro" : "bg-acento"}`}
                            style={{ width: `${(cantidad / maximo) * 100}%`, minWidth: cantidad ? "4px" : 0 }}
                          />
                        </div>
                        <span className="w-6 text-right font-medium tabular-nums text-texto">{cantidad}</span>
                      </div>
                      {valor ? <p className="text-xs text-texto-suave">{valor}</p> : null}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <Link href="/crm/pipeline" className="text-sm font-medium text-acento hover:underline">
            Ir al pipeline →
          </Link>
        </section>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <section aria-labelledby="titulo-doa" className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-4">
          <h2 id="titulo-doa" className="flex items-center gap-2 text-base font-semibold text-texto">
            <AlertTriangle className="size-4 text-peligro" aria-hidden /> Alertas DOA (&gt; {pct(p.umbralDoa)})
            <span className="text-sm font-normal text-texto-suave">{d.alertasDoa.length}</span>
          </h2>
          {d.alertasDoa.length === 0 ? (
            <p className="text-sm text-texto-suave">Ningún despacho supera el umbral.</p>
          ) : (
            <ul className="flex flex-col divide-y divide-borde">
              {d.alertasDoa.map((t) => (
                <li key={t.id} className="flex items-center justify-between gap-3 py-2.5">
                  <div className="min-w-0">
                    <Link href="/crm/vuelos" className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
                      {t.clienteNombre}
                    </Link>
                    <p className="text-xs text-texto-suave">
                      {t.codigo} · AWB {t.noAwb ?? "—"} · {fecha(t.fechaDespachoVuelo)}
                      {t.notaCreditoNo ? ` · NC ${t.notaCreditoNo}` : " · sin nota crédito"}
                    </p>
                  </div>
                  <span className="shrink-0 rounded-md bg-peligro/15 px-2 py-1 text-sm font-semibold tabular-nums text-peligro">
                    {pct(t.porcentajeDoa ?? 0)}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section aria-labelledby="titulo-vencidas" className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-4">
          <h2 id="titulo-vencidas" className="flex items-center gap-2 text-base font-semibold text-texto">
            <Clock className="size-4 text-peligro" aria-hidden /> Tareas vencidas
            <span className="text-sm font-normal text-texto-suave">{d.tareasVencidas.length}</span>
          </h2>
          {d.tareasVencidas.length === 0 ? (
            <p className="text-sm text-texto-suave">Ninguna tarea vencida.</p>
          ) : (
            <ul className="flex flex-col divide-y divide-borde">
              {d.tareasVencidas.map((t) => (
                <li key={t.id} className="flex items-center justify-between gap-3 py-2.5">
                  <div className="min-w-0">
                    <Link href="/crm/tareas" className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
                      {t.accion}
                    </Link>
                    <p className="text-xs text-texto-suave">
                      {t.clienteNombre} · {t.comercialNombre}
                    </p>
                  </div>
                  <span className="shrink-0 text-sm font-semibold whitespace-nowrap text-peligro">hace {-t.diasRestantes} d</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}
