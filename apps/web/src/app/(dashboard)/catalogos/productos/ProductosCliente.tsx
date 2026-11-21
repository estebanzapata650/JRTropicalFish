"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { History, Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import {
  ETIQUETAS_HISTORIAL_CATALOGO,
  TIPOS_PRODUCTO,
  disponible,
  textoBolsa,
  textoTalla,
  type ListaPrecios,
  type ParametrosPrecios,
  type Producto,
} from "@/lib/catalogos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import { dinero } from "@/lib/formato";
import { eliminarProductoAction, guardarProductoAction } from "../actions";
import { FormularioProducto, type CentroOpcion, type GrupoOpcion } from "./FormularioProducto";

/** El catálogo tiene ~800 productos: se pinta por páginas para que el móvil no sufra. */
const POR_PAGINA = 50;

export function ProductosCliente({
  productos,
  listas,
  parametros,
  grupos,
  centros,
  puedeEditar,
  puedeEditarPrecios,
}: {
  productos: Producto[];
  listas: ListaPrecios[];
  parametros: ParametrosPrecios | null;
  grupos: GrupoOpcion[];
  centros: CentroOpcion[];
  puedeEditar: boolean;
  puedeEditarPrecios: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [grupo, setGrupo] = useState("");
  const [tipo, setTipo] = useState("");
  const [centro, setCentro] = useState("");
  const [soloDisponibles, setSoloDisponibles] = useState(false);
  const [listaVisible, setListaVisible] = useState(listas.find((l) => l.codigo === "STANDARD")?.id ?? listas[0]?.id ?? "");
  const [mostrar, setMostrar] = useState(POR_PAGINA);
  const [editando, setEditando] = useState<Producto | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<Producto | null>(null);
  const [eliminando, setEliminando] = useState<Producto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const lista = listas.find((l) => l.id === listaVisible);
  const filtrados = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return productos.filter(
      (p) =>
        (!grupo || p.grupoId === grupo) &&
        (!tipo || p.tipo === tipo) &&
        (!centro || p.centrosAcopio.some((c) => c.id === centro)) &&
        (!soloDisponibles || (p.activo && disponible(p.disponibilidad))) &&
        (!t || [p.codigo, p.nombreCientifico, p.nombreComun, p.nombreEspanol].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [productos, busqueda, grupo, tipo, centro, soloDisponibles]);

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setEliminando(null);
    setError(null);
  };

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      const r = await guardarProductoAction(editando?.id ?? null, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  // Cambiar un filtro vuelve a la primera página (en el handler, no en un efecto).
  const filtrar = <T,>(set: (v: T) => void) => (v: T) => {
    set(v);
    setMostrar(POR_PAGINA);
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Productos"
        descripcion="Catálogo de especies y presentaciones (stock list)."
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nuevo producto
            </button>
          ) : null
        }
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <label className="relative sm:col-span-2 lg:col-span-4">
          <span className="sr-only">Buscar producto</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input
            value={busqueda}
            onChange={(e) => filtrar(setBusqueda)(e.target.value)}
            placeholder="Código, nombre científico, común o en español…"
            className={`${inputClass} pl-9`}
          />
        </label>
        <select aria-label="Filtrar por grupo" value={grupo} onChange={(e) => filtrar(setGrupo)(e.target.value)} className={selectClass}>
          <option value="">Todos los grupos</option>
          {grupos.map((g) => (
            <option key={g.id} value={g.id}>
              {g.codigo} · {g.nombre}
            </option>
          ))}
        </select>
        <select aria-label="Filtrar por tipo" value={tipo} onChange={(e) => filtrar(setTipo)(e.target.value)} className={selectClass}>
          <option value="">Todos los tipos</option>
          {Object.entries(TIPOS_PRODUCTO).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
        <select aria-label="Filtrar por centro de acopio" value={centro} onChange={(e) => filtrar(setCentro)(e.target.value)} className={selectClass}>
          <option value="">Todos los centros</option>
          {centros.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nombre}
            </option>
          ))}
        </select>
        {listas.length > 0 ? (
          <select aria-label="Lista de precios a mostrar" value={listaVisible} onChange={(e) => setListaVisible(e.target.value)} className={selectClass}>
            {listas.map((l) => (
              <option key={l.id} value={l.id}>
                Precio: {l.nombre} ({l.moneda})
              </option>
            ))}
          </select>
        ) : null}
      </div>
      <button
        type="button"
        aria-pressed={soloDisponibles}
        onClick={() => filtrar(setSoloDisponibles)(!soloDisponibles)}
        className={`inline-flex h-11 w-fit items-center rounded-full border px-4 text-sm transition-colors ${
          soloDisponibles ? "border-exito-texto bg-exito-fondo font-medium text-exito-texto" : "border-borde text-texto hover:bg-acento-suave"
        }`}
      >
        Solo disponibles para ofrecer
      </button>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Producto</th>
            <th className="hidden px-4 py-3 font-medium lg:table-cell">Grupo · tipo</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Talla</th>
            <th className="hidden px-4 py-3 font-medium xl:table-cell">Bolsa</th>
            <th className="hidden px-4 py-3 font-medium sm:table-cell">Disponibilidad</th>
            {lista ? <th className="px-4 py-3 text-right font-medium">{lista.nombre}</th> : null}
            <th className="px-4 py-3 text-right font-medium">
              <span className="sr-only sm:not-sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.slice(0, mostrar).map((p) => (
            <tr key={p.id} className={`border-b border-borde last:border-0 ${p.activo ? "" : "opacity-60"}`}>
              <td className="px-4 py-3">
                <p className="text-texto">
                  <span className="mr-2 font-mono text-xs text-texto-suave">{p.codigo}</span>
                  <span className="font-medium italic">{p.nombreCientifico}</span>
                </p>
                <p className="text-xs text-texto-suave">{p.nombreEspanol ?? p.nombreComun}</p>
                <p className="text-xs text-texto-suave sm:hidden">{p.disponibilidad ?? "Sin disponibilidad"}</p>
              </td>
              <td className="hidden px-4 py-3 text-texto lg:table-cell">
                {p.grupoNombre}
                <p className="text-xs text-texto-suave">{TIPOS_PRODUCTO[p.tipo]}</p>
              </td>
              <td className="hidden px-4 py-3 whitespace-nowrap text-texto md:table-cell">{textoTalla(p)}</td>
              <td className="hidden px-4 py-3 text-texto tabular-nums xl:table-cell">{textoBolsa(p)}</td>
              <td className="hidden px-4 py-3 sm:table-cell">
                {p.disponibilidad ? <Etiqueta texto={p.disponibilidad} tono={disponible(p.disponibilidad) ? "exito" : "neutro"} /> : <span className="text-texto-tenue">—</span>}
              </td>
              {lista ? (
                <td className="px-4 py-3 text-right whitespace-nowrap tabular-nums text-texto">
                  {p.precios[lista.id] != null ? dinero(p.precios[lista.id], lista.moneda) : <span className="text-texto-tenue">—</span>}
                </td>
              ) : null}
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  {/* En móvil el historial va dentro del diálogo del producto: así el lápiz cabe a 390 px. */}
                  <span className={puedeEditar ? "hidden sm:inline-flex" : "inline-flex"}>
                    <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${p.codigo}`} onClick={() => setHistorialDe(p)} />
                  </span>
                  {puedeEditar ? <BotonIcono accion="editar" etiqueta={`Editar ${p.codigo} ${p.nombreCientifico}`} onClick={() => setEditando(p)} /> : null}
                </div>
              </td>
            </tr>
          ))}
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={7} className="px-4 py-8 text-center text-texto-suave">
                {productos.length === 0 ? "Aún no hay productos." : "Ningún producto coincide con el filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <div className="flex flex-col items-center gap-3 sm:flex-row sm:justify-between">
        <p className="text-sm text-texto-suave">
          {Math.min(mostrar, filtrados.length)} de {filtrados.length} productos
          {filtrados.length !== productos.length ? ` (${productos.length} en total)` : ""}
        </p>
        {mostrar < filtrados.length ? (
          <button type="button" onClick={() => setMostrar((m) => m + POR_PAGINA)} className={botonSecundarioClass}>
            Mostrar {Math.min(POR_PAGINA, filtrados.length - mostrar)} más
          </button>
        ) : null}
      </div>

      <Dialogo
        titulo={editando ? `${editando.codigo} · ${editando.nombreCientifico}` : "Nuevo producto"}
        abierto={creando || editando !== null}
        onCerrar={cerrar}
        ancho="max-w-5xl"
      >
        <FormularioProducto
          key={editando?.id ?? "nuevo"}
          producto={editando}
          listas={listas}
          parametros={parametros}
          grupos={grupos}
          centros={centros}
          puedeEditarPrecios={puedeEditarPrecios}
          guardando={procesando}
          error={error}
          onGuardar={guardar}
          onCancelar={cerrar}
        />
        {editando && puedeEditar ? (
          <div className="mt-6 flex flex-wrap justify-between gap-3 border-t border-borde pt-4">
            <button type="button" onClick={() => setHistorialDe(editando)} className="text-sm font-medium text-acento hover:underline">
              Ver historial
            </button>
            <button type="button" onClick={() => setEliminando(editando)} className="text-sm font-medium text-peligro hover:underline">
              Eliminar este producto
            </button>
          </div>
        ) : null}
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.codigo}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? <HistorialRemoto ruta={`/api/v1/catalogos/productos/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CATALOGO} /> : null}
      </Dialogo>
      <DialogoConfirmacion
        titulo="Eliminar producto"
        mensaje={
          eliminando ? (
            <>
              ¿Eliminar <strong>{eliminando.codigo}</strong> ({eliminando.nombreCientifico})? Si solo dejó de ofrecerse, mejor desmárcalo como
              activo o pon su disponibilidad en «Not available».
            </>
          ) : null
        }
        textoConfirmar="Eliminar"
        peligro
        abierto={eliminando !== null}
        procesando={procesando}
        error={error}
        onConfirmar={() =>
          eliminando &&
          iniciar(async () => {
            const r = await eliminarProductoAction(eliminando.id);
            if (r.error) {
              setError(r.error);
              return;
            }
            cerrar();
            router.refresh();
          })
        }
        onCerrar={() => {
          setEliminando(null);
          setError(null);
        }}
      />
    </div>
  );
}
