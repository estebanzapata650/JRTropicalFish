import type { Metadata } from "next";
import { apiFetch } from "@/lib/api";
import { hoyNegocio } from "@/lib/formato";
import { PERMISOS } from "@/lib/permisos";
import type { Empleado, OpcionMaestro, ValorMaestro } from "@/lib/types";
import { exigirPermiso } from "@/lib/usuario-actual";
import { EmpleadosCliente } from "./EmpleadosCliente";

export const metadata: Metadata = { title: "Empleados" };

export default async function EmpleadosPage() {
  const { token, permisos } = await exigirPermiso(PERMISOS.EMPLEADOS_LEER);

  // Opciones de los selectores: solo valores activos (la API los entrega a cualquier usuario con
  // sesión). El valor inactivo que ya tenga un empleado lo agrega el formulario al editarlo.
  const opciones = (clave: string) =>
    apiFetch<ValorMaestro[]>(`/api/v1/maestros/${clave}?soloActivos=true`, { token }).then((valores) =>
      valores.map(({ id, codigo, nombre }): OpcionMaestro => ({ id, codigo, nombre })),
    );

  const [empleados, tiposIdentificacion, cargos, areas, tiposContrato] = await Promise.all([
    apiFetch<Empleado[]>("/api/v1/administracion/empleados", { token }),
    opciones("tipos-identificacion"),
    opciones("cargos"),
    opciones("areas"),
    opciones("tipos-contrato"),
  ]);

  return (
    <EmpleadosCliente
      empleados={empleados}
      opciones={{ tiposIdentificacion, cargos, areas, tiposContrato }}
      puedeCrear={permisos.includes(PERMISOS.EMPLEADOS_CREAR)}
      puedeEditar={permisos.includes(PERMISOS.EMPLEADOS_EDITAR)}
      puedeEliminar={permisos.includes(PERMISOS.EMPLEADOS_ELIMINAR)}
      puedeVerSalario={permisos.includes(PERMISOS.EMPLEADOS_VER_SALARIO)}
      puedeAdministrarMaestros={permisos.includes(PERMISOS.MAESTROS_EDITAR)}
      hoy={hoyNegocio()}
    />
  );
}
