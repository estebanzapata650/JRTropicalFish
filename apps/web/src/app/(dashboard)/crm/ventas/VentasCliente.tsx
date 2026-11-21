"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { History, PackageOpen, PlaneTakeoff, Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import {
  ESTADOS_PAGO,
  ESTADOS_PREPARACION,
  ETIQUETAS_HISTORIAL_CRM,
  ORIGENES_CANAL,
  tonoPago,
  type Comercial,
  type Venta,
} from "@/lib/crm";
import { botonPrimarioClass, inputClass } from "@/lib/estilos";
import { dinero, fecha, moneda } from "@/lib/formato";
import { guardarVentaAction } from "../actions";
import type { ClienteOpcion } from "../pipeline/DialogosOportunidad";
import { FormularioVenta } from "./FormularioVenta";

type Pestana = "todas" | "exportacion" | "nacional";

export function VentasCliente({
  ventas,
  clientes,
  comerciales,
  puedeGestionar,
  puedeAsignar,
  verPedidos,
  verExportaciones,
  hoy,
}: {
  ventas: Venta[];
  clientes: ClienteOpcion[];
  comerciales: Comercial[];
  puedeGestionar: boolean;
  puedeAsignar: boolean;
  /** Con `inventario:leer`: enlace al pedido de bodega de cada venta. */
  verPedidos: boolean;
  /** Con `exportaciones:leer`: enlace al embarque (documentos) de cada venta de exportación. */
  verExportaciones: boolean;
  hoy: string;
}) {
  const router = useRouter();
  const [pestana, setPestana] = useState<Pestana>("todas");
  const [soloPendientes, setSoloPendientes] = useState(false);
  const [busqueda, setBusqueda] = useState("");
  const [editando, setEditando] = useState<Venta | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<Venta | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();

  const filtradas = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return ventas.filter(
      (v) =>
        (pestana === "todas" || v.tipoVenta === pestana) &&
        (!soloPendientes || v.estadoPago === "pendiente") &&
        (!texto || [v.codigo, v.clienteNombre, v.noGuiaAwb, v.oportunidadCodigo].some((x) => x?.toLowerCase().includes(texto))),
    );
  }, [ventas, pestana, soloPendientes, busqueda]);

  const totalCop = filtradas.reduce((s, v) => s + v.totalCop, 0);
  const pendientes = ventas.filter((v) => v.estadoPago === "pendiente").length;

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setError(null);
  };

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      const r = await guardarVentaAction(editando?.id ?? null, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  const contar = (p: Pestana) => (p === "todas" ? ventas.length : ventas.filter((v) => v.tipoVenta === p).length);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Ventas"
        descripcion="Órdenes firmes nacionales y de exportación."
        acciones={
          puedeGestionar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Venta directa
            </button>
          ) : null
        }
      />

      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div role="tablist" aria-label="Tipo de venta" className="flex gap-1 rounded-md border border-borde bg-superficie p-1">
          {(
            [
              ["todas", "Todas"],
              ["exportacion", "Exportación"],
              ["nacional", "Nacional"],
            ] as const
          ).map(([id, titulo]) => (
            <button
              key={id}
              type="button"
              role="tab"
              aria-selected={pestana === id}
              onClick={() => setPestana(id)}
              className={`flex-1 rounded px-3 py-2 text-sm transition-colors lg:flex-none ${
                pestana === id ? "bg-acento font-medium text-acento-texto" : "text-texto-suave hover:text-texto"
              }`}
            >
              {titulo} <span className="text-xs opacity-80">{contar(id)}</span>
            </button>
          ))}
        </div>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <button
            type="button"
            aria-pressed={soloPendientes}
            onClick={() => setSoloPendientes((v) => !v)}
            className={`inline-flex h-11 items-center justify-center gap-2 rounded-full border px-4 text-sm transition-colors ${
              soloPendientes ? "border-advertencia-texto bg-advertencia-fondo font-medium text-advertencia-texto" : "border-borde text-texto hover:bg-acento-suave"
            }`}
          >
            Pago pendiente <span className="tabular-nums">{pendientes}</span>
          </button>
          <label className="relative sm:w-72">
            <span className="sr-only">Buscar venta</span>
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
            <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Código, cliente o AWB…" className={`${inputClass} pl-9`} />
          </label>
        </div>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Venta</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Fecha</th>
            <th className="px-4 py-3 text-right font-medium">Total</th>
            <th className="hidden px-4 py-3 text-right font-medium lg:table-cell">Cajas</th>
            <th className="px-4 py-3 font-medium">Pago</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Preparación</th>
            <th className="hidden px-4 py-3 font-medium xl:table-cell">AWB / entrega</th>
            <th className="px-4 py-3 text-right font-medium">Acciones</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtradas.map((v) => (
            <tr key={v.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <Link href={`/crm/clientes/${v.clienteId}`} className="font-medium text-texto hover:text-acento hover:underline">
                  {v.clienteNombre}
                </Link>
                <p className="text-xs text-texto-suave">
                  {v.codigo} · {v.tipoVenta === "exportacion" ? "Exportación" : "Nacional"}
                  {v.oportunidadCodigo ? ` · ${v.oportunidadCodigo}` : ` · ${ORIGENES_CANAL[v.origenCanal]}`}
                </p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">{fecha(v.fechaPedido)}</td>
              <td className="px-4 py-3 text-right whitespace-nowrap tabular-nums">
                <span className="text-texto">{dinero(v.total, v.moneda)}</span>
                {v.moneda === "USD" ? <p className="text-xs text-texto-suave">{moneda(v.totalCop)}</p> : null}
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto lg:table-cell">{v.totalCajas}</td>
              <td className="px-4 py-3">
                <Etiqueta texto={ESTADOS_PAGO[v.estadoPago]} tono={tonoPago(v.estadoPago)} />
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">{ESTADOS_PREPARACION[v.estadoPreparacion]}</td>
              <td className="hidden px-4 py-3 text-texto xl:table-cell">
                {v.tipoVenta === "exportacion" ? (
                  <span className="font-mono text-xs">{v.noGuiaAwb ?? "—"}</span>
                ) : (
                  <span className="text-xs">{[v.ciudadEntrega, v.direccionEntrega].filter(Boolean).join(" · ") || "—"}</span>
                )}
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  {verExportaciones && v.tipoVenta === "exportacion" ? (
                    <Link
                      href={`/exportaciones?venta=${v.id}`}
                      title={`Embarque y documentos de ${v.codigo}`}
                      aria-label={`Embarque y documentos de ${v.codigo}`}
                      className="rounded-md p-2 text-texto-suave transition-colors hover:bg-acento-suave hover:text-texto"
                    >
                      <PlaneTakeoff className="size-4" aria-hidden />
                    </Link>
                  ) : null}
                  {verPedidos ? (
                    <Link
                      href={`/crm/pedidos?venta=${v.id}`}
                      title={`Pedido de bodega de ${v.codigo}`}
                      aria-label={`Pedido de bodega de ${v.codigo}`}
                      className="rounded-md p-2 text-texto-suave transition-colors hover:bg-acento-suave hover:text-texto"
                    >
                      <PackageOpen className="size-4" aria-hidden />
                    </Link>
                  ) : null}
                  <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${v.codigo}`} onClick={() => setHistorialDe(v)} />
                  {puedeGestionar ? <BotonIcono accion="editar" etiqueta={`Editar ${v.codigo}`} onClick={() => setEditando(v)} /> : null}
                </div>
              </td>
            </tr>
          ))}
          {filtradas.length === 0 ? (
            <tr>
              <td colSpan={8} className="px-4 py-8 text-center text-texto-suave">
                {ventas.length === 0 ? "Aún no hay ventas. Nacen al ganar una oportunidad o como venta directa." : "Ninguna venta coincide con el filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <p className="text-sm text-texto-suave">
        {filtradas.length} ventas · total <strong className="text-texto tabular-nums">{moneda(totalCop)}</strong>
      </p>

      <Dialogo titulo={editando ? `Editar ${editando.codigo}` : "Venta directa"} abierto={creando || editando !== null} onCerrar={cerrar} ancho="max-w-4xl">
        <FormularioVenta
          key={editando?.id ?? "nueva"}
          venta={editando}
          clientes={clientes}
          comerciales={comerciales}
          puedeAsignar={puedeAsignar}
          hoy={hoy}
          guardando={guardando}
          error={error}
          onGuardar={guardar}
          onCancelar={cerrar}
        />
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.codigo}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? <HistorialRemoto ruta={`/api/crm/ventas/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CRM} /> : null}
      </Dialogo>
    </div>
  );
}
