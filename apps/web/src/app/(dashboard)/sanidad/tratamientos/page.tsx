import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Acuario } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import type { Medicamento, Protocolo, Tratamiento } from "@/lib/sanidad";
import { exigirPermiso } from "@/lib/usuario-actual";
import { TratamientosCliente } from "./TratamientosCliente";

export const metadata: Metadata = { title: "Tratamientos" };

/** Tratamientos por acuario. `?id=` abre la ficha; `?todos=1` incluye los terminados fuera de retiro y cancelados. */
export default async function TratamientosPage({ searchParams }: { searchParams: Promise<{ id?: string; todos?: string }> }) {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const { id, todos } = await searchParams;
  const [tratamientos, protocolos, medicamentos, acuarios] = await Promise.all([
    apiFetch<Tratamiento[]>(`/api/v1/sanidad/tratamientos?vigentes=${todos ? "false" : "true"}`, { token }),
    apiFetch<Protocolo[]>("/api/v1/sanidad/protocolos", { token }),
    apiFetch<Medicamento[]>("/api/v1/sanidad/medicamentos", { token }),
    apiFetch<Acuario[]>("/api/v1/inventario/acuarios", { token }),
  ]);
  return (
    <TratamientosCliente
      tratamientos={tratamientos}
      protocolos={protocolos.filter((p) => p.estado === "aprobado")}
      medicamentos={medicamentos}
      acuarios={acuarios.filter((a) => a.activo).map((a) => ({ id: a.id, codigo: a.codigo, litros: a.capacidadLitros }))}
      abiertoId={typeof id === "string" && /^[0-9a-f-]{36}$/i.test(id) ? id : null}
      todos={Boolean(todos)}
      operar={permisos.includes(PERMISOS.SANIDAD_OPERAR)}
      gestionar={permisos.includes(PERMISOS.SANIDAD_GESTIONAR)}
    />
  );
}
