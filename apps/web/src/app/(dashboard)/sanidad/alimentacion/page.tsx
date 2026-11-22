import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { Alimento, Dieta, FichaResumen } from "@/lib/sanidad";
import type { ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { AlimentacionCliente } from "./AlimentacionCliente";

export const metadata: Metadata = { title: "Alimentos y dietas" };

export default async function AlimentacionPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const [alimentos, dietas, grupos, productos] = await Promise.all([
    apiFetch<Alimento[]>("/api/v1/sanidad/alimentos", { token }),
    apiFetch<Dieta[]>("/api/v1/sanidad/dietas", { token }),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/grupos-producto?soloActivos=true", { token }),
    apiFetch<FichaResumen[]>("/api/v1/sanidad/fichas", { token }),
  ]);
  return (
    <AlimentacionCliente
      alimentos={alimentos}
      dietas={dietas}
      grupos={grupos.map((g) => ({ id: g.id, nombre: g.nombre }))}
      productos={productos.map((p) => ({ id: p.productoId, nombre: `${p.productoCodigo} · ${p.nombreCientifico}` }))}
      gestionar={permisos.includes(PERMISOS.SANIDAD_GESTIONAR)}
    />
  );
}
