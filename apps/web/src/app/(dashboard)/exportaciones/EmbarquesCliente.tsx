"use client";

import { useMemo, useState, useTransition } from "react";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import {
  Campo,
  EncabezadoTabla,
  Etiqueta,
  TablaResponsive,
} from "@/components/campos";
import {
  ESTADOS_EMBARQUE,
  tonoEmbarque,
  type Embarque,
} from "@/lib/exportaciones";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
} from "@/lib/estilos";
import { fecha } from "@/lib/formato";
import { crearEmbarqueAction } from "./actions";

type VentaOpcion = {
  id: string;
  codigo: string;
  cliente: string;
  fecha: string;
};

/** Lista de embarques; se crean desde una venta de exportación del CRM. */
export function EmbarquesCliente({
  embarques,
  ventasDisponibles,
  ventaInicial,
  puedeCrear,
}: {
  embarques: Embarque[];
  ventasDisponibles: VentaOpcion[];
  ventaInicial: string | null;
  puedeCrear: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [estado, setEstado] = useState("abiertos");
  const [creando, setCreando] = useState(puedeCrear && ventaInicial !== null);
  const [ventaId, setVentaId] = useState(ventaInicial ?? "");
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return embarques.filter(
      (e) =>
        (estado === "todos" ||
          (estado === "abiertos"
            ? !["cerrado", "cancelado"].includes(e.estado)
            : e.estado === estado)) &&
        (!t ||
          [
            e.codigo,
            e.clienteNombre,
            e.ventaCodigo,
            e.awb,
            e.facturaNumero?.toString(),
            e.preinvoiceNumero,
          ].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [embarques, busqueda, estado]);

  const crear = () =>
    iniciar(async () => {
      const r = await crearEmbarqueAction(ventaId);
      if (r.error || !r.datos) {
        setError(r.error ?? "No se pudo crear el embarque.");
        return;
      }
      router.push(`/exportaciones/${r.datos.id}`);
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Embarques de exportación"
        descripcion="Preinvoice, flete, orden de cargue, empaque, factura, DOA y balance de cada despacho."
        acciones={
          puedeCrear ? (
            <button
              type="button"
              onClick={() => setCreando(true)}
              className={`${botonPrimarioClass} gap-2`}
            >
              <Plus className="size-4" aria-hidden /> Nuevo embarque
            </button>
          ) : null
        }
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto]">
        <label className="relative">
          <span className="sr-only">Buscar embarque</span>
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue"
            aria-hidden
          />
          <input
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Código, cliente, venta, AWB o factura…"
            className={`${inputClass} pl-9`}
          />
        </label>
        <select
          aria-label="Estado"
          value={estado}
          onChange={(e) => setEstado(e.target.value)}
          className={`${selectClass} sm:w-52`}
        >
          <option value="abiertos">Abiertos</option>
          <option value="todos">Todos</option>
          {Object.entries(ESTADOS_EMBARQUE).map(([v, t]) => (
            <option key={v} value={v}>
              {t}
            </option>
          ))}
        </select>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Embarque</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">
              Despacho · AWB
            </th>
            <th className="hidden px-4 py-3 font-medium sm:table-cell">
              Documentos
            </th>
            <th className="px-4 py-3 text-right font-medium">Peces</th>
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((e) => (
            <tr key={e.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <Link
                  href={`/exportaciones/${e.id}`}
                  className="font-medium whitespace-nowrap text-acento hover:underline"
                >
                  {e.codigo}
                </Link>
                <p className="text-sm text-texto">{e.clienteNombre}</p>
                <p className="text-xs text-texto-suave">
                  Venta {e.ventaCodigo} · {e.comercialNombre}
                </p>
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">
                {e.fechaDespacho ? (
                  fecha(e.fechaDespacho)
                ) : (
                  <span className="text-texto-tenue">Sin fecha</span>
                )}
                <p className="text-xs text-texto-suave">{e.awb ?? "Sin AWB"}</p>
              </td>
              <td className="hidden px-4 py-3 text-sm text-texto sm:table-cell">
                {e.preinvoiceNumero ? (
                  <p>Preinvoice {e.preinvoiceNumero}</p>
                ) : null}
                {e.facturaNumero ? <p>Factura {e.facturaNumero}</p> : null}
                {!e.preinvoiceNumero && !e.facturaNumero ? (
                  <span className="text-texto-tenue">—</span>
                ) : null}
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-texto">
                {e.unidades.toLocaleString("es-CO")}
                <p className="text-xs text-texto-suave">
                  {e.cajasEmpacadas
                    ? `${e.cajasEmpacadas} cajas`
                    : `${e.lineas} especies`}
                </p>
              </td>
              <td className="px-4 py-3">
                <Etiqueta
                  texto={ESTADOS_EMBARQUE[e.estado]}
                  tono={tonoEmbarque(e.estado)}
                />
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td
                colSpan={5}
                className="px-4 py-8 text-center text-texto-suave"
              >
                {embarques.length === 0
                  ? "Aún no hay embarques. Se crean desde una venta de exportación."
                  : "Ningún embarque con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo="Nuevo embarque"
        abierto={creando}
        onCerrar={() => setCreando(false)}
      >
        <div className="flex flex-col gap-5">
          <p className="text-sm text-texto-suave">
            El embarque toma de la venta el cliente, el comercial, la aerolínea
            y el AWB; y del cliente su lista de precios, descuento y formato de
            factura. Si la venta ya tiene pedido de cliente, trae sus especies.
          </p>
          <Campo label="Venta de exportación" htmlFor="ventaEmbarque">
            <SelectorBuscable
              id="ventaEmbarque"
              opciones={ventasDisponibles.map((v) => ({
                id: v.id,
                nombre: `${v.codigo} · ${v.cliente}`,
                detalle: fecha(v.fecha),
              }))}
              valor={ventaId}
              onChange={setVentaId}
              placeholder="Código de la venta o cliente…"
            />
          </Campo>
          {ventasDisponibles.length === 0 ? (
            <p className="text-sm text-advertencia-texto">
              Todas las ventas de exportación ya tienen embarque.
            </p>
          ) : null}
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button
              type="button"
              onClick={() => setCreando(false)}
              className={botonSecundarioClass}
            >
              Cancelar
            </button>
            <button
              type="button"
              onClick={crear}
              disabled={!ventaId || procesando}
              className={botonPrimarioClass}
            >
              {procesando ? "Creando…" : "Crear embarque"}
            </button>
          </div>
        </div>
      </Dialogo>
    </div>
  );
}
