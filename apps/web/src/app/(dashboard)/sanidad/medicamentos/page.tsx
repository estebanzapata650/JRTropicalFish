import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { Medicamento } from "@/lib/sanidad";
import { exigirPermiso } from "@/lib/usuario-actual";
import { MedicamentosCliente } from "./MedicamentosCliente";

export const metadata: Metadata = { title: "Medicamentos" };

export default async function MedicamentosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const medicamentos = await apiFetch<Medicamento[]>("/api/v1/sanidad/medicamentos", { token });
  return <MedicamentosCliente medicamentos={medicamentos} gestionar={permisos.includes(PERMISOS.SANIDAD_GESTIONAR)} />;
}
