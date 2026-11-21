import type { Metadata } from "next";
import Link from "next/link";
import { PageHeader } from "@/components/PageHeader";
import { BarrasRanking, ListaMedidores } from "@/components/Graficos";
import { apiFetch } from "@/lib/api";
import { pct, piezas, type TableroClientes } from "@/lib/inventario";
import { fecha } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { Kpi } from "../../crm/comun";
import { Panel, PestanasTableros, SelectorPeriodo, rangoPeriodo } from "../tableros";
import { PendientesDespacho } from "./PendientesDespacho";

export const metadata: Metadata = { title: "Inventario · Clientes" };

/**
 * Tablero «Clientes» (Looker: Clientes / Despachos). Lo pendiente por despachar y si el stock alcanza;
 * cumplimiento por cliente (sin el 1536 % de Looker: nunca supera lo pedido); a quién se despacha y qué
 * especies salen más. Un comercial sin `crm:ver-todo` ve solo sus clientes.
 */
export default async function ClientesTableroPage({ searchParams }: { searchParams: Promise<{ periodo?: string }> }) {
  const { token } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const { periodo, desde, hasta } = rangoPeriodo((await searchParams).periodo);
  const d = await apiFetch<TableroClientes>(`/api/v1/inventario/tableros/clientes?desde=${desde}&hasta=${hasta}`, { token });

  const conPedidos = d.clientes.filter((c) => c.solicitadas > 0).sort((a, b) => a.cumplimiento - b.cumplimiento);
  const conDespachos = d.clientes.filter((c) => c.despachadoPeriodo > 0);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Inventario" descripcion="Lo pendiente por despachar, el cumplimiento con cada cliente y lo que más sale." />
      <PestanasTableros actual="/inventario/clientes" />
      <div className="flex flex-col gap-2">
        <SelectorPeriodo ruta="/inventario/clientes" actual={periodo} />
        <p className="text-xs text-texto-suave">
          {fecha(d.desde)} – {fecha(d.hasta)}. Lo pendiente por despachar no depende del periodo.
        </p>
      </div>

      <section aria-label="Totales de clientes" className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <Kpi titulo="Pedidos por despachar" valor={piezas(d.pedidosPendientes)} detalle="Confirmados o en preparación" />
        <Kpi titulo="Peces por despachar" valor={piezas(d.unidadesPendientes)} />
        <Kpi titulo="Despachados" valor={piezas(d.unidadesDespachadas)} detalle="Salidas del periodo" />
        <Kpi
          titulo="Cumplimiento (in full)"
          valor={d.solicitadas > 0 ? pct(d.cumplimiento) : "—"}
          avance={d.solicitadas > 0 ? d.cumplimiento : undefined}
          detalle={d.solicitadas > 0 ? `${piezas(d.despachadasPedidos)} de ${piezas(d.solicitadas)} pedidos` : "Sin pedidos en el periodo"}
        />
      </section>

      <Panel titulo="Pendiente por despachar" ayuda="Por especie, con el stock sano en acuarios de esa especie." accion={<Link href="/crm/pedidos" className="text-acento hover:underline">Pedidos de clientes</Link>}>
        <PendientesDespacho lineas={d.pendientes} />
      </Panel>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Panel titulo="Cumplimiento por cliente" ayuda="Despachado sobre lo pedido en los pedidos del periodo. Los más bajos primero.">
          <ListaMedidores
            etiqueta="Cumplimiento por cliente"
            vacio="Sin pedidos de clientes en el periodo."
            filas={conPedidos.map((c) => ({
              clave: c.clienteId,
              etiqueta: c.clienteNombre,
              porcentaje: c.cumplimiento,
              detalle: `${piezas(c.despachadasPedidos)} de ${piezas(c.solicitadas)} · ${c.pedidos} ${c.pedidos === 1 ? "pedido" : "pedidos"}`,
            }))}
          />
        </Panel>

        <Panel titulo="Despachos por cliente" ayuda="Peces enviados a cada cliente en el periodo (antes era una torta).">
          <BarrasRanking
            etiqueta="Peces despachados por cliente"
            vacio="Sin despachos en el periodo."
            filas={conDespachos.map((c) => ({
              clave: c.clienteId,
              etiqueta: c.clienteNombre,
              valor: c.despachadoPeriodo,
              texto: piezas(c.despachadoPeriodo),
              detalle: `${pct((c.despachadoPeriodo * 100) / Math.max(d.unidadesDespachadas, 1))} del total`,
            }))}
          />
        </Panel>
      </div>

      <Panel titulo="20 especies más vendidas" ayuda="Por peces despachados en el periodo.">
        <BarrasRanking
          etiqueta="Especies más vendidas"
          vacio="Sin despachos en el periodo."
          filas={d.especies.map((e) => ({
            clave: e.productoId,
            etiqueta: (
              <>
                <span className="mr-1 font-mono text-xs text-texto-suave">{e.productoCodigo}</span>
                <span className="italic">{e.nombreCientifico}</span>
              </>
            ),
            valor: e.cantidad,
            texto: piezas(e.cantidad),
            detalle: `${e.clientes} ${e.clientes === 1 ? "cliente" : "clientes"}`,
          }))}
        />
      </Panel>
    </div>
  );
}
