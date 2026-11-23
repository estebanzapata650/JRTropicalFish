"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { ShoppingCart } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import {
  Campo,
  EncabezadoTabla,
  Etiqueta,
  TablaResponsive,
} from "@/components/campos";
import { piezas, type InventarioProducto } from "@/lib/inventario";
import { botonPrimarioClass, selectClass } from "@/lib/estilos";
import { guardarPedidoProveedorAction } from "../../inventario/actions";

/**
 * Paso 6 del Excel: lo pedido por clientes (confirmado o en preparación) agrupado por especie y centro de
 * acopio, contra lo que hay sano y lo que ya viene en camino. Se marcan las especies a comprar y se arma
 * el pedido al proveedor en un clic, con el faltante como cantidad.
 */
export function NecesidadesCliente({
  necesidades,
  proveedores,
  puedeGestionar,
}: {
  necesidades: InventarioProducto[];
  proveedores: { id: string; nombre: string }[];
  puedeGestionar: boolean;
}) {
  const router = useRouter();
  const [centro, setCentro] = useState("");
  const [soloFaltantes, setSoloFaltantes] = useState(true);
  const [marcadas, setMarcadas] = useState<Set<string>>(new Set());
  const [proveedorId, setProveedorId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [creando, iniciar] = useTransition();

  const centros = useMemo(
    () =>
      [
        ...new Set(necesidades.map((n) => n.centroNombre ?? "Sin centro")),
      ].sort(),
    [necesidades],
  );
  const visibles = necesidades.filter(
    (n) =>
      (!centro || (n.centroNombre ?? "Sin centro") === centro) &&
      (!soloFaltantes || n.faltante > 0),
  );
  const seleccion = visibles.filter(
    (n) => marcadas.has(n.productoId) && n.faltante > 0,
  );

  const alternar = (id: string) =>
    setMarcadas((m) => {
      const s = new Set(m);
      if (s.has(id)) s.delete(id);
      else s.add(id);
      return s;
    });

  const crearPedido = () =>
    iniciar(async () => {
      const r = await guardarPedidoProveedorAction(null, {
        proveedorId,
        observaciones: "Creado desde necesidades de compra.",
        lineas: seleccion.map((n) => ({
          productoId: n.productoId,
          cantidad: n.faltante,
          costoUnitario: null,
          observacion: null,
        })),
      });
      if (r.error) {
        setError(r.error);
        return;
      }
      const id = (r.datos as { id: string } | undefined)?.id;
      router.push(id ? `/compras/pedidos?id=${id}` : "/compras/pedidos");
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Necesidades de compra"
        descripcion="Lo que piden los clientes contra lo que hay sano y lo que viene en camino."
      />

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <select
          aria-label="Centro de acopio"
          value={centro}
          onChange={(e) => setCentro(e.target.value)}
          className={`${selectClass} sm:w-60`}
        >
          <option value="">Todos los centros</option>
          {centros.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </select>
        <label className="inline-flex min-h-11 items-center gap-2 text-sm text-texto">
          <input
            type="checkbox"
            checked={soloFaltantes}
            onChange={(e) => setSoloFaltantes(e.target.checked)}
            className="size-4 accent-acento"
          />
          Solo lo que falta comprar
        </label>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            {puedeGestionar ? (
              <th className="w-10 px-3 py-3">
                <span className="sr-only">Marcar</span>
              </th>
            ) : null}
            <th className="px-4 py-3 font-medium">Especie</th>
            <th className="px-4 py-3 text-right font-medium">
              Pedido clientes
            </th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">
              Sano
            </th>
            <th className="hidden px-4 py-3 text-right font-medium md:table-cell">
              En tránsito
            </th>
            <th className="px-4 py-3 text-right font-medium">Faltante</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((n) => (
            <tr
              key={n.productoId}
              className="border-b border-borde last:border-0"
            >
              {puedeGestionar ? (
                <td className="px-3 py-3">
                  {n.faltante > 0 ? (
                    <input
                      type="checkbox"
                      checked={marcadas.has(n.productoId)}
                      onChange={() => alternar(n.productoId)}
                      aria-label={`Comprar ${n.nombreCientifico}`}
                      className="size-4 accent-acento"
                    />
                  ) : null}
                </td>
              ) : null}
              <td className="px-4 py-3">
                <span className="mr-1 font-mono text-xs text-texto-suave">
                  {n.productoCodigo}
                </span>
                <span className="text-texto italic">{n.nombreCientifico}</span>
                <p className="text-xs text-texto-suave">
                  {n.centroNombre ?? "Sin centro"}
                </p>
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-texto">
                {piezas(n.pendienteClientes)}
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">
                {piezas(n.sanos + n.porUbicar)}
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto md:table-cell">
                {n.enTransito ? piezas(n.enTransito) : "—"}
              </td>
              <td className="px-4 py-3 text-right">
                {n.faltante > 0 ? (
                  <Etiqueta texto={piezas(n.faltante)} tono="peligro" />
                ) : (
                  <Etiqueta texto="Cubierto" tono="exito" />
                )}
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td
                colSpan={6}
                className="px-4 py-8 text-center text-texto-suave"
              >
                {necesidades.length === 0
                  ? "No hay pedidos de clientes confirmados pendientes."
                  : "Todo lo pedido está cubierto."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      {puedeGestionar && necesidades.some((n) => n.faltante > 0) ? (
        <section
          aria-label="Crear pedido a proveedor"
          className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4 sm:flex-row sm:items-end"
        >
          <Campo
            label={`Proveedor para ${seleccion.length} especie(s) marcada(s)`}
            htmlFor="proveedorNecesidad"
          >
            <SelectorBuscable
              id="proveedorNecesidad"
              opciones={proveedores.map((p) => ({
                id: p.id,
                nombre: p.nombre,
              }))}
              valor={proveedorId}
              onChange={(v) => setProveedorId(v)}
              className="sm:w-72"
              placeholder="Escribe para buscar…"
            />
          </Campo>
          <button
            type="button"
            disabled={creando || !proveedorId || seleccion.length === 0}
            onClick={crearPedido}
            className={`${botonPrimarioClass} gap-2`}
          >
            <ShoppingCart className="size-4" aria-hidden />{" "}
            {creando ? "Creando…" : "Crear pedido a proveedor"}
          </button>
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
        </section>
      ) : null}
    </div>
  );
}
