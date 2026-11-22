import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { PERMISOS } from "@/lib/permisos";
import type { FichaResumen } from "@/lib/sanidad";
import { exigirPermiso } from "@/lib/usuario-actual";
import { FichasCliente } from "./FichasCliente";

export const metadata: Metadata = { title: "Fichas técnicas" };

export default async function FichasPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const fichas = await apiFetch<FichaResumen[]>("/api/v1/sanidad/fichas", { token });
  return <FichasCliente fichas={fichas} gestionar={permisos.includes(PERMISOS.SANIDAD_GESTIONAR)} />;
}
