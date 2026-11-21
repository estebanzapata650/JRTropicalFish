import type { Metadata } from "next";
import Link from "next/link";
import { AlertTriangle } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { apiFetch } from "@/lib/api";
import { pct, piezas, type TableroBodega } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { Kpi } from "../crm/comun";
import { DisponibilidadPorEspecie } from "./DisponibilidadPorEspecie";
import { UbicacionesAcuario } from "./UbicacionesAcuario";
import { Panel, PestanasTableros } from "./tableros";

export const metadata: Metadata = { title: "Inventario · Bodega" };

/**
 * Tablero «Bodega» (Looker: Inventario y bodega): cuánto hay y dónde. Lo que llega y lo que sale vive en
 * las pestañas Proveedores y Clientes; el recorrido de cada lote, en Trazabilidad.
 */
export default async function InventarioPage() {
  const { token } = await exigirPermiso(PERMISOS.INVENTARIO_LEER);
  const d = await apiFetch<TableroBodega>("/api/v1/inventario/tableros/bodega", { token });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Inventario" descripcion="Cuánto hay, en qué estado y en qué acuario." />
      <PestanasTableros actual="/inventario" />

      <section aria-label="Totales de bodega" className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-6">
        <Kpi titulo="En acuarios" valor={piezas(d.sanos)} detalle="Sanos y vendibles" />
        <Kpi titulo="En aclimatación" valor={piezas(d.aclimatacion)} detalle="Recibidos, aún sin acuario" />
        <Kpi titulo="Enfermos" valor={piezas(d.enfermos)} alerta={d.enfermos > 0} detalle={`${d.acuariosConEnfermos} ${d.acuariosConEnfermos === 1 ? "acuario" : "acuarios"} en cuarentena`} />
        <Kpi titulo="Ocupación de acuarios" valor={pct(d.ocupacion)} avance={d.ocupacion} detalle={`${d.acuariosOcupados} de ${d.acuariosActivos} · ${d.acuariosVacios} vacíos`} />
        <Kpi titulo="Lotes activos" valor={piezas(d.lotesActivos)} detalle="Con peces en bodega" />
        <Kpi
          titulo="Lotes de más de 20 días"
          valor={piezas(d.lotesAntiguos)}
          alerta={d.lotesAntiguos > 0}
          detalle={
            <Link href="/inventario/lotes?antiguos=1" className="text-acento hover:underline">
              Ver lotes
            </Link>
          }
        />
      </section>

      <DisponibilidadPorEspecie productos={d.productos} />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[3fr_2fr]">
        <Panel titulo="Ubicaciones por acuario" ayuda="Los acuarios con más peces primero." accion={<Link href="/inventario/acuarios" className="text-acento hover:underline">Ver mapa</Link>}>
          <UbicacionesAcuario ubicaciones={d.ubicaciones} />
        </Panel>

        <Panel titulo="Acuarios con peces enfermos" ayuda="En cuarentena: no se pueden despachar.">
          {d.existenciasEnfermas.length === 0 ? (
            <p className="text-sm text-texto-suave">No hay peces enfermos.</p>
          ) : (
            <ul className="flex flex-col divide-y divide-borde">
              {d.existenciasEnfermas.map((e) => (
                <li key={e.id} className="flex items-center justify-between gap-3 py-2.5">
                  <div className="min-w-0">
                    <p className="truncate text-sm text-texto">
                      <AlertTriangle className="mr-1 inline size-3.5 text-peligro" aria-hidden />
                      <span className="font-mono">{e.acuarioCodigo ?? "Aclimatación"}</span> · <span className="italic">{e.nombreCientifico}</span>
                    </p>
                    <Link href={`/inventario/lotes/${e.loteId}`} className="text-xs text-acento hover:underline">
                      {e.loteCodigo} · ver recorrido
                    </Link>
                  </div>
                  <span className="shrink-0 text-sm font-semibold tabular-nums text-peligro">{piezas(e.cantidad)}</span>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>
    </div>
  );
}
