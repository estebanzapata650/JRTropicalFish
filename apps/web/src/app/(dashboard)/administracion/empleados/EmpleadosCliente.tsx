"use client";

import { useMemo, useState, useTransition, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Eye, History, Power, PowerOff, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, EncabezadoTabla, EtiquetaEstado, TablaResponsive } from "@/components/campos";
import { botonPrimarioClass, inputClass, selectClass } from "@/lib/estilos";
import { edad, fecha, moneda } from "@/lib/formato";
import type { Empleado } from "@/lib/types";
import { cambiarEstadoEmpleadoAction, eliminarEmpleadoAction, guardarEmpleadoAction } from "../actions";
import { FormularioEmpleado, type OpcionesEmpleado } from "./FormularioEmpleado";
import { FotoEmpleado } from "./FotoEmpleado";

const ETIQUETAS_HISTORIAL = {
  TipoIdentificacion: "Tipo de identificación",
  NumeroIdentificacion: "Número de identificación",
  Nombres: "Nombres",
  Apellidos: "Apellidos",
  Genero: "Género",
  FechaNacimiento: "Fecha de nacimiento",
  Telefono: "Teléfono",
  Email: "Correo",
  Direccion: "Dirección",
  Ciudad: "Ciudad",
  Cargo: "Cargo",
  Area: "Área",
  TipoContrato: "Tipo de contrato",
  FechaIngreso: "Fecha de ingreso",
  FechaRetiro: "Fecha de retiro",
  Salario: "Salario",
  Activo: "Activo",
  Foto: "Foto",
};

type FiltroEstado = "activos" | "inactivos" | "todos";
type Confirmacion = { tipo: "estado" | "eliminar"; empleado: Empleado };

const nombreCompleto = (e: Empleado) => `${e.nombres} ${e.apellidos}`;

export function EmpleadosCliente({
  empleados,
  opciones,
  puedeCrear,
  puedeEditar,
  puedeEliminar,
  puedeVerSalario,
  puedeAdministrarMaestros,
  hoy,
}: {
  empleados: Empleado[];
  opciones: OpcionesEmpleado;
  puedeCrear: boolean;
  puedeEditar: boolean;
  puedeEliminar: boolean;
  puedeVerSalario: boolean;
  puedeAdministrarMaestros: boolean;
  /** "Hoy" de negocio calculado en el servidor (para la edad, sin desajustes al hidratar). */
  hoy: string;
}) {
  const router = useRouter();

  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<FiltroEstado>("activos");
  const [aviso, setAviso] = useState<string | null>(null);

  const [editando, setEditando] = useState<Empleado | null>(null);
  const [creando, setCreando] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [guardando, iniciarGuardado] = useTransition();

  const [fichaDe, setFichaDe] = useState<Empleado | null>(null);
  const [historialDe, setHistorialDe] = useState<Empleado | null>(null);
  const [confirmacion, setConfirmacion] = useState<Confirmacion | null>(null);
  const [errorConfirmacion, setErrorConfirmacion] = useState<string | null>(null);
  const [procesando, iniciarProceso] = useTransition();

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return empleados.filter((e) => {
      if (filtro === "activos" && !e.activo) return false;
      if (filtro === "inactivos" && e.activo) return false;
      if (!texto) return true;
      return (
        nombreCompleto(e).toLowerCase().includes(texto) ||
        e.numeroIdentificacion.toLowerCase().includes(texto) ||
        e.cargoNombre.toLowerCase().includes(texto) ||
        (e.areaNombre ?? "").toLowerCase().includes(texto) ||
        (e.email ?? "").toLowerCase().includes(texto)
      );
    });
  }, [empleados, busqueda, filtro]);

  const abrirEdicion = (empleado: Empleado) => {
    setFichaDe(null);
    setCreando(false);
    setEditando(empleado);
  };

  const cerrarFormulario = () => {
    setCreando(false);
    setEditando(null);
    setErrorFormulario(null);
  };

  const guardar = (formData: FormData) =>
    iniciarGuardado(async () => {
      const resultado = await guardarEmpleadoAction(editando?.id ?? null, puedeVerSalario, formData);
      if (resultado.error && !resultado.id) {
        setErrorFormulario(resultado.error);
        return;
      }
      // Con id y error, el empleado sí se guardó pero la foto no: se cierra para no duplicarlo.
      setAviso(resultado.error ?? null);
      cerrarFormulario();
      router.refresh();
    });

  const cerrarConfirmacion = () => {
    setConfirmacion(null);
    setErrorConfirmacion(null);
  };

  const confirmar = () => {
    if (!confirmacion) return;
    const { empleado } = confirmacion;
    iniciarProceso(async () => {
      const resultado =
        confirmacion.tipo === "estado"
          ? await cambiarEstadoEmpleadoAction(empleado.id, !empleado.activo)
          : await eliminarEmpleadoAction(empleado.id);
      if (resultado.error) {
        setErrorConfirmacion(resultado.error);
        return;
      }
      cerrarConfirmacion();
      router.refresh();
    });
  };

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <PageHeader
        titulo="Empleados"
        descripcion="Información básica y laboral del personal"
        acciones={
          puedeCrear ? (
            <button
              type="button"
              onClick={() => {
                setEditando(null);
                setCreando(true);
              }}
              className={botonPrimarioClass}
            >
              Nuevo empleado
            </button>
          ) : null
        }
      />

      {aviso ? (
        <div className="flex items-start justify-between gap-3 rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          <span>{aviso}</span>
          <button type="button" onClick={() => setAviso(null)} className="font-medium underline">
            Cerrar
          </button>
        </div>
      ) : null}

      <div className="flex flex-col gap-4 sm:flex-row">
        <div className="relative w-full sm:max-w-sm">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input
            type="search"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Buscar por nombre, documento, cargo o área"
            aria-label="Buscar empleados"
            className={`${inputClass} pl-9`}
          />
        </div>
        <select
          value={filtro}
          onChange={(e) => setFiltro(e.target.value as FiltroEstado)}
          aria-label="Filtrar por estado"
          className={`${selectClass} sm:w-44`}
        >
          <option value="activos">Activos</option>
          <option value="inactivos">Inactivos</option>
          <option value="todos">Todos</option>
        </select>
      </div>

      <TablaResponsive>
        <EncabezadoTabla>
          <tr>
            <th className="px-4 py-3 font-medium text-texto">Empleado</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Cargo</th>
            <th className="hidden px-4 py-3 font-medium text-texto lg:table-cell">Teléfono</th>
            <th className="hidden px-4 py-3 font-medium text-texto lg:table-cell">Ingreso</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Estado</th>
            <th className="px-4 py-3">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-4 py-6 text-center text-sm text-texto-suave">
                {empleados.length === 0 ? "Aún no hay empleados registrados." : "Ningún empleado coincide con el filtro."}
              </td>
            </tr>
          ) : null}
          {filtrados.map((empleado) => {
            const nombre = nombreCompleto(empleado);
            return (
              <tr key={empleado.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <div className="flex min-w-0 items-center gap-3">
                    <FotoEmpleado empleado={empleado} />
                    <div className="flex min-w-0 flex-col">
                      <span className="truncate font-medium text-texto">{nombre}</span>
                      <span className="text-sm text-texto-suave">
                        {empleado.tipoIdentificacionCodigo} {empleado.numeroIdentificacion}
                      </span>
                      <span className="truncate text-xs text-texto-suave md:hidden">{empleado.cargoNombre}</span>
                      <span className="mt-1 md:hidden">
                        <EtiquetaEstado activo={empleado.activo} />
                      </span>
                    </div>
                  </div>
                </td>
                <td className="hidden px-4 py-3 md:table-cell">
                  <div className="flex flex-col">
                    <span className="text-texto">{empleado.cargoNombre}</span>
                    {empleado.areaNombre ? <span className="text-xs text-texto-suave">{empleado.areaNombre}</span> : null}
                  </div>
                </td>
                <td className="hidden px-4 py-3 text-texto lg:table-cell">{empleado.telefono ?? "—"}</td>
                <td className="hidden px-4 py-3 text-sm text-texto-suave lg:table-cell">{fecha(empleado.fechaIngreso)}</td>
                <td className="hidden px-4 py-3 md:table-cell">
                  <EtiquetaEstado activo={empleado.activo} />
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1">
                    <BotonIcono accion="otra" icono={Eye} etiqueta={`Ver ficha de ${nombre}`} onClick={() => setFichaDe(empleado)} />
                    {puedeEditar ? (
                      <BotonIcono accion="editar" etiqueta={`Editar a ${nombre}`} onClick={() => abrirEdicion(empleado)} />
                    ) : null}
                    {puedeEliminar ? (
                      <>
                        <BotonIcono
                          accion="otra"
                          icono={empleado.activo ? PowerOff : Power}
                          etiqueta={`${empleado.activo ? "Desactivar" : "Reactivar"} a ${nombre}`}
                          onClick={() => setConfirmacion({ tipo: "estado", empleado })}
                        />
                        <BotonIcono
                          accion="eliminar"
                          etiqueta={`Eliminar a ${nombre}`}
                          onClick={() => setConfirmacion({ tipo: "eliminar", empleado })}
                        />
                      </>
                    ) : null}
                    <BotonIcono
                      accion="otra"
                      icono={History}
                      etiqueta={`Historial de ${nombre}`}
                      onClick={() => setHistorialDe(empleado)}
                    />
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo={editando ? `Editar a ${nombreCompleto(editando)}` : "Nuevo empleado"}
        abierto={creando || editando !== null}
        onCerrar={cerrarFormulario}
        ancho="max-w-5xl"
      >
        {puedeAdministrarMaestros ? (
          <p className="mb-4 text-sm text-texto-suave">
            ¿Falta un cargo, área o tipo de contrato?{" "}
            <Link href="/administracion/maestros" className="font-medium text-texto underline">
              Agrégalo en Maestros
            </Link>
            .
          </p>
        ) : null}
        <FormularioEmpleado
          key={editando?.id ?? "nuevo"}
          empleado={editando}
          opciones={opciones}
          puedeVerSalario={puedeVerSalario}
          hoy={hoy}
          guardando={guardando}
          error={errorFormulario}
          onGuardar={guardar}
          onCancelar={cerrarFormulario}
        />
      </Dialogo>

      <Dialogo
        titulo={fichaDe ? nombreCompleto(fichaDe) : "Ficha"}
        abierto={fichaDe !== null}
        onCerrar={() => setFichaDe(null)}
      >
        {fichaDe ? (
          <FichaEmpleado
            empleado={fichaDe}
            hoy={hoy}
            puedeVerSalario={puedeVerSalario}
            onEditar={puedeEditar ? () => abrirEdicion(fichaDe) : undefined}
          />
        ) : null}
      </Dialogo>

      <DialogoConfirmacion
        abierto={confirmacion !== null}
        titulo={
          confirmacion?.tipo === "eliminar"
            ? "Eliminar empleado"
            : confirmacion?.empleado.activo
              ? "Desactivar empleado"
              : "Reactivar empleado"
        }
        mensaje={
          confirmacion?.tipo === "eliminar" ? (
            <>
              <strong>{nombreCompleto(confirmacion.empleado)}</strong> dejará de aparecer en el sistema. Su historial se
              conserva. Si solo dejó de trabajar, es mejor desactivarlo.
            </>
          ) : confirmacion?.empleado.activo ? (
            <>
              <strong>{nombreCompleto(confirmacion.empleado)}</strong> quedará como inactivo. Puedes reactivarlo cuando
              quieras.
            </>
          ) : (
            <>
              <strong>{confirmacion ? nombreCompleto(confirmacion.empleado) : ""}</strong> volverá a figurar como activo.
            </>
          )
        }
        textoConfirmar={
          confirmacion?.tipo === "eliminar" ? "Eliminar" : confirmacion?.empleado.activo ? "Desactivar" : "Reactivar"
        }
        peligro={confirmacion?.tipo === "eliminar" || confirmacion?.empleado.activo}
        procesando={procesando}
        error={errorConfirmacion}
        onConfirmar={confirmar}
        onCerrar={cerrarConfirmacion}
      />

      <Dialogo
        titulo={historialDe ? `Historial de ${nombreCompleto(historialDe)}` : "Historial"}
        abierto={historialDe !== null}
        onCerrar={() => setHistorialDe(null)}
      >
        {historialDe ? (
          <HistorialRemoto
            ruta={`/api/administracion/empleados/${historialDe.id}/historial`}
            etiquetas={ETIQUETAS_HISTORIAL}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function FichaEmpleado({
  empleado,
  hoy,
  puedeVerSalario,
  onEditar,
}: {
  empleado: Empleado;
  hoy: string;
  puedeVerSalario: boolean;
  onEditar?: () => void;
}) {
  const anios = edad(empleado.fechaNacimiento, hoy);

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col items-center gap-3 text-center sm:flex-row sm:text-left">
        <FotoEmpleado empleado={empleado} tamano="lg" />
        <div className="flex min-w-0 flex-col gap-1">
          <span className="text-lg font-semibold text-texto">{nombreCompleto(empleado)}</span>
          <span className="text-sm text-texto-suave">
            {empleado.cargoNombre}
            {empleado.areaNombre ? ` · ${empleado.areaNombre}` : ""}
          </span>
          <span>
            <EtiquetaEstado activo={empleado.activo} />
          </span>
        </div>
      </div>

      <GrupoFicha titulo="Identificación">
        <Dato etiqueta="Documento">
          {empleado.tipoIdentificacionCodigo} {empleado.numeroIdentificacion}
        </Dato>
        <Dato etiqueta="Fecha de nacimiento">
          {fecha(empleado.fechaNacimiento)}
          {anios !== null ? ` (${anios} años)` : ""}
        </Dato>
        <Dato etiqueta="Género">{empleado.genero ?? "—"}</Dato>
      </GrupoFicha>

      <GrupoFicha titulo="Contacto">
        <Dato etiqueta="Teléfono">{empleado.telefono ?? "—"}</Dato>
        <Dato etiqueta="Correo">{empleado.email ?? "—"}</Dato>
        <Dato etiqueta="Ciudad">{empleado.ciudad ?? "—"}</Dato>
        <Dato etiqueta="Dirección">{empleado.direccion ?? "—"}</Dato>
      </GrupoFicha>

      <GrupoFicha titulo="Datos laborales">
        <Dato etiqueta="Tipo de contrato">{empleado.tipoContratoNombre}</Dato>
        <Dato etiqueta="Fecha de ingreso">{fecha(empleado.fechaIngreso)}</Dato>
        <Dato etiqueta="Fecha de retiro">{fecha(empleado.fechaRetiro)}</Dato>
        {puedeVerSalario ? <Dato etiqueta="Salario mensual">{moneda(empleado.salario)}</Dato> : null}
      </GrupoFicha>

      {onEditar ? (
        <div className="flex justify-end">
          <button type="button" onClick={onEditar} className={botonPrimarioClass}>
            Editar
          </button>
        </div>
      ) : null}
    </div>
  );
}

function GrupoFicha({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold text-texto">{titulo}</h3>
      <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">{children}</dl>
    </section>
  );
}

function Dato({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5">
      <dt className="text-xs text-texto-suave">{etiqueta}</dt>
      <dd className="break-words text-texto">{children}</dd>
    </div>
  );
}
