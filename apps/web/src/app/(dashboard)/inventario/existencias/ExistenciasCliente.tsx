"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowRightLeft, HeartPulse, MapPin, Search, Skull, Stethoscope, Wrench } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import { CONDICIONES, DIAS_LOTE_ANTIGUO, piezas, type Existencia } from "@/lib/inventario";
import { botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass, selectClass } from "@/lib/estilos";
import { operarStockAction } from "../actions";

type AcuarioOpcion = { id: string; codigo: string };
type Operacion = "ubicacion" | "traslado" | "enfermo" | "aliviado" | "mortalidad" | "ajuste";

const TITULOS: Record<Operacion, string> = {
  ubicacion: "Ubicar en acuario",
  traslado: "Trasladar a otro acuario",
  enfermo: "Registrar enfermos",
  aliviado: "Registrar aliviados",
  mortalidad: "Registrar mortalidad",
  ajuste: "Ajuste de inventario",
};

/**
 * Existencias: el stock vivo por lote, acuario y condición. Desde cada fila se registran las
 * novedades de bodega (todas son la misma operación origen → destino; la API valida y bloquea filas).
 */
export function ExistenciasCliente({
  existencias,
  acuarios,
  busquedaInicial,
  puedeOperar,
  puedeAjustar,
}: {
  existencias: Existencia[];
  acuarios: AcuarioOpcion[];
  busquedaInicial: string;
  puedeOperar: boolean;
  puedeAjustar: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState(busquedaInicial);
  const [condicion, setCondicion] = useState("");
  const [soloPorUbicar, setSoloPorUbicar] = useState(false);
  const [operando, setOperando] = useState<{ tipo: Operacion; e: Existencia } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return existencias.filter(
      (e) =>
        (!condicion || e.condicion === condicion) &&
        (!soloPorUbicar || e.acuarioId === null) &&
        (!t || [e.productoCodigo, e.nombreCientifico, e.nombreEspanol, e.loteCodigo, e.acuarioCodigo, e.proveedorNombre].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [existencias, busqueda, condicion, soloPorUbicar]);

  const total = (f: (e: Existencia) => boolean) => existencias.filter(f).reduce((s, e) => s + e.cantidad, 0);

  const registrar = (operacion: Record<string, unknown>, mensaje: string) =>
    iniciar(async () => {
      const r = await operarStockAction([operacion]);
      if (r.error) {
        setError(r.error);
        return;
      }
      setOperando(null);
      setError(null);
      setAviso(mensaje);
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Existencias" descripcion="Stock vivo por lote, acuario y condición. Lo enfermo no se vende." />

      <div className="grid grid-cols-3 gap-3">
        <Resumen titulo="Sanos ubicados" valor={total((e) => e.condicion === "sano" && e.acuarioId !== null)} />
        <Resumen titulo="En aclimatación" valor={total((e) => e.acuarioId === null)} alerta={total((e) => e.acuarioId === null) > 0} />
        <Resumen titulo="Enfermos" valor={total((e) => e.condicion === "enfermo")} peligro />
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto] lg:grid-cols-[1fr_auto_auto] lg:items-center">
        <label className="relative">
          <span className="sr-only">Buscar</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Especie, lote, acuario o proveedor…" className={`${inputClass} pl-9`} />
        </label>
        <select aria-label="Condición" value={condicion} onChange={(e) => setCondicion(e.target.value)} className={`${selectClass} sm:w-44`}>
          <option value="">Sanos y enfermos</option>
          <option value="sano">Solo sanos</option>
          <option value="enfermo">Solo enfermos</option>
        </select>
        <label className="flex items-center gap-2 text-sm text-texto">
          <input type="checkbox" checked={soloPorUbicar} onChange={(e) => setSoloPorUbicar(e.target.checked)} className={casillaClass} />
          Solo en aclimatación
        </label>
      </div>

      {aviso ? <p role="status" className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">{aviso}</p> : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Especie · lote</th>
            <th className="px-4 py-3 font-medium">Acuario</th>
            <th className="px-4 py-3 text-right font-medium">Cantidad</th>
            <th className="hidden px-4 py-3 font-medium lg:table-cell">Proveedor · centro</th>
            <th className="hidden px-4 py-3 text-right font-medium md:table-cell">Días</th>
            {puedeOperar ? <th className="px-4 py-3 text-right font-medium"><span className="sr-only sm:not-sr-only">Acciones</span></th> : null}
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.slice(0, 300).map((e) => (
            <tr key={e.id} className={`border-b border-borde last:border-0 ${e.condicion === "enfermo" ? "bg-peligro/5" : ""}`}>
              <td className="px-4 py-3">
                <p className="text-texto">
                  <span className="mr-1.5 font-mono text-xs text-texto-suave">{e.productoCodigo}</span>
                  <span className="italic">{e.nombreCientifico}</span>
                </p>
                <Link href={`/inventario/lotes/${e.loteId}`} className="text-xs text-acento hover:underline">
                  {e.loteCodigo}
                </Link>
              </td>
              <td className="px-4 py-3 whitespace-nowrap">
                {e.acuarioCodigo ? <span className="font-mono text-texto">{e.acuarioCodigo}</span> : <Etiqueta texto="Aclimatación" tono="advertencia" />}
                {e.condicion === "enfermo" ? <div className="mt-1"><Etiqueta texto="Enfermo" tono="peligro" /></div> : null}
              </td>
              <td className="px-4 py-3 text-right font-medium tabular-nums text-texto">{piezas(e.cantidad)}</td>
              <td className="hidden px-4 py-3 text-texto lg:table-cell">
                {e.proveedorNombre ?? "—"}
                <p className="text-xs text-texto-suave">{e.centroNombre}</p>
              </td>
              <td className={`hidden px-4 py-3 text-right tabular-nums md:table-cell ${e.diasEnBodega > DIAS_LOTE_ANTIGUO ? "font-medium text-advertencia-texto" : "text-texto"}`}>
                {e.diasEnBodega}
              </td>
              {puedeOperar ? (
                <td className="px-2 py-2">
                  <div className="flex flex-wrap justify-end">
                    {e.acuarioId === null ? (
                      <BotonIcono accion="otra" icono={MapPin} etiqueta={`Ubicar ${e.loteCodigo}`} onClick={() => setOperando({ tipo: "ubicacion", e })} />
                    ) : (
                      <BotonIcono accion="otra" icono={ArrowRightLeft} etiqueta={`Trasladar ${e.loteCodigo} desde ${e.acuarioCodigo}`} onClick={() => setOperando({ tipo: "traslado", e })} />
                    )}
                    {e.condicion === "sano" ? (
                      <BotonIcono accion="otra" icono={Stethoscope} etiqueta={`Registrar enfermos de ${e.loteCodigo}`} onClick={() => setOperando({ tipo: "enfermo", e })} />
                    ) : (
                      <BotonIcono accion="otra" icono={HeartPulse} etiqueta={`Registrar aliviados de ${e.loteCodigo}`} onClick={() => setOperando({ tipo: "aliviado", e })} />
                    )}
                    <BotonIcono accion="otra" icono={Skull} etiqueta={`Registrar mortalidad de ${e.loteCodigo}`} onClick={() => setOperando({ tipo: "mortalidad", e })} />
                    {puedeAjustar ? (
                      <BotonIcono accion="otra" icono={Wrench} etiqueta={`Ajustar ${e.loteCodigo}`} onClick={() => setOperando({ tipo: "ajuste", e })} />
                    ) : null}
                  </div>
                </td>
              ) : null}
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-4 py-8 text-center text-texto-suave">
                {existencias.length === 0 ? "No hay stock. Entra por recepciones de pedidos a proveedor." : "Nada coincide con el filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <p className="text-sm text-texto-suave">
        {Math.min(visibles.length, 300)} de {visibles.length} existencias · {piezas(visibles.reduce((s, e) => s + e.cantidad, 0))} peces
        {visibles.length > 300 ? " (filtra para ver el resto)" : ""}
      </p>

      <Dialogo titulo={operando ? TITULOS[operando.tipo] : ""} abierto={operando !== null} onCerrar={() => { setOperando(null); setError(null); }} ancho="max-w-xl">
        {operando ? (
          <FormularioOperacion
            key={`${operando.tipo}-${operando.e.id}`}
            tipo={operando.tipo}
            existencia={operando.e}
            acuarios={acuarios}
            guardando={procesando}
            error={error}
            onRegistrar={registrar}
            onCancelar={() => { setOperando(null); setError(null); }}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function Resumen({ titulo, valor, alerta = false, peligro = false }: { titulo: string; valor: number; alerta?: boolean; peligro?: boolean }) {
  return (
    <div className="flex min-w-0 flex-col gap-1 rounded-md border border-borde bg-superficie p-3 sm:p-4">
      <p className="text-xs text-texto-suave sm:text-sm">{titulo}</p>
      <p className={`text-xl font-semibold tabular-nums sm:text-2xl ${peligro && valor > 0 ? "text-peligro" : alerta ? "text-advertencia-texto" : "text-texto"}`}>
        {piezas(valor)}
      </p>
    </div>
  );
}

/** Formulario de una operación: arma origen y destino según el tipo (igual que ReglasStock en la API). */
function FormularioOperacion({
  tipo,
  existencia: e,
  acuarios,
  guardando,
  error,
  onRegistrar,
  onCancelar,
}: {
  tipo: Operacion;
  existencia: Existencia;
  acuarios: AcuarioOpcion[];
  guardando: boolean;
  error: string | null;
  onRegistrar: (operacion: Record<string, unknown>, mensaje: string) => void;
  onCancelar: () => void;
}) {
  const [cantidad, setCantidad] = useState(String(e.cantidad));
  const [destino, setDestino] = useState(tipo === "enfermo" || tipo === "aliviado" ? (e.acuarioId ?? "") : "");
  const [sentido, setSentido] = useState<"suma" | "resta">("resta");
  const [nota, setNota] = useState("");

  const pideDestino = tipo === "ubicacion" || tipo === "traslado" || tipo === "enfermo" || tipo === "aliviado";
  const opciones = acuarios.filter((a) => tipo !== "traslado" || a.id !== e.acuarioId);
  const n = Number(cantidad);

  const enviar = () => {
    const origen = { acuarioOrigenId: e.acuarioId, condicionOrigen: e.condicion };
    const base = { loteId: e.loteId, cantidad: n, nota: nota || null };
    const operacion: Record<string, unknown> =
      tipo === "mortalidad"
        ? { tipo, ...base, ...origen }
        : tipo === "ajuste"
          ? sentido === "resta"
            ? { tipo, ...base, ...origen }
            : { tipo, ...base, acuarioDestinoId: e.acuarioId, condicionDestino: e.condicion }
          : {
              tipo,
              ...base,
              ...origen,
              acuarioDestinoId: destino || null,
              condicionDestino: tipo === "enfermo" ? "enfermo" : tipo === "aliviado" ? "sano" : e.condicion,
            };
    onRegistrar(operacion, `${TITULOS[tipo]}: ${n} de ${e.loteCodigo} registrado.`);
  };

  return (
    <form
      onSubmit={(ev) => {
        ev.preventDefault();
        enviar();
      }}
      className="flex flex-col gap-6"
    >
      <p className="text-sm text-texto">
        <span className="font-mono text-texto-suave">{e.productoCodigo}</span> <span className="italic">{e.nombreCientifico}</span> · {e.loteCodigo}
        <br />
        En {e.acuarioCodigo ?? "aclimatación"} · {CONDICIONES[e.condicion]} · <strong>{piezas(e.cantidad)}</strong> disponibles
      </p>
      <RejillaCampos>
        {tipo === "ajuste" ? (
          <Campo label="Sentido" htmlFor="sentido">
            <select id="sentido" value={sentido} onChange={(ev) => setSentido(ev.target.value as "suma" | "resta")} className={selectClass}>
              <option value="resta">Restar (faltan peces)</option>
              <option value="suma">Sumar (sobran peces)</option>
            </select>
          </Campo>
        ) : null}
        <Campo label="Cantidad" htmlFor="cantidadOp">
          <input
            id="cantidadOp"
            type="number"
            inputMode="numeric"
            min={1}
            max={tipo === "ajuste" && sentido === "suma" ? undefined : e.cantidad}
            step={1}
            value={cantidad}
            onChange={(ev) => setCantidad(ev.target.value)}
            className={inputClass}
          />
        </Campo>
        {pideDestino ? (
          <Campo
            label={tipo === "enfermo" || tipo === "aliviado" ? "Acuario (puede ser el mismo)" : "Acuario de destino"}
            htmlFor="destino"
          >
            <select id="destino" value={destino} onChange={(ev) => setDestino(ev.target.value)} className={selectClass}>
              <option value="">{tipo === "enfermo" || tipo === "aliviado" ? (e.acuarioId ? "Selecciona…" : "Aclimatación") : "Selecciona…"}</option>
              {opciones.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.codigo}
                </option>
              ))}
            </select>
          </Campo>
        ) : null}
        <Campo label={tipo === "ajuste" ? "Motivo" : "Nota (opcional)"} htmlFor="nota">
          <input id="nota" value={nota} onChange={(ev) => setNota(ev.target.value)} maxLength={500} className={inputClass} />
        </Campo>
      </RejillaCampos>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando || !(n > 0)} className={botonPrimarioClass}>
          {guardando ? "Registrando…" : "Registrar"}
        </button>
      </div>
    </form>
  );
}
