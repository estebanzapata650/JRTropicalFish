"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { ArrowDown, ArrowUp, Plus, Trash2 } from "lucide-react";
import { Campo, CampoAncho, RejillaCampos } from "@/components/campos";
import { FORMATOS_FACTURA, cifra, usd, type EmbarqueDetalle, type TipoCaja } from "@/lib/exportaciones";
import type { ProductoOpcion } from "@/lib/inventario";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { SelectorProducto } from "../../inventario/comun";
import { guardarEmbarqueAction, guardarLineasEmbarqueAction } from "../actions";
import { Aviso, BotonesDocumento, CampoFila, ResumenTotales, aIso, aLocal, cuerpoBase, filasCargo, numero } from "./comun";

type Lista = { id: string; nombre: string };
type Linea = { clave: string; productoId: string; cantidad: string; precio: string; fraccion: string; empaque: string; nota: string };

/** Paso 1: datos del despacho, cargos y cajas estimadas, y las especies con su precio (preinvoice). */
export function PasoPreinvoice({
  d,
  productos,
  listas,
  cajas,
  editable,
  puedeGestionar,
}: {
  d: EmbarqueDetalle;
  productos: ProductoOpcion[];
  listas: Lista[];
  cajas: TipoCaja[];
  editable: boolean;
  puedeGestionar: boolean;
}) {
  const p = d.calculos.preinvoice;
  const [aviso, setAviso] = useState<string | null>(null);
  const puede = editable && puedeGestionar;

  return (
    <div className="flex flex-col gap-6">
      <Aviso texto={aviso} />
      {puede ? <DatosGenerales d={d} listas={listas} cajas={cajas} onGuardado={setAviso} /> : <DatosSoloLectura d={d} />}
      <Especies d={d} productos={productos} puede={puede} onGuardado={setAviso} />

      <section aria-labelledby="titulo-resumen-pre" className="flex flex-col gap-3">
        <h2 id="titulo-resumen-pre" className="text-base font-semibold text-texto">
          Preinvoice {d.embarque.preinvoiceNumero ?? "(el número se asigna al generarla)"}
        </h2>
        <ResumenTotales
          filas={[
            { concepto: "Peces", detalle: `${p.unidades.toLocaleString("es-CO")} · ${cifra(p.cajasSugeridas)} cajas según fracciones`, total: p.subtotalPeces },
            ...(p.descuento > 0 ? [{ concepto: "Descuento peces", detalle: `${cifra(d.embarque.descuentoPeces)} %`, total: p.descuento, negativo: true }] : []),
            ...filasCargo(p.cargos),
            { concepto: "AWB estimado", detalle: "ver paso Flete", total: p.awbEstimado },
            { concepto: "Total preinvoice", total: p.total, fuerte: true },
            { concepto: "Anticipo (50 % peces + 100 % AWB)", total: p.anticipo, fuerte: true },
          ]}
        />
        <BotonesDocumento embarqueId={d.embarque.id} tipo="preinvoice" correoCliente={d.clienteEmail} puedeEnviar={puedeGestionar} onEnviado={setAviso} />
      </section>
    </div>
  );
}

function DatosSoloLectura({ d }: { d: EmbarqueDetalle }) {
  const e = d.embarque;
  return (
    <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2 lg:grid-cols-3">
      {[
        ["Lista de precios", e.listaNombre ?? "—"],
        ["Descuento peces", `${cifra(e.descuentoPeces)} %`],
        ["Formato de factura", FORMATOS_FACTURA[e.formatoFactura]],
        ["Aerolínea", e.aerolinea ?? "—"],
        ["Ruta", e.ruta ?? "—"],
        ["AWB", e.awb ?? "—"],
      ].map(([k, v]) => (
        <div key={k}>
          <dt className="text-texto-suave">{k}</dt>
          <dd className="text-texto">{v}</dd>
        </div>
      ))}
    </dl>
  );
}

function DatosGenerales({ d, listas, cajas, onGuardado }: { d: EmbarqueDetalle; listas: Lista[]; cajas: TipoCaja[]; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const e = d.embarque;
  const [listaId, setListaId] = useState(e.listaPreciosId ?? "");
  const [descuento, setDescuento] = useState(String(e.descuentoPeces));
  const [formato, setFormato] = useState(e.formatoFactura);
  const [aerolinea, setAerolinea] = useState(e.aerolinea ?? "");
  const [ruta, setRuta] = useState(e.ruta ?? "");
  const [awb, setAwb] = useState(e.awb ?? "");
  const [destino, setDestino] = useState(e.destino ?? "");
  const [fechaDespacho, setFechaDespacho] = useState(e.fechaDespacho ?? "");
  const [salida, setSalida] = useState(aLocal(e.salidaVuelo));
  const [llegada, setLlegada] = useState(aLocal(e.llegadaVuelo));
  const [notas, setNotas] = useState(e.notas ?? "");
  const [cargos, setCargos] = useState(() => Object.fromEntries(d.cargos.map((c) => [c.codigo, { cantidad: String(c.cantidad), precio: String(c.precio) }])));
  const [estimadas, setEstimadas] = useState(() => Object.fromEntries(cajas.map((c) => [c.id, String(d.cajasEstimadas.find((x) => x.tipoCajaId === c.id)?.cantidad ?? "")])));
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const sugeridas = Math.ceil(d.calculos.preinvoice.cajasSugeridas);

  return (
    <form
      onSubmit={(ev) => {
        ev.preventDefault();
        iniciar(async () => {
          const r = await guardarEmbarqueAction(e.id, {
            ...cuerpoBase(d),
            listaPreciosId: listaId || null,
            descuentoPeces: numero(descuento) ?? 0,
            formatoFactura: formato,
            aerolinea,
            ruta,
            awb,
            destino,
            fechaDespacho: fechaDespacho || null,
            salidaVuelo: aIso(salida),
            llegadaVuelo: aIso(llegada),
            notas,
            cargos: d.cargos.map((c) => ({ ...c, cantidad: numero(cargos[c.codigo].cantidad) ?? 0, precio: numero(cargos[c.codigo].precio) ?? 0 })),
            cajasEstimadas: cajas.map((c) => ({ tipoCajaId: c.id, cantidad: numero(estimadas[c.id]) ?? 0 })).filter((c) => c.cantidad > 0),
          });
          if (r.error) {
            setError(r.error);
            return;
          }
          setError(null);
          onGuardado("Datos del despacho guardados.");
          router.refresh();
        });
      }}
      className="flex flex-col gap-6"
    >
      <section aria-labelledby="titulo-despacho" className="flex flex-col gap-4">
        <h2 id="titulo-despacho" className="text-base font-semibold text-texto">
          Despacho
        </h2>
        <RejillaCampos>
          <Campo label="Lista de precios" htmlFor="lista" hint="Para las especies sin precio escrito.">
            <select id="lista" value={listaId} onChange={(ev) => setListaId(ev.target.value)} className={selectClass}>
              <option value="">Sin lista</option>
              {listas.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Descuento sobre peces (%)" htmlFor="descuento">
            <input id="descuento" type="number" inputMode="decimal" min={0} max={100} step="0.01" value={descuento} onChange={(ev) => setDescuento(ev.target.value)} className={inputClass} />
          </Campo>
          <Campo label="Formato de factura" htmlFor="formato">
            <select id="formato" value={formato} onChange={(ev) => setFormato(ev.target.value)} className={selectClass}>
              {Object.entries(FORMATOS_FACTURA).map(([v, t]) => (
                <option key={v} value={v}>
                  {t}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Aerolínea" htmlFor="aerolinea">
            <input id="aerolinea" value={aerolinea} onChange={(ev) => setAerolinea(ev.target.value)} maxLength={150} className={inputClass} />
          </Campo>
          <Campo label="Ruta" htmlFor="ruta" hint="BOG - MIA - NRT">
            <input id="ruta" value={ruta} onChange={(ev) => setRuta(ev.target.value)} maxLength={150} className={inputClass} />
          </Campo>
          <Campo label="AWB (guía aérea)" htmlFor="awb">
            <input id="awb" value={awb} onChange={(ev) => setAwb(ev.target.value)} maxLength={50} className={inputClass} />
          </Campo>
          <Campo label="Destino" htmlFor="destino" hint="Tokyo, Japan. Way: direct">
            <input id="destino" value={destino} onChange={(ev) => setDestino(ev.target.value)} maxLength={200} className={inputClass} />
          </Campo>
          <Campo label="Fecha de despacho" htmlFor="fechaDespacho">
            <input id="fechaDespacho" type="date" value={fechaDespacho} onChange={(ev) => setFechaDespacho(ev.target.value)} className={inputClass} />
          </Campo>
          <Campo label="Salida del vuelo" htmlFor="salida">
            <input id="salida" type="datetime-local" value={salida} onChange={(ev) => setSalida(ev.target.value)} className={inputClass} />
          </Campo>
          <Campo label="Llegada del vuelo" htmlFor="llegada" hint="Hora de este equipo.">
            <input id="llegada" type="datetime-local" value={llegada} onChange={(ev) => setLlegada(ev.target.value)} className={inputClass} />
          </Campo>
          <CampoAncho>
            <Campo label="Notas (opcional)" htmlFor="notasEmb">
              <textarea id="notasEmb" value={notas} onChange={(ev) => setNotas(ev.target.value)} maxLength={2000} className={textareaClass} />
            </Campo>
          </CampoAncho>
        </RejillaCampos>
      </section>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <fieldset className="flex flex-col gap-3">
          <legend className="mb-1 text-base font-semibold text-texto">Cajas estimadas</legend>
          <p className="text-sm text-texto-suave">Según las fracciones de las especies: {sugeridas} {sugeridas === 1 ? "caja" : "cajas"}.</p>
          {cajas.filter((c) => c.activo).map((c) => (
            <label key={c.id} className="grid grid-cols-[1fr_6rem] items-center gap-3 text-sm text-texto">
              <span>
                {c.nombre}
                <span className="block text-xs text-texto-suave">
                  {usd(c.precioUsd)} · {cifra(c.kgPorCaja)} kg · {c.pisos} {c.pisos === 1 ? "piso" : "pisos"}
                </span>
              </span>
              <input
                aria-label={`Cajas estimadas: ${c.nombre}`}
                type="number"
                inputMode="numeric"
                min={0}
                value={estimadas[c.id] ?? ""}
                onChange={(ev) => setEstimadas((x) => ({ ...x, [c.id]: ev.target.value }))}
                className={inputClass}
              />
            </label>
          ))}
        </fieldset>
        <fieldset className="flex flex-col gap-3">
          <legend className="mb-1 text-base font-semibold text-texto">Certificados y cargos</legend>
          <p className="text-sm text-texto-suave">El heat pack se calcula solo (uno por piso de caja).</p>
          {d.cargos.map((c) => (
            <div key={c.codigo} className="grid grid-cols-[1fr_5rem_6rem] items-center gap-2 text-sm text-texto">
              <span>{c.nombre}</span>
              <input
                aria-label={`Cantidad: ${c.nombre}`}
                type="number"
                inputMode="numeric"
                min={0}
                disabled={c.codigo === "heat_pack"}
                value={c.codigo === "heat_pack" ? "" : cargos[c.codigo].cantidad}
                placeholder={c.codigo === "heat_pack" ? "auto" : "0"}
                onChange={(ev) => setCargos((x) => ({ ...x, [c.codigo]: { ...x[c.codigo], cantidad: ev.target.value } }))}
                className={inputClass}
              />
              <input
                aria-label={`Precio USD: ${c.nombre}`}
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                value={cargos[c.codigo].precio}
                onChange={(ev) => setCargos((x) => ({ ...x, [c.codigo]: { ...x[c.codigo], precio: ev.target.value } }))}
                className={inputClass}
              />
            </div>
          ))}
        </fieldset>
      </div>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex justify-end">
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar despacho"}
        </button>
      </div>
    </form>
  );
}

const nueva = (parcial: Partial<Linea> = {}): Linea => ({
  clave: crypto.randomUUID(),
  productoId: "",
  cantidad: "",
  precio: "",
  fraccion: "",
  empaque: "",
  nota: "",
  ...parcial,
});

/** Especies pedidas en orden (el orden es el de la orden de cargue). Precio vacío = el de la lista. */
function Especies({ d, productos, puede, onGuardado }: { d: EmbarqueDetalle; productos: ProductoOpcion[]; puede: boolean; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [lineas, setLineas] = useState<Linea[]>(() =>
    d.lineas.map((l) =>
      nueva({ productoId: l.productoId, cantidad: String(l.cantidad), precio: String(l.precio), fraccion: l.fraccionCaja ? String(l.fraccionCaja) : "", empaque: l.empaque ?? "", nota: l.nota ?? "" }),
    ),
  );
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const cambiar = (clave: string, cambio: Partial<Linea>) => setLineas((ls) => ls.map((l) => (l.clave === clave ? { ...l, ...cambio } : l)));
  const mover = (i: number, delta: number) =>
    setLineas((ls) => {
      const j = i + delta;
      if (j < 0 || j >= ls.length) return ls;
      const copia = [...ls];
      [copia[i], copia[j]] = [copia[j], copia[i]];
      return copia;
    });

  if (!puede) {
    return (
      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Especies ({d.lineas.length})</h2>
        <ul className="flex flex-col divide-y divide-borde rounded-md border border-borde text-sm">
          {d.lineas.map((l) => (
            <li key={l.id} className="flex items-baseline justify-between gap-3 px-3 py-2">
              <span className="min-w-0 text-texto">
                <span className="mr-1 font-mono text-xs text-texto-suave">{l.productoCodigo}</span>
                <span className="italic">{l.nombreCientifico}</span>
              </span>
              <span className="shrink-0 tabular-nums text-texto">
                {l.cantidad.toLocaleString("es-CO")} × {usd(l.precio)}
              </span>
            </li>
          ))}
        </ul>
      </section>
    );
  }

  return (
    <section aria-labelledby="titulo-especies" className="flex flex-col gap-3">
      <div>
        <h2 id="titulo-especies" className="text-base font-semibold text-texto">
          Especies ({lineas.length})
        </h2>
        <p className="text-sm text-texto-suave">
          Precio vacío = el de la lista. Fracción de caja (0,25 · 0,5 · 1) y empaque (&quot;2*140&quot;) arman la orden de cargue. Se sincronizan con el
          pedido de cliente.
        </p>
      </div>
      <ul className="flex flex-col gap-3">
        {lineas.map((l, i) => (
          <li key={l.clave} className="grid grid-cols-2 gap-2 rounded-md border border-borde p-3 lg:grid-cols-[2.5fr_6rem_6rem_5rem_6rem_1.5fr_auto] lg:items-center">
            <div className="col-span-2 lg:col-span-1">
              <SelectorProducto productos={productos} valor={l.productoId} onChange={(id) => cambiar(l.clave, { productoId: id })} etiqueta={`Especie ${i + 1}`} />
            </div>
            <CampoFila etiqueta="Cantidad"><input aria-label={`Cantidad ${i + 1}`} type="number" inputMode="numeric" min={1} placeholder="Cantidad" value={l.cantidad} onChange={(ev) => cambiar(l.clave, { cantidad: ev.target.value })} className={inputClass} /></CampoFila>
            <CampoFila etiqueta="Precio USD (vacío = lista)"><input aria-label={`Precio USD ${i + 1}`} type="number" inputMode="decimal" min={0} step="0.0001" placeholder="Precio lista" value={l.precio} onChange={(ev) => cambiar(l.clave, { precio: ev.target.value })} className={inputClass} /></CampoFila>
            <CampoFila etiqueta="Fracción de caja"><input aria-label={`Fracción de caja ${i + 1}`} type="number" inputMode="decimal" min={0} step="0.25" placeholder="Caja" value={l.fraccion} onChange={(ev) => cambiar(l.clave, { fraccion: ev.target.value })} className={inputClass} /></CampoFila>
            <CampoFila etiqueta="Empaque"><input aria-label={`Empaque ${i + 1}`} placeholder="2*140" maxLength={60} value={l.empaque} onChange={(ev) => cambiar(l.clave, { empaque: ev.target.value })} className={inputClass} /></CampoFila>
            <input aria-label={`Nota ${i + 1}`} placeholder="Nota" maxLength={300} value={l.nota} onChange={(ev) => cambiar(l.clave, { nota: ev.target.value })} className={`${inputClass} col-span-2 lg:col-span-1`} />
            <div className="col-span-2 flex justify-end gap-1 lg:col-span-1">
              <button type="button" onClick={() => mover(i, -1)} disabled={i === 0} aria-label={`Subir especie ${i + 1}`} className="inline-flex size-10 items-center justify-center rounded-md text-texto-suave hover:bg-acento-suave disabled:opacity-30">
                <ArrowUp className="size-4" aria-hidden />
              </button>
              <button type="button" onClick={() => mover(i, 1)} disabled={i === lineas.length - 1} aria-label={`Bajar especie ${i + 1}`} className="inline-flex size-10 items-center justify-center rounded-md text-texto-suave hover:bg-acento-suave disabled:opacity-30">
                <ArrowDown className="size-4" aria-hidden />
              </button>
              <button type="button" onClick={() => setLineas((ls) => ls.filter((x) => x.clave !== l.clave))} aria-label={`Quitar especie ${i + 1}`} className="inline-flex size-10 items-center justify-center rounded-md text-peligro hover:bg-acento-suave">
                <Trash2 className="size-4" aria-hidden />
              </button>
            </div>
          </li>
        ))}
      </ul>
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <button type="button" onClick={() => setLineas((ls) => [...ls, nueva()])} className={`${botonSecundarioClass} w-fit gap-2`}>
          <Plus className="size-4" aria-hidden /> Agregar especie
        </button>
        <button
          type="button"
          disabled={guardando}
          onClick={() =>
            iniciar(async () => {
              const r = await guardarLineasEmbarqueAction(
                d.embarque.id,
                lineas
                  .filter((l) => l.productoId)
                  .map((l) => ({
                    productoId: l.productoId,
                    cantidad: numero(l.cantidad) ?? 0,
                    precio: numero(l.precio),
                    fraccionCaja: numero(l.fraccion) ?? 0,
                    empaque: l.empaque,
                    nota: l.nota,
                  })),
              );
              if (r.error) {
                setError(r.error);
                return;
              }
              setError(null);
              const avisos = r.datos?.avisos ?? [];
              onGuardado(avisos.length ? `Especies guardadas. ${avisos.join(" ")}` : "Especies guardadas y llevadas al pedido de cliente.");
              router.refresh();
            })
          }
          className={botonPrimarioClass}
        >
          {guardando ? "Guardando…" : "Guardar especies"}
        </button>
      </div>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
    </section>
  );
}
