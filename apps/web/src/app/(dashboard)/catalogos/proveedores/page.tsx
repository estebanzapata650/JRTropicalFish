import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { CentroAcopio, Proveedor } from "@/lib/catalogos";
import { PERMISOS } from "@/lib/permisos";
import type { ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { ProveedoresCliente } from "./ProveedoresCliente";

export const metadata: Metadata = { title: "Proveedores" };

export default async function ProveedoresPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.PROVEEDORES_LEER);
  const [proveedores, centros, tipos] = await Promise.all([
    apiFetch<Proveedor[]>("/api/v1/catalogos/proveedores", { token }),
    apiFetch<CentroAcopio[]>("/api/v1/catalogos/centros-acopio", { token }),
    apiFetch<ValorMaestro[]>("/api/v1/maestros/tipos-identificacion?soloActivos=true", { token }),
  ]);
  return (
    <ProveedoresCliente
      proveedores={proveedores}
      centros={centros.map(({ id, nombre, activo }) => ({ id, nombre, activo }))}
      tipos={tipos.map(({ id, codigo, nombre }) => ({ id, codigo, nombre }))}
      puedeEditar={permisos.includes(PERMISOS.PROVEEDORES_EDITAR)}
    />
  );
}
