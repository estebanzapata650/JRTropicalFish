"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { FileCheck2, TriangleAlert } from "lucide-react";
import { Campo, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive, type TonoEtiqueta } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import { fecha } from "@/lib/formato";
import { ESTADOS_AUNAP, type EmbarqueDetalle, type FilaAunap, type ResumenAunap } from "@/lib/exportaciones";
import { useDetalle } from "../../compras/useDetalle";
import { generarProformaAction, guardarPermisoAunapAction } from "../actions";
import { Aviso, BotonesDocumento } from "./comun";

const TONO: Record<FilaAunap["estado"], TonoEtiqueta> = { sin_permiso: "neutro", no_autorizada: "peligro", excede: "peligro", ok: "exito" };

/** Estados en que el cliente ya aprobó la preinvoice: se puede generar la proforma. */
const CON_PROFORMA = new Set(["confirmado", "empacado", "facturado", "cerrado"]);

const entero = (n: number | null | undefined) => (n === null || n === undefined ? "—" : n.toLocaleString("es-CO"));

/**
 * Trámites del despacho, en el orden en que se hacen: la preinvoice (paso 1) es la cotización; cuando el cliente
 * aprueba se genera la **proforma**, base de la **VUCE**; después el DEX, la factura DIAN y, si aplica, el CITES; por
 * último los documentos de aeropuerto y aduana. Todo sale de lo real y alimenta el informe mensual AUNAP.
 */
export function PasoAunap({ d, puedeGestionar }: { d: EmbarqueDetalle; puedeGestionar: boolean }) {
  const e = d.embarque;
  const [version, setVersion] = useState(0);
  const { cargando, datos: resumen, error } = useDetalle<ResumenAunap>(`/api/exportaciones/embarques/${e.id}/aunap`, version);

  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (error || !resumen) return <p className="text-sm text-peligro">{error ?? "No se pudo cargar."}</p>;

  const bloqueado = resumen.sinCodigo.length > 0;
  const puede = puedeGestionar && e.estado !== "cancelado";
  return (
    <div className="flex flex-col gap-6">
      {resumen.avisos.map((a) => (
        <p key={a} className="flex items-start gap-2 rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden /> {a}
        </p>
      ))}
      {bloqueado ? (
        <p className="text-sm text-texto-suave">
          Revisa los códigos en{" "}
          <Link href="/exportaciones/aunap" className="text-acento hover:underline">
            Exportaciones → AUNAP
          </Link>
          . Si la especie no está en la resolución, hay que retirarla del pedido o tramitar su autorización ante la AUNAP.
        </p>
      ) : null}

      <Proforma d={d} resumen={resumen} puede={puede} bloqueado={bloqueado} onGenerada={() => setVersion((v) => v + 1)} />

      <TramitesForm
        key={`${resumen.permiso?.actualizadoEn ?? "nuevo"}-${version}`}
        embarqueId={e.id}
        resumen={resumen}
        puede={puede}
        onGuardado={() => setVersion((v) => v + 1)}
      />

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">5 · Aeropuerto y aduana</h2>
        <p className="text-sm text-texto-suave">Salen del empaque real, con el código y el nombre de la Resolución AUNAP.</p>
        <Documento
          titulo="Packing list de aeropuerto y aduana"
          detalle="El empaque real, caja por caja."
          embarqueId={e.id}
          tipo="packing_aduana"
          motivo={bloqueado ? "Hay especies sin código AUNAP." : !resumen.hayEmpaque ? "Falta registrar el empaque real (paso Empaque)." : null}
        />
        <Documento
          titulo="Custom invoice"
          detalle="La factura real agrupada por especie AUNAP: mismos totales que la factura comercial."
          embarqueId={e.id}
          tipo="factura_aduana"
          motivo={bloqueado ? "Hay especies sin código AUNAP." : !resumen.hayEmpaque ? "Falta registrar el empaque real (paso Empaque)." : null}
        />
        <div className="flex flex-col gap-2 rounded-md border border-dashed border-borde p-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="min-w-0">
            <p className="font-medium text-texto">Certificado ICA</p>
            <p className="text-sm text-texto-suave">Para los despachos que lo exigen. Se agrega en una próxima fase.</p>
          </div>
          <Etiqueta texto="Próximamente" tono="neutro" />
        </div>
      </section>
    </div>
  );
}

function Proforma({
  d,
  resumen,
  puede,
  bloqueado,
  onGenerada,
}: {
  d: EmbarqueDetalle;
  resumen: ResumenAunap;
  puede: boolean;
  bloqueado: boolean;
  onGenerada: () => void;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<string | null>(null);
  const e = d.embarque;
  const numero = resumen.permiso?.proformaNumero ?? null;
  const aprobado = CON_PROFORMA.has(e.estado);

  const generar = () =>
    iniciar(async () => {
      const r = await generarProformaAction(e.id);
      setMensaje(r.error ?? null);
      if (!r.error) {
        onGenerada();
        router.refresh();
      }
    });

  return (
    <section className="flex flex-col gap-3">
      <div>
        <h2 className="text-base font-semibold text-texto">1 · Proforma</h2>
        <p className="text-sm text-texto-suave">
          La preinvoice es la cotización al cliente. Cuando la aprueba, la proforma es el documento con el que se tramita la VUCE.
        </p>
      </div>
      {numero ? (
        <div className="flex flex-col gap-2 rounded-md border border-borde bg-superficie p-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-center gap-2">
            <FileCheck2 className="size-5 shrink-0 text-exito-texto" aria-hidden />
            <div>
              <p className="font-medium text-texto">Proforma {numero}</p>
              <p className="text-sm text-texto-suave">{fecha(resumen.permiso?.proformaFecha)} · especies y cantidades del pedido</p>
            </div>
          </div>
          <BotonesDocumento embarqueId={e.id} tipo="solicitud_aunap" correoCliente={null} puedeEnviar={false} />
        </div>
      ) : aprobado ? (
        puede ? (
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <button type="button" disabled={pendiente || bloqueado || d.lineas.length === 0} onClick={generar} className={`${botonPrimarioClass} w-full sm:w-auto`}>
              {pendiente ? "Generando…" : "Generar proforma"}
            </button>
            <p className="text-sm text-texto-suave">
              {bloqueado ? "Hay especies sin código AUNAP." : d.lineas.length === 0 ? "El embarque no tiene especies." : "Le asigna número y fecha."}
            </p>
          </div>
        ) : (
          <p className="text-sm text-texto-suave">Aún no se ha generado.</p>
        )
      ) : (
        <p className="rounded-md bg-info-fondo px-3 py-2 text-sm text-info-texto">
          Se genera cuando el cliente aprueba la preinvoice: marca el embarque como <strong>confirmado</strong>.
        </p>
      )}
      {mensaje ? <Aviso texto={mensaje} error /> : null}
    </section>
  );
}

function Documento({ titulo, detalle, embarqueId, tipo, motivo }: { titulo: string; detalle: string; embarqueId: string; tipo: string; motivo: string | null }) {
  return (
    <div className="flex flex-col gap-2 rounded-md border border-borde bg-superficie p-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <p className="font-medium text-texto">{titulo}</p>
        <p className="text-sm text-texto-suave">{motivo ?? detalle}</p>
      </div>
      {motivo ? <Etiqueta texto="No disponible" tono="neutro" /> : <BotonesDocumento embarqueId={embarqueId} tipo={tipo} correoCliente={null} puedeEnviar={false} />}
    </div>
  );
}

function TramitesForm({ embarqueId, resumen, puede, onGuardado }: { embarqueId: string; resumen: ResumenAunap; puede: boolean; onGuardado: () => void }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{ texto: string; error: boolean } | null>(null);
  const t = resumen.permiso;
  const [campos, setCampos] = useState({
    numero: t?.numero ?? "",
    vuceFechaSolicitud: t?.vuceFechaSolicitud ?? "",
    fecha: t?.fecha ?? "",
    dexNumero: t?.dexNumero ?? "",
    facturaDian: t?.facturaDian ?? "",
    citesNumero: t?.citesNumero ?? "",
    citesFecha: t?.citesFecha ?? "",
  });
  const [autorizados, setAutorizados] = useState<Record<string, string>>(
    Object.fromEntries(resumen.filas.map((f) => [f.especieAunapId, f.autorizado === null ? "" : String(f.autorizado)])),
  );
  const cambiar = (clave: keyof typeof campos) => (ev: React.ChangeEvent<HTMLInputElement>) =>
    setCampos((c) => ({ ...c, [clave]: ev.target.value }));
  const texto = (clave: keyof typeof campos, id: string, maximo: number, placeholder?: string) => (
    <input id={id} className={inputClass} value={campos[clave]} onChange={cambiar(clave)} maxLength={maximo} placeholder={placeholder} disabled={!puede} />
  );
  const dia = (clave: keyof typeof campos, id: string) => (
    <input id={id} type="date" className={inputClass} value={campos[clave]} onChange={cambiar(clave)} disabled={!puede} />
  );

  const guardar = () =>
    iniciar(async () => {
      const r = await guardarPermisoAunapAction(embarqueId, {
        numero: campos.numero || null,
        fecha: campos.fecha || null,
        vuceFechaSolicitud: campos.vuceFechaSolicitud || null,
        dexNumero: campos.dexNumero || null,
        facturaDian: campos.facturaDian || null,
        citesNumero: campos.citesNumero || null,
        citesFecha: campos.citesFecha || null,
        notas: t?.notas ?? null,
        lineas: resumen.filas.map((f) => ({ especieAunapId: f.especieAunapId, cantidad: Number(autorizados[f.especieAunapId] || 0) })),
      });
      setMensaje(r.error ? { texto: r.error, error: true } : { texto: "Trámites guardados.", error: false });
      if (!r.error) {
        onGuardado();
        router.refresh();
      }
    });

  return (
    <div className="flex flex-col gap-6">
      <section className="flex flex-col gap-3">
        <div>
          <h2 className="text-base font-semibold text-texto">2 · VUCE</h2>
          <p className="text-sm text-texto-suave">Todo despacho tiene su VUCE. Escribe lo que aprobó, especie por especie.</p>
        </div>
        <RejillaCampos>
          <Campo label="N.º de trámite (TM)" htmlFor="vuce-numero">
            {texto("numero", "vuce-numero", 60, "TM-00017927-20260918")}
          </Campo>
          <Campo label="Fecha de solicitud" htmlFor="vuce-solicitud">
            {dia("vuceFechaSolicitud", "vuce-solicitud")}
          </Campo>
          <Campo label="Fecha de aprobación" htmlFor="vuce-aprobacion">
            {dia("fecha", "vuce-aprobacion")}
          </Campo>
        </RejillaCampos>

        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Especie AUNAP</th>
              <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Pedido</th>
              <th className="px-3 py-2 text-right font-medium">Aprobado</th>
              <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Empacado</th>
              <th className="px-3 py-2 font-medium">Estado</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {resumen.filas.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-3 py-6 text-center text-texto-suave">
                  Sin especies con código AUNAP.
                </td>
              </tr>
            ) : null}
            {resumen.filas.map((f) => (
              <tr key={f.especieAunapId} className="border-b border-borde align-top last:border-0">
                <td className="px-3 py-2">
                  <span className="font-mono text-xs text-texto-suave">{f.codigo}</span> <span className="italic text-texto">{f.nombreCientifico}</span>
                  <span className="block text-xs text-texto-suave">{f.productos.join(", ")}</span>
                  <span className="block text-xs text-texto-suave md:hidden">
                    Pedido {entero(f.solicitado)} · empacado {entero(f.empacado)}
                  </span>
                </td>
                <td className="hidden px-3 py-2 text-right tabular-nums md:table-cell">{entero(f.solicitado)}</td>
                <td className="px-3 py-2 text-right">
                  <input
                    type="number"
                    min={0}
                    inputMode="numeric"
                    aria-label={`Cantidad aprobada de ${f.nombreCientifico}`}
                    className={`${inputClass} w-24 text-right`}
                    value={autorizados[f.especieAunapId] ?? ""}
                    onChange={(ev) => setAutorizados((a) => ({ ...a, [f.especieAunapId]: ev.target.value }))}
                    disabled={!puede}
                  />
                </td>
                <td className="hidden px-3 py-2 text-right tabular-nums md:table-cell">{entero(f.empacado)}</td>
                <td className="px-3 py-2">
                  <Etiqueta texto={ESTADOS_AUNAP[f.estado]} tono={TONO[f.estado]} />
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
        {puede && resumen.filas.length > 0 ? (
          <button
            type="button"
            className={`${botonSecundarioClass} w-full sm:w-fit`}
            onClick={() => setAutorizados(Object.fromEntries(resumen.filas.map((f) => [f.especieAunapId, String(f.solicitado)])))}
          >
            Copiar lo pedido
          </button>
        ) : null}
        <p className="text-xs text-texto-suave">Si lo empacado supera lo aprobado, pide la ampliación antes de despachar.</p>
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">3 · DEX y factura DIAN</h2>
        <RejillaCampos>
          <Campo label="N.º documento DEX" htmlFor="dex">
            {texto("dexNumero", "dex", 40)}
          </Campo>
          <Campo label="Factura DIAN (Siigo)" htmlFor="factura-dian" hint="La factura electrónica, ej. JRE437.">
            {texto("facturaDian", "factura-dian", 40)}
          </Campo>
        </RejillaCampos>
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">4 · CITES (si aplica)</h2>
        <RejillaCampos>
          <Campo label="N.º CITES" htmlFor="cites">
            {texto("citesNumero", "cites", 60)}
          </Campo>
          <Campo label="Fecha de aprobación CITES" htmlFor="cites-fecha">
            {dia("citesFecha", "cites-fecha")}
          </Campo>
        </RejillaCampos>
      </section>

      {mensaje ? <Aviso texto={mensaje.texto} error={mensaje.error} /> : null}
      {puede ? (
        <button type="button" disabled={pendiente} className={`${botonPrimarioClass} w-full sm:w-fit`} onClick={guardar}>
          {pendiente ? "Guardando…" : "Guardar trámites"}
        </button>
      ) : null}
    </div>
  );
}
