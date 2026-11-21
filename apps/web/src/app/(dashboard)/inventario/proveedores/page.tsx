import type { Metadata } from "next";
import Link from "next/link";
import { PageHeader } from "@/components/PageHeader";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { BarrasRanking, Columnas, ListaMedidores } from "@/components/Graficos";
import { apiFetch } from "@/lib/api";
import { pct, piezas, type TableroProveedores } from "@/lib/inventario";
import { fecha, hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { Kpi } from "../../crm/comun";
import { Panel, PestanasTableros, SelectorPeriodo, rangoPeriodo } from "../tableros";
import { EnTransito } from "./EnTransito";

export const metadata: Metadata = { title: "Inventario · Proveedores" };

/** Mortalidad que merece revisarse con el proveedor (por encima de esto se resalta). */
const MORTALIDAD_ALTA = 5;

const fechaCorta = (iso: string) => {
  const [a, m, d] = iso.split("-").map(Number);
  return new Date(Date.UTC(a, m - 1, d)).toLocaleDateString("es-CO", { day: "numeric", month: "short", timeZone: "UTC" });
};

/**
 * Tablero «Proveedores» (Looker: Pedidos en tránsito + Cumplimiento + Lotes por proveedor + Evolutivo).
 * Lo que viene en camino, si el proveedor cumplió (in full por línea, sin pasar de 100 %) y la calidad
 * de lo que manda (muertos al llegar y después).
 */
export default async function ProveedoresTableroPage({ searchParams }: { searchParams: Promise<{ periodo?: string }> }) {
  const { token } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const { periodo, desde, hasta } = rangoPeriodo((await searchParams).periodo);
  const d = await apiFetch<TableroProveedores>(`/api/v1/inventario/tableros/proveedores?desde=${desde}&hasta=${hasta}`, { token });

  const conPedidos = d.proveedores.filter((p) => p.pedidas > 0).sort((a, b) => a.cumplimiento - b.cumplimiento);
  const conRecibidos = d.proveedores.filter((p) => p.recibidos > 0);
  const porMortalidad = [...conRecibidos].sort((a, b) => b.porcentajeMuertos - a.porcentajeMuertos).slice(0, 10);
  const porCompras = conRecibidos.slice(0, 10);
  const otros = conRecibidos.slice(10);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Inventario" descripcion="Lo que viene en camino, si cada proveedor cumple y cómo llegan sus peces." />
      <PestanasTableros actual="/inventario/proveedores" />
      <div className="flex flex-col gap-2">
        <SelectorPeriodo ruta="/inventario/proveedores" actual={periodo} />
        <p className="text-xs text-texto-suave">
          {fecha(d.desde)} – {fecha(d.hasta)}. Lo que está en camino no depende del periodo.
        </p>
      </div>

      <section aria-label="Totales de proveedores" className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-5">
        <Kpi titulo="Pedidos en camino" valor={piezas(d.pedidosEnTransito)} detalle={`${d.centrosEnTransito} centros de acopio`} />
        <Kpi titulo="Peces en camino" valor={piezas(d.unidadesEnTransito)} detalle="Pendientes por recibir" />
        <Kpi
          titulo="Cumplimiento (in full)"
          valor={d.pedidas > 0 ? pct(d.cumplimiento) : "—"}
          avance={d.pedidas > 0 ? d.cumplimiento : undefined}
          detalle={d.pedidas > 0 ? `${piezas(d.recibidasEnFull)} de ${piezas(d.pedidas)} pedidos` : "Sin pedidos en el periodo"}
        />
        <Kpi titulo="Recibidos" valor={piezas(d.recibidos)} detalle="Lotes que ingresaron en el periodo" />
        <Kpi
          titulo="Mortalidad"
          valor={d.recibidos > 0 ? pct(d.porcentajeMuertos) : "—"}
          alerta={d.porcentajeMuertos > MORTALIDAD_ALTA}
          detalle={`${piezas(d.muertos)} muertos (al llegar y en bodega)`}
        />
      </section>

      <Panel titulo="En camino" ayuda="Líneas de pedidos enviados, en tránsito o recibidos en parte." accion={<Link href="/compras/pedidos" className="text-acento hover:underline">Pedidos a proveedor</Link>}>
        <EnTransito lineas={d.enTransito} hoy={hoyNegocio()} />
      </Panel>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Panel titulo="Cumplimiento por proveedor" ayuda="Lo recibido hasta lo pedido, por especie. Los más bajos primero.">
          <ListaMedidores
            etiqueta="Cumplimiento por proveedor"
            vacio="Sin pedidos a proveedor en el periodo."
            filas={conPedidos.map((p) => ({
              clave: p.proveedorId ?? p.proveedorNombre,
              etiqueta: p.proveedorNombre,
              porcentaje: p.cumplimiento,
              detalle: `${piezas(p.recibidasEnFull)} de ${piezas(p.pedidas)} · ${p.pedidos} ${p.pedidos === 1 ? "pedido" : "pedidos"}`,
            }))}
          />
        </Panel>

        <Panel titulo="Mortalidad por proveedor" ayuda="Muertos sobre recibidos de los lotes del periodo. Los más altos primero; la escala llega al menos a 10 %.">
          <BarrasRanking
            etiqueta="Mortalidad por proveedor"
            vacio="Sin lotes recibidos en el periodo."
            escalaMinima={10}
            filas={porMortalidad.map((p) => ({
              clave: p.proveedorId ?? p.proveedorNombre,
              etiqueta: p.proveedorNombre,
              valor: p.porcentajeMuertos,
              texto: pct(p.porcentajeMuertos),
              detalle: `${piezas(p.muertosLlegada)} al llegar · ${piezas(p.muertosDespues)} en bodega · de ${piezas(p.recibidos)}`,
            }))}
          />
        </Panel>

        <Panel titulo="Compras por proveedor" ayuda="Peces recibidos en el periodo (antes era una torta).">
          <BarrasRanking
            etiqueta="Peces recibidos por proveedor"
            vacio="Sin lotes recibidos en el periodo."
            filas={[
              ...porCompras.map((p) => ({
                clave: p.proveedorId ?? p.proveedorNombre,
                etiqueta: p.proveedorNombre,
                valor: p.recibidos,
                texto: piezas(p.recibidos),
                detalle: `${p.lotes} ${p.lotes === 1 ? "lote" : "lotes"} · ${pct((p.recibidos * 100) / Math.max(d.recibidos, 1))} del total`,
              })),
              ...(otros.length
                ? [
                    {
                      clave: "otros",
                      etiqueta: `Otros ${otros.length} proveedores`,
                      valor: otros.reduce((s, p) => s + p.recibidos, 0),
                      texto: piezas(otros.reduce((s, p) => s + p.recibidos, 0)),
                    },
                  ]
                : []),
            ]}
          />
        </Panel>

        <Panel titulo="Recibidos por semana" ayuda="Peces que ingresaron cada semana; el detalle muestra los muertos.">
          <Columnas
            etiqueta="Peces recibidos por semana"
            puntos={d.semanas.map((s) => ({
              clave: s.semana,
              eje: fechaCorta(s.semana),
              valor: s.recibidos,
              tooltip: `Semana del ${fechaCorta(s.semana)}: ${piezas(s.recibidos)} recibidos · ${piezas(s.muertos)} muertos (${pct(s.porcentajeMuertos)})`,
            }))}
          />
          <details className="text-sm">
            <summary className="cursor-pointer text-acento">Ver como tabla</summary>
            <TablaResponsive compactaEnMovil>
              <EncabezadoTabla>
                <tr className="text-texto-suave">
                  <th className="px-3 py-2 font-medium">Semana</th>
                  <th className="px-3 py-2 text-right font-medium">Recibidos</th>
                  <th className="px-3 py-2 text-right font-medium">Muertos</th>
                  <th className="px-3 py-2 text-right font-medium">%</th>
                </tr>
              </EncabezadoTabla>
              <tbody>
                {d.semanas.map((s) => (
                  <tr key={s.semana} className="border-b border-borde last:border-0">
                    <td className="px-3 py-2 text-texto">{fecha(s.semana)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-texto">{piezas(s.recibidos)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-texto">{piezas(s.muertos)}</td>
                    <td className="px-3 py-2 text-right tabular-nums text-texto">{pct(s.porcentajeMuertos)}</td>
                  </tr>
                ))}
              </tbody>
            </TablaResponsive>
          </details>
        </Panel>
      </div>

      <Panel titulo="Pedidos del periodo" ayuda="Cumplimiento de cada pedido a proveedor.">
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Pedido</th>
              <th className="hidden px-3 py-2 font-medium sm:table-cell">Fecha</th>
              <th className="px-3 py-2 text-right font-medium">Recibido</th>
              <th className="px-3 py-2 text-right font-medium">In full</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {d.pedidos.map((p) => (
              <tr key={p.pedidoId} className="border-b border-borde last:border-0">
                <td className="px-3 py-2">
                  <Link href={`/compras/pedidos?id=${p.pedidoId}`} className="font-medium whitespace-nowrap text-acento hover:underline">
                    {p.pedidoCodigo}
                  </Link>
                  <p className="text-xs text-texto-suave">{p.proveedorNombre}</p>
                </td>
                <td className="hidden px-3 py-2 whitespace-nowrap text-texto sm:table-cell">{fecha(p.fecha)}</td>
                <td className="px-3 py-2 text-right tabular-nums text-texto">
                  {piezas(p.recibidas)} <span className="text-xs text-texto-suave">/ {piezas(p.pedidas)}</span>
                </td>
                <td className={`px-3 py-2 text-right font-medium tabular-nums ${p.cumplimiento < 80 ? "text-peligro" : "text-texto"}`}>{pct(p.cumplimiento)}</td>
              </tr>
            ))}
            {d.pedidos.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-3 py-6 text-center text-texto-suave">
                  Sin pedidos a proveedor en el periodo.
                </td>
              </tr>
            ) : null}
          </tbody>
        </TablaResponsive>
      </Panel>
    </div>
  );
}
