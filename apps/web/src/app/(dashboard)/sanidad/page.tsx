import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Acuario } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import type { MiDia } from "@/lib/sanidad";
import { exigirPermiso } from "@/lib/usuario-actual";
import { MiDiaCliente } from "./MiDiaCliente";

export const metadata: Metadata = { title: "Mi día en bodega" };

/** Lo que el operario hace hoy: llegadas, dosis ya calculadas, ronda de alimentación (semáforo) y salidas. */
export default async function MiDiaPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const [dia, acuarios] = await Promise.all([
    apiFetch<MiDia>("/api/v1/sanidad/mi-dia", { token }),
    apiFetch<Acuario[]>("/api/v1/inventario/acuarios", { token }),
  ]);
  return (
    <MiDiaCliente
      dia={dia}
      acuarios={acuarios.filter((a) => a.activo).map((a) => ({ id: a.id, codigo: a.codigo, litros: a.capacidadLitros }))}
      operar={permisos.includes(PERMISOS.SANIDAD_OPERAR)}
      ubicar={permisos.includes(PERMISOS.INVENTARIO_OPERAR)}
    />
  );
}
