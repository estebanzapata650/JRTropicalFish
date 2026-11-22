"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { History, Plus, Search, Truck } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { Campo, CampoAncho, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import {
  ACCIONES_PEDIDO_CLIENTE,
  ESTADOS_PEDIDO_CLIENTE,
  pct,
  piezas,
  tonoPedidoCliente,
  type Existencia,
  type PedidoCliente,
  type PedidoClienteDetalle,
  type ProductoOpcion,
} from "@/lib/inventario";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { dinero, fecha } from "@/lib/formato";
import { EditorLineas, aNumero, nuevaLinea, type LineaEditable } from "../../inventario/comun";
import { cambiarEstadoPedidoClienteAction, despacharPedidoAction, guardarPedidoClienteAction } from "../../inventario/actions";
import { useDetalle } from "../../compras/useDetalle";
import { fijarReservaAction } from "../../sanidad/actions";
import type { Reserva } from "@/lib/sanidad";

type Opcion = { id: string; nombre: string };
export type VentaOrigen = { id: string; codigo: string; clienteNombre: string; comercialNombre: string; ciudad: string | null };
type Vista =
  | { tipo: "detalle"; id: string }
  | { tipo: "editar"; detalle: PedidoClienteDetalle | null }
  | { tipo: "despachar"; detalle: PedidoClienteDetalle }
  | { tipo: "historial"; pedido: PedidoCliente };

const ETIQUETAS_HISTORIAL: Record<string, string> = {
  Cliente: "Cliente",
  Comercial: "Comercial",
  Venta: "Venta",
  Fecha: "Fecha",
  Ciudad: "Ciudad",
  Estado: "Estado",
  Observaciones: "Observaciones",
  Lineas: "Especies",
};

/** Pedidos de clientes: los arma el comercial (desde su venta o suelto) y bodega los despacha. */
export function PedidosClienteCliente({
  pedidos,
  productos,
  clientes,
  comerciales,
  abrirId,
  ventaOrigen,
  puedeGestionar,
  puedeDespachar,
  puedeAsignar,
}: {
  pedidos: PedidoCliente[];
  productos: ProductoOpcion[];
  clientes: Opcion[];
  comerciales: Opcion[];
  abrirId: string | null;
  ventaOrigen: VentaOrigen | null;
  puedeGestionar: boolean;
  puedeDespachar: boolean;
  puedeAsignar: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [estado, setEstado] = useState("activos");
  const [vista, setVista] = useState<Vista | null>(
    abrirId ? { tipo: "detalle", id: abrirId } : ventaOrigen && puedeGestionar ? { tipo: "editar", detalle: null } : null,
  );
  const [desdeVenta, setDesdeVenta] = useState<VentaOrigen | null>(ventaOrigen);
  const [version, setVersion] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return pedidos.filter(
      (p) =>
        (estado === "todos" || (estado === "activos" ? !["despachado", "cancelado"].includes(p.estado) : p.estado === estado)) &&
        (!t || [p.codigo, p.clienteNombre, p.comercialNombre, p.ventaCodigo].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [pedidos, busqueda, estado]);

  const cerrar = () => {
    setVista(null);
    setDesdeVenta(null);
    setError(null);
  };

  const ejecutar = (fn: () => Promise<{ error?: string }>, idTras: string) =>
    iniciar(async () => {
      const r = await fn();
      if (r.error) {
        setError(r.error);
        return;
      }
      setError(null);
      setVersion((v) => v + 1);
      setVista({ tipo: "detalle", id: idTras });
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Pedidos de clientes"
        descripcion="Lo que cada cliente pidió, lo despachado desde bodega y lo pendiente."
        acciones={
          puedeGestionar ? (
            <button type="button" onClick={() => setVista({ tipo: "editar", detalle: null })} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Pedido suelto
            </button>
          ) : null
        }
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto]">
        <label className="relative">
          <span className="sr-only">Buscar pedido</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Código, cliente, comercial o venta…" className={`${inputClass} pl-9`} />
        </label>
        <select aria-label="Estado" value={estado} onChange={(e) => setEstado(e.target.value)} className={`${selectClass} sm:w-52`}>
          <option value="activos">Sin despachar</option>
          <option value="todos">Todos</option>
          {Object.entries(ESTADOS_PEDIDO_CLIENTE).map(([v, t]) => (
            <option key={v} value={v}>
              {t}
            </option>
          ))}
        </select>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Pedido</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Fecha · comercial</th>
            <th className="px-4 py-3 text-right font-medium">Despachado</th>
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((p) => (
            <tr key={p.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <button type="button" onClick={() => setVista({ tipo: "detalle", id: p.id })} className="text-left font-medium whitespace-nowrap text-acento hover:underline">
                  {p.codigo}
                </button>
                <p className="text-sm text-texto">{p.clienteNombre}</p>
                {p.ventaCodigo ? <p className="text-xs text-texto-suave">Venta {p.ventaCodigo}</p> : null}
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">
                {fecha(p.fecha)}
                <p className="text-xs text-texto-suave">{p.comercialNombre}</p>
              </td>
              <td className="px-4 py-3 text-right whitespace-nowrap tabular-nums text-texto">
                {piezas(p.despachadas)} / {piezas(p.unidades)}
                <p className="text-xs text-texto-suave">{pct(p.cumplimiento)}</p>
              </td>
              <td className="px-4 py-3">
                <Etiqueta texto={ESTADOS_PEDIDO_CLIENTE[p.estado]} tono={tonoPedidoCliente(p.estado)} />
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-4 py-8 text-center text-texto-suave">
                {pedidos.length === 0 ? "Aún no hay pedidos de clientes. Se crean desde la venta o como pedido suelto." : "Ningún pedido con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo="Pedido de cliente" abierto={vista?.tipo === "detalle"} onCerrar={cerrar} ancho="max-w-4xl">
        {vista?.tipo === "detalle" ? (
          <DetallePedido
            ruta={`/api/pedidos/${vista.id}`}
            version={version}
            puedeGestionar={puedeGestionar}
            puedeDespachar={puedeDespachar}
            procesando={procesando}
            error={error}
            onEstado={(d, nuevo) => ejecutar(() => cambiarEstadoPedidoClienteAction(d.pedido.id, d.pedido.estado, nuevo), d.pedido.id)}
            onEditar={(d) => setVista({ tipo: "editar", detalle: d })}
            onHistorial={(d) => setVista({ tipo: "historial", pedido: d.pedido })}
            onDespachar={(d) => {
              setError(null);
              setVista({ tipo: "despachar", detalle: d });
            }}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={vista?.tipo === "editar" && vista.detalle ? `Editar ${vista.detalle.pedido.codigo}` : desdeVenta ? `Pedido de la venta ${desdeVenta.codigo}` : "Pedido suelto"}
        abierto={vista?.tipo === "editar"}
        onCerrar={cerrar}
        ancho="max-w-5xl"
      >
        {vista?.tipo === "editar" ? (
          <FormularioPedido
            detalle={vista.detalle}
            desdeVenta={vista.detalle ? null : desdeVenta}
            productos={productos}
            clientes={clientes}
            comerciales={comerciales}
            puedeAsignar={puedeAsignar}
            guardando={procesando}
            error={error}
            onCancelar={cerrar}
            onGuardar={(datos) =>
              iniciar(async () => {
                const r = await guardarPedidoClienteAction(vista.detalle?.pedido.id ?? null, datos);
                if (r.error) {
                  setError(r.error);
                  return;
                }
                setError(null);
                setDesdeVenta(null);
                setVersion((v) => v + 1);
                const id = vista.detalle?.pedido.id ?? (r.datos as { id: string } | undefined)?.id;
                setVista(id ? { tipo: "detalle", id } : null);
                router.refresh();
              })
            }
          />
        ) : null}
      </Dialogo>

      <Dialogo titulo={vista?.tipo === "despachar" ? `Despachar ${vista.detalle.pedido.codigo}` : ""} abierto={vista?.tipo === "despachar"} onCerrar={cerrar} ancho="max-w-5xl">
        {vista?.tipo === "despachar" ? (
          <FormularioDespacho
            detalle={vista.detalle}
            guardando={procesando}
            error={error}
            onCancelar={() => setVista({ tipo: "detalle", id: vista.detalle.pedido.id })}
            onDespachar={(salidas) => ejecutar(() => despacharPedidoAction(vista.detalle.pedido.id, salidas), vista.detalle.pedido.id)}
          />
        ) : null}
      </Dialogo>

      <Dialogo titulo={vista?.tipo === "historial" ? `Historial de ${vista.pedido.codigo}` : ""} abierto={vista?.tipo === "historial"} onCerrar={cerrar}>
        {vista?.tipo === "historial" ? <HistorialRemoto ruta={`/api/pedidos/${vista.pedido.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL} /> : null}
      </Dialogo>
    </div>
  );
}

function DetallePedido({
  ruta,
  version,
  puedeGestionar,
  puedeDespachar,
  procesando,
  error,
  onEstado,
  onEditar,
  onHistorial,
  onDespachar,
}: {
  ruta: string;
  version: number;
  puedeGestionar: boolean;
  puedeDespachar: boolean;
  procesando: boolean;
  error: string | null;
  onEstado: (d: PedidoClienteDetalle, nuevo: string) => void;
  onEditar: (d: PedidoClienteDetalle) => void;
  onHistorial: (d: PedidoClienteDetalle) => void;
  onDespachar: (d: PedidoClienteDetalle) => void;
}) {
  const { cargando, datos: d, error: errorCarga } = useDetalle<PedidoClienteDetalle>(ruta, version);
  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (errorCarga || !d) return <p className="text-sm text-peligro">{errorCarga ?? "No se encontró el pedido."}</p>;
  const p = d.pedido;
  const editable = puedeGestionar && ["borrador", "confirmado", "en_preparacion"].includes(p.estado);
  const despachable = puedeDespachar && ["confirmado", "en_preparacion"].includes(p.estado) && d.lineas.some((l) => l.pendiente > 0);

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="text-lg font-semibold text-texto">
            {p.codigo} · {p.clienteNombre}
          </p>
          <p className="text-sm text-texto-suave">
            {fecha(p.fecha)} · {p.comercialNombre}
            {p.ciudad ? ` · ${p.ciudad}` : ""}
            {p.ventaCodigo ? (
              <>
                {" · venta "}
                <Link href="/crm/ventas" className="text-acento hover:underline">
                  {p.ventaCodigo}
                </Link>
              </>
            ) : (
              " · pedido suelto"
            )}
          </p>
        </div>
        <Etiqueta texto={ESTADOS_PEDIDO_CLIENTE[p.estado]} tono={tonoPedidoCliente(p.estado)} />
      </div>
      {p.observaciones ? <p className="text-sm whitespace-pre-line text-texto">{p.observaciones}</p> : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="px-3 py-2 text-right font-medium">Pedido</th>
            <th className="px-3 py-2 text-right font-medium">Despachado</th>
            <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Precio u.</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {d.lineas.map((l) => (
            <tr key={l.id} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                <span className="text-texto italic">{l.nombreCientifico}</span>
                {l.observacion ? <p className="text-xs text-texto-suave">{l.observacion}</p> : null}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">{piezas(l.cantidad)}</td>
              <td className={`px-3 py-2 text-right tabular-nums ${l.pendiente > 0 ? "text-advertencia-texto" : "text-exito-texto"}`}>{piezas(l.despachada)}</td>
              <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">
                {l.precioUnitario != null ? dinero(l.precioUnitario, p.moneda ?? "COP") : "—"}
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
      {p.total != null ? (
        <p className="text-right text-sm text-texto-suave">
          Total: <strong className="text-texto tabular-nums">{dinero(p.total, p.moneda ?? "COP")}</strong>
        </p>
      ) : null}

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:flex-wrap sm:justify-end">
        <button type="button" onClick={() => onHistorial(d)} className={`${botonSecundarioClass} gap-2`}>
          <History className="size-4" aria-hidden /> Historial
        </button>
        {puedeGestionar
          ? (ACCIONES_PEDIDO_CLIENTE[p.estado] ?? []).map((a) => (
              <button key={a.estado} type="button" disabled={procesando} onClick={() => onEstado(d, a.estado)} className={botonSecundarioClass}>
                {a.texto}
              </button>
            ))
          : null}
        {editable ? (
          <button type="button" onClick={() => onEditar(d)} className={botonSecundarioClass}>
            Editar
          </button>
        ) : null}
        {despachable ? (
          <button type="button" onClick={() => onDespachar(d)} className={`${botonPrimarioClass} gap-2`}>
            <Truck className="size-4" aria-hidden /> Picking y despacho
          </button>
        ) : null}
      </div>
    </div>
  );
}

function FormularioPedido({
  detalle,
  desdeVenta,
  productos,
  clientes,
  comerciales,
  puedeAsignar,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  detalle: PedidoClienteDetalle | null;
  desdeVenta: VentaOrigen | null;
  productos: ProductoOpcion[];
  clientes: Opcion[];
  comerciales: Opcion[];
  puedeAsignar: boolean;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const p = detalle?.pedido;
  const [clienteId, setClienteId] = useState(p?.clienteId ?? "");
  const [comercialId, setComercialId] = useState(p?.comercialId ?? "");
  const [fechaPedido, setFechaPedido] = useState(p?.fecha ?? "");
  const [ciudad, setCiudad] = useState(p?.ciudad ?? desdeVenta?.ciudad ?? "");
  const [observaciones, setObservaciones] = useState(p?.observaciones ?? "");
  const [lineas, setLineas] = useState<LineaEditable[]>(() =>
    detalle?.lineas.length
      ? detalle.lineas.map((l) =>
          nuevaLinea({
            id: l.id,
            productoId: l.productoId,
            cantidad: String(l.cantidad),
            valor: l.precioUnitario != null ? String(l.precioUnitario) : "",
            observacion: l.observacion ?? "",
            minimo: l.despachada,
          }),
        )
      : [nuevaLinea()],
  );
  const enlazado = !!desdeVenta || !!p?.ventaId;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          ventaId: desdeVenta?.id ?? null,
          clienteId: enlazado ? null : clienteId || null,
          comercialId: enlazado ? null : comercialId || null,
          fecha: fechaPedido || null,
          ciudad,
          observaciones,
          lineas: lineas
            .filter((l) => l.productoId)
            .map((l) => ({ id: l.id ?? null, productoId: l.productoId, cantidad: aNumero(l.cantidad) ?? 0, precioUnitario: aNumero(l.valor), observacion: l.observacion })),
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        {enlazado ? (
          <CampoAncho>
            <p className="rounded-md bg-acento-suave px-3 py-2 text-sm text-texto">
              {desdeVenta
                ? `Enlazado a la venta ${desdeVenta.codigo}: cliente ${desdeVenta.clienteNombre}, comercial ${desdeVenta.comercialNombre}.`
                : `Enlazado a la venta ${p?.ventaCodigo}: cliente ${p?.clienteNombre}, comercial ${p?.comercialNombre}.`}
            </p>
          </CampoAncho>
        ) : (
          <>
            <Campo label="Cliente" htmlFor="clientePedido">
              <select id="clientePedido" value={clienteId} onChange={(e) => setClienteId(e.target.value)} disabled={!!p} className={selectClass}>
                <option value="">Selecciona…</option>
                {(p && !clientes.some((c) => c.id === p.clienteId) ? [...clientes, { id: p.clienteId, nombre: p.clienteNombre }] : clientes).map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                  </option>
                ))}
              </select>
            </Campo>
            {puedeAsignar && !p ? (
              <Campo label="Comercial" htmlFor="comercialPedido" hint="Vacío = tú.">
                <select id="comercialPedido" value={comercialId} onChange={(e) => setComercialId(e.target.value)} className={selectClass}>
                  <option value="">Yo</option>
                  {comerciales.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nombre}
                    </option>
                  ))}
                </select>
              </Campo>
            ) : null}
          </>
        )}
        <Campo label="Fecha" htmlFor="fechaPedidoCliente" hint="Vacío = hoy.">
          <input id="fechaPedidoCliente" type="date" value={fechaPedido} onChange={(e) => setFechaPedido(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Ciudad (opcional)" htmlFor="ciudadPedido">
          <input id="ciudadPedido" value={ciudad} onChange={(e) => setCiudad(e.target.value)} maxLength={120} className={inputClass} />
        </Campo>
        <CampoAncho>
          <Campo label="Observaciones (opcional)" htmlFor="obsPedidoCliente">
            <textarea id="obsPedidoCliente" value={observaciones} onChange={(e) => setObservaciones(e.target.value)} maxLength={2000} className={textareaClass} />
          </Campo>
        </CampoAncho>
      </RejillaCampos>
      <fieldset className="flex flex-col gap-3">
        <legend className="mb-2 text-sm font-semibold text-texto">Especies</legend>
        <EditorLineas lineas={lineas} onChange={setLineas} productos={productos} etiquetaValor="Precio u." />
      </fieldset>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar pedido"}
        </button>
      </div>
    </form>
  );
}

/**
 * Picking: por cada especie pendiente se listan sus existencias **sanas y ubicadas** (lote × acuario) y se
 * escribe cuánto sale de cada una. Lo enfermo y lo que sigue en aclimatación nunca se despacha. La base valida de nuevo
 * con la fila bloqueada, así que dos despachos a la vez no pueden dejar stock negativo.
 */
function FormularioDespacho({
  detalle,
  guardando,
  error,
  onDespachar,
  onCancelar,
}: {
  detalle: PedidoClienteDetalle;
  guardando: boolean;
  error: string | null;
  onDespachar: (salidas: Record<string, unknown>[]) => void;
  onCancelar: () => void;
}) {
  const router = useRouter();
  const pendientes = detalle.lineas.filter((l) => l.pendiente > 0);
  const [cantidades, setCantidades] = useState<Record<string, string>>({});
  const [avisoLocal, setAvisoLocal] = useState<string | null>(null);
  const [okReserva, setOkReserva] = useState<string | null>(null);
  const [versionReservas, setVersionReservas] = useState(0);
  const [reservando, iniciarReserva] = useTransition();
  const reservas = useDetalle<Reserva[]>(`/api/sanidad/reservas/pedido/${detalle.pedido.id}`, versionReservas);

  /** Lo reservado de una línea en un lote y acuario (0 si nada). */
  const reservado = (lineaId: string, e: Existencia) =>
    reservas.datos?.find((r) => r.lineaId === lineaId && r.loteId === e.loteId && r.acuarioId === e.acuarioId)?.cantidad ?? 0;
  /** Lo escrito, o lo reservado mientras no se escriba nada: el picking parte de la reserva. */
  const valor = (lineaId: string, e: Existencia) => cantidades[`${lineaId}|${e.id}`] ?? (reservado(lineaId, e) ? String(reservado(lineaId, e)) : "");

  /** Fija las reservas que cambiaron (0 = quitar). Reservar es lo que pone el acuario en ayuno antes del vuelo. */
  const reservar = (stock: Record<string, Existencia[]>) =>
    iniciarReserva(async () => {
      setAvisoLocal(null);
      setOkReserva(null);
      const errores: string[] = [];
      let cambios = 0;
      for (const l of pendientes) {
        for (const e of stock[l.productoId] ?? []) {
          const n = aNumero(valor(l.id, e)) ?? 0;
          if (n === reservado(l.id, e) || !e.acuarioId) continue;
          cambios++;
          const r = await fijarReservaAction(l.id, e.loteId, e.acuarioId, n);
          if (r.error) errores.push(`${e.acuarioCodigo}: ${r.error}`);
        }
      }
      if (cambios === 0) setAvisoLocal("No hay cambios en las reservas.");
      else if (errores.length) setAvisoLocal(errores.join(" "));
      else setOkReserva("Reservas guardadas. Si el pedido sale en un vuelo, esos acuarios entran en ayuno 48 h antes.");
      setCantidades({});
      setVersionReservas((v) => v + 1);
      router.refresh();
    });

  const enviar = (stock: Record<string, Existencia[]>) => {
    const salidas: Record<string, unknown>[] = [];
    for (const l of pendientes) {
      let suma = 0;
      for (const e of stock[l.productoId] ?? []) {
        const n = aNumero(valor(l.id, e)) ?? 0;
        if (n <= 0) continue;
        suma += n;
        salidas.push({ lineaId: l.id, loteId: e.loteId, acuarioId: e.acuarioId, cantidad: n });
      }
      if (suma > l.pendiente) {
        setAvisoLocal(`${l.nombreCientifico}: ${piezas(suma)} supera lo pendiente (${piezas(l.pendiente)}).`);
        return;
      }
    }
    if (salidas.length === 0) {
      setAvisoLocal("Escribe cuánto sale de al menos un acuario.");
      return;
    }
    setAvisoLocal(null);
    onDespachar(salidas);
  };

  return (
    <StockPorProducto productos={[...new Set(pendientes.map((l) => l.productoId))]}>
      {(stock, cargando) => (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            enviar(stock);
          }}
          className="flex flex-col gap-5"
        >
          <p className="text-sm text-texto-suave">
            Solo aparece el stock sano y ubicado en acuario. Escribe cuántos salen de cada lote y acuario. <strong>Reservar</strong> aparta esos peces
            para el pedido (bodega los ve en Mi día y, si salen en un vuelo, el acuario entra en ayuno); <strong>Despachar</strong> los descuenta.
          </p>
          {pendientes.map((l) => {
            const opciones = stock[l.productoId] ?? [];
            const suma = opciones.reduce((s, e) => s + (aNumero(valor(l.id, e)) ?? 0), 0);
            return (
              <fieldset key={l.id} className="flex flex-col gap-2 rounded-md border border-borde p-3">
                <legend className="px-1 text-sm text-texto">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                  <span className="italic">{l.nombreCientifico}</span>
                </legend>
                <p className={`text-xs ${suma > l.pendiente ? "text-peligro" : "text-texto-suave"}`}>
                  Pendiente {piezas(l.pendiente)} · seleccionado {piezas(suma)}
                </p>
                {cargando ? (
                  <p className="text-sm text-texto-suave">Cargando stock…</p>
                ) : opciones.length === 0 ? (
                  <p className="text-sm text-advertencia-texto">Sin stock sano ubicado para esta especie.</p>
                ) : (
                  <ul className="flex flex-col gap-2">
                    {opciones.map((e) => (
                      <li key={e.id} className="grid grid-cols-[1fr_7rem] items-center gap-3">
                        <span className="text-sm text-texto">
                          <span className="font-mono">{e.acuarioCodigo}</span> · {e.loteCodigo}
                          <span className="block text-xs text-texto-suave">
                            {piezas(e.cantidad)} disponibles · {e.diasEnBodega} días
                            {reservado(l.id, e) ? <span className="font-medium text-info-texto"> · {piezas(reservado(l.id, e))} reservados</span> : null}
                          </span>
                        </span>
                        <input
                          aria-label={`Cantidad a despachar de ${e.acuarioCodigo}, lote ${e.loteCodigo}`}
                          type="number"
                          inputMode="numeric"
                          min={0}
                          max={e.cantidad}
                          value={valor(l.id, e)}
                          onChange={(ev) => setCantidades((c) => ({ ...c, [`${l.id}|${e.id}`]: ev.target.value }))}
                          className={inputClass}
                        />
                      </li>
                    ))}
                  </ul>
                )}
              </fieldset>
            );
          })}
          {avisoLocal || error ? <p className="text-sm text-peligro">{avisoLocal ?? error}</p> : null}
          {okReserva ? <p className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">{okReserva}</p> : null}
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
              Volver
            </button>
            <button type="button" disabled={reservando || cargando || reservas.cargando} onClick={() => reservar(stock)} className={botonSecundarioClass}>
              {reservando ? "Reservando…" : "Reservar acuarios"}
            </button>
            <button type="submit" disabled={guardando || cargando} className={`${botonPrimarioClass} gap-2`}>
              <Truck className="size-4" aria-hidden /> {guardando ? "Despachando…" : "Despachar"}
            </button>
          </div>
        </form>
      )}
    </StockPorProducto>
  );
}

/**
 * Trae las existencias de las especies pendientes y deja solo las despachables: sanas, con acuario y
 * stock. Con una especie se filtra en la API; con varias se trae todo una vez y se filtra aquí.
 */
function StockPorProducto({
  productos,
  children,
}: {
  productos: string[];
  children: (stock: Record<string, Existencia[]>, cargando: boolean) => React.ReactNode;
}) {
  const ruta =
    productos.length === 0 ? null : productos.length === 1 ? `/api/inventario/existencias?productoId=${productos[0]}` : "/api/inventario/existencias";
  const { cargando, datos } = useDetalle<Existencia[]>(ruta);
  const stock: Record<string, Existencia[]> = {};
  for (const e of datos ?? []) {
    if (e.condicion !== "sano" || !e.acuarioId || e.cantidad <= 0 || !productos.includes(e.productoId)) continue;
    (stock[e.productoId] ??= []).push(e);
  }
  // Primero lo que lleva más días en bodega.
  for (const k of Object.keys(stock)) stock[k].sort((a, b) => b.diasEnBodega - a.diasEnBodega);
  return <>{children(stock, cargando)}</>;
}
