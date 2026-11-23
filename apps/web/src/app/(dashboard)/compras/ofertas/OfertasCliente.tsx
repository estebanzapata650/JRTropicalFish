"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import {
  Campo,
  CampoAncho,
  EncabezadoTabla,
  Etiqueta,
  RejillaCampos,
  TablaResponsive,
} from "@/components/campos";
import {
  ESTADOS_OFERTA,
  piezas,
  type Oferta,
  type OfertaLinea,
  type ProductoOpcion,
} from "@/lib/inventario";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
  textareaClass,
} from "@/lib/estilos";
import { fecha } from "@/lib/formato";
import {
  EditorLineas,
  aNumero,
  nuevaLinea,
  type LineaEditable,
} from "../../inventario/comun";
import { guardarOfertaAction } from "../../inventario/actions";
import { useDetalle } from "../useDetalle";

type Opcion = { id: string; nombre: string };
type OfertaDetalle = { oferta: Oferta; lineas: OfertaLinea[] };

/** Paso 2: la disponibilidad que manda cada proveedor (especie y cantidad, sin precio). */
export function OfertasCliente({
  ofertas,
  productos,
  proveedores,
  puedeGestionar,
}: {
  ofertas: Oferta[];
  productos: ProductoOpcion[];
  proveedores: Opcion[];
  puedeGestionar: boolean;
}) {
  const router = useRouter();
  const [verId, setVerId] = useState<string | null>(null);
  const [editando, setEditando] = useState<OfertaDetalle | null | undefined>(
    undefined,
  );
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const [version, setVersion] = useState(0);
  const [estado, setEstado] = useState("abierta");
  const visibles = ofertas.filter((o) => !estado || o.estado === estado);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Disponibilidad de proveedores"
        descripcion="Lo que cada proveedor ofrece: punto de partida de los pedidos a proveedor."
        acciones={
          puedeGestionar ? (
            <button
              type="button"
              onClick={() => setEditando(null)}
              className={`${botonPrimarioClass} gap-2`}
            >
              <Plus className="size-4" aria-hidden /> Nueva disponibilidad
            </button>
          ) : null
        }
      />
      <select
        aria-label="Estado"
        value={estado}
        onChange={(e) => setEstado(e.target.value)}
        className={`${selectClass} sm:w-52`}
      >
        <option value="">Todas</option>
        {Object.entries(ESTADOS_OFERTA).map(([v, t]) => (
          <option key={v} value={v}>
            {t}
          </option>
        ))}
      </select>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Disponibilidad</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">
              Fecha · llegada
            </th>
            <th className="px-4 py-3 text-right font-medium">Unidades</th>
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((o) => (
            <tr key={o.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <button
                  type="button"
                  onClick={() => setVerId(o.id)}
                  className="text-left font-medium whitespace-nowrap text-acento hover:underline"
                >
                  {o.codigo}
                </button>
                <p className="text-sm text-texto">{o.proveedorNombre}</p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">
                {fecha(o.fecha)}
                {o.fechaEstimadaLlegada ? (
                  <p className="text-xs text-texto-suave">
                    Llega {fecha(o.fechaEstimadaLlegada)}
                  </p>
                ) : null}
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-texto">
                {piezas(o.unidades)}
                <p className="text-xs text-texto-suave">{o.lineas} especies</p>
              </td>
              <td className="px-4 py-3">
                <Etiqueta
                  texto={ESTADOS_OFERTA[o.estado]}
                  tono={o.estado === "abierta" ? "exito" : "neutro"}
                />
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td
                colSpan={4}
                className="px-4 py-8 text-center text-texto-suave"
              >
                No hay disponibilidades{" "}
                {estado
                  ? ESTADOS_OFERTA[estado].toLowerCase() + "s"
                  : "registradas"}
                .
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo="Disponibilidad del proveedor"
        abierto={verId !== null}
        onCerrar={() => setVerId(null)}
        ancho="max-w-3xl"
      >
        {verId ? (
          <DetalleOferta
            ruta={`/api/compras/ofertas/${verId}`}
            version={version}
            puedeGestionar={puedeGestionar}
            onEditar={(d) => {
              setVerId(null);
              setEditando(d);
            }}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={
          editando ? `Editar ${editando.oferta.codigo}` : "Nueva disponibilidad"
        }
        abierto={editando !== undefined}
        onCerrar={() => {
          setEditando(undefined);
          setError(null);
        }}
        ancho="max-w-5xl"
      >
        {editando !== undefined ? (
          <FormularioOferta
            detalle={editando}
            productos={productos}
            proveedores={proveedores}
            guardando={guardando}
            error={error}
            onCancelar={() => {
              setEditando(undefined);
              setError(null);
            }}
            onGuardar={(datos) =>
              iniciar(async () => {
                const r = await guardarOfertaAction(
                  editando?.oferta.id ?? null,
                  datos,
                );
                if (r.error) {
                  setError(r.error);
                  return;
                }
                setError(null);
                setVersion((v) => v + 1);
                setEditando(undefined);
                router.refresh();
              })
            }
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function DetalleOferta({
  ruta,
  version,
  puedeGestionar,
  onEditar,
}: {
  ruta: string;
  version: number;
  puedeGestionar: boolean;
  onEditar: (d: OfertaDetalle) => void;
}) {
  const {
    cargando,
    datos: d,
    error,
  } = useDetalle<OfertaDetalle>(ruta, version);
  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (error || !d)
    return <p className="text-sm text-peligro">{error ?? "No se encontró."}</p>;
  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-lg font-semibold text-texto">
            {d.oferta.codigo} · {d.oferta.proveedorNombre}
          </p>
          <p className="text-sm text-texto-suave">
            {fecha(d.oferta.fecha)}
            {d.oferta.fechaEstimadaLlegada
              ? ` · llegada estimada ${fecha(d.oferta.fechaEstimadaLlegada)}`
              : ""}
          </p>
        </div>
        <Etiqueta
          texto={ESTADOS_OFERTA[d.oferta.estado]}
          tono={d.oferta.estado === "abierta" ? "exito" : "neutro"}
        />
      </div>
      {d.oferta.observaciones ? (
        <p className="text-sm whitespace-pre-line text-texto">
          {d.oferta.observaciones}
        </p>
      ) : null}
      <ul className="flex flex-col divide-y divide-borde rounded-md border border-borde">
        {d.lineas.map((l) => (
          <li
            key={l.id}
            className="flex items-center justify-between gap-3 px-3 py-2 text-sm"
          >
            <span className="min-w-0 truncate text-texto">
              <span className="mr-1 font-mono text-xs text-texto-suave">
                {l.productoCodigo}
              </span>
              <span className="italic">{l.nombreCientifico}</span>
            </span>
            <span className="shrink-0 tabular-nums text-texto">
              {piezas(l.cantidad)}
            </span>
          </li>
        ))}
      </ul>
      {puedeGestionar ? (
        <div className="flex justify-end">
          <button
            type="button"
            onClick={() => onEditar(d)}
            className={botonSecundarioClass}
          >
            Editar
          </button>
        </div>
      ) : null}
    </div>
  );
}

function FormularioOferta({
  detalle,
  productos,
  proveedores,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  detalle: OfertaDetalle | null;
  productos: ProductoOpcion[];
  proveedores: Opcion[];
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const o = detalle?.oferta;
  const [proveedorId, setProveedorId] = useState(o?.proveedorId ?? "");
  const [fechaOferta, setFechaOferta] = useState(o?.fecha ?? "");
  const [llegada, setLlegada] = useState(o?.fechaEstimadaLlegada ?? "");
  const [estado, setEstado] = useState(o?.estado ?? "abierta");
  const [observaciones, setObservaciones] = useState(o?.observaciones ?? "");
  const [lineas, setLineas] = useState<LineaEditable[]>(() =>
    detalle?.lineas.length
      ? detalle.lineas.map((l) =>
          nuevaLinea({
            productoId: l.productoId,
            cantidad: String(l.cantidad),
          }),
        )
      : [nuevaLinea()],
  );
  const opciones =
    o && !proveedores.some((p) => p.id === o.proveedorId)
      ? [...proveedores, { id: o.proveedorId, nombre: o.proveedorNombre }]
      : proveedores;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          proveedorId,
          fecha: fechaOferta || null,
          fechaEstimadaLlegada: llegada || null,
          estado,
          observaciones,
          lineas: lineas
            .filter((l) => l.productoId)
            .map((l) => ({
              productoId: l.productoId,
              cantidad: aNumero(l.cantidad) ?? 0,
            })),
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <Campo label="Proveedor" htmlFor="provOferta">
          <SelectorBuscable
            id="provOferta"
            opciones={opciones.map((p) => ({ id: p.id, nombre: p.nombre }))}
            valor={proveedorId}
            onChange={(v) => setProveedorId(v)}
            placeholder="Escribe para buscar…"
          />
        </Campo>
        <Campo label="Fecha" htmlFor="fechaOferta" hint="Vacío = hoy.">
          <input
            id="fechaOferta"
            type="date"
            value={fechaOferta}
            onChange={(e) => setFechaOferta(e.target.value)}
            className={inputClass}
          />
        </Campo>
        <Campo label="Llegada estimada (opcional)" htmlFor="llegadaOferta">
          <input
            id="llegadaOferta"
            type="date"
            value={llegada}
            onChange={(e) => setLlegada(e.target.value)}
            className={inputClass}
          />
        </Campo>
        {o ? (
          <Campo label="Estado" htmlFor="estadoOferta">
            <select
              id="estadoOferta"
              value={estado}
              onChange={(e) => setEstado(e.target.value)}
              className={selectClass}
            >
              {Object.entries(ESTADOS_OFERTA).map(([v, t]) => (
                <option key={v} value={v}>
                  {t}
                </option>
              ))}
            </select>
          </Campo>
        ) : null}
        <CampoAncho>
          <Campo label="Observaciones (opcional)" htmlFor="obsOferta">
            <textarea
              id="obsOferta"
              value={observaciones}
              onChange={(e) => setObservaciones(e.target.value)}
              maxLength={2000}
              className={textareaClass}
            />
          </Campo>
        </CampoAncho>
      </RejillaCampos>
      <fieldset className="flex flex-col gap-3">
        <legend className="mb-2 text-sm font-semibold text-texto">
          Especies ofrecidas
        </legend>
        <EditorLineas
          lineas={lineas}
          onChange={setLineas}
          productos={productos}
          conObservacion={false}
        />
      </fieldset>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCancelar}
          className={botonSecundarioClass}
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={guardando}
          className={botonPrimarioClass}
        >
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
