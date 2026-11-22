import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import type { Acuario } from "@/lib/inventario";
import { PERMISOS } from "@/lib/permisos";
import type { Protocolo, TableroSanidad } from "@/lib/sanidad";
import { exigirPermiso } from "@/lib/usuario-actual";
import { TableroSanidadCliente } from "./TableroSanidadCliente";

export const metadata: Metadata = { title: "Tablero de acuarios" };

export default async function TableroSanidadPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.SANIDAD_LEER);
  const [tablero, protocolos, acuarios] = await Promise.all([
    apiFetch<TableroSanidad>("/api/v1/sanidad/tablero", { token }),
    apiFetch<Protocolo[]>("/api/v1/sanidad/protocolos", { token }),
    apiFetch<Acuario[]>("/api/v1/inventario/acuarios", { token }),
  ]);
  return (
    <TableroSanidadCliente
      tablero={tablero}
      protocolos={protocolos.filter((p) => p.estado === "aprobado").map((p) => ({ id: p.id, nombre: `${p.codigo} · ${p.nombre}`, dias: p.diasCuarentena }))}
      acuarios={acuarios.filter((a) => a.activo).map((a) => ({ id: a.id, codigo: a.codigo }))}
      operar={permisos.includes(PERMISOS.SANIDAD_OPERAR)}
      ubicar={permisos.includes(PERMISOS.INVENTARIO_OPERAR)}
    />
  );
}
