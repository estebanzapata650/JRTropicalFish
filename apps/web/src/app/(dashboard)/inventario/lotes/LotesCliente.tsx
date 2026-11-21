"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Search } from "lucide-react";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { BarraSegmentada, partesLote } from "@/components/Graficos";
import { DIAS_LOTE_ANTIGUO, dias, pct, piezas, type LoteTrazable } from "@/lib/inventario";
import { casillaClass, inputClass, selectClass } from "@/lib/estilos";
import { fecha } from "@/lib/formato";

const POR_PAGINA = 50;

type Estado = "bodega" | "todos" | "agotados";

/**
 * Lista de trazabilidad: por lote, en qué quedó cada pez (barra), cuánto estuvo en aclimatación y en
 * cuarentena y a cuántos clientes se envió. El detalle completo está en el recorrido del lote.
 */
export function LotesCliente({ lotes, soloAntiguos }: { lotes: LoteTrazable[]; soloAntiguos: boolean }) {
  const [busqueda, setBusqueda] = useState("");
  const [proveedor, setProveedor] = useState("");
  const [estado, setEstado] = useState<Estado>("bodega");
  const [antiguos, setAntiguos] = useState(soloAntiguos);
  const [cuarentena, setCuarentena] = useState(false);
  const [mostrar, setMostrar] = useState(POR_PAGINA);

  const proveedores = useMemo(
    () => [...new Set(lotes.map((x) => x.lote.proveedorNombre ?? "Sin proveedor"))].sort((a, b) => a.localeCompare(b)),
    [lotes],
  );

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return lotes.filter(({ lote: l, tiempos: ti }) => {
      const enBodega = l.disponible > 0;
      return (
        (estado === "todos" || (estado === "bodega" ? enBodega : !enBodega)) &&
        (!proveedor || (l.proveedorNombre ?? "Sin proveedor") === proveedor) &&
        (!antiguos || (enBodega && l.diasEnBodega > DIAS_LOTE_ANTIGUO)) &&
        (!cuarentena || ti.cuarentenaEnCurso) &&
        (!t || [l.codigo, l.productoCodigo, l.nombreCientifico, l.nombreEspanol, l.proveedorNombre, l.centroNombre].some((v) => v?.toLowerCase().includes(t)))
      );
    });
  }, [lotes, busqueda, proveedor, estado, antiguos, cuarentena]);

  // Resumen de lo filtrado (los KPI siguen al filtro).
  const recibidos = visibles.reduce((s, x) => s + x.lote.recibidos, 0);
  const muertos = visibles.reduce((s, x) => s + x.lote.muertos, 0);
  const conAclimatacion = visibles.filter((x) => x.tiempos.diasAclimatacion > 0);
  const aclimatacionPromedio = conAclimatacion.length ? conAclimatacion.reduce((s, x) => s + x.tiempos.diasAclimatacion, 0) / conAclimatacion.length : 0;
  const enCuarentena = visibles.filter((x) => x.tiempos.cuarentenaEnCurso).length;

  const reiniciar = () => setMostrar(POR_PAGINA);

  return (
    <div className="flex flex-col gap-5">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-[2fr_1fr_1fr]">
        <label className="relative">
          <span className="sr-only">Buscar lote</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input
            value={busqueda}
            onChange={(e) => {
              setBusqueda(e.target.value);
              reiniciar();
            }}
            placeholder="Lote, especie, proveedor o centro…"
            className={`${inputClass} pl-9`}
          />
        </label>
        <select
          aria-label="Proveedor"
          value={proveedor}
          onChange={(e) => {
            setProveedor(e.target.value);
            reiniciar();
          }}
          className={selectClass}
        >
          <option value="">Todos los proveedores</option>
          {proveedores.map((p) => (
            <option key={p} value={p}>
              {p}
            </option>
          ))}
        </select>
        <select
          aria-label="Estado del lote"
          value={estado}
          onChange={(e) => {
            setEstado(e.target.value as Estado);
            reiniciar();
          }}
          className={selectClass}
        >
          <option value="bodega">Con peces en bodega</option>
          <option value="agotados">Agotados</option>
          <option value="todos">Todos</option>
        </select>
      </div>
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        <label className="flex min-h-11 items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={antiguos} onChange={(e) => setAntiguos(e.target.checked)} className={casillaClass} />
          Más de {DIAS_LOTE_ANTIGUO} días en bodega
        </label>
        <label className="flex min-h-11 items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={cuarentena} onChange={(e) => setCuarentena(e.target.checked)} className={casillaClass} />
          En cuarentena ahora
        </label>
      </div>

      <section aria-label="Resumen de los lotes filtrados" className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <Resumen titulo="Lotes" valor={piezas(visibles.length)} detalle={`${piezas(recibidos)} peces recibidos`} />
        <Resumen titulo="Mortalidad" valor={recibidos ? pct((muertos * 100) / recibidos) : "—"} detalle={`${piezas(muertos)} muertos`} />
        <Resumen titulo="Aclimatación promedio" valor={conAclimatacion.length ? dias(aclimatacionPromedio) : "—"} detalle="Desde que llega hasta que se ubica" />
        <Resumen titulo="En cuarentena" valor={piezas(enCuarentena)} detalle="Lotes con peces enfermos hoy" alerta={enCuarentena > 0} />
      </section>

      <Leyenda />
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-3 font-medium">Lote</th>
            <th className="hidden px-3 py-3 font-medium lg:table-cell">Proveedor</th>
            <th className="px-3 py-3 font-medium">Dónde está cada pez</th>
            <th className="hidden px-3 py-3 font-medium md:table-cell">Aclimatación</th>
            <th className="hidden px-3 py-3 font-medium md:table-cell">Cuarentena</th>
            <th className="hidden px-3 py-3 text-right font-medium sm:table-cell">Clientes</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.slice(0, mostrar).map(({ lote: l, tiempos: ti }) => (
            <tr key={l.id} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-3">
                <Link href={`/inventario/lotes/${l.id}`} className="font-medium whitespace-nowrap text-acento hover:underline">
                  {l.codigo}
                </Link>
                <p className="text-sm text-texto">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                  <span className="italic">{l.nombreCientifico}</span>
                </p>
                <p className="text-xs text-texto-suave">
                  {fecha(l.fechaIngreso)}
                  {l.disponible > 0 ? (
                    <span className={l.diasEnBodega > DIAS_LOTE_ANTIGUO ? " font-medium text-advertencia-texto" : ""}> · {l.diasEnBodega} días en bodega</span>
                  ) : null}
                  <span className="lg:hidden"> · {l.proveedorNombre ?? "Sin proveedor"}</span>
                </p>
              </td>
              <td className="hidden px-3 py-3 text-texto lg:table-cell">
                {l.proveedorNombre ?? "Sin proveedor"}
                <p className="text-xs text-texto-suave">{l.centroNombre ?? "—"}</p>
              </td>
              <td className="min-w-40 px-3 py-3">
                <BarraSegmentada partes={partesLote(l)} etiqueta={`Lote ${l.codigo}`} />
                <p className="mt-1.5 text-xs text-texto-suave">
                  <span className="font-medium text-texto">{piezas(l.disponible)}</span> en bodega de {piezas(l.recibidos)}
                  {l.despachados ? ` · ${piezas(l.despachados)} enviados` : ""}
                  {l.muertos ? ` · ${pct(l.porcentajeMuertos)} muertos` : ""}
                </p>
              </td>
              <td className="hidden px-3 py-3 whitespace-nowrap md:table-cell">
                {ti.diasAclimatacion > 0 ? <span className="text-texto">{dias(ti.diasAclimatacion)}</span> : <span className="text-texto-tenue">—</span>}
                {ti.aclimatacionEnCurso ? <p className="text-xs text-texto-suave">en curso</p> : null}
              </td>
              <td className="hidden px-3 py-3 whitespace-nowrap md:table-cell">
                {ti.diasCuarentena > 0 ? <span className="text-texto">{dias(ti.diasCuarentena)}</span> : <span className="text-texto-tenue">—</span>}
                {ti.cuarentenaEnCurso ? (
                  <p>
                    <Etiqueta texto="Enfermos hoy" tono="peligro" />
                  </p>
                ) : ti.episodiosCuarentena > 1 ? (
                  <p className="text-xs text-texto-suave">{ti.episodiosCuarentena} episodios</p>
                ) : null}
              </td>
              <td className="hidden px-3 py-3 text-right tabular-nums text-texto sm:table-cell">{ti.clientes || <span className="text-texto-tenue">—</span>}</td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-3 py-8 text-center text-texto-suave">
                {lotes.length === 0 ? "Aún no hay lotes." : "Ningún lote con ese filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      {mostrar < visibles.length ? (
        <button type="button" onClick={() => setMostrar((m) => m + POR_PAGINA)} className="w-fit text-sm font-medium text-acento hover:underline">
          Mostrar más ({visibles.length - mostrar} lotes)
        </button>
      ) : null}
      <p className="text-xs text-texto-suave">
        Aclimatación: tiempo con peces del lote sin acuario. Cuarentena: tiempo con peces enfermos. En los lotes del saldo inicial la aclimatación
        se cuenta desde la fecha de ingreso del Excel y la cuarentena desde la migración (el Excel no traía esa historia).
      </p>
    </div>
  );
}

function Resumen({ titulo, valor, detalle, alerta = false }: { titulo: string; valor: string; detalle: string; alerta?: boolean }) {
  return (
    <div className="flex min-w-0 flex-col gap-1 rounded-md border border-borde bg-superficie p-3">
      <p className="text-sm text-texto-suave">{titulo}</p>
      <p className={`text-xl font-semibold ${alerta ? "text-peligro" : "text-texto"}`}>{valor}</p>
      <p className="text-xs text-texto-suave">{detalle}</p>
    </div>
  );
}

/** Una leyenda para toda la tabla (la barra de cada fila no lleva texto). */
function Leyenda() {
  return (
    <ul aria-label="Colores de la barra" className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-texto-suave">
      {partesLote({ sanos: 0, porUbicar: 0, enfermos: 0, despachados: 0, muertos: 0 }).map((p) => (
        <li key={p.clave} className="flex items-center gap-1.5">
          <span className={`size-2.5 rounded-sm ${p.color}`} aria-hidden />
          {p.etiqueta}
        </li>
      ))}
    </ul>
  );
}
