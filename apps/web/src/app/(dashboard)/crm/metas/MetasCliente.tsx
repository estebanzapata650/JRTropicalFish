"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/PageHeader";
import { Campo, RejillaCampos, Seccion } from "@/components/campos";
import type { MetaMes, ParametrosComerciales } from "@/lib/crm";
import { botonPrimarioClass, inputClass } from "@/lib/estilos";
import { moneda } from "@/lib/formato";
import { guardarMetasAction, guardarParametrosAction } from "../actions";

const numero = (texto: string) => (texto.trim() === "" ? 0 : Number(texto.replace(",", ".")));

/**
 * Metas mensuales por comercial y parámetros comerciales de la empresa. Los comerciales son los
 * usuarios con un perfil que tiene «crm:vender»; un comercial sin meta en el mes usa la meta por
 * defecto en el dashboard.
 */
export function MetasCliente({ mes, metas, parametros }: { mes: string; metas: MetaMes[]; parametros: ParametrosComerciales }) {
  const router = useRouter();
  const [valores, setValores] = useState<Record<string, string>>(() =>
    Object.fromEntries(metas.map((m) => [m.comercialId, m.metaCop == null ? "" : String(m.metaCop)])),
  );
  const [p, setP] = useState(() => ({
    metaEmpresaCop: String(parametros.metaEmpresaCop),
    metaComercialCop: String(parametros.metaComercialCop),
    puntoEquilibrioDespachos: String(parametros.puntoEquilibrioDespachos),
    porcentajeComision: String(parametros.porcentajeComision),
    umbralDoa: String(parametros.umbralDoa),
    metaClientesNuevos: String(parametros.metaClientesNuevos),
  }));
  const [mensajeMetas, setMensajeMetas] = useState<{ ok: boolean; texto: string } | null>(null);
  const [mensajeParametros, setMensajeParametros] = useState<{ ok: boolean; texto: string } | null>(null);
  const [guardando, iniciar] = useTransition();

  const sumaMetas = metas.reduce((s, m) => s + (valores[m.comercialId] ? numero(valores[m.comercialId]) : parametros.metaComercialCop), 0);

  const guardarMetas = () =>
    iniciar(async () => {
      const lista = metas
        .filter((m) => valores[m.comercialId]?.trim())
        .map((m) => ({ comercialId: m.comercialId, metaCop: numero(valores[m.comercialId]) }));
      const r = await guardarMetasAction(`${mes}-01`, lista);
      setMensajeMetas(r.error ? { ok: false, texto: r.error } : { ok: true, texto: "Metas guardadas." });
      if (!r.error) router.refresh();
    });

  const guardarParametros = () =>
    iniciar(async () => {
      const r = await guardarParametrosAction({
        metaEmpresaCop: numero(p.metaEmpresaCop),
        metaComercialCop: numero(p.metaComercialCop),
        puntoEquilibrioDespachos: Math.trunc(numero(p.puntoEquilibrioDespachos)),
        porcentajeComision: numero(p.porcentajeComision),
        umbralDoa: numero(p.umbralDoa),
        metaClientesNuevos: Math.trunc(numero(p.metaClientesNuevos)),
      });
      setMensajeParametros(r.error ? { ok: false, texto: r.error } : { ok: true, texto: "Parámetros guardados." });
      if (!r.error) router.refresh();
    });

  const setParam = (campo: keyof typeof p) => (e: React.ChangeEvent<HTMLInputElement>) => setP((x) => ({ ...x, [campo]: e.target.value }));

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Metas comerciales" descripcion="Meta mensual de cada comercial y parámetros del dashboard y las comisiones." />

      <section className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <h2 className="text-base font-semibold text-texto">Metas del mes</h2>
          <label className="flex items-center gap-2 text-sm text-texto-suave">
            Mes
            <input type="month" value={mes} onChange={(e) => e.target.value && router.push(`/crm/metas?mes=${e.target.value}`)} className={`${inputClass} w-44`} />
          </label>
        </div>
        {metas.length === 0 ? (
          <p className="text-sm text-texto-suave">
            No hay comerciales. Un comercial es un usuario cuyo perfil tiene el permiso «Ser comercial» (crm:vender).
          </p>
        ) : (
          <ul className="flex flex-col divide-y divide-borde">
            {metas.map((m) => (
              <li key={m.comercialId} className="flex flex-col gap-2 py-3 sm:flex-row sm:items-center sm:justify-between">
                <label htmlFor={`meta-${m.comercialId}`} className="text-sm font-medium text-texto">
                  {m.comercialNombre}
                </label>
                <div className="flex items-center gap-2 sm:w-72">
                  <span className="text-sm text-texto-suave">COP</span>
                  <input
                    id={`meta-${m.comercialId}`}
                    type="number"
                    inputMode="numeric"
                    min={0}
                    step={100000}
                    value={valores[m.comercialId] ?? ""}
                    placeholder={`${parametros.metaComercialCop} (por defecto)`}
                    onChange={(e) => setValores((v) => ({ ...v, [m.comercialId]: e.target.value }))}
                    className={inputClass}
                  />
                </div>
              </li>
            ))}
          </ul>
        )}
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-texto-suave">
            Suma de metas: <strong className="text-texto tabular-nums">{moneda(sumaMetas)}</strong> · meta de la empresa {moneda(parametros.metaEmpresaCop)}
          </p>
          <button type="button" disabled={guardando || metas.length === 0} onClick={guardarMetas} className={botonPrimarioClass}>
            {guardando ? "Guardando…" : "Guardar metas"}
          </button>
        </div>
        <MensajeEstado m={mensajeMetas} />
      </section>

      <Seccion titulo="Parámetros comerciales">
        <RejillaCampos>
          <Campo label="Meta mensual de la empresa (COP)" htmlFor="metaEmpresa">
            <input id="metaEmpresa" type="number" min={0} step={1000000} value={p.metaEmpresaCop} onChange={setParam("metaEmpresaCop")} className={inputClass} />
          </Campo>
          <Campo label="Meta por comercial, por defecto (COP)" htmlFor="metaComercial">
            <input id="metaComercial" type="number" min={0} step={1000000} value={p.metaComercialCop} onChange={setParam("metaComercialCop")} className={inputClass} />
          </Campo>
          <Campo label="Punto de equilibrio (despachos al mes)" htmlFor="equilibrio">
            <input id="equilibrio" type="number" min={0} step={1} value={p.puntoEquilibrioDespachos} onChange={setParam("puntoEquilibrioDespachos")} className={inputClass} />
          </Campo>
          <Campo label="Comisión sobre la venta neta (%)" htmlFor="comision" hint="Se reparte por igual entre los comerciales con meta.">
            <input id="comision" type="number" min={0} max={100} step="0.1" value={p.porcentajeComision} onChange={setParam("porcentajeComision")} className={inputClass} />
          </Campo>
          <Campo label="Umbral de alerta DOA (%)" htmlFor="umbral">
            <input id="umbral" type="number" min={0} max={100} step="0.1" value={p.umbralDoa} onChange={setParam("umbralDoa")} className={inputClass} />
          </Campo>
          <Campo label="Meta de clientes nuevos al mes" htmlFor="clientesNuevos">
            <input id="clientesNuevos" type="number" min={0} step={1} value={p.metaClientesNuevos} onChange={setParam("metaClientesNuevos")} className={inputClass} />
          </Campo>
        </RejillaCampos>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-end">
          <MensajeEstado m={mensajeParametros} />
          <button type="button" disabled={guardando} onClick={guardarParametros} className={botonPrimarioClass}>
            {guardando ? "Guardando…" : "Guardar parámetros"}
          </button>
        </div>
      </Seccion>
    </div>
  );
}

function MensajeEstado({ m }: { m: { ok: boolean; texto: string } | null }) {
  return m ? <p role="status" className={`text-sm ${m.ok ? "text-exito-texto" : "text-peligro"}`}>{m.texto}</p> : null;
}
