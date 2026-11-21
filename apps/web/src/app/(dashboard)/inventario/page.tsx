import type { Metadata } from "next";
import Link from "next/link";
import { AlertTriangle, Clock, Truck } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { apiFetch } from "@/lib/api";
import { ESTADOS_PEDIDO_CLIENTE, ESTADOS_PEDIDO_PROVEEDOR, pct, piezas, tonoPedidoCliente, tonoPedidoProveedor, type DashboardInventario } from "@/lib/inventario";
import { fecha } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { Kpi } from "../crm/comun";
import { DisponibilidadPorEspecie } from "./DisponibilidadPorEspecie";

export const metadata: Metadata = { title: "Inventario" };

/**
 * Dashboard de inventario: los 10 indicadores de la app anterior (hoja «Inicio» del Excel BASES).
 * 1 disponibilidad por especie · 2 pedidos por llegar · 3 filtro por centro · 4 trazabilidad (lotes) ·
 * 5 acuarios con enfermos · 6 cumplimiento · 7 más vendidas · 8 proveedor más comprado ·
 * 9 lotes de más de 20 días · 10 pedidos de cliente ya pedidos o no.
 */
export default async function InventarioPage() {
  const { token } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const d = await apiFetch<DashboardInventario>("/api/v1/inventario/dashboard", { token });
  const acuariosEnfermos = [...new Map(d.existenciasEnfermas.map((e) => [e.acuarioCodigo ?? "Por ubicar", 0])).keys()];

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Inventario" descripcion="Disponibilidad, lo que viene en camino, lo pendiente por despachar y las alertas de bodega." />

      <section aria-label="Totales" className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        <Kpi titulo="Sanos en acuarios" valor={piezas(d.sanos)} />
        <Kpi titulo="Por ubicar" valor={piezas(d.porUbicar)} alerta={d.porUbicar > 0} detalle={d.porUbicar > 0 ? "Recibidos sin acuario" : undefined} />
        <Kpi titulo="Enfermos" valor={piezas(d.enfermos)} alerta={d.enfermos > 0} detalle={`${d.acuariosConEnfermos} acuarios`} />
        <Kpi titulo="Pendiente a clientes" valor={piezas(d.pendienteClientes)} detalle={`${d.pedidosClientePendientes.length} pedidos sin despachar`} />
        <Kpi titulo="En tránsito" valor={piezas(d.enTransito)} detalle={`${d.pedidosPorLlegar.length} pedidos a proveedor`} />
        <Kpi titulo="Lotes > 20 días" valor={`${d.lotesAntiguos} / ${d.lotesActivos}`} alerta={d.lotesAntiguos > 0} detalle="con stock" />
      </section>

      <DisponibilidadPorEspecie productos={d.productos} />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <Panel titulo="Pedidos a proveedor por llegar" icono={<Truck className="size-4 text-acento" aria-hidden />} vacio="No hay pedidos en camino." enlace={{ href: "/compras/pedidos", texto: "Ver pedidos" }}>
          {d.pedidosPorLlegar.map((p) => (
            <li key={p.id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <Link href={`/compras/pedidos?id=${p.id}`} className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
                  {p.codigo} · {p.proveedorNombre}
                </Link>
                <p className="text-xs text-texto-suave">
                  Pedido {fecha(p.fecha)}
                  {p.fechaEstimadaLlegada ? ` · llega ${fecha(p.fechaEstimadaLlegada)}` : ""} · {piezas(p.unidades)} u.
                </p>
              </div>
              <Etiqueta texto={ESTADOS_PEDIDO_PROVEEDOR[p.estado]} tono={tonoPedidoProveedor(p.estado)} />
            </li>
          ))}
        </Panel>

        <Panel titulo="Pedidos de clientes sin despachar" icono={<Clock className="size-4 text-acento" aria-hidden />} vacio="Todo despachado." enlace={{ href: "/crm/pedidos", texto: "Ver pedidos" }}>
          {d.pedidosClientePendientes.map((p) => (
            <li key={p.id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <Link href={`/crm/pedidos?id=${p.id}`} className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
                  {p.codigo} · {p.clienteNombre}
                </Link>
                <p className="text-xs text-texto-suave">
                  {fecha(p.fecha)} · {piezas(p.despachadas)} de {piezas(p.unidades)} despachados ({pct(p.cumplimiento)})
                </p>
              </div>
              <Etiqueta texto={ESTADOS_PEDIDO_CLIENTE[p.estado]} tono={tonoPedidoCliente(p.estado)} />
            </li>
          ))}
        </Panel>

        <Panel titulo={`Acuarios con enfermos (${acuariosEnfermos.length})`} icono={<AlertTriangle className="size-4 text-peligro" aria-hidden />} vacio="No hay peces enfermos." enlace={{ href: "/inventario/acuarios", texto: "Ver mapa" }}>
          {d.existenciasEnfermas.map((e) => (
            <li key={e.id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <p className="truncate text-sm text-texto">
                  <span className="font-mono">{e.acuarioCodigo ?? "Por ubicar"}</span> · <span className="italic">{e.nombreCientifico}</span>
                </p>
                <Link href={`/inventario/lotes/${e.loteId}`} className="text-xs text-acento hover:underline">
                  {e.loteCodigo}
                </Link>
              </div>
              <span className="shrink-0 text-sm font-semibold tabular-nums text-peligro">{piezas(e.cantidad)}</span>
            </li>
          ))}
        </Panel>

        <Panel titulo={`Lotes con más de 20 días (${d.lotesConMasDeVeinteDias.length})`} icono={<Clock className="size-4 text-advertencia-texto" aria-hidden />} vacio="Ningún lote antiguo con stock." enlace={{ href: "/inventario/lotes", texto: "Ver lotes" }}>
          {d.lotesConMasDeVeinteDias.slice(0, 12).map((l) => (
            <li key={l.id} className="flex items-center justify-between gap-3 py-2.5">
              <div className="min-w-0">
                <Link href={`/inventario/lotes/${l.id}`} className="block truncate text-sm font-medium text-texto hover:text-acento hover:underline">
                  {l.codigo} · <span className="italic font-normal">{l.nombreCientifico}</span>
                </Link>
                <p className="text-xs text-texto-suave">{l.proveedorNombre ?? "—"} · {pct(l.porcentajeMuertos)} muertos</p>
              </div>
              <span className="shrink-0 text-right text-sm tabular-nums">
                <span className="font-semibold text-texto">{piezas(l.disponible)}</span>
                <span className="block text-xs text-advertencia-texto">{l.diasEnBodega} días</span>
              </span>
            </li>
          ))}
        </Panel>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-2">
        <section className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-4">
          <h2 className="text-base font-semibold text-texto">Especies más vendidas (30 días)</h2>
          {d.masVendidas.length === 0 ? (
            <p className="text-sm text-texto-suave">Sin despachos en los últimos 30 días.</p>
          ) : (
            <ol className="flex flex-col gap-2">
              {d.masVendidas.map((v) => (
                <li key={v.productoId} className="flex flex-col gap-1" title={`${v.productoCodigo}: ${piezas(v.cantidad)}`}>
                  <div className="flex justify-between gap-2 text-sm">
                    <span className="truncate text-texto">
                      <span className="mr-1 font-mono text-xs text-texto-suave">{v.productoCodigo}</span>
                      <span className="italic">{v.nombreCientifico}</span>
                    </span>
                    <span className="shrink-0 font-medium tabular-nums text-texto">{piezas(v.cantidad)}</span>
                  </div>
                  <div className="h-2 rounded-full bg-neutro-fondo">
                    <div className="h-full rounded-full bg-acento" style={{ width: `${(v.cantidad / d.masVendidas[0].cantidad) * 100}%` }} />
                  </div>
                </li>
              ))}
            </ol>
          )}
        </section>

        <section className="flex flex-col gap-3">
          <h2 className="text-base font-semibold text-texto">Compras por proveedor y cumplimiento (30 días)</h2>
          <TablaResponsive compactaEnMovil>
            <EncabezadoTabla>
              <tr className="text-texto-suave">
                <th className="px-4 py-3 font-medium">Proveedor</th>
                <th className="px-4 py-3 text-right font-medium">Recibido</th>
                <th className="px-4 py-3 text-right font-medium">Cumplimiento</th>
              </tr>
            </EncabezadoTabla>
            <tbody>
              {d.proveedores.map((p) => (
                <tr key={p.proveedorId} className="border-b border-borde last:border-0">
                  <td className="px-4 py-3 text-texto">
                    {p.proveedorNombre}
                    <p className="text-xs text-texto-suave">{p.recepciones} recepciones</p>
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums text-texto">
                    {piezas(p.unidades)} <span className="text-xs text-texto-suave">/ {piezas(p.pedidas)}</span>
                  </td>
                  <td className={`px-4 py-3 text-right tabular-nums ${p.cumplimiento < 90 ? "font-medium text-advertencia-texto" : "text-texto"}`}>{pct(p.cumplimiento)}</td>
                </tr>
              ))}
              {d.proveedores.length === 0 ? (
                <tr>
                  <td colSpan={3} className="px-4 py-6 text-center text-texto-suave">
                    Sin recepciones en los últimos 30 días.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </TablaResponsive>
        </section>
      </div>
    </div>
  );
}

function Panel({
  titulo,
  icono,
  vacio,
  enlace,
  children,
}: {
  titulo: string;
  icono: React.ReactNode;
  vacio: string;
  enlace: { href: string; texto: string };
  children: React.ReactNode[];
}) {
  return (
    <section className="flex flex-col gap-2 rounded-md border border-borde bg-superficie p-4">
      <div className="flex items-center justify-between gap-2">
        <h2 className="flex items-center gap-2 text-base font-semibold text-texto">
          {icono} {titulo}
        </h2>
        <Link href={enlace.href} className="shrink-0 text-sm text-acento hover:underline">
          {enlace.texto}
        </Link>
      </div>
      {children.length === 0 ? <p className="text-sm text-texto-suave">{vacio}</p> : <ul className="flex flex-col divide-y divide-borde">{children}</ul>}
    </section>
  );
}
