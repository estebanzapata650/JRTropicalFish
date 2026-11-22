import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CentroAcopio } from "@/lib/catalogos";
import { PERMISOS } from "@/lib/permisos";
import type { FichaResumen, Medicamento, Protocolo } from "@/lib/sanidad";
import type { ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ProtocolosCliente } from "./ProtocolosCliente";

export const metadata: Metadata = { title: "Protocolos de cuarentena" };

export default async function ProtocolosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const [protocolos, medicamentos, productos, grupos, cuencas, centros] = await Promise.all([
    apiFetch<Protocolo[]>("/api/v1/sanidad/protocolos", { token }),
    apiFetch<Medicamento[]>("/api/v1/sanidad/medicamentos", { token }),
    apiFetch<FichaResumen[]>("/api/v1/sanidad/fichas", { token }),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/grupos-producto?soloActivos=true", { token }),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/cuencas?soloActivos=true", { token }),
    apiFetch<CentroAcopio[]>("/api/v1/catalogos/centros-acopio", { token }).catch(() => [] as CentroAcopio[]),
  ]);
  return (
    <ProtocolosCliente
      protocolos={protocolos}
      medicamentos={medicamentos}
      productos={productos.map((p) => ({ id: p.productoId, nombre: `${p.productoCodigo} · ${p.nombreCientifico}` }))}
      grupos={grupos.map((g) => ({ id: g.id, nombre: g.nombre }))}
      cuencas={cuencas.map((c) => ({ id: c.id, nombre: c.nombre }))}
      centros={centros.filter((c) => c.activo).map((c) => ({ id: c.id, nombre: c.nombre }))}
      gestionar={permisos.includes(PERMISOS.SANIDAD_GESTIONAR)}
      aprobar={permisos.includes(PERMISOS.SANIDAD_APROBAR_PROTOCOLO)}
    />
  );
}
