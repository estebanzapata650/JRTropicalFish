"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import { Link2, Search, TriangleAlert } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import {
  BotonIcono,
  Campo,
  EncabezadoTabla,
  Etiqueta,
  TablaResponsive,
} from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { SelectorBuscable, coincide } from "@/components/SelectorBuscable";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
} from "@/lib/estilos";
import {
  cuencasAunap,
  type EspecieAunap,
  type ProductoAunap,
} from "@/lib/exportaciones";
import {
  enlazarAunapAutomaticoAction,
  enlazarProductoAunapAction,
} from "../actions";

const LIMITE = 100;

/**
 * AUNAP: el listado de la Resolución 1924 de 2015 y el código de cada producto. Lo que se declara a la autoridad
 * sale de aquí; un producto sin código no se puede declarar ni exportar.
 */
export function AunapCliente({
  especies,
  productos,
  configurar,
}: {
  especies: EspecieAunap[];
  productos: ProductoAunap[];
  configurar: boolean;
}) {
  const router = useRouter();
  const [vista, setVista] = useState<"productos" | "especies">("productos");
  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<"sin" | "con" | "todos">("sin");
  const [editando, setEditando] = useState<ProductoAunap | null>(null);
  const [mensaje, setMensaje] = useState<{
    tipo: "ok" | "error";
    texto: string;
  } | null>(null);
  const [pendiente, iniciar] = useTransition();

  const activos = productos.filter((p) => p.activo);
  const sinCodigo = activos.filter((p) => !p.especieAunapId).length;

  const productosFiltrados = useMemo(
    () =>
      productos.filter(
        (p) =>
          p.activo &&
          (filtro === "todos" || (filtro === "sin") === !p.especieAunapId) &&
          coincide(
            {
              id: p.productoId,
              nombre: `${p.productoCodigo} ${p.nombreCientifico}`,
              detalle: `${p.nombreComun ?? ""} ${p.aunapNombreCientifico ?? ""}`,
            },
            busqueda,
          ),
      ),
    [productos, filtro, busqueda],
  );
  const especiesFiltradas = useMemo(
    () =>
      especies.filter((e) =>
        coincide(
          {
            id: e.id,
            nombre: `${e.codigo} ${e.nombreCientifico}`,
            detalle: `${e.nombreComun ?? ""} ${e.familia ?? ""}`,
          },
          busqueda,
        ),
      ),
    [especies, busqueda],
  );

  const correr = (
    fn: () => Promise<{ error?: string; datos?: unknown }>,
    exito: (datos: unknown) => string,
  ) =>
    iniciar(async () => {
      const r = await fn();
      setMensaje(
        r.error
          ? { tipo: "error", texto: r.error }
          : { tipo: "ok", texto: exito(r.datos) },
      );
      if (!r.error) {
        setEditando(null);
        router.refresh();
      }
    });

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        titulo="AUNAP"
        descripcion={`${especies.length} especies autorizadas (Resolución 1924 de 2015) · ${activos.length - sinCodigo} de ${activos.length} productos con código.`}
        acciones={
          configurar && vista === "productos" ? (
            <button
              type="button"
              disabled={pendiente || especies.length === 0}
              className={botonSecundarioClass}
              onClick={() =>
                correr(
                  () => enlazarAunapAutomaticoAction(),
                  (d) =>
                    `Enlazados por nombre científico idéntico: ${(d as { enlazados: number }).enlazados}.`,
                )
              }
            >
              Enlazar por nombre idéntico
            </button>
          ) : null
        }
      />

      <p className="flex items-start gap-2 rounded-md bg-info-fondo px-3 py-2 text-sm text-info-texto">
        <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden />
        La solicitud AUNAP, el packing list de aduana y la custom invoice salen
        de las especies y cantidades reales del embarque, con este código.
        Enlaza un producto solo con su misma especie (o su sinónimo taxonómico).
        Si la especie no está en la resolución, déjala sin código: no se puede
        declarar ni exportar.
      </p>
      {especies.length === 0 ? (
        <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          El listado aún no está cargado:{" "}
          <code>
            npm run aunap:importar -- &quot;docs/excelBD/Listado de la
            Aunap.xlsx&quot;
          </code>
        </p>
      ) : null}
      {mensaje ? (
        <p
          role="status"
          className={`rounded-md px-3 py-2 text-sm ${mensaje.tipo === "ok" ? "bg-exito-fondo text-exito-texto" : "bg-peligro/15 text-peligro"}`}
        >
          {mensaje.texto}
        </p>
      ) : null}

      <div role="tablist" aria-label="Vista" className="flex gap-2">
        {(
          [
            ["productos", `Productos (${sinCodigo} sin código)`],
            ["especies", `Listado AUNAP (${especies.length})`],
          ] as const
        ).map(([v, t]) => (
          <button
            key={v}
            type="button"
            role="tab"
            aria-selected={vista === v}
            onClick={() => setVista(v)}
            className={`inline-flex h-11 items-center rounded-full border px-4 text-sm font-medium ${vista === v ? "border-acento bg-acento text-acento-texto" : "border-borde bg-superficie text-texto"}`}
          >
            {t}
          </button>
        ))}
      </div>

      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search
            className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue"
            aria-hidden
          />
          <input
            type="search"
            aria-label="Buscar"
            placeholder="Código, nombre o iniciales"
            className={`${inputClass} pl-9`}
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
        </div>
        {vista === "productos" ? (
          <select
            aria-label="Filtrar productos"
            className={`${selectClass} sm:w-56`}
            value={filtro}
            onChange={(e) => setFiltro(e.target.value as typeof filtro)}
          >
            <option value="sin">Sin código AUNAP</option>
            <option value="con">Con código AUNAP</option>
            <option value="todos">Todos</option>
          </select>
        ) : null}
      </div>

      {vista === "productos" ? (
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Producto</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Grupo
              </th>
              <th className="px-3 py-2 font-medium">Especie AUNAP</th>
              <th className="px-3 py-2">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {productosFiltrados.slice(0, LIMITE).map((p) => (
              <tr
                key={p.productoId}
                className="border-b border-borde last:border-0"
              >
                <td className="px-3 py-2">
                  <span className="font-medium text-texto">
                    {p.productoCodigo}
                  </span>{" "}
                  <span className="italic text-texto">
                    {p.nombreCientifico}
                  </span>
                  {p.nombreComun ? (
                    <span className="block text-xs text-texto-suave">
                      {p.nombreComun}
                    </span>
                  ) : null}
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  {p.grupoNombre ?? "—"}
                </td>
                <td className="px-3 py-2">
                  {p.especieAunapId ? (
                    <>
                      <span className="font-mono text-xs text-texto-suave">
                        {p.aunapCodigo}
                      </span>{" "}
                      <span className="italic">{p.aunapNombreCientifico}</span>
                    </>
                  ) : (
                    <Etiqueta
                      texto="Sin código"
                      tono="advertencia"
                    />
                  )}
                </td>
                <td className="px-3 py-2 text-right">
                  {configurar ? (
                    <BotonIcono
                      accion="otra"
                      icono={Link2}
                      etiqueta={`Código AUNAP de ${p.productoCodigo}`}
                      onClick={() => setEditando(p)}
                    />
                  ) : null}
                </td>
              </tr>
            ))}
            {productosFiltrados.length === 0 ? (
              <tr>
                <td
                  colSpan={4}
                  className="px-3 py-8 text-center text-texto-suave"
                >
                  Sin productos para este filtro.
                </td>
              </tr>
            ) : null}
          </tbody>
        </TablaResponsive>
      ) : (
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Código</th>
              <th className="px-3 py-2 font-medium">Especie</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Familia
              </th>
              <th className="hidden px-3 py-2 font-medium lg:table-cell">
                Cuencas
              </th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Productos
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {especiesFiltradas.slice(0, LIMITE).map((e) => (
              <tr key={e.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2 font-mono text-xs">{e.codigo}</td>
                <td className="px-3 py-2">
                  <span className="italic text-texto">
                    {e.nombreCientifico}
                  </span>
                  {e.nombreComun ? (
                    <span className="block text-xs text-texto-suave">
                      {e.nombreComun}
                    </span>
                  ) : null}
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  {e.familia ?? "—"}
                </td>
                <td className="hidden px-3 py-2 text-xs lg:table-cell">
                  {cuencasAunap(e) || "—"}
                </td>
                <td className="hidden px-3 py-2 tabular-nums md:table-cell">
                  {e.productos}
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      )}
      {(vista === "productos"
        ? productosFiltrados.length
        : especiesFiltradas.length) > LIMITE ? (
        <p className="text-sm text-texto-suave">
          Mostrando {LIMITE}: busca para acotar.
        </p>
      ) : null}

      <Dialogo
        titulo={editando ? `Código AUNAP de ${editando.productoCodigo}` : ""}
        abierto={editando !== null}
        onCerrar={() => setEditando(null)}
        ancho="max-w-xl"
      >
        {editando ? (
          <FormEnlace
            key={editando.productoId}
            producto={editando}
            especies={especies.filter((e) => e.activo)}
            pendiente={pendiente}
            onGuardar={(especieId) =>
              correr(
                () =>
                  enlazarProductoAunapAction(editando.productoId, especieId),
                () =>
                  especieId
                    ? `${editando.productoCodigo} enlazado.`
                    : `${editando.productoCodigo} quedó sin código AUNAP.`,
              )
            }
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function FormEnlace({
  producto,
  especies,
  pendiente,
  onGuardar,
}: {
  producto: ProductoAunap;
  especies: EspecieAunap[];
  pendiente: boolean;
  onGuardar: (especieId: string | null) => void;
}) {
  const [especieId, setEspecieId] = useState(producto.especieAunapId ?? "");
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar(especieId || null);
      }}
    >
      <p className="text-sm text-texto">
        <span className="font-medium">{producto.productoCodigo}</span>{" "}
        <span className="italic">{producto.nombreCientifico}</span>
        {producto.nombreComun ? (
          <span className="text-texto-suave"> · {producto.nombreComun}</span>
        ) : null}
      </p>
      <Campo
        label="Especie de la resolución"
        htmlFor="especie-aunap"
        hint="Solo la misma especie o su sinónimo taxonómico. Vacío = sin código (no exportable)."
      >
        <SelectorBuscable
          id="especie-aunap"
          opciones={especies.map((e) => ({
            id: e.id,
            nombre: e.nombreCientifico,
            detalle: `${e.codigo} · ${e.nombreComun ?? ""}`,
          }))}
          valor={especieId}
          onChange={setEspecieId}
          placeholder="Nombre científico, común o código…"
        />
      </Campo>
      <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
        Guardar
      </button>
    </form>
  );
}
