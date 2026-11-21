"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Search } from "lucide-react";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { piezas, type InventarioProducto } from "@/lib/inventario";
import { fecha } from "@/lib/formato";
import { inputClass, selectClass } from "@/lib/estilos";

const POR_PAGINA = 15;

/**
 * «Disponibilidad por referencia» de Looker, mejorada: por especie, lo que hay en acuarios, en
 * aclimatación y enfermo; lo pedido por clientes sin despachar, lo que viene en camino y la última
 * oferta; y si falta comprar. Filtro por especie y centro de acopio.
 */
export function DisponibilidadPorEspecie({ productos }: { productos: InventarioProducto[] }) {
  const [busqueda, setBusqueda] = useState("");
  const [centro, setCentro] = useState("");
  const [mostrar, setMostrar] = useState(POR_PAGINA);
  const centros = useMemo(() => [...new Set(productos.map((p) => p.centroNombre).filter((c): c is string => !!c))].sort(), [productos]);

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return productos.filter(
      (p) => (!centro || p.centroNombre === centro) && (!t || [p.productoCodigo, p.nombreCientifico, p.nombreEspanol].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [productos, busqueda, centro]);

  return (
    <section aria-labelledby="titulo-especies" className="flex flex-col gap-3">
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <h2 id="titulo-especies" className="text-base font-semibold text-texto">
          Disponibilidad por especie
        </h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto] md:w-[36rem]">
          <label className="relative">
            <span className="sr-only">Buscar especie</span>
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
            <input
              value={busqueda}
              onChange={(e) => {
                setBusqueda(e.target.value);
                setMostrar(POR_PAGINA);
              }}
              placeholder="Especie o código…"
              className={`${inputClass} pl-9`}
            />
          </label>
          <select
            aria-label="Centro de acopio"
            value={centro}
            onChange={(e) => {
              setCentro(e.target.value);
              setMostrar(POR_PAGINA);
            }}
            className={`${selectClass} sm:w-52`}
          >
            <option value="">Todos los centros</option>
            {centros.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
      </div>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Especie</th>
            <th className="px-4 py-3 text-right font-medium">En acuarios</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Aclimatación</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Enfermos</th>
            <th className="hidden px-4 py-3 text-right font-medium lg:table-cell">Total</th>
            <th className="hidden px-4 py-3 text-right font-medium md:table-cell">Pedido clientes</th>
            <th className="hidden px-4 py-3 text-right font-medium md:table-cell">En tránsito</th>
            <th className="hidden px-4 py-3 font-medium xl:table-cell">Última oferta</th>
            <th className="px-4 py-3 font-medium">Estado</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.slice(0, mostrar).map((p) => (
            <tr key={p.productoId} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <Link href={`/inventario/existencias?q=${encodeURIComponent(p.productoCodigo)}`} className="text-texto hover:text-acento hover:underline">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{p.productoCodigo}</span>
                  <span className="italic">{p.nombreCientifico}</span>
                </Link>
                <p className="text-xs text-texto-suave">{p.centroNombre ?? "—"}</p>
              </td>
              <td className="px-4 py-3 text-right tabular-nums text-texto">
                {piezas(p.sanos)}
                {p.porUbicar > 0 ? <p className="text-xs text-texto-suave sm:hidden">+{piezas(p.porUbicar)} aclim.</p> : null}
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums sm:table-cell">{p.porUbicar ? <span className="text-texto">{piezas(p.porUbicar)}</span> : <span className="text-texto-tenue">—</span>}</td>
              <td className={`hidden px-4 py-3 text-right tabular-nums sm:table-cell ${p.enfermos ? "font-medium text-peligro" : "text-texto-tenue"}`}>{p.enfermos ? piezas(p.enfermos) : "—"}</td>
              <td className="hidden px-4 py-3 text-right font-medium tabular-nums text-texto lg:table-cell">{piezas(p.sanos + p.porUbicar + p.enfermos)}</td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto md:table-cell">{p.pendienteClientes ? piezas(p.pendienteClientes) : "—"}</td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto md:table-cell">{p.enTransito ? piezas(p.enTransito) : "—"}</td>
              <td className="hidden px-4 py-3 text-texto xl:table-cell">
                {p.ultimaOfertaProveedor ? (
                  <>
                    {piezas(p.ultimaOfertaCantidad ?? 0)} · {p.ultimaOfertaProveedor}
                    <p className="text-xs text-texto-suave">{fecha(p.ultimaOfertaFecha)}</p>
                  </>
                ) : (
                  <span className="text-texto-tenue">—</span>
                )}
              </td>
              <td className="px-4 py-3">
                {p.faltante > 0 ? (
                  <Etiqueta texto={`Faltan ${piezas(p.faltante)}`} tono="peligro" />
                ) : p.cubierto ? (
                  <Etiqueta texto="Cubierto" tono="exito" />
                ) : (
                  <Etiqueta texto="Disponible" tono="neutro" />
                )}
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={9} className="px-4 py-8 text-center text-texto-suave">
                Sin especies con stock, pedidos u ofertas.
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      {mostrar < visibles.length ? (
        <button type="button" onClick={() => setMostrar((m) => m + POR_PAGINA)} className="w-fit text-sm font-medium text-acento hover:underline">
          Mostrar más ({visibles.length - mostrar} restantes)
        </button>
      ) : null}
    </section>
  );
}
