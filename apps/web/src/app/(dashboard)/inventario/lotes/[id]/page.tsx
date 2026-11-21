import type { Metadata } from "next";
import type { ReactNode } from "react";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Fish, HeartPulse, PackageCheck, Skull, Thermometer, Truck } from "lucide-react";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { BarraSegmentada, LeyendaLote, partesLote } from "@/components/Graficos";
import { ApiError, apiFetch } from "@/lib/api";
import { TIPOS_MOVIMIENTO, dias, pct, piezas, type Despacho, type RecorridoLote } from "@/lib/inventario";
import { fecha, fechaHora, hoyNegocio, moneda } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";

export const metadata: Metadata = { title: "Recorrido del lote" };

/**
 * Recorrido del lote: de qué proveedor llegó, cuánto estuvo en aclimatación y en cuarentena, dónde está
 * hoy, a qué clientes se envió y cuántos murieron; abajo, el kardex completo. Pensado para sumar después
 * la bitácora de enfermedad y alimentación como eventos de la misma línea de tiempo.
 */
export default async function RecorridoLotePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const { token, permisos } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  let datos: RecorridoLote;
  try {
    datos = await apiFetch(`/api/v1/inventario/tableros/trazabilidad/${encodeURIComponent(id)}`, { token });
  } catch (err) {
    if (err instanceof ApiError && (err.status === 404 || err.status === 400)) notFound();
    throw err;
  }
  const { lote: l, tiempos: t, existencias, despachos, movimientos } = datos;
  const verCostos = permisos.includes(PERMISOS.CATALOGOS_VER_COSTOS);
  const enAcuarios = existencias.filter((e) => e.acuarioId && e.condicion === "sano");
  const enfermos = existencias.filter((e) => e.condicion === "enfermo");
  const muertosDespues = l.muertos - l.muertosLlegada;
  const porCliente = agruparPorCliente(despachos);
  const saldoInicial = l.origen === "saldo_inicial";

  const lugar = (codigo: string | null, condicion: string | null) =>
    condicion ? `${codigo ?? "aclimatación"}${condicion === "enfermo" ? " (enfermo)" : ""}` : "—";

  return (
    <div className="flex flex-col gap-6">
      <Link href="/inventario/lotes" className="inline-flex w-fit items-center gap-1.5 text-sm text-texto-suave hover:text-texto">
        <ArrowLeft className="size-4" aria-hidden /> Trazabilidad
      </Link>
      <header className="flex flex-col gap-1">
        <p className="text-xs font-medium text-texto-suave">
          {l.codigo} · {saldoInicial ? "Saldo inicial migrado" : `Recepción ${l.recepcionCodigo ?? ""}`}
        </p>
        <h1 className="text-xl font-semibold text-texto">
          <span className="mr-2 font-mono text-base text-texto-suave">{l.productoCodigo}</span>
          <span className="italic">{l.nombreCientifico}</span>
        </h1>
        {l.nombreEspanol ? <p className="text-sm text-texto-suave">{l.nombreEspanol}</p> : null}
      </header>

      <section aria-labelledby="titulo-composicion" className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
        <div className="flex flex-wrap items-baseline justify-between gap-2">
          <h2 id="titulo-composicion" className="text-base font-semibold text-texto">
            Dónde está cada pez
          </h2>
          <p className="text-sm text-texto-suave">
            De <strong className="text-texto">{piezas(l.recibidos)}</strong> recibidos, quedan <strong className="text-texto">{piezas(l.disponible)}</strong> en bodega
          </p>
        </div>
        <BarraSegmentada partes={partesLote(l)} etiqueta={`Lote ${l.codigo}`} gruesa />
        <LeyendaLote partes={partesLote(l)} base={l.recibidos} />
      </section>

      <section aria-labelledby="titulo-recorrido" className="flex flex-col gap-3">
        <h2 id="titulo-recorrido" className="text-base font-semibold text-texto">
          Recorrido
        </h2>
        <ol className="flex flex-col">
          <Paso icono={<Truck className="size-4" aria-hidden />} titulo="Llegada" fechaTexto={fecha(l.fechaIngreso)}>
            <p>
              De <strong className="text-texto">{l.proveedorNombre ?? "proveedor sin registrar"}</strong>
              {l.centroNombre ? ` · centro de acopio ${l.centroNombre}` : ""}.
            </p>
            <p>
              {piezas(l.recibidos)} recibidos
              {l.muertosLlegada ? `, ${piezas(l.muertosLlegada)} llegaron muertos (${pct((l.muertosLlegada * 100) / Math.max(l.recibidos, 1))})` : ", ninguno llegó muerto"}.
              {verCostos && l.costoUnitario != null ? ` Costo unitario ${moneda(l.costoUnitario)}.` : ""}
            </p>
            <p className="text-xs">Lleva {l.diasEnBodega} días desde que ingresó.</p>
          </Paso>

          <Paso icono={<Thermometer className="size-4" aria-hidden />} titulo="Aclimatación" estado={t.aclimatacionEnCurso ? <Etiqueta texto="En curso" tono="advertencia" /> : null}>
            {t.diasAclimatacion > 0 || !saldoInicial ? (
              <p>
                {t.aclimatacionEnCurso ? "Lleva" : "Estuvo"} <strong className="text-texto">{dias(t.diasAclimatacion)}</strong> en aclimatación
                {t.aclimatacionEnCurso ? `; ${piezas(l.porUbicar)} peces aún sin acuario.` : " hasta quedar ubicado en acuarios."}
              </p>
            ) : (
              <p>Migró ya ubicado en acuarios (el Excel no traía la aclimatación).</p>
            )}
          </Paso>

          <Paso icono={<Fish className="size-4" aria-hidden />} titulo="En acuarios">
            {enAcuarios.length === 0 ? (
              <p>No quedan peces sanos en acuarios.</p>
            ) : (
              <ul className="flex flex-wrap gap-2">
                {enAcuarios.map((e) => (
                  <li key={e.id} className="rounded-md border border-borde bg-superficie px-2.5 py-1 text-sm">
                    <span className="font-mono text-texto">{e.acuarioCodigo}</span>
                    <span className="ml-2 font-medium tabular-nums text-texto">{piezas(e.cantidad)}</span>
                  </li>
                ))}
              </ul>
            )}
          </Paso>

          <Paso
            icono={<HeartPulse className="size-4" aria-hidden />}
            titulo="Cuarentena"
            estado={t.cuarentenaEnCurso ? <Etiqueta texto="Enfermos hoy" tono="peligro" /> : null}
          >
            {t.episodiosCuarentena === 0 ? (
              <p>Sin peces enfermos registrados.</p>
            ) : (
              <>
                <p>
                  <strong className="text-texto">{dias(t.diasCuarentena)}</strong> con peces enfermos
                  {t.episodiosCuarentena > 1 ? ` en ${t.episodiosCuarentena} episodios` : ""}
                  {t.cuarentenaEnCurso ? " (sigue en curso)." : "."}
                </p>
                {enfermos.length > 0 ? (
                  <ul className="flex flex-wrap gap-2">
                    {enfermos.map((e) => (
                      <li key={e.id} className="rounded-md border border-peligro px-2.5 py-1 text-sm">
                        <span className="font-mono text-texto">{e.acuarioCodigo ?? "Aclimatación"}</span>
                        <span className="ml-2 font-medium tabular-nums text-peligro">{piezas(e.cantidad)} enfermos</span>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </>
            )}
          </Paso>

          <Paso
            icono={<PackageCheck className="size-4" aria-hidden />}
            titulo={`Enviado a clientes${porCliente.length ? ` (${porCliente.length})` : ""}`}
            fechaTexto={t.primerDespacho ? `${fecha(diaBogota(t.primerDespacho))}${t.ultimoDespacho && diaBogota(t.ultimoDespacho) !== diaBogota(t.primerDespacho) ? ` – ${fecha(diaBogota(t.ultimoDespacho))}` : ""}` : undefined}
          >
            {porCliente.length === 0 ? (
              <p>Aún no se ha enviado a ningún cliente.</p>
            ) : (
              <ul className="flex flex-col divide-y divide-borde rounded-md border border-borde">
                {porCliente.map((c) => (
                  <li key={c.clienteId} className="flex items-start justify-between gap-3 px-3 py-2">
                    <div className="min-w-0">
                      <p className="truncate font-medium text-texto">{c.clienteNombre}</p>
                      <p className="text-xs">
                        {c.pedidos.map((p, i) => (
                          <span key={p.id}>
                            {i > 0 ? ", " : ""}
                            <Link href={`/crm/pedidos?id=${p.id}`} className="text-acento hover:underline">
                              {p.codigo}
                            </Link>
                          </span>
                        ))}{" "}
                        · {fecha(diaBogota(c.ultima))}
                      </p>
                    </div>
                    <span className="shrink-0 font-semibold tabular-nums text-texto">{piezas(c.cantidad)}</span>
                  </li>
                ))}
              </ul>
            )}
          </Paso>

          <Paso icono={<Skull className="size-4" aria-hidden />} titulo="Mortalidad" ultimo>
            <p>
              <strong className="text-texto">{piezas(l.muertos)}</strong> muertos ({pct(l.porcentajeMuertos)} de lo recibido): {piezas(l.muertosLlegada)} al llegar y{" "}
              {piezas(muertosDespues)} en bodega.
            </p>
          </Paso>
        </ol>
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

/** Día de negocio (Bogotá) de un instante UTC. */
const diaBogota = (iso: string) => hoyNegocio(new Date(iso));

function agruparPorCliente(despachos: Despacho[]) {
  const mapa = new Map<string, { clienteId: string; clienteNombre: string; cantidad: number; ultima: string; pedidos: { id: string; codigo: string }[] }>();
  for (const d of despachos) {
    const c = mapa.get(d.clienteId) ?? { clienteId: d.clienteId, clienteNombre: d.clienteNombre, cantidad: 0, ultima: d.fecha, pedidos: [] };
    c.cantidad += d.cantidad;
    if (d.fecha > c.ultima) c.ultima = d.fecha;
    if (!c.pedidos.some((p) => p.id === d.pedidoId)) c.pedidos.push({ id: d.pedidoId, codigo: d.pedidoCodigo });
    mapa.set(d.clienteId, c);
  }
  return [...mapa.values()].sort((a, b) => b.cantidad - a.cantidad);
}

/** Un paso de la línea de tiempo: icono en un círculo, unido al siguiente por una línea vertical. */
function Paso({
  icono,
  titulo,
  fechaTexto,
  estado,
  ultimo = false,
  children,
}: {
  icono: ReactNode;
  titulo: string;
  fechaTexto?: string;
  estado?: ReactNode;
  ultimo?: boolean;
  children: ReactNode;
}) {
  return (
    <li className="grid grid-cols-[2rem_1fr] gap-3">
      <div className="flex flex-col items-center">
        <span className="flex size-8 shrink-0 items-center justify-center rounded-full border border-borde bg-acento-suave text-acento">{icono}</span>
        {ultimo ? null : <span className="w-px flex-1 bg-borde" aria-hidden />}
      </div>
      <div className={`flex min-w-0 flex-col gap-1.5 text-sm text-texto-suave ${ultimo ? "" : "pb-5"}`}>
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1 pt-1">
          <h3 className="font-semibold text-texto">{titulo}</h3>
          {fechaTexto ? <span className="text-xs">{fechaTexto}</span> : null}
          {estado}
        </div>
        {children}
      </div>
    </li>
  );
}
