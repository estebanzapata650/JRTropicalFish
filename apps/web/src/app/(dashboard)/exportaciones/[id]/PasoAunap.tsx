"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { TriangleAlert } from "lucide-react";
import { Campo, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive, type TonoEtiqueta } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import { ESTADOS_AUNAP, type EmbarqueDetalle, type FilaAunap, type ResumenAunap } from "@/lib/exportaciones";
import { useDetalle } from "../../compras/useDetalle";
import { guardarPermisoAunapAction } from "../actions";
import { Aviso, BotonesDocumento } from "./comun";

const TONO: Record<FilaAunap["estado"], TonoEtiqueta> = { sin_permiso: "neutro", no_autorizada: "peligro", excede: "peligro", ok: "exito" };

const entero = (n: number | null | undefined) => (n === null || n === undefined ? "—" : n.toLocaleString("es-CO"));

/**
 * Permiso AUNAP y documentos para autoridades. Todo sale de lo real: la solicitud (proforma) de las especies
 * del pedido; el packing list de aduana y la custom invoice del empaque. Cada especie se declara con su código y
 * nombre de la resolución; una especie sin código no se puede declarar.
 */
export function PasoAunap({ d, puedeGestionar }: { d: EmbarqueDetalle; puedeGestionar: boolean }) {
  const e = d.embarque;
  const [version, setVersion] = useState(0);
  const { cargando, datos: resumen, error } = useDetalle<ResumenAunap>(`/api/exportaciones/embarques/${e.id}/aunap`, version);

  if (cargando) return <p className="text-sm text-texto-suave">Cargando…</p>;
  if (error || !resumen) return <p className="text-sm text-peligro">{error ?? "No se pudo cargar."}</p>;

  const bloqueado = resumen.sinCodigo.length > 0;
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

      <PermisoForm key={`${resumen.permiso?.actualizadoEn ?? "nuevo"}-${version}`} embarqueId={e.id} resumen={resumen} puede={puedeGestionar && e.estado !== "cancelado"} onGuardado={() => setVersion((v) => v + 1)} />

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">Documentos para autoridades</h2>
        <p className="text-sm text-texto-suave">Salen con las especies, cantidades y valores reales del embarque, con el código y el nombre de la Resolución AUNAP.</p>
        <Documento
          titulo="Proforma · solicitud de permiso AUNAP"
          detalle="Las especies y cantidades del pedido, con los precios de la preinvoice."
          embarqueId={e.id}
          tipo="solicitud_aunap"
          motivo={bloqueado ? "Hay especies sin código AUNAP." : d.lineas.length === 0 ? "El embarque no tiene especies." : null}
        />
        <Documento
          titulo="Packing list de aduana y aeropuerto"
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
      </section>
    </div>
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

function PermisoForm({ embarqueId, resumen, puede, onGuardado }: { embarqueId: string; resumen: ResumenAunap; puede: boolean; onGuardado: () => void }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{ texto: string; error: boolean } | null>(null);
  const [numeroPermiso, setNumeroPermiso] = useState(resumen.permiso?.numero ?? "");
  const [fecha, setFecha] = useState(resumen.permiso?.fecha ?? "");
  const [autorizados, setAutorizados] = useState<Record<string, string>>(
    Object.fromEntries(resumen.filas.map((f) => [f.especieAunapId, f.autorizado === null ? "" : String(f.autorizado)])),
  );

  const guardar = () =>
    iniciar(async () => {
      const r = await guardarPermisoAunapAction(embarqueId, {
        numero: numeroPermiso || null,
        fecha: fecha || null,
        notas: resumen.permiso?.notas ?? null,
        lineas: resumen.filas.map((f) => ({ especieAunapId: f.especieAunapId, cantidad: Number(autorizados[f.especieAunapId] || 0) })),
      });
      setMensaje(r.error ? { texto: r.error, error: true } : { texto: "Permiso guardado.", error: false });
      if (!r.error) {
        onGuardado();
        router.refresh();
      }
    });

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-base font-semibold text-texto">Permiso AUNAP</h2>
      <RejillaCampos>
        <Campo label="Número del permiso" htmlFor="permiso-numero">
          <input id="permiso-numero" className={inputClass} value={numeroPermiso} onChange={(ev) => setNumeroPermiso(ev.target.value)} maxLength={60} disabled={!puede} />
        </Campo>
        <Campo label="Fecha del permiso" htmlFor="permiso-fecha">
          <input id="permiso-fecha" type="date" className={inputClass} value={fecha} onChange={(ev) => setFecha(ev.target.value)} disabled={!puede} />
        </Campo>
      </RejillaCampos>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr>
            <th className="px-3 py-2 font-medium">Especie AUNAP</th>
            <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Pedido</th>
            <th className="px-3 py-2 text-right font-medium">Autorizado</th>
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
                  aria-label={`Cantidad autorizada de ${f.nombreCientifico}`}
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

      {mensaje ? <Aviso texto={mensaje.texto} error={mensaje.error} /> : null}
      {puede ? (
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            className={botonSecundarioClass}
            onClick={() => setAutorizados(Object.fromEntries(resumen.filas.map((f) => [f.especieAunapId, String(f.solicitado)])))}
          >
            Copiar lo pedido
          </button>
          <button type="button" disabled={pendiente} className={botonPrimarioClass} onClick={guardar}>
            Guardar permiso
          </button>
        </div>
      ) : null}
      <p className="text-xs text-texto-suave">Escribe lo que autoriza el acto de la AUNAP. Si lo empacado supera lo autorizado, pide la ampliación antes de despachar.</p>
    </section>
  );
}
