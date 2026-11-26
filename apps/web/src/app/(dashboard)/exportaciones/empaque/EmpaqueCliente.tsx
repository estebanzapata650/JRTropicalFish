"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import { History, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { BotonIcono, Campo, CampoAncho, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { coincide } from "@/components/SelectorBuscable";
import { botonPeligroClass, botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { pesos, type CostosEmpaque, type DoaEmpaque, type EstandarEmpaque } from "@/lib/exportaciones";
import {
  eliminarEstandarEmpaqueAction,
  guardarCostoAditivoAction,
  guardarEstandarEmpaqueAction,
  guardarLitrosBolsaAction,
  guardarParametrosEmpaqueAction,
} from "../actions";

export type VistaEmpaque = "estandares" | "costos" | "doa";

const LIMITE = 150;
const numero = (t: string) => (t.trim() === "" ? null : Number(t.replace(",", ".")));
const rango = (a: number | null, b: number | null) => (a == null && b == null ? "—" : a === b || b == null ? `${a}` : a == null ? `hasta ${b}` : `${a}–${b}`);

/**
 * Empaque: el estándar de cada especie y talla, lo que cuesta empacar y el DOA por forma de empaque. Los costos no se
 * inventan: los que faltan se ven vacíos y el costo de un despacho los lista como pendientes.
 */
export function EmpaqueCliente({
  vista,
  estandares,
  costos,
  doa,
  desde,
  hasta,
  configurar,
}: {
  vista: VistaEmpaque;
  estandares: EstandarEmpaque[];
  costos: CostosEmpaque;
  doa: DoaEmpaque[];
  desde: string | null;
  hasta: string | null;
  configurar: boolean;
}) {
  const conEstandar = estandares.filter((e) => e.tieneEstandar).length;
  const revisados = estandares.filter((e) => e.revisado).length;
  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Empaque"
        descripcion={`Estándar por especie y talla: ${conEstandar} definidos (${revisados} revisados) de ${estandares.length}. Sin estándar se usan los peces por bolsa del catálogo.`}
      />
      <nav aria-label="Vista" className="-mx-4 flex gap-1 overflow-x-auto overflow-y-hidden border-b border-borde px-4 md:mx-0 md:px-0">
        {(
          [
            ["estandares", "Estándares"],
            ["costos", "Costos"],
            ["doa", "DOA por empaque"],
          ] as const
        ).map(([v, t]) => (
          <Link
            key={v}
            href={`/exportaciones/empaque?vista=${v}`}
            aria-current={vista === v ? "page" : undefined}
            className={`-mb-px inline-flex min-h-11 items-center border-b-2 px-3 text-sm font-medium whitespace-nowrap ${vista === v ? "border-acento text-texto" : "border-transparent text-texto-suave hover:text-texto"}`}
          >
            {t}
          </Link>
        ))}
      </nav>
      {vista === "estandares" ? <Estandares estandares={estandares} costos={costos} configurar={configurar} /> : null}
      {vista === "costos" ? <Costos costos={costos} configurar={configurar} /> : null}
      {vista === "doa" ? <Doa doa={doa} desde={desde} hasta={hasta} /> : null}
    </div>
  );
}

// --- Estándares ---

function Estandares({ estandares, costos, configurar }: { estandares: EstandarEmpaque[]; costos: CostosEmpaque; configurar: boolean }) {
  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<"todos" | "sin" | "por_revisar" | "revisados">("todos");
  const [editando, setEditando] = useState<EstandarEmpaque | null>(null);
  const [historial, setHistorial] = useState<EstandarEmpaque | null>(null);
  const filtrados = useMemo(
    () =>
      estandares.filter(
        (e) =>
          (filtro === "todos" ||
            (filtro === "sin" && !e.tieneEstandar) ||
            (filtro === "por_revisar" && e.tieneEstandar && !e.revisado) ||
            (filtro === "revisados" && e.revisado)) &&
          coincide({ id: e.productoId, nombre: `${e.productoCodigo} ${e.nombreEspanol ?? ""} ${e.nombreCientifico}`, detalle: e.grupoNombre }, busqueda),
      ),
    [estandares, filtro, busqueda],
  );
  const aditivo = (id: string) => costos.aditivos.find((a) => a.medicamentoId === id)?.nombre ?? "?";

  return (
    <section className="flex flex-col gap-4">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto]">
        <label className="relative">
          <span className="sr-only">Buscar especie</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Código, nombre o grupo…" className={`${inputClass} pl-9`} />
        </label>
        <select aria-label="Filtrar" value={filtro} onChange={(e) => setFiltro(e.target.value as typeof filtro)} className={`${selectClass} sm:w-52`}>
          <option value="todos">Todas las especies</option>
          <option value="sin">Sin estándar</option>
          <option value="por_revisar">Por revisar con el equipo</option>
          <option value="revisados">Revisados</option>
        </select>
      </div>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="px-3 py-2 font-medium">Empaque</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Aditivos</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Estado</th>
            <th className="px-3 py-2">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.slice(0, LIMITE).map((e) => (
            <tr key={e.productoId} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">{e.productoCodigo}</span>
                <span className="text-texto">{e.nombreEspanol ?? e.nombreCientifico}</span>
                {e.talla ? <span className="ml-1 text-xs whitespace-nowrap text-texto-suave">{e.talla}</span> : null}
                <span className="block text-xs text-texto-suave italic">{e.nombreCientifico}</span>
              </td>
              <td className="px-3 py-2 text-texto">
                {e.tieneEstandar ? (
                  <>
                    <span className="block">{e.tipoBolsaNombre ?? "Bolsa por definir"}</span>
                    <span className="block text-xs text-texto-suave">
                      {rango(e.pecesMin, e.pecesMax)} peces/bolsa{e.litrosAgua != null ? ` · ${e.litrosAgua} L` : ""}
                    </span>
                  </>
                ) : (
                  <span className="text-xs text-texto-suave">Catálogo: {rango(e.bolsaMin, e.bolsaMax)} peces/bolsa</span>
                )}
                <span className="mt-1 block md:hidden">
                  <EstadoEstandar e={e} />
                </span>
              </td>
              <td className="hidden px-3 py-2 text-xs text-texto-suave lg:table-cell">{e.aditivos.length ? e.aditivos.map(aditivo).join(", ") : "—"}</td>
              <td className="hidden px-3 py-2 md:table-cell">
                <EstadoEstandar e={e} />
              </td>
              <td className="px-2 py-2">
                <div className="flex justify-end">
                  <BotonIcono accion="editar" etiqueta={`${configurar ? "Editar" : "Ver"} el empaque de ${e.productoCodigo}`} onClick={() => setEditando(e)} />
                  {e.tieneEstandar ? <BotonIcono accion="otra" icono={History} etiqueta={`Historial del empaque de ${e.productoCodigo}`} onClick={() => setHistorial(e)} /> : null}
                </div>
              </td>
            </tr>
          ))}
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-3 py-8 text-center text-texto-suave">
                Ninguna especie coincide.
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      {filtrados.length > LIMITE ? <p className="text-sm text-texto-suave">Se muestran {LIMITE} de {filtrados.length}: busca o filtra para ver el resto.</p> : null}

      <Dialogo
        titulo={editando ? `Empaque · ${editando.productoCodigo} ${editando.nombreEspanol ?? editando.nombreCientifico}${editando.talla ? ` (${editando.talla})` : ""}` : ""}
        abierto={editando !== null}
        onCerrar={() => setEditando(null)}
      >
        {editando ? <FormEstandar key={editando.productoId} e={editando} costos={costos} configurar={configurar} onListo={() => setEditando(null)} /> : null}
      </Dialogo>
      <Dialogo titulo={historial ? `Historial · ${historial.productoCodigo}` : ""} abierto={historial !== null} onCerrar={() => setHistorial(null)}>
        {historial ? <HistorialRemoto ruta={`/api/exportaciones/empaque/estandares/${historial.productoId}/historial`} etiquetas={{}} /> : null}
      </Dialogo>
    </section>
  );
}

function EstadoEstandar({ e }: { e: EstandarEmpaque }) {
  if (!e.tieneEstandar) return <Etiqueta texto="Sin estándar" tono="neutro" />;
  return e.revisado ? <Etiqueta texto="Revisado" tono="exito" /> : <Etiqueta texto="Por revisar" tono="advertencia" />;
}

function FormEstandar({ e, costos, configurar, onListo }: { e: EstandarEmpaque; costos: CostosEmpaque; configurar: boolean; onListo: () => void }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [f, setF] = useState({
    tipoBolsaId: e.tipoBolsaId ?? "",
    pecesMin: String(e.pecesMin ?? (e.tieneEstandar ? "" : (e.bolsaMin ?? ""))),
    pecesMax: String(e.pecesMax ?? (e.tieneEstandar ? "" : (e.bolsaMax ?? ""))),
    litrosAgua: e.litrosAgua != null ? String(e.litrosAgua) : "",
    notas: e.notas ?? "",
    revisado: e.revisado,
    aditivos: new Set(e.aditivos),
  });
  const bolsa = costos.bolsas.find((b) => b.tipoBolsaId === f.tipoBolsaId);
  const ejecutar = (accion: () => Promise<{ error?: string }>) =>
    iniciar(async () => {
      const r = await accion();
      if (r.error) setError(r.error);
      else {
        onListo();
        router.refresh();
      }
    });

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(ev) => {
        ev.preventDefault();
        ejecutar(() =>
          guardarEstandarEmpaqueAction(e.productoId, {
            tipoBolsaId: f.tipoBolsaId || null,
            pecesMin: numero(f.pecesMin),
            pecesMax: numero(f.pecesMax),
            litrosAgua: numero(f.litrosAgua),
            notas: f.notas || null,
            revisado: f.revisado,
            aditivos: [...f.aditivos],
          }),
        );
      }}
    >
      {!e.tieneEstandar ? (
        <p className="rounded-md bg-info-fondo px-3 py-2 text-sm text-info-texto">
          Aún no tiene estándar: hoy se usan los peces por bolsa del catálogo ({rango(e.bolsaMin, e.bolsaMax)}). Al guardar queda «por revisar» hasta que el equipo lo confirme.
        </p>
      ) : null}
      <RejillaCampos>
        <Campo label="Tipo de bolsa" htmlFor="eb-bolsa">
          <select id="eb-bolsa" value={f.tipoBolsaId} onChange={(ev) => setF({ ...f, tipoBolsaId: ev.target.value })} className={selectClass} disabled={!configurar}>
            <option value="">Por definir</option>
            {costos.bolsas
              .filter((b) => b.activo || b.tipoBolsaId === f.tipoBolsaId)
              .map((b) => (
                <option key={b.tipoBolsaId} value={b.tipoBolsaId}>
                  {b.nombre}
                  {b.tamano ? ` · ${b.tamano}` : ""}
                </option>
              ))}
          </select>
        </Campo>
        <Campo label="Peces por bolsa: mínimo" htmlFor="eb-min">
          <input id="eb-min" inputMode="numeric" value={f.pecesMin} onChange={(ev) => setF({ ...f, pecesMin: ev.target.value })} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Peces por bolsa: máximo" htmlFor="eb-max">
          <input id="eb-max" inputMode="numeric" value={f.pecesMax} onChange={(ev) => setF({ ...f, pecesMax: ev.target.value })} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo
          label="Litros de agua por bolsa"
          htmlFor="eb-litros"
          hint={bolsa?.litrosAgua != null ? `Vacío = ${bolsa.litrosAgua} L de la bolsa ${bolsa.nombre}.` : "Vacío = los litros del tipo de bolsa (en Costos)."}
        >
          <input id="eb-litros" inputMode="decimal" value={f.litrosAgua} onChange={(ev) => setF({ ...f, litrosAgua: ev.target.value })} className={inputClass} disabled={!configurar} />
        </Campo>
        <CampoAncho>
          <fieldset className="flex flex-col gap-2">
            <legend className="mb-1.5 text-sm font-medium text-texto">Aditivos del agua (dosis por litro, de la ficha del medicamento)</legend>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {costos.aditivos.map((a) => (
                <label key={a.medicamentoId} className="flex min-h-11 items-center gap-2 text-sm text-texto">
                  <input
                    type="checkbox"
                    className={casillaClass}
                    checked={f.aditivos.has(a.medicamentoId)}
                    disabled={!configurar}
                    onChange={(ev) => {
                      const s = new Set(f.aditivos);
                      if (ev.target.checked) s.add(a.medicamentoId);
                      else s.delete(a.medicamentoId);
                      setF({ ...f, aditivos: s });
                    }}
                  />
                  <span>
                    {a.nombre}{" "}
                    <span className="text-xs text-texto-suave">
                      {a.dosisCantidad.toLocaleString("es-CO")} {a.unidad}/{a.dosisBase.toLocaleString("es-CO")} L
                    </span>
                  </span>
                </label>
              ))}
            </div>
          </fieldset>
        </CampoAncho>
        <CampoAncho>
          <Campo label="Notas para bodega" htmlFor="eb-notas">
            <textarea id="eb-notas" value={f.notas} maxLength={500} onChange={(ev) => setF({ ...f, notas: ev.target.value })} className={textareaClass} disabled={!configurar} />
          </Campo>
        </CampoAncho>
        <CampoAncho>
          <label className="flex min-h-11 items-center gap-2 text-sm text-texto">
            <input type="checkbox" className={casillaClass} checked={f.revisado} disabled={!configurar} onChange={(ev) => setF({ ...f, revisado: ev.target.checked })} />
            Revisado con el equipo
          </label>
        </CampoAncho>
      </RejillaCampos>
      {error ? <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">{error}</p> : null}
      {configurar ? (
        <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-between">
          {e.tieneEstandar ? (
            <button type="button" disabled={pendiente} onClick={() => ejecutar(() => eliminarEstandarEmpaqueAction(e.productoId))} className={botonPeligroClass}>
              Quitar estándar
            </button>
          ) : (
            <span />
          )}
          <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
            {pendiente ? "Guardando…" : "Guardar"}
          </button>
        </div>
      ) : null}
    </form>
  );
}

// --- Costos ---

function Costos({ costos, configurar }: { costos: CostosEmpaque; configurar: boolean }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{ texto: string; error: boolean } | null>(null);
  const [heat, setHeat] = useState(costos.parametros.costoHeatPackCop != null ? String(costos.parametros.costoHeatPackCop) : "");
  const [oxigeno, setOxigeno] = useState(costos.parametros.costoOxigenoBolsaCop != null ? String(costos.parametros.costoOxigenoBolsaCop) : "");
  const [litros, setLitros] = useState<Record<string, string>>(
    Object.fromEntries(costos.bolsas.map((b) => [b.tipoBolsaId, b.litrosAgua != null ? String(b.litrosAgua) : ""])),
  );
  const [aditivos, setAditivos] = useState<Record<string, string>>(
    Object.fromEntries(costos.aditivos.map((a) => [a.medicamentoId, a.costoCopUnidad != null ? String(a.costoCopUnidad) : ""])),
  );
  const correr = (accion: () => Promise<{ error?: string }>, ok: string) =>
    iniciar(async () => {
      const r = await accion();
      setMensaje(r.error ? { texto: r.error, error: true } : { texto: ok, error: false });
      if (!r.error) router.refresh();
    });
  const faltan =
    (costos.parametros.costoOxigenoBolsaCop == null ? 1 : 0) +
    (costos.parametros.costoHeatPackCop == null ? 1 : 0) +
    costos.bolsas.filter((b) => b.activo && b.litrosAgua == null).length +
    costos.aditivos.filter((a) => a.costoCopUnidad == null).length;

  return (
    <div className="flex flex-col gap-6">
      <p className="rounded-md bg-info-fondo px-3 py-2 text-sm text-info-texto">
        {faltan > 0 ? `Faltan ${faltan} datos por llenar con el equipo. ` : "Todos los costos están cargados. "}
        El costo de las cajas y de cada bolsa se edita en{" "}
        <Link href="/exportaciones/configuracion" className="font-medium underline">
          Exportaciones → Configuración
        </Link>
        .
      </p>
      {mensaje ? (
        <p role="status" className={`rounded-md px-3 py-2 text-sm ${mensaje.error ? "bg-peligro/15 text-peligro" : "bg-exito-fondo text-exito-texto"}`}>
          {mensaje.texto}
        </p>
      ) : null}

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Heat pack y oxígeno</h2>
        <RejillaCampos>
          <Campo label="Heat pack (COP por unidad)" htmlFor="c-heat" hint="Uno por piso de caja.">
            <input id="c-heat" inputMode="decimal" value={heat} onChange={(e) => setHeat(e.target.value)} className={inputClass} disabled={!configurar} />
          </Campo>
          <Campo label="Oxígeno (COP por bolsa)" htmlFor="c-oxigeno">
            <input id="c-oxigeno" inputMode="decimal" value={oxigeno} onChange={(e) => setOxigeno(e.target.value)} placeholder="Por definir" className={inputClass} disabled={!configurar} />
          </Campo>
        </RejillaCampos>
        {configurar ? (
          <button
            type="button"
            disabled={pendiente}
            className={`${botonPrimarioClass} w-full sm:w-fit`}
            onClick={() => correr(() => guardarParametrosEmpaqueAction({ costoHeatPackCop: numero(heat), costoOxigenoBolsaCop: numero(oxigeno) }), "Costos guardados.")}
          >
            Guardar
          </button>
        ) : null}
      </section>

      <section className="flex flex-col gap-3">
        <div>
          <h2 className="text-base font-semibold text-texto">Bolsas: litros de agua</h2>
          <p className="text-sm text-texto-suave">Con los litros se dosifican los aditivos. Una especie puede tener los suyos en su estándar.</p>
        </div>
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Bolsa</th>
              <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Costo</th>
              <th className="px-3 py-2 font-medium">Litros de agua</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {costos.bolsas
              .filter((b) => b.activo)
              .map((b) => (
                <tr key={b.tipoBolsaId} className="border-b border-borde last:border-0">
                  <td className="px-3 py-2 text-texto">
                    {b.nombre}
                    <span className="block text-xs text-texto-suave">
                      {b.tamano ?? ""}
                      <span className="sm:hidden"> · {pesos(b.costoCop)}</span>
                    </span>
                  </td>
                  <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">{pesos(b.costoCop)}</td>
                  <td className="px-3 py-2">
                    <div className="flex items-center gap-2">
                      <input
                        aria-label={`Litros de agua de la bolsa ${b.nombre}`}
                        inputMode="decimal"
                        value={litros[b.tipoBolsaId] ?? ""}
                        placeholder="Por definir"
                        onChange={(e) => setLitros({ ...litros, [b.tipoBolsaId]: e.target.value })}
                        className={`${inputClass} w-28`}
                        disabled={!configurar}
                      />
                      {configurar ? (
                        <button
                          type="button"
                          disabled={pendiente}
                          className={botonSecundarioClass}
                          onClick={() => correr(() => guardarLitrosBolsaAction(b.tipoBolsaId, numero(litros[b.tipoBolsaId] ?? "")), `Litros de la bolsa ${b.nombre} guardados.`)}
                        >
                          Guardar
                        </button>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </TablaResponsive>
      </section>

      <section className="flex flex-col gap-3">
        <div>
          <h2 className="text-base font-semibold text-texto">Aditivos: costo por unidad</h2>
          <p className="text-sm text-texto-suave">Los medicamentos de los reportes AQUAVET que se dosifican por agua. La dosis sale de la ficha del medicamento (Sanidad).</p>
        </div>
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Aditivo</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">Dosis</th>
              <th className="px-3 py-2 font-medium">COP por unidad</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {costos.aditivos.map((a) => (
              <tr key={a.medicamentoId} className="border-b border-borde last:border-0">
                <td className="px-3 py-2 text-texto">
                  {a.nombre}
                  <span className="block text-xs text-texto-suave md:hidden">
                    {a.dosisCantidad.toLocaleString("es-CO")} {a.unidad}/{a.dosisBase.toLocaleString("es-CO")} L
                  </span>
                </td>
                <td className="hidden px-3 py-2 text-texto md:table-cell">
                  {a.dosisCantidad.toLocaleString("es-CO")} {a.unidad} por {a.dosisBase.toLocaleString("es-CO")} L
                  <span className="block text-xs text-texto-suave">{a.fuente}</span>
                </td>
                <td className="px-3 py-2">
                  <div className="flex items-center gap-2">
                    <input
                      aria-label={`Costo de ${a.nombre} por ${a.unidad}`}
                      inputMode="decimal"
                      value={aditivos[a.medicamentoId] ?? ""}
                      placeholder={`Por ${a.unidad}`}
                      onChange={(e) => setAditivos({ ...aditivos, [a.medicamentoId]: e.target.value })}
                      className={`${inputClass} w-28`}
                      disabled={!configurar}
                    />
                    {configurar ? (
                      <button
                        type="button"
                        disabled={pendiente}
                        className={botonSecundarioClass}
                        onClick={() => correr(() => guardarCostoAditivoAction(a.medicamentoId, numero(aditivos[a.medicamentoId] ?? "")), `Costo de ${a.nombre} guardado.`)}
                      >
                        Guardar
                      </button>
                    ) : null}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      </section>
    </div>
  );
}

// --- DOA por empaque ---

function Doa({ doa, desde, hasta }: { doa: DoaEmpaque[]; desde: string | null; hasta: string | null }) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const filtrados = doa.filter((d) => coincide({ id: d.productoId, nombre: `${d.productoCodigo} ${d.nombreEspanol ?? ""} ${d.nombreCientifico}` }, busqueda));
  // La forma con menos DOA de cada especie (si se empacó de más de una forma).
  const mejor = new Map<string, number>();
  for (const d of doa) {
    const formas = doa.filter((x) => x.productoId === d.productoId);
    if (formas.length > 1) mejor.set(d.productoId, Math.min(...formas.map((x) => x.porcentajeDoa)));
  }
  const ir = (campo: "desde" | "hasta", valor: string) => {
    const p = new URLSearchParams({ vista: "doa", ...(desde ? { desde } : {}), ...(hasta ? { hasta } : {}) });
    if (valor) p.set(campo, valor);
    else p.delete(campo);
    router.push(`/exportaciones/empaque?${p}`);
  };

  return (
    <section className="flex flex-col gap-4">
      <p className="text-sm text-texto-suave">
        DOA (DOA + ADD + FINAL) sobre los peces enviados, por especie, tipo de bolsa y peces por bolsa. Solo embarques facturados o cerrados. Por defecto, los
        últimos 12 meses.
      </p>
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-[10rem_10rem_1fr]">
        <Campo label="Desde" htmlFor="doa-desde">
          <input id="doa-desde" type="date" value={desde ?? ""} onChange={(e) => ir("desde", e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Hasta" htmlFor="doa-hasta">
          <input id="doa-hasta" type="date" value={hasta ?? ""} onChange={(e) => ir("hasta", e.target.value)} className={inputClass} />
        </Campo>
        <div className="col-span-2 flex items-end sm:col-span-1">
          <label className="relative w-full">
            <span className="sr-only">Buscar especie</span>
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
            <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Código o nombre…" className={`${inputClass} pl-9`} />
          </label>
        </div>
      </div>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="px-3 py-2 font-medium">Empaque</th>
            <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Embarques</th>
            <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Peces</th>
            <th className="px-3 py-2 text-right font-medium">DOA</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.map((d) => (
            <tr key={`${d.productoId}-${d.tipoBolsaId}-${d.pecesPorBolsa}`} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-2">
                <span className="mr-1 font-mono text-xs text-texto-suave">{d.productoCodigo}</span>
                <span className="text-texto">{d.nombreEspanol ?? d.nombreCientifico}</span>
                {d.talla ? <span className="ml-1 text-xs text-texto-suave">{d.talla}</span> : null}
              </td>
              <td className="px-3 py-2 text-texto">
                {d.tipoBolsaNombre ?? "Sin tipo"} · {d.pecesPorBolsa}/bolsa
                <span className="block text-xs text-texto-suave sm:hidden">
                  {d.peces.toLocaleString("es-CO")} peces · {d.embarques} emb.
                </span>
              </td>
              <td className="hidden px-3 py-2 text-right tabular-nums text-texto md:table-cell">{d.embarques}</td>
              <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">{d.peces.toLocaleString("es-CO")}</td>
              <td className="px-3 py-2 text-right whitespace-nowrap">
                <span className={`tabular-nums ${d.porcentajeDoa >= 5 ? "font-medium text-peligro" : "text-texto"}`}>
                  {d.porcentajeDoa.toLocaleString("es-CO", { maximumFractionDigits: 2 })} %
                </span>
                <span className="block text-xs text-texto-suave">{d.doa.toLocaleString("es-CO")} peces</span>
                {mejor.get(d.productoId) === d.porcentajeDoa ? <Etiqueta texto="Menor DOA" tono="exito" /> : null}
              </td>
            </tr>
          ))}
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-3 py-8 text-center text-texto-suave">
                {doa.length === 0 ? "Aún no hay embarques facturados con su empaque y DOA en este periodo." : "Ninguna especie coincide."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
    </section>
  );
}
