"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Calculator, History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, CampoAncho, Casilla, EncabezadoTabla, Etiqueta, RejillaCampos, Seccion, TablaResponsive } from "@/components/campos";
import { ETIQUETAS_HISTORIAL_CATALOGO, textoRegla, type ListaPrecios, type ParametrosPrecios } from "@/lib/catalogos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import { guardarListaAction, guardarParametrosPreciosAction, recalcularListaAction } from "../actions";

type Opcion = { id: string; nombre: string };
const numero = (t: string) => (t.trim() === "" ? null : Number(t.replace(",", ".")));
const texto = (v: number | null | undefined) => (v == null ? "" : String(v));

/**
 * Listas de precios con su regla (lista base × factor por tipo, IVA y pasarela, redondeo) y los
 * parámetros. «Recalcular» aplica la regla a todos los productos que tienen precio en la lista base.
 */
export function PreciosCliente({
  listas,
  parametros,
  clientes,
  puedeEditar,
  puedeVerCostos,
}: {
  listas: ListaPrecios[];
  parametros: ParametrosPrecios;
  clientes: Opcion[];
  puedeEditar: boolean;
  puedeVerCostos: boolean;
}) {
  const router = useRouter();
  const [editando, setEditando] = useState<ListaPrecios | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<ListaPrecios | null>(null);
  const [recalculando, setRecalculando] = useState<ListaPrecios | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [aviso, setAviso] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setRecalculando(null);
    setError(null);
  };

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      const r = await guardarListaAction(editando?.id ?? null, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  const recalcular = () =>
    recalculando &&
    iniciar(async () => {
      const r = await recalcularListaAction(recalculando.id);
      if (r.error || !r.datos) {
        setError(r.error ?? "No se pudo recalcular.");
        return;
      }
      setAviso(
        `«${recalculando.nombre}»: ${r.datos.cambiados} precios actualizados de ${r.datos.calculados} calculados` +
          (r.datos.sinBase ? `; ${r.datos.sinBase} productos sin precio base quedaron igual.` : "."),
      );
      cerrar();
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Listas de precios"
        descripcion="Cada producto tiene un precio por lista. Las listas con regla se pueden recalcular desde su lista base."
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nueva lista
            </button>
          ) : null
        }
      />

      {aviso ? <p role="status" className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">{aviso}</p> : null}

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Lista</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Regla</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Productos</th>
            <th className="px-4 py-3 text-right font-medium">Acciones</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {listas.map((l) => (
            <tr key={l.id} className={`border-b border-borde last:border-0 ${l.activo ? "" : "opacity-60"}`}>
              <td className="px-4 py-3">
                <p className="font-medium text-texto">
                  {l.nombre} <span className="text-xs font-normal text-texto-suave">{l.moneda}</span>
                </p>
                <p className="flex flex-wrap items-center gap-1.5 text-xs text-texto-suave">
                  <span className="font-mono">{l.codigo}</span>
                  {l.esCosto ? <Etiqueta texto="Costo (reservado)" tono="advertencia" /> : null}
                  {l.clienteNombre ? <span>· Cliente: {l.clienteNombre}</span> : null}
                  {!l.activo ? <Etiqueta texto="Inactiva" tono="neutro" /> : null}
                </p>
                <p className="text-xs text-texto-suave md:hidden">{textoRegla(l)}</p>
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">{textoRegla(l)}</td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">{l.productos}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  {puedeEditar && l.listaBaseId ? (
                    <BotonIcono accion="otra" icono={Calculator} etiqueta={`Recalcular ${l.nombre}`} onClick={() => setRecalculando(l)} />
                  ) : null}
                  <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${l.nombre}`} onClick={() => setHistorialDe(l)} />
                  {puedeEditar ? <BotonIcono accion="editar" etiqueta={`Editar ${l.nombre}`} onClick={() => setEditando(l)} /> : null}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <FormularioParametros parametros={parametros} puedeEditar={puedeEditar} />

      <Dialogo titulo={editando ? `Editar ${editando.nombre}` : "Nueva lista de precios"} abierto={creando || editando !== null} onCerrar={cerrar} ancho="max-w-4xl">
        <FormularioLista
          key={editando?.id ?? "nueva"}
          lista={editando}
          listas={listas}
          clientes={clientes}
          puedeVerCostos={puedeVerCostos}
          guardando={procesando}
          error={error}
          onGuardar={guardar}
          onCancelar={cerrar}
        />
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.nombre}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? (
          <HistorialRemoto ruta={`/api/v1/catalogos/precios/listas/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CATALOGO} />
        ) : null}
      </Dialogo>
      <DialogoConfirmacion
        titulo="Recalcular lista"
        mensaje={
          recalculando ? (
            <>
              Se recalculará <strong>{recalculando.nombre}</strong> con su regla: {textoRegla(recalculando)}. Los precios de los
              productos que tienen precio en «{recalculando.listaBaseNombre}» se reemplazan; los demás quedan igual. Cada cambio
              queda en el historial.
            </>
          ) : null
        }
        textoConfirmar="Recalcular"
        abierto={recalculando !== null}
        procesando={procesando}
        error={error}
        onConfirmar={recalcular}
        onCerrar={cerrar}
      />
    </div>
  );
}

function FormularioLista({
  lista,
  listas,
  clientes,
  puedeVerCostos,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  lista: ListaPrecios | null;
  listas: ListaPrecios[];
  clientes: Opcion[];
  puedeVerCostos: boolean;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [codigo, setCodigo] = useState(lista?.codigo ?? "");
  const [nombre, setNombre] = useState(lista?.nombre ?? "");
  const [moneda, setMoneda] = useState(lista?.moneda ?? "USD");
  const [esCosto, setEsCosto] = useState(lista?.esCosto ?? false);
  const [clienteId, setClienteId] = useState(lista?.clienteId ?? "");
  const [orden, setOrden] = useState(texto(lista?.orden ?? listas.length * 10 + 10));
  const [activo, setActivo] = useState(lista?.activo ?? true);
  const [baseId, setBaseId] = useState(lista?.listaBaseId ?? "");
  const [fs, setFs] = useState(texto(lista?.factorSalvaje));
  const [fc, setFc] = useState(texto(lista?.factorCriadero));
  const [fe, setFe] = useState(texto(lista?.factorEspecial));
  const [impuestos, setImpuestos] = useState(lista?.aplicaImpuestos ?? false);
  const [redondeo, setRedondeo] = useState(texto(lista?.redondeo ?? 0.01));

  // Base: misma moneda y nunca la propia lista.
  const bases = listas.filter((l) => l.id !== lista?.id && l.moneda === moneda);
  const opcionesCliente =
    lista?.clienteId && !clientes.some((c) => c.id === lista.clienteId)
      ? [...clientes, { id: lista.clienteId, nombre: lista.clienteNombre ?? "—" }]
      : clientes;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          codigo: lista ? null : codigo,
          nombre,
          moneda,
          esCosto,
          clienteId: clienteId || null,
          orden: numero(orden) ?? 0,
          activo,
          listaBaseId: baseId || null,
          factorSalvaje: numero(fs),
          factorCriadero: numero(fc),
          factorEspecial: numero(fe),
          aplicaImpuestos: impuestos,
          redondeo: numero(redondeo) ?? 0.01,
        });
      }}
      className="flex flex-col gap-6"
    >
      <Seccion titulo="Lista">
        <RejillaCampos>
          <Campo label="Código" htmlFor="codigoLista" hint={lista ? "No cambia." : "Mayúsculas sin espacios, ej. CLIENTE_JAPON."}>
            <input
              id="codigoLista"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase().replace(/\s+/g, "_"))}
              readOnly={!!lista}
              maxLength={30}
              className={`${inputClass} font-mono read-only:bg-superficie-alterna`}
            />
          </Campo>
          <Campo label="Nombre" htmlFor="nombreLista">
            <input id="nombreLista" value={nombre} onChange={(e) => setNombre(e.target.value)} maxLength={100} className={inputClass} />
          </Campo>
          <Campo label="Moneda" htmlFor="monedaLista" hint={lista ? "No cambia." : undefined}>
            <select id="monedaLista" value={moneda} onChange={(e) => { setMoneda(e.target.value); setBaseId(""); }} disabled={!!lista} className={selectClass}>
              <option value="USD">USD</option>
              <option value="COP">COP</option>
            </select>
          </Campo>
          <Campo label="Cliente (opcional)" htmlFor="clienteLista" hint="Para listas especiales de un cliente.">
            <select id="clienteLista" value={clienteId} onChange={(e) => setClienteId(e.target.value)} className={selectClass}>
              <option value="">Ninguno</option>
              {opcionesCliente.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Orden" htmlFor="ordenLista">
            <input id="ordenLista" type="number" min={0} step={10} value={orden} onChange={(e) => setOrden(e.target.value)} className={inputClass} />
          </Campo>
          <div className="flex flex-col justify-end gap-1">
            <Casilla label="Activa" checked={activo} onChange={setActivo} />
            {!lista && puedeVerCostos ? <Casilla label="Es una lista de costo (reservada)" checked={esCosto} onChange={setEsCosto} /> : null}
          </div>
        </RejillaCampos>
      </Seccion>

      <Seccion titulo="Regla de cálculo (opcional)">
        <RejillaCampos>
          <CampoAncho>
            <Campo label="Lista base" htmlFor="baseLista" hint="Sin lista base, los precios se escriben a mano en cada producto.">
              <select id="baseLista" value={baseId} onChange={(e) => setBaseId(e.target.value)} className={selectClass}>
                <option value="">Sin regla (manual)</option>
                {bases.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.nombre}
                  </option>
                ))}
              </select>
            </Campo>
          </CampoAncho>
          {baseId ? (
            <>
              <Campo label="Factor salvaje" htmlFor="fs">
                <input id="fs" type="number" inputMode="decimal" min={0} step="0.0001" value={fs} onChange={(e) => setFs(e.target.value)} className={inputClass} />
              </Campo>
              <Campo label="Factor criadero" htmlFor="fc">
                <input id="fc" type="number" inputMode="decimal" min={0} step="0.0001" value={fc} onChange={(e) => setFc(e.target.value)} className={inputClass} />
              </Campo>
              <Campo label="Factor especial" htmlFor="fe">
                <input id="fe" type="number" inputMode="decimal" min={0} step="0.0001" value={fe} onChange={(e) => setFe(e.target.value)} className={inputClass} />
              </Campo>
              <div className="flex items-end">
                <Casilla label="Sumar IVA y comisión de pasarela" checked={impuestos} onChange={setImpuestos} />
              </div>
            </>
          ) : null}
          <Campo label="Redondear al múltiplo de" htmlFor="redondeo" hint="50 o 100 en pesos; 0.01 en dólares.">
            <input id="redondeo" type="number" inputMode="decimal" min={0.01} step="0.01" value={redondeo} onChange={(e) => setRedondeo(e.target.value)} className={inputClass} />
          </Campo>
        </RejillaCampos>
      </Seccion>

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}

function FormularioParametros({ parametros, puedeEditar }: { parametros: ParametrosPrecios; puedeEditar: boolean }) {
  const router = useRouter();
  const [iva, setIva] = useState(texto(parametros.iva));
  const [pasarela, setPasarela] = useState(texto(parametros.pasarela));
  const [trm, setTrm] = useState(texto(parametros.trmReferencia));
  const [mensaje, setMensaje] = useState<{ ok: boolean; texto: string } | null>(null);
  const [guardando, iniciar] = useTransition();

  return (
    <Seccion titulo="Parámetros de precios">
      <form
        onSubmit={(e) => {
          e.preventDefault();
          iniciar(async () => {
            const r = await guardarParametrosPreciosAction({ iva: numero(iva) ?? 0, pasarela: numero(pasarela) ?? 0, trmReferencia: numero(trm) ?? 0 });
            setMensaje(r.error ? { ok: false, texto: r.error } : { ok: true, texto: "Parámetros guardados." });
            if (!r.error) router.refresh();
          });
        }}
        className="flex flex-col gap-4"
      >
        <fieldset disabled={!puedeEditar} className="contents">
          <RejillaCampos>
            <Campo label="IVA nacional (%)" htmlFor="iva">
              <input id="iva" type="number" min={0} max={100} step="0.01" value={iva} onChange={(e) => setIva(e.target.value)} className={inputClass} />
            </Campo>
            <Campo label="Comisión pasarela de pagos (%)" htmlFor="pasarela" hint="Wompi.">
              <input id="pasarela" type="number" min={0} max={100} step="0.01" value={pasarela} onChange={(e) => setPasarela(e.target.value)} className={inputClass} />
            </Campo>
            <Campo label="TRM de referencia (COP por USD)" htmlFor="trmRef" hint="Para cotizar. Cada venta guarda su propia TRM.">
              <input id="trmRef" type="number" min={1000} max={20000} step="0.01" value={trm} onChange={(e) => setTrm(e.target.value)} className={inputClass} />
            </Campo>
          </RejillaCampos>
        </fieldset>
        {puedeEditar ? (
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
            {mensaje ? <p role="status" className={`text-sm ${mensaje.ok ? "text-exito-texto" : "text-peligro"}`}>{mensaje.texto}</p> : null}
            <button type="submit" disabled={guardando} className={botonPrimarioClass}>
              {guardando ? "Guardando…" : "Guardar parámetros"}
            </button>
          </div>
        ) : null}
      </form>
    </Seccion>
  );
}
