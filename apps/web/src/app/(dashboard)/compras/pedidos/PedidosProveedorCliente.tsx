"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { PackageCheck, Plus, Search } from "lucide-react";
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
  ACCIONES_PEDIDO_PROVEEDOR,
  ESTADOS_PEDIDO_PROVEEDOR,
  PEDIDO_PROVEEDOR_RECIBIBLE,
  pct,
  piezas,
  tonoPedidoProveedor,
  type PedidoProveedor,
  type PedidoProveedorDetalle,
  type ProductoOpcion,
} from "@/lib/inventario";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
  textareaClass,
} from "@/lib/estilos";
import { fecha, moneda } from "@/lib/formato";
import {
  EditorLineas,
  aNumero,
  nuevaLinea,
  type LineaEditable,
} from "../../inventario/comun";
import {
  cambiarEstadoPedidoProveedorAction,
  guardarPedidoProveedorAction,
  recibirPedidoAction,
} from "../../inventario/actions";
import { useDetalle } from "../useDetalle";

type Opcion = { id: string; nombre: string };
type OfertaOpcion = { id: string; codigo: string; proveedorId: string };
type Vista =
  | { tipo: "detalle"; id: string }
  | { tipo: "editar"; detalle: PedidoProveedorDetalle | null }
  | { tipo: "recibir"; detalle: PedidoProveedorDetalle };

/** Pedidos a proveedor (paso 7) y su llegada (paso 8): la recepción crea los lotes. */
export function PedidosProveedorCliente({
  pedidos,
  productos,
  proveedores,
  ofertas,
  abrirId,
  puedeGestionar,
  puedeRecibir,
  verCostos,
}: {
  pedidos: PedidoProveedor[];
  productos: ProductoOpcion[];
  proveedores: Opcion[];
  ofertas: OfertaOpcion[];
  abrirId: string | null;
  puedeGestionar: boolean;
  puedeRecibir: boolean;
  verCostos: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [estado, setEstado] = useState("pendientes");
  const [vista, setVista] = useState<Vista | null>(
    abrirId ? { tipo: "detalle", id: abrirId } : null,
  );
  const [version, setVersion] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return pedidos.filter(
      (p) =>
        (estado === "todos" ||
          (estado === "pendientes"
            ? !["recibido", "cancelado"].includes(p.estado)
            : p.estado === estado)) &&
        (!t ||
          [p.codigo, p.proveedorNombre, p.ofertaCodigo].some((v) =>
            v?.toLowerCase().includes(t),
          )),
    );
  }, [pedidos, busqueda, estado]);

  const cerrar = () => {
    setVista(null);
    setError(null);
  };

  const accion = (
    fn: () => Promise<{ error?: string; ok?: boolean }>,
    despues?: () => void,
  ) =>
    iniciar(async () => {
      const r = await fn();
      if (r.error) {
        setError(r.error);
        return;
      }
      setError(null);
      setVersion((v) => v + 1);
      despues?.();
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Pedidos a proveedor"
        descripcion="Lo que se le pide a cada proveedor y lo que ya llegó."
        acciones={
          puedeGestionar ? (
            <button
              type="button"
              onClick={() => setVista({ tipo: "editar", detalle: null })}
              className={`${botonPrimarioClass} gap-2`}
            >
              <Plus className="size-4" aria-hidden /> Nuevo pedido
            </button>
          ) : null
        }
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto]">
        <label className="relative">
          <span className="sr-only">Buscar pedido</span>
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue"
            aria-hidden
          />
          <input
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Código o proveedor…"
            className={`${inputClass} pl-9`}
          />
        </label>
        <select
          aria-label="Estado"
          value={estado}
          onChange={(e) => setEstado(e.target.value)}
          className={`${selectClass} sm:w-52`}
        >
          <option value="pendientes">Abiertos</option>
          <option value="todos">Todos</option>
          {Object.entries(ESTADOS_PEDIDO_PROVEEDOR).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
      </div>
      {aviso ? (
        <p
          role="status"
          className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto"
        >
          {aviso}
        </p>
      ) : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Pedido</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">
              Fecha · llegada
            </th>
            <th className="px-4 py-3 text-right font-medium">Recibido</th>
            {verCostos ? (
              <th className="hidden px-4 py-3 text-right font-medium lg:table-cell">
                Costo
              </th>
            ) : null}
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((p) => (
            <tr key={p.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <button
                  type="button"
                  onClick={() => setVista({ tipo: "detalle", id: p.id })}
                  className="text-left font-medium whitespace-nowrap text-acento hover:underline"
                >
                  {p.codigo}
                </button>
                <p className="text-sm text-texto">{p.proveedorNombre}</p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">
                {fecha(p.fecha)}
                <p className="text-xs text-texto-suave">
                  {p.fechaEstimadaLlegada
                    ? `Llega ${fecha(p.fechaEstimadaLlegada)}`
                    : "Sin fecha de llegada"}
                </p>
              </td>
              <td className="px-4 py-3 text-right whitespace-nowrap tabular-nums text-texto">
                {piezas(p.recibidas)} / {piezas(p.unidades)}
                <p className="text-xs text-texto-suave">
                  {pct(p.cumplimiento)}
                </p>
              </td>
              {verCostos ? (
                <td className="hidden px-4 py-3 text-right tabular-nums text-texto lg:table-cell">
                  {moneda(p.costoTotal)}
                </td>
              ) : null}
              <td className="px-4 py-3">
                <Etiqueta
                  texto={ESTADOS_PEDIDO_PROVEEDOR[p.estado]}
                  tono={tonoPedidoProveedor(p.estado)}
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
                {pedidos.length === 0
                  ? "Aún no hay pedidos a proveedor."
                  : "Ningún pedido con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo="Pedido a proveedor"
        abierto={vista?.tipo === "detalle"}
        onCerrar={cerrar}
        ancho="max-w-4xl"
      >
        {vista?.tipo === "detalle" ? (
          <DetallePedido
            ruta={`/api/compras/pedidos/${vista.id}`}
            version={version}
            verCostos={verCostos}
            puedeGestionar={puedeGestionar}
            puedeRecibir={puedeRecibir}
            procesando={procesando}
            error={error}
            onEstado={(d, nuevo) =>
              accion(() =>
                cambiarEstadoPedidoProveedorAction(
                  d.pedido.id,
                  d.pedido.estado,
                  nuevo,
                ),
              )
            }
            onEditar={(d) => setVista({ tipo: "editar", detalle: d })}
            onRecibir={(d) => setVista({ tipo: "recibir", detalle: d })}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={
          vista?.tipo === "editar" && vista.detalle
            ? `Editar ${vista.detalle.pedido.codigo}`
            : "Nuevo pedido a proveedor"
        }
        abierto={vista?.tipo === "editar"}
        onCerrar={cerrar}
        ancho="max-w-5xl"
      >
        {vista?.tipo === "editar" ? (
          <FormularioPedido
            detalle={vista.detalle}
            productos={productos}
            proveedores={proveedores}
            ofertas={ofertas}
            verCostos={verCostos}
            guardando={procesando}
            error={error}
            onGuardar={(datos) =>
              iniciar(async () => {
                const r = await guardarPedidoProveedorAction(
                  vista.detalle?.pedido.id ?? null,
                  datos,
                );
                if (r.error) {
                  setError(r.error);
                  return;
                }
                setError(null);
                setVersion((v) => v + 1);
                const id =
                  vista.detalle?.pedido.id ??
                  (r.datos as { id: string } | undefined)?.id;
                setVista(id ? { tipo: "detalle", id } : null);
                router.refresh();
              })
            }
            onCancelar={cerrar}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={
          vista?.tipo === "recibir"
            ? `Recibir ${vista.detalle.pedido.codigo}`
            : ""
        }
        abierto={vista?.tipo === "recibir"}
        onCerrar={cerrar}
        ancho="max-w-5xl"
      >
        {vista?.tipo === "recibir" ? (
          <FormularioRecepcion
            detalle={vista.detalle}
            verCostos={verCostos}
            guardando={procesando}
            error={error}
            onRecibir={(datos) =>
              iniciar(async () => {
                const r = await recibirPedidoAction(datos);
                if (r.error || !r.datos) {
                  setError(r.error ?? "No se pudo registrar.");
                  return;
                }
                setError(null);
                setAviso(
                  `Recepción ${r.datos.codigo}: lotes ${r.datos.lotes.join(", ")} creados en aclimatación.` +
                    (r.datos.avisos.length
                      ? ` Aviso: ${r.datos.avisos.join(" ")}`
                      : ""),
                );
                setVersion((v) => v + 1);
                setVista({ tipo: "detalle", id: vista.detalle.pedido.id });
                router.refresh();
              })
            }
            onCancelar={() =>
              setVista({ tipo: "detalle", id: vista.detalle.pedido.id })
            }
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function DetallePedido({
  ruta,
  version,
  verCostos,
  puedeGestionar,
  puedeRecibir,
  procesando,
  error,
  onEstado,
  onEditar,
  onRecibir,
}: {
  ruta: string;
  version: number;
  verCostos: boolean;
  puedeGestionar: boolean;
  puedeRecibir: boolean;
  procesando: boolean;
  error: string | null;
  onEstado: (d: PedidoProveedorDetalle, nuevo: string) => void;
  onEditar: (d: PedidoProveedorDetalle) => void;
  onRecibir: (d: PedidoProveedorDetalle) => void;
}) {
  const {
    cargando,
    datos: d,
    error: errorCarga,
  } = useDetalle<PedidoProveedorDetalle>(ruta, version);
  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (errorCarga || !d)
    return (
      <p className="text-sm text-peligro">
        {errorCarga ?? "No se encontró el pedido."}
      </p>
    );
  const p = d.pedido;
  const editable =
    puedeGestionar &&
    p.recibidas === 0 &&
    ["borrador", "enviado", "en_transito"].includes(p.estado);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-lg font-semibold text-texto">
            {p.codigo} · {p.proveedorNombre}
          </p>
          <p className="text-sm text-texto-suave">
            Pedido {fecha(p.fecha)}
            {p.fechaEstimadaLlegada
              ? ` · llegada estimada ${fecha(p.fechaEstimadaLlegada)}`
              : ""}
            {p.ofertaCodigo ? ` · desde ${p.ofertaCodigo}` : ""}
          </p>
        </div>
        <Etiqueta
          texto={ESTADOS_PEDIDO_PROVEEDOR[p.estado]}
          tono={tonoPedidoProveedor(p.estado)}
        />
      </div>
      {p.observaciones ? (
        <p className="text-sm whitespace-pre-line text-texto">
          {p.observaciones}
        </p>
      ) : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="px-3 py-2 text-right font-medium">Pedido</th>
            <th className="px-3 py-2 text-right font-medium">Recibido</th>
            {verCostos ? (
              <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">
                Costo u.
              </th>
            ) : null}
          </tr>
        </EncabezadoTabla>
        <tbody>
          {d.lineas.map((l) => (
            <tr key={l.id} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">
                  {l.productoCodigo}
                </span>
                <span className="text-texto italic">{l.nombreCientifico}</span>
                {l.observacion ? (
                  <p className="text-xs text-texto-suave">{l.observacion}</p>
                ) : null}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">
                {piezas(l.cantidad)}
              </td>
              <td
                className={`px-3 py-2 text-right tabular-nums ${l.pendiente > 0 && l.recibida > 0 ? "text-advertencia-texto" : "text-texto"}`}
              >
                {piezas(l.recibida)}
              </td>
              {verCostos ? (
                <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">
                  {moneda(l.costoUnitario)}
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
      {verCostos && p.costoTotal != null ? (
        <p className="text-right text-sm text-texto-suave">
          Costo pactado:{" "}
          <strong className="text-texto">{moneda(p.costoTotal)}</strong>
        </p>
      ) : null}

      {d.recepciones.length > 0 ? (
        <div className="flex flex-col gap-1">
          <h3 className="text-sm font-semibold text-texto">Recepciones</h3>
          <ul className="text-sm text-texto">
            {d.recepciones.map((r) => (
              <li key={r.id}>
                <Link
                  href={`/compras/recepciones?id=${r.id}`}
                  className="text-acento hover:underline"
                >
                  {r.codigo}
                </Link>{" "}
                · {fecha(r.fecha)} · {piezas(r.unidades)} recibidos (
                {piezas(r.enfermas)} enf., {piezas(r.muertas)} muertos) ·{" "}
                {r.usuarioNombre}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:flex-wrap sm:justify-end">
        {puedeGestionar
          ? (ACCIONES_PEDIDO_PROVEEDOR[p.estado] ?? []).map((a) => (
              <button
                key={a.estado}
                type="button"
                disabled={procesando}
                onClick={() => onEstado(d, a.estado)}
                className={botonSecundarioClass}
              >
                {a.texto}
              </button>
            ))
          : null}
        {editable ? (
          <button
            type="button"
            onClick={() => onEditar(d)}
            className={botonSecundarioClass}
          >
            Editar
          </button>
        ) : null}
        {puedeRecibir && PEDIDO_PROVEEDOR_RECIBIBLE.includes(p.estado) ? (
          <button
            type="button"
            onClick={() => onRecibir(d)}
            className={`${botonPrimarioClass} gap-2`}
          >
            <PackageCheck className="size-4" aria-hidden /> Registrar llegada
          </button>
        ) : null}
      </div>
    </div>
  );
}

function FormularioPedido({
  detalle,
  productos,
  proveedores,
  ofertas,
  verCostos,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  detalle: PedidoProveedorDetalle | null;
  productos: ProductoOpcion[];
  proveedores: Opcion[];
  ofertas: OfertaOpcion[];
  verCostos: boolean;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const p = detalle?.pedido;
  const [proveedorId, setProveedorId] = useState(p?.proveedorId ?? "");
  const [ofertaId, setOfertaId] = useState(p?.ofertaId ?? "");
  const [fechaPedido, setFechaPedido] = useState(p?.fecha ?? "");
  const [llegada, setLlegada] = useState(p?.fechaEstimadaLlegada ?? "");
  const [observaciones, setObservaciones] = useState(p?.observaciones ?? "");
  const [lineas, setLineas] = useState<LineaEditable[]>(() =>
    detalle?.lineas.length
      ? detalle.lineas.map((l) =>
          nuevaLinea({
            productoId: l.productoId,
            cantidad: String(l.cantidad),
            valor: l.costoUnitario != null ? String(l.costoUnitario) : "",
            observacion: l.observacion ?? "",
          }),
        )
      : [nuevaLinea()],
  );
  const [cargandoOferta, setCargandoOferta] = useState(false);
  const opcionesProveedor =
    p && !proveedores.some((x) => x.id === p.proveedorId)
      ? [...proveedores, { id: p.proveedorId, nombre: p.proveedorNombre }]
      : proveedores;

  // Elegir una disponibilidad del proveedor trae sus especies y cantidades como punto de partida.
  const usarOferta = async (id: string) => {
    setOfertaId(id);
    if (!id) return;
    setCargandoOferta(true);
    try {
      const r = await fetch(`/api/compras/ofertas/${id}`, {
        cache: "no-store",
      });
      if (r.ok) {
        const o = (await r.json()) as {
          oferta: { proveedorId: string };
          lineas: { productoId: string; cantidad: number }[];
        };
        setProveedorId(o.oferta.proveedorId);
        setLineas(
          o.lineas.map((l) =>
            nuevaLinea({
              productoId: l.productoId,
              cantidad: String(l.cantidad),
            }),
          ),
        );
      }
    } finally {
      setCargandoOferta(false);
    }
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          proveedorId,
          ofertaId: ofertaId || null,
          fecha: fechaPedido || null,
          fechaEstimadaLlegada: llegada || null,
          observaciones,
          lineas: lineas
            .filter((l) => l.productoId)
            .map((l) => ({
              productoId: l.productoId,
              cantidad: aNumero(l.cantidad) ?? 0,
              costoUnitario: verCostos ? aNumero(l.valor) : null,
              observacion: l.observacion,
            })),
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        {!p ? (
          <Campo
            label="Desde una disponibilidad (opcional)"
            htmlFor="oferta"
            hint="Trae sus especies y cantidades."
          >
            <SelectorBuscable
              id="oferta"
              opciones={ofertas.map((o) => ({
                id: o.id,
                nombre: `${o.codigo} · ${proveedores.find((x) => x.id === o.proveedorId)?.nombre ?? ""}`,
              }))}
              valor={ofertaId}
              onChange={(v) => usarOferta(v)}
              vacio="Ninguna"
              disabled={cargandoOferta}
            />
          </Campo>
        ) : null}
        <Campo label="Proveedor" htmlFor="proveedor">
          <SelectorBuscable
            id="proveedor"
            opciones={opcionesProveedor.map((x) => ({
              id: x.id,
              nombre: x.nombre,
            }))}
            valor={proveedorId}
            onChange={(v) => setProveedorId(v)}
            placeholder="Escribe para buscar…"
          />
        </Campo>
        <Campo label="Fecha del pedido" htmlFor="fechaPedido">
          <input
            id="fechaPedido"
            type="date"
            value={fechaPedido}
            onChange={(e) => setFechaPedido(e.target.value)}
            className={inputClass}
          />
        </Campo>
        <Campo label="Llegada estimada (opcional)" htmlFor="llegada">
          <input
            id="llegada"
            type="date"
            value={llegada}
            onChange={(e) => setLlegada(e.target.value)}
            className={inputClass}
          />
        </Campo>
        <CampoAncho>
          <Campo label="Observaciones (opcional)" htmlFor="obsPedido">
            <textarea
              id="obsPedido"
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
          Especies
        </legend>
        <EditorLineas
          lineas={lineas}
          onChange={setLineas}
          productos={productos}
          etiquetaValor={verCostos ? "Costo u. (COP)" : undefined}
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
          {guardando ? "Guardando…" : "Guardar pedido"}
        </button>
      </div>
    </form>
  );
}

/** Llegada: por especie, lo que llegó (total), enfermos y muertos; el costo total se calcula en vivo. */
function FormularioRecepcion({
  detalle,
  verCostos,
  guardando,
  error,
  onRecibir,
  onCancelar,
}: {
  detalle: PedidoProveedorDetalle;
  verCostos: boolean;
  guardando: boolean;
  error: string | null;
  onRecibir: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [fechaRec, setFechaRec] = useState("");
  const [observaciones, setObservaciones] = useState("");
  const [filas, setFilas] = useState(() =>
    Object.fromEntries(
      detalle.lineas.map((l) => [
        l.id,
        {
          recibida: l.pendiente > 0 ? String(l.pendiente) : "",
          enferma: "",
          muerta: "",
          costo: l.costoUnitario != null ? String(l.costoUnitario) : "",
        },
      ]),
    ),
  );
  const set = (
    id: string,
    campo: "recibida" | "enferma" | "muerta" | "costo",
    v: string,
  ) => setFilas((f) => ({ ...f, [id]: { ...f[id], [campo]: v } }));
  const total = detalle.lineas.reduce(
    (s, l) =>
      s +
      (aNumero(filas[l.id].recibida) ?? 0) * (aNumero(filas[l.id].costo) ?? 0),
    0,
  );

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onRecibir({
          pedidoProveedorId: detalle.pedido.id,
          estadoPedido: detalle.pedido.estado,
          fecha: fechaRec || null,
          observaciones,
          lineas: detalle.lineas
            .map((l) => ({
              pedidoLineaId: l.id,
              recibida: aNumero(filas[l.id].recibida) ?? 0,
              enferma: aNumero(filas[l.id].enferma) ?? 0,
              muerta: aNumero(filas[l.id].muerta) ?? 0,
              costoUnitario: verCostos ? aNumero(filas[l.id].costo) : null,
            }))
            .filter((l) => l.recibida > 0),
        });
      }}
      className="flex flex-col gap-5"
    >
      <p className="text-sm text-texto-suave">
        «Llegó» es el total de la especie (sanos + enfermos + muertos). Los
        vivos quedan <strong>en aclimatación</strong> en un lote nuevo por
        especie, hasta ubicarlos en acuarios.
      </p>
      <ul className="flex flex-col gap-3">
        {detalle.lineas.map((l) => (
          <li
            key={l.id}
            className="grid grid-cols-2 gap-2 rounded-md border border-borde p-3 md:grid-cols-[2fr_repeat(4,7rem)] md:items-end"
          >
            <div className="col-span-2 md:col-span-1">
              <p className="text-sm text-texto">
                <span className="mr-1 font-mono text-xs text-texto-suave">
                  {l.productoCodigo}
                </span>
                <span className="italic">{l.nombreCientifico}</span>
              </p>
              <p className="text-xs text-texto-suave">
                Pedido {piezas(l.cantidad)} · recibido {piezas(l.recibida)} ·
                faltan {piezas(l.pendiente)}
              </p>
            </div>
            <Campo label="Llegó" htmlFor={`rec-${l.id}`}>
              <input
                id={`rec-${l.id}`}
                type="number"
                inputMode="numeric"
                min={0}
                value={filas[l.id].recibida}
                onChange={(e) => set(l.id, "recibida", e.target.value)}
                className={inputClass}
              />
            </Campo>
            <Campo label="Enfermos" htmlFor={`enf-${l.id}`}>
              <input
                id={`enf-${l.id}`}
                type="number"
                inputMode="numeric"
                min={0}
                value={filas[l.id].enferma}
                onChange={(e) => set(l.id, "enferma", e.target.value)}
                className={inputClass}
              />
            </Campo>
            <Campo label="Muertos" htmlFor={`mue-${l.id}`}>
              <input
                id={`mue-${l.id}`}
                type="number"
                inputMode="numeric"
                min={0}
                value={filas[l.id].muerta}
                onChange={(e) => set(l.id, "muerta", e.target.value)}
                className={inputClass}
              />
            </Campo>
            {verCostos ? (
              <Campo label="Costo u." htmlFor={`cos-${l.id}`}>
                <input
                  id={`cos-${l.id}`}
                  type="number"
                  inputMode="decimal"
                  min={0}
                  value={filas[l.id].costo}
                  onChange={(e) => set(l.id, "costo", e.target.value)}
                  className={inputClass}
                />
              </Campo>
            ) : null}
          </li>
        ))}
      </ul>
      <RejillaCampos>
        <Campo label="Fecha de llegada" htmlFor="fechaRec" hint="Vacío = hoy.">
          <input
            id="fechaRec"
            type="date"
            value={fechaRec}
            onChange={(e) => setFechaRec(e.target.value)}
            className={inputClass}
          />
        </Campo>
        <CampoAncho>
          <Campo label="Observaciones (opcional)" htmlFor="obsRec">
            <textarea
              id="obsRec"
              value={observaciones}
              onChange={(e) => setObservaciones(e.target.value)}
              maxLength={2000}
              className={textareaClass}
            />
          </Campo>
        </CampoAncho>
      </RejillaCampos>
      {verCostos ? (
        <p className="text-right text-sm text-texto-suave">
          Costo total de lo recibido:{" "}
          <strong className="text-texto tabular-nums">{moneda(total)}</strong>
        </p>
      ) : null}
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCancelar}
          className={botonSecundarioClass}
        >
          Volver
        </button>
        <button
          type="submit"
          disabled={guardando}
          className={botonPrimarioClass}
        >
          {guardando ? "Registrando…" : "Registrar llegada"}
        </button>
      </div>
    </form>
  );
}
