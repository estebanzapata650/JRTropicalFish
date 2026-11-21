"use client";

import { useState, useTransition } from "react";
import { Download, Eye, Send } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { Campo } from "@/components/campos";
import { nombreDocumento, usd, type EmbarqueDetalle, type LineaCargo } from "@/lib/exportaciones";
import { botonPrimarioClass, botonSecundarioClass, inputClass, textareaClass } from "@/lib/estilos";
import { enviarDocumentoAction } from "../actions";

/** Cuerpo completo de PUT /embarques/{id} a partir del detalle (la API reemplaza todo). */
export function cuerpoBase(d: EmbarqueDetalle) {
  const e = d.embarque;
  return {
    listaPreciosId: e.listaPreciosId,
    descuentoPeces: e.descuentoPeces,
    aerolinea: e.aerolinea,
    ruta: e.ruta,
    awb: e.awb,
    destino: e.destino,
    salidaVuelo: e.salidaVuelo,
    llegadaVuelo: e.llegadaVuelo,
    fechaDespacho: e.fechaDespacho,
    formatoFactura: e.formatoFactura,
    awbValor: e.awbValor,
    anticipoRecibido: e.anticipoRecibido,
    tarifas: d.tarifas,
    cargos: d.cargos,
    cajasEstimadas: d.cajasEstimadas,
    cajasReales: d.cajasReales,
    notas: e.notas,
  };
}

/** Número desde un input ("1,5" o "1.5"); vacío = null. */
export const numero = (t: string) => (t.trim() === "" ? null : Number(t.replace(",", ".")));

/** ISO UTC → valor de <input type="datetime-local"> en la hora del navegador, y de vuelta. */
export const aLocal = (iso: string | null) => {
  if (!iso) return "";
  const d = new Date(iso);
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
};
export const aIso = (local: string) => (local ? new Date(local).toISOString() : null);

/** Ver (nueva pestaña), descargar y enviar por correo un documento del embarque. */
export function BotonesDocumento({
  embarqueId,
  tipo,
  correoCliente,
  puedeEnviar,
  onEnviado,
}: {
  embarqueId: string;
  tipo: string;
  correoCliente: string | null;
  puedeEnviar: boolean;
  onEnviado?: (texto: string) => void;
}) {
  const [abierto, setAbierto] = useState(false);
  const [para, setPara] = useState(correoCliente ?? "");
  const [mensaje, setMensaje] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [enviando, iniciar] = useTransition();
  const ruta = `/archivos/exportaciones/${embarqueId}/${tipo}`;
  const nombre = nombreDocumento(tipo);

  return (
    <div className="flex flex-wrap gap-2">
      <a href={ruta} target="_blank" rel="noopener" className={`${botonSecundarioClass} gap-2`}>
        <Eye className="size-4" aria-hidden /> Ver {nombre.toLowerCase()}
      </a>
      <a href={`${ruta}?descargar=1`} className={`${botonSecundarioClass} gap-2`} aria-label={`Descargar ${nombre} en PDF`}>
        <Download className="size-4" aria-hidden /> PDF
      </a>
      {puedeEnviar ? (
        <button type="button" onClick={() => setAbierto(true)} className={`${botonSecundarioClass} gap-2`} aria-label={`Enviar ${nombre} al cliente`}>
          <Send className="size-4" aria-hidden /> Enviar
        </button>
      ) : null}
      <Dialogo titulo={`Enviar ${nombre.toLowerCase()}`} abierto={abierto} onCerrar={() => setAbierto(false)}>
        <form
          onSubmit={(ev) => {
            ev.preventDefault();
            iniciar(async () => {
              const r = await enviarDocumentoAction(embarqueId, tipo, para, mensaje);
              if (r.error || !r.datos) {
                setError(r.error ?? "No se pudo enviar.");
                return;
              }
              setError(null);
              setAbierto(false);
              onEnviado?.(
                r.datos.llegoAlDestinatario
                  ? `${nombre} enviada a ${r.datos.para}.`
                  : `${nombre} registrada para ${r.datos.para} (proveedor ${r.datos.proveedor}: no sale a internet en este ambiente).`,
              );
            });
          }}
          className="flex flex-col gap-4"
        >
          <p className="text-sm text-texto-suave">Se adjunta el PDF y queda registro del envío.</p>
          <Campo label="Para" htmlFor={`para-${tipo}`} hint={correoCliente ? "Correo del cliente por defecto." : "El cliente no tiene correo registrado."}>
            <input id={`para-${tipo}`} type="email" value={para} onChange={(ev) => setPara(ev.target.value)} required className={inputClass} />
          </Campo>
          <Campo label="Mensaje adicional (opcional)" htmlFor={`msg-${tipo}`}>
            <textarea id={`msg-${tipo}`} value={mensaje} onChange={(ev) => setMensaje(ev.target.value)} maxLength={2000} className={textareaClass} />
          </Campo>
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={() => setAbierto(false)} className={botonSecundarioClass}>
              Cancelar
            </button>
            <button type="submit" disabled={enviando} className={`${botonPrimarioClass} gap-2`}>
              <Send className="size-4" aria-hidden /> {enviando ? "Enviando…" : "Enviar"}
            </button>
          </div>
        </form>
      </Dialogo>
    </div>
  );
}

/** Resumen de totales al estilo de la factura: concepto, cantidad × precio, total; la última fila en negrita. */
export function ResumenTotales({ filas }: { filas: { concepto: string; detalle?: string; total: number; fuerte?: boolean; negativo?: boolean }[] }) {
  return (
    <dl className="flex flex-col divide-y divide-borde rounded-md border border-borde bg-superficie">
      {filas.map((f) => (
        <div key={f.concepto} className={`flex items-baseline justify-between gap-3 px-3 py-2 text-sm ${f.fuerte ? "bg-superficie-alterna font-semibold" : ""}`}>
          <dt className="min-w-0 text-texto">
            {f.concepto}
            {f.detalle ? <span className="ml-1 text-xs font-normal text-texto-suave">{f.detalle}</span> : null}
          </dt>
          <dd className="shrink-0 tabular-nums text-texto">{f.negativo ? `− ${usd(f.total)}` : usd(f.total)}</dd>
        </div>
      ))}
    </dl>
  );
}

export const filasCargo = (cargos: LineaCargo[]) =>
  cargos.map((c) => ({ concepto: c.descripcion, detalle: `${c.cantidad.toLocaleString("es-CO")} × ${usd(c.precio)}`, total: c.total }));

/** Mensaje de resultado bajo una acción (éxito o error). */
export function Aviso({ texto, error }: { texto: string | null; error?: boolean }) {
  if (!texto) return null;
  return (
    <p role="status" className={`rounded-md px-3 py-2 text-sm ${error ? "bg-peligro/15 text-peligro" : "bg-exito-fondo text-exito-texto"}`}>
      {texto}
    </p>
  );
}

/** Campo de una fila editable: en móvil lleva su etiqueta visible; en escritorio la da el contexto de la grilla. */
export function CampoFila({ etiqueta, className = "", children }: { etiqueta: string; className?: string; children: React.ReactNode }) {
  return (
    <div className={`flex min-w-0 flex-col gap-1 ${className}`}>
      <span aria-hidden className="text-xs text-texto-suave lg:hidden">
        {etiqueta}
      </span>
      {children}
    </div>
  );
}
