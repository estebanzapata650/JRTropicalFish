"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { FileCheck2 } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { Campo, EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { cifra, usd, type EmbarqueDetalle, type TipoCaja } from "@/lib/exportaciones";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import { fecha } from "@/lib/formato";
import { facturarEmbarqueAction, guardarDoaAction, guardarEmbarqueAction } from "../actions";
import { Aviso, BotonesDocumento, ResumenTotales, cuerpoBase, filasCargo, numero } from "./comun";

/** Paso 5: cajas reales, valor real del AWB y emisión de la factura (número y PDF que no cambian). */
export function PasoFactura({ d, cajas, editable, puedeGestionar }: { d: EmbarqueDetalle; cajas: TipoCaja[]; editable: boolean; puedeGestionar: boolean }) {
  const router = useRouter();
  const e = d.embarque;
  const f = d.calculos.factura;
  const [reales, setReales] = useState(() => Object.fromEntries(cajas.map((c) => [c.id, String(d.cajasReales.find((x) => x.tipoCajaId === c.id)?.cantidad ?? "")])));
  const [awbValor, setAwbValor] = useState(e.awbValor != null ? String(e.awbValor) : "");
  const [confirmar, setConfirmar] = useState(false);
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();
  const puede = editable && puedeGestionar;
  const empacadas = new Set(d.empaque.map((x) => x.numeroCaja)).size;

  return (
    <div className="flex flex-col gap-6">
      <Aviso texto={aviso} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <section aria-labelledby="titulo-reales" className="flex flex-col gap-4">
          <h2 id="titulo-reales" className="text-base font-semibold text-texto">
            Cajas reales y guía aérea
          </h2>
          <p className="text-sm text-texto-suave">
            El empaque tiene {empacadas} {empacadas === 1 ? "caja" : "cajas"}. Indica de qué tipo son (de aquí salen las cajas y los heat packs de la factura).
          </p>
          {cajas.filter((c) => c.activo).map((c) => (
            <label key={c.id} className="grid grid-cols-[1fr_6rem] items-center gap-3 text-sm text-texto">
              {c.nombre}
              <input
                aria-label={`Cajas reales: ${c.nombre}`}
                type="number"
                inputMode="numeric"
                min={0}
                disabled={!puede}
                value={reales[c.id] ?? ""}
                onChange={(ev) => setReales((x) => ({ ...x, [c.id]: ev.target.value }))}
                className={inputClass}
              />
            </label>
          ))}
          <Campo label="Valor real del AWB (USD)" htmlFor="awbValor" hint={`Estimado: ${usd(d.calculos.flete.total)}. Vacío = se factura el estimado.`}>
            <input id="awbValor" type="number" inputMode="decimal" min={0} step="0.01" disabled={!puede} value={awbValor} onChange={(ev) => setAwbValor(ev.target.value)} className={inputClass} />
          </Campo>
          {puede ? (
            <button
              type="button"
              disabled={procesando}
              onClick={() =>
                iniciar(async () => {
                  const r = await guardarEmbarqueAction(e.id, {
                    ...cuerpoBase(d),
                    awbValor: numero(awbValor),
                    cajasReales: cajas.map((c) => ({ tipoCajaId: c.id, cantidad: numero(reales[c.id]) ?? 0 })).filter((c) => c.cantidad > 0),
                  });
                  if (r.error) {
                    setError(r.error);
                    return;
                  }
                  setError(null);
                  setAviso("Cajas y AWB guardados.");
                  router.refresh();
                })
              }
              className={`${botonSecundarioClass} w-fit`}
            >
              Guardar cajas y AWB
            </button>
          ) : null}
        </section>

        <section aria-labelledby="titulo-factura" className="flex flex-col gap-3">
          <h2 id="titulo-factura" className="text-base font-semibold text-texto">
            {e.facturaNumero ? `Factura ${e.facturaNumero}` : "Factura (borrador)"}
          </h2>
          <ResumenTotales
            filas={[
              { concepto: "Peces", detalle: `${f.unidades.toLocaleString("es-CO")} en ${f.cajas} cajas`, total: f.subtotalPeces },
              ...(f.descuento > 0 ? [{ concepto: "Descuento peces", detalle: `${cifra(e.descuentoPeces)} %`, total: f.descuento, negativo: true }] : []),
              ...filasCargo(f.cargos),
              { concepto: `AWB ${e.awb ?? ""}`.trim(), detalle: f.awbEstimado ? "estimado" : undefined, total: f.awb },
              { concepto: "Total factura", total: f.total, fuerte: true },
            ]}
          />
          {e.facturaNumero ? (
            <p className="text-sm text-texto-suave">Emitida el {fecha(e.facturaFecha)}: el PDF quedó guardado y no cambia. La venta y la torre de vuelos se actualizaron.</p>
          ) : puedeGestionar ? (
            <button type="button" disabled={e.estado !== "empacado"} onClick={() => setConfirmar(true)} className={`${botonPrimarioClass} w-fit gap-2`}>
              <FileCheck2 className="size-4" aria-hidden /> Emitir factura
            </button>
          ) : null}
          {!e.facturaNumero && e.estado !== "empacado" ? <p className="text-xs text-texto-suave">Se emite cuando el embarque está empacado.</p> : null}
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
          <BotonesDocumento embarqueId={e.id} tipo="factura" correoCliente={d.clienteEmail} puedeEnviar={puedeGestionar} onEnviado={setAviso} />
        </section>
      </div>

      <Dialogo titulo="Emitir factura" abierto={confirmar} onCerrar={() => setConfirmar(false)}>
        <div className="flex flex-col gap-4">
          <p className="text-sm text-texto">
            Se asigna el siguiente número de factura y el PDF queda guardado tal cual: después no se pueden cambiar especies, empaque ni cargos.
            Total: <strong>{usd(f.total)}</strong>.
          </p>
          {f.awbEstimado ? <p className="text-sm text-advertencia-texto">El AWB todavía es el estimado: registra su valor real antes de emitir.</p> : null}
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={() => setConfirmar(false)} className={botonSecundarioClass}>
              Cancelar
            </button>
            <button
              type="button"
              disabled={procesando}
              onClick={() =>
                iniciar(async () => {
                  const r = await facturarEmbarqueAction(e.id);
                  setConfirmar(false);
                  if (r.error || !r.datos) {
                    setError(r.error ?? "No se pudo emitir.");
                    return;
                  }
                  setError(null);
                  setAviso(`Factura ${r.datos.numero} emitida.`);
                  router.refresh();
                })
              }
              className={botonPrimarioClass}
            >
              {procesando ? "Emitiendo…" : "Emitir"}
            </button>
          </div>
        </div>
      </Dialogo>
    </div>
  );
}

/** Paso 6: muertos que reporta el cliente por fila del empaque (DOA · ADD · FINAL) y la nota crédito. */
export function PasoDoa({ d, puede }: { d: EmbarqueDetalle; puede: boolean }) {
  const router = useRouter();
  const facturado = d.embarque.facturaNumero != null;
  const [valores, setValores] = useState(() => Object.fromEntries(d.empaque.map((f) => [f.id, { doa: String(f.doa || ""), add: String(f.doaAdicional || ""), fin: String(f.doaFinal || "") }])));
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const n = d.calculos.notaCredito;
  const set = (id: string, campo: "doa" | "add" | "fin", v: string) => setValores((x) => ({ ...x, [id]: { ...x[id], [campo]: v } }));

  if (!facturado) return <p className="text-sm text-texto-suave">El DOA se registra cuando el cliente reporta los muertos, después de emitir la factura.</p>;

  return (
    <div className="flex flex-col gap-5">
      <Aviso texto={aviso} />
      <p className="text-sm text-texto-suave">Como en la factura JRCH: DOA, ADD y FINAL se suman y se descuentan al precio de cada fila.</p>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Caja · especie</th>
            <th className="px-3 py-2 text-right font-medium">Enviados</th>
            <th className="px-3 py-2 font-medium">DOA</th>
            <th className="px-3 py-2 font-medium">ADD</th>
            <th className="px-3 py-2 font-medium">FINAL</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {d.empaque.map((f) => (
            <tr key={f.id} className="border-b border-borde last:border-0">
              <td className="px-3 py-2 text-sm text-texto">
                <span className="font-medium">{f.numeroCaja}</span> · <span className="mr-1 font-mono text-xs text-texto-suave">{f.productoCodigo}</span>
                <span className="italic">{f.nombreCientifico}</span>
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">{f.cantidad.toLocaleString("es-CO")}</td>
              {(["doa", "add", "fin"] as const).map((c) => (
                <td key={c} className="px-2 py-2">
                  <input
                    aria-label={`${c === "doa" ? "DOA" : c === "add" ? "ADD" : "FINAL"} caja ${f.numeroCaja} ${f.productoCodigo}`}
                    type="number"
                    inputMode="numeric"
                    min={0}
                    max={f.cantidad}
                    disabled={!puede}
                    value={valores[f.id][c]}
                    onChange={(ev) => set(f.id, c, ev.target.value)}
                    className={`${inputClass} w-20`}
                  />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
      {puede ? (
        <button
          type="button"
          disabled={guardando}
          onClick={() =>
            iniciar(async () => {
              const r = await guardarDoaAction(
                d.embarque.id,
                d.empaque.map((f) => ({ id: f.id, doa: numero(valores[f.id].doa) ?? 0, doaAdicional: numero(valores[f.id].add) ?? 0, doaFinal: numero(valores[f.id].fin) ?? 0 })),
              );
              if (r.error) {
                setError(r.error);
                return;
              }
              setError(null);
              setAviso("DOA guardado: la nota crédito y la torre de vuelos se actualizaron.");
              router.refresh();
            })
          }
          className={`${botonPrimarioClass} w-fit`}
        >
          {guardando ? "Guardando…" : "Guardar DOA"}
        </button>
      ) : null}
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Nota crédito {n.total > 0 ? `${d.embarque.facturaNumero}-01` : ""}</h2>
        <ResumenTotales
          filas={[
            ...n.lineas.map((l) => ({ concepto: `${l.productoCodigo} ${l.nombreCientifico}`, detalle: `${l.muertos.toLocaleString("es-CO")} × ${usd(l.precio)}`, total: l.total })),
            ...(n.descuento > 0 ? [{ concepto: "Descuento", total: n.descuento, negativo: true }] : []),
            { concepto: `Total nota crédito (${n.muertos.toLocaleString("es-CO")} peces)`, total: n.total, fuerte: true },
          ]}
        />
        <BotonesDocumento embarqueId={d.embarque.id} tipo="nota_credito" correoCliente={d.clienteEmail} puedeEnviar={puede && n.total > 0} onEnviado={setAviso} />
      </section>
    </div>
  );
}

/** Paso 7: balance con el cliente (factura − nota crédito − anticipo). */
export function PasoBalance({ d, puede }: { d: EmbarqueDetalle; puede: boolean }) {
  const router = useRouter();
  const b = d.calculos.balance;
  const [anticipo, setAnticipo] = useState(String(d.embarque.anticipoRecibido || ""));
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();

  return (
    <div className="flex flex-col gap-5">
      <Aviso texto={aviso} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <ResumenTotales
          filas={[
            { concepto: `Factura ${d.embarque.facturaNumero ?? "(borrador)"}`, total: b.factura },
            ...(b.notaCredito > 0 ? [{ concepto: "Nota crédito", total: b.notaCredito, negativo: true }] : []),
            { concepto: "Anticipo recibido", total: b.anticipo, negativo: true },
            { concepto: b.saldo >= 0 ? "Saldo a favor de JR" : "Saldo a favor del cliente", total: Math.abs(b.saldo), fuerte: true },
          ]}
        />
        <div className="flex flex-col gap-3">
          <Campo label="Anticipo recibido (USD)" htmlFor="anticipo" hint={`Pedido en la preinvoice: ${usd(d.calculos.preinvoice.anticipo)}.`}>
            <input id="anticipo" type="number" inputMode="decimal" min={0} step="0.01" disabled={!puede} value={anticipo} onChange={(ev) => setAnticipo(ev.target.value)} className={inputClass} />
          </Campo>
          {puede ? (
            <button
              type="button"
              disabled={guardando}
              onClick={() =>
                iniciar(async () => {
                  const r = await guardarEmbarqueAction(d.embarque.id, { ...cuerpoBase(d), anticipoRecibido: numero(anticipo) ?? 0 });
                  if (r.error) {
                    setError(r.error);
                    return;
                  }
                  setError(null);
                  setAviso("Anticipo guardado.");
                  router.refresh();
                })
              }
              className={`${botonSecundarioClass} w-fit`}
            >
              Guardar anticipo
            </button>
          ) : null}
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
        </div>
      </div>
      <BotonesDocumento embarqueId={d.embarque.id} tipo="balance" correoCliente={d.clienteEmail} puedeEnviar={puede} onEnviado={setAviso} />
    </div>
  );
}
