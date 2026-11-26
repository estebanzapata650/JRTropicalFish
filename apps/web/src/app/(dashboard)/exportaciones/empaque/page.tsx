import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CostosEmpaque, DoaEmpaque, EstandarEmpaque } from "@/lib/exportaciones";
import { PERMISOS } from "@/lib/permisos";
import { exigirPermiso } from "@/lib/usuario-actual";
import { EmpaqueCliente, type VistaEmpaque } from "./EmpaqueCliente";

export const metadata: Metadata = { title: "Empaque" };

const VISTAS: VistaEmpaque[] = ["estandares", "costos", "doa"];
const fechaValida = (v: string | undefined) => (v && /^\d{4}-\d{2}-\d{2}$/.test(v) ? v : undefined);

/**
 * Empaque: estándar por especie y talla (bolsa, peces por bolsa, litros de agua, aditivos), costos de empaque y DOA
 * por forma de empaque. `?vista=estandares|costos|doa`; en DOA, `?desde=&hasta=` (por defecto, los últimos 12 meses).
 */
export default async function EmpaquePage({ searchParams }: { searchParams: Promise<{ vista?: string; desde?: string; hasta?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.EXPORTACIONES_LEER);
  const { vista, desde, hasta } = await searchParams;
  const d = fechaValida(desde), h = fechaValida(hasta);
  const consulta = new URLSearchParams({ ...(d ? { desde: d } : {}), ...(h ? { hasta: h } : {}) }).toString();
  const [estandares, costos, doa] = await Promise.all([
    apiFetch<EstandarEmpaque[]>("/api/v1/exportaciones/empaque/estandares", { token }),
    apiFetch<CostosEmpaque>("/api/v1/exportaciones/empaque/costos", { token }),
    apiFetch<DoaEmpaque[]>(`/api/v1/exportaciones/empaque/doa${consulta ? `?${consulta}` : ""}`, { token }),
  ]);
  return (
    <EmpaqueCliente
      vista={VISTAS.includes(vista as VistaEmpaque) ? (vista as VistaEmpaque) : "estandares"}
      estandares={estandares}
      costos={costos}
      doa={doa}
      desde={d ?? null}
      hasta={h ?? null}
      configurar={permisos.includes(PERMISOS.EXPORTACIONES_CONFIGURAR)}
    />
  );
}
