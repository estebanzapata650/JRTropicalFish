"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { cifra, type EmbarqueDetalle, type Tarifas } from "@/lib/exportaciones";
import { botonPrimarioClass, inputClass } from "@/lib/estilos";
import { guardarEmbarqueAction } from "../actions";
import { Aviso, BotonesDocumento, ResumenTotales, cuerpoBase, numero } from "./comun";

/** Etiquetas de las tarifas, en el orden de la hoja Parametros del Excel. */
export const CAMPOS_TARIFA: { campo: keyof Tarifas; etiqueta: string; ayuda?: string }[] = [
  { campo: "tarifaKgCliente", etiqueta: "Tarifa por kg al cliente" },
  { campo: "tarifaKgJr", etiqueta: "Tarifa por kg de la agencia a JR", ayuda: "Referencia interna." },
  { campo: "fuelKg", etiqueta: "Fuel surcharge por kg" },
  { campo: "jpFeeKg", etiqueta: "JP fee por kg" },
  { campo: "vscKg", etiqueta: "VSC por kg" },
  { campo: "vscMinimo", etiqueta: "VSC mínimo" },
  { campo: "dbcPorcentaje", etiqueta: "DBC (%)", ayuda: "Sobre el cargo por peso." },
  { campo: "dbcMinimo", etiqueta: "DBC mínimo" },
  { campo: "dfPorcentaje", etiqueta: "DF (%)", ayuda: "Sobre el due agent." },
  { campo: "dfMinimo", etiqueta: "DF mínimo" },
  { campo: "dueAgentCliente", etiqueta: "Due agent al cliente" },
  { campo: "dueAgentJr", etiqueta: "Due agent de la agencia a JR", ayuda: "Referencia interna." },
  { campo: "dueCarrier", etiqueta: "Due carrier", ayuda: "Vet fee, transfer fee, bee fee, HAWB…" },
];

/** Paso 2: flete estimado (hoja Est. Freight Costs) con las tarifas propias del embarque. */
export function PasoFlete({ d, puede, puedeEnviar }: { d: EmbarqueDetalle; puede: boolean; puedeEnviar: boolean }) {
  const router = useRouter();
  const f = d.calculos.flete;
  const [valores, setValores] = useState(() => Object.fromEntries(CAMPOS_TARIFA.map((c) => [c.campo, String(d.tarifas[c.campo])])));
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const horas = d.embarque.salidaVuelo && d.embarque.llegadaVuelo ? (new Date(d.embarque.llegadaVuelo).getTime() - new Date(d.embarque.salidaVuelo).getTime()) / 3600000 : null;

  return (
    <div className="flex flex-col gap-6">
      <Aviso texto={aviso} />
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <section aria-labelledby="titulo-peso" className="flex flex-col gap-3">
          <h2 id="titulo-peso" className="text-base font-semibold text-texto">
            Peso del envío
          </h2>
          <TablaResponsive compactaEnMovil>
            <EncabezadoTabla>
              <tr className="text-texto-suave">
                <th className="px-3 py-2 font-medium">Cajas</th>
                <th className="px-3 py-2 text-right font-medium">Kg/caja</th>
                <th className="px-3 py-2 text-right font-medium">Kg</th>
              </tr>
            </EncabezadoTabla>
            <tbody>
              {f.pesos.map((p) => (
                <tr key={p.descripcion} className="border-b border-borde last:border-0">
                  <td className="px-3 py-2 text-texto">
                    {p.cajas} × {p.descripcion}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-texto">{cifra(p.kgPorCaja)}</td>
                  <td className="px-3 py-2 text-right tabular-nums text-texto">{cifra(p.kg)}</td>
                </tr>
              ))}
              {f.pesos.length === 0 ? (
                <tr>
                  <td colSpan={3} className="px-3 py-4 text-center text-sm text-texto-suave">
                    Indica las cajas estimadas en el paso 1.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </TablaResponsive>
          <p className="text-sm text-texto-suave">
            Total {cifra(f.kg)} kg{horas != null ? ` · vuelo de ${cifra(horas, 1)} h` : ""}.
          </p>
          <ResumenTotales
            filas={[
              { concepto: "Cargo por peso", detalle: "tarifa + fuel + JP fee", total: f.cargoPeso },
              { concepto: "VSC", total: f.vsc },
              { concepto: "DBC", total: f.dbc },
              { concepto: "DF", total: f.df },
              { concepto: "Due agent", total: f.dueAgent },
              { concepto: "Due carrier", total: f.dueCarrier },
              { concepto: "Flete estimado (AWB)", total: f.total, fuerte: true },
            ]}
          />
          <BotonesDocumento embarqueId={d.embarque.id} tipo="flete" correoCliente={d.clienteEmail} puedeEnviar={puedeEnviar} onEnviado={setAviso} />
        </section>

        <form
          onSubmit={(ev) => {
            ev.preventDefault();
            iniciar(async () => {
              const tarifas = Object.fromEntries(CAMPOS_TARIFA.map((c) => [c.campo, numero(valores[c.campo]) ?? 0]));
              const r = await guardarEmbarqueAction(d.embarque.id, { ...cuerpoBase(d), tarifas });
              if (r.error) {
                setError(r.error);
                return;
              }
              setError(null);
              setAviso("Tarifas del embarque guardadas.");
              router.refresh();
            });
          }}
          className="flex flex-col gap-3"
        >
          <h2 className="text-base font-semibold text-texto">Tarifas de este embarque (USD)</h2>
          <p className="text-sm text-texto-suave">Se copiaron de Configuración al crear el embarque; ajústalas con la cotización de la agencia.</p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {CAMPOS_TARIFA.map((c) => (
              <label key={c.campo} className="flex flex-col gap-1.5 text-sm text-texto">
                {c.etiqueta}
                <input
                  type="number"
                  inputMode="decimal"
                  min={0}
                  step="0.0001"
                  disabled={!puede}
                  value={valores[c.campo]}
                  onChange={(ev) => setValores((v) => ({ ...v, [c.campo]: ev.target.value }))}
                  className={inputClass}
                />
                {c.ayuda ? <span className="text-xs text-texto-suave">{c.ayuda}</span> : null}
              </label>
            ))}
          </div>
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
          {puede ? (
            <div className="flex justify-end">
              <button type="submit" disabled={guardando} className={botonPrimarioClass}>
                {guardando ? "Guardando…" : "Guardar tarifas"}
              </button>
            </div>
          ) : null}
        </form>
      </div>
    </div>
  );
}

/** Paso 3: orden de cargue para bodega (qué especie va en qué caja y cómo se empaca). */
export function PasoOrdenCargue({ d }: { d: EmbarqueDetalle }) {
  const filas = d.calculos.ordenCargue;
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-texto-suave">
        Sale de las especies del paso 1 en su orden, con su fracción de caja y su empaque. Para cambiarla, edita las especies.
      </p>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-3 py-2 font-medium">Caja</th>
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="px-3 py-2 text-right font-medium">Cantidad</th>
            <th className="hidden px-3 py-2 font-medium sm:table-cell">Empaque</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Notas</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filas.map((o) => (
            <tr key={o.orden} className="border-b border-borde last:border-0">
              <td className="px-3 py-2 whitespace-nowrap font-medium text-texto">{o.cajas}</td>
              <td className="px-3 py-2 text-texto">
                <span className="mr-1 font-mono text-xs text-texto-suave">{o.productoCodigo}</span>
                {o.especie}
                {o.empaque ? <p className="text-xs text-texto-suave sm:hidden">{o.empaque}</p> : null}
              </td>
              <td className="px-3 py-2 text-right tabular-nums text-texto">{o.cantidad.toLocaleString("es-CO")}</td>
              <td className="hidden px-3 py-2 text-texto sm:table-cell">{o.empaque ?? "—"}</td>
              <td className="hidden px-3 py-2 text-texto-suave md:table-cell">{o.nota ?? ""}</td>
            </tr>
          ))}
          {filas.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-3 py-6 text-center text-sm text-texto-suave">
                Agrega especies en el paso 1.
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <BotonesDocumento embarqueId={d.embarque.id} tipo="orden_cargue" correoCliente={null} puedeEnviar={false} />
    </div>
  );
}
