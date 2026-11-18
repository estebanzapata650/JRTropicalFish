"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { History } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, EncabezadoTabla, Etiqueta, EtiquetaEstado, TablaResponsive } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass } from "@/lib/estilos";
import { etiquetaModulo } from "@/lib/permisos";
import type { PerfilDetalle, PerfilResumen, PermisoCatalogo } from "@/lib/types";
import { eliminarPerfilAction, guardarPerfilAction } from "../actions";

const ETIQUETAS_HISTORIAL = { Nombre: "Nombre", Descripcion: "Descripción", Activo: "Activo", Permisos: "Permisos" };

export function PerfilesCliente({
  perfiles,
  catalogo,
  detalles,
  puedeCrear,
  puedeEditar,
  puedeEliminar,
}: {
  perfiles: PerfilResumen[];
  catalogo: PermisoCatalogo[];
  detalles: Record<string, PerfilDetalle>;
  puedeCrear: boolean;
  puedeEditar: boolean;
  puedeEliminar: boolean;
}) {
  const router = useRouter();
  const [editando, setEditando] = useState<PerfilDetalle | null>(null);
  const [creando, setCreando] = useState(false);
  const [seleccionados, setSeleccionados] = useState<string[]>([]);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [guardando, iniciarGuardado] = useTransition();

  const [eliminandoPerfil, setEliminandoPerfil] = useState<PerfilResumen | null>(null);
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null);
  const [eliminando, iniciarEliminacion] = useTransition();

  const [historialDe, setHistorialDe] = useState<PerfilResumen | null>(null);

  // Los permisos se agrupan por módulo a partir del propio código ("usuarios:crear" → "usuarios").
  const porModulo = useMemo(() => {
    const mapa = new Map<string, PermisoCatalogo[]>();
    for (const permiso of catalogo) {
      const modulo = permiso.codigo.split(":")[0];
      mapa.set(modulo, [...(mapa.get(modulo) ?? []), permiso]);
    }
    return [...mapa.entries()].sort(([a], [b]) => etiquetaModulo(a).localeCompare(etiquetaModulo(b)));
  }, [catalogo]);

  const abierto = creando || editando !== null;

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setErrorFormulario(null);
  };

  const abrirCrear = () => {
    setSeleccionados([]);
    setEditando(null);
    setCreando(true);
  };

  const abrirEditar = (id: string) => {
    const detalle = detalles[id];
    if (!detalle) return;
    setSeleccionados(detalle.permisos);
    setCreando(false);
    setEditando(detalle);
  };

  const guardar = (formData: FormData) =>
    iniciarGuardado(async () => {
      const resultado = await guardarPerfilAction(editando?.id ?? null, formData);
      if (resultado.error) {
        setErrorFormulario(resultado.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  const alternar = (codigos: string[], marcar: boolean) =>
    setSeleccionados((previos) =>
      marcar ? [...new Set([...previos, ...codigos])] : previos.filter((p) => !codigos.includes(p)),
    );

  const eliminar = () => {
    if (!eliminandoPerfil) return;
    iniciarEliminacion(async () => {
      const resultado = await eliminarPerfilAction(eliminandoPerfil.id);
      if (resultado.error) {
        setErrorEliminar(resultado.error);
        return;
      }
      setEliminandoPerfil(null);
      router.refresh();
    });
  };

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <PageHeader
        titulo="Perfiles"
        descripcion="Conjuntos de permisos que se asignan a los usuarios"
        acciones={
          puedeCrear ? (
            <button type="button" onClick={abrirCrear} className={botonPrimarioClass}>
              Nuevo perfil
            </button>
          ) : null
        }
      />

      <TablaResponsive>
        <EncabezadoTabla>
          <tr>
            <th className="px-4 py-3 font-medium text-texto">Perfil</th>
            <th className="px-4 py-3 font-medium text-texto">Permisos</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Usuarios</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Estado</th>
            <th className="px-4 py-3">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {perfiles.map((perfil) => (
            <tr key={perfil.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <div className="flex min-w-0 flex-col gap-0.5">
                  <span className="flex flex-wrap items-center gap-2 font-medium text-texto">
                    {perfil.nombre}
                    {perfil.esPlantilla ? <Etiqueta texto="Base" tono="neutro" /> : null}
                  </span>
                  {perfil.descripcion ? <span className="text-sm text-texto-suave">{perfil.descripcion}</span> : null}
                  <span className="text-xs text-texto-suave md:hidden">
                    {perfil.totalUsuarios} usuario(s) · {perfil.activo ? "Activo" : "Inactivo"}
                  </span>
                </div>
              </td>
              <td className="px-4 py-3 text-texto">
                {perfil.totalPermisos} de {catalogo.length}
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">{perfil.totalUsuarios}</td>
              <td className="hidden px-4 py-3 md:table-cell">
                <EtiquetaEstado activo={perfil.activo} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1">
                  {puedeEditar ? (
                    <BotonIcono accion="editar" etiqueta={`Editar ${perfil.nombre}`} onClick={() => abrirEditar(perfil.id)} />
                  ) : null}
                  {puedeEliminar && !perfil.esPlantilla ? (
                    <BotonIcono
                      accion="eliminar"
                      etiqueta={`Eliminar ${perfil.nombre}`}
                      onClick={() => {
                        setErrorEliminar(null);
                        setEliminandoPerfil(perfil);
                      }}
                    />
                  ) : null}
                  <BotonIcono
                    accion="otra"
                    icono={History}
                    etiqueta={`Historial de ${perfil.nombre}`}
                    onClick={() => setHistorialDe(perfil)}
                  />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo={editando ? `Editar ${editando.nombre}` : "Nuevo perfil"} abierto={abierto} onCerrar={cerrar}>
        <form key={editando?.id ?? "nuevo"} action={guardar} className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Campo
              label="Nombre"
              htmlFor="nombre"
              hint={editando?.esPlantilla ? "Los perfiles base conservan su nombre." : undefined}
            >
              <input
                id="nombre"
                name="nombre"
                required
                minLength={2}
                maxLength={100}
                readOnly={editando?.esPlantilla}
                defaultValue={editando?.nombre ?? ""}
                className={`${inputClass} read-only:bg-superficie-alterna read-only:text-texto-suave`}
              />
            </Campo>
            <Campo label="Descripción" htmlFor="descripcion">
              <input id="descripcion" name="descripcion" maxLength={250} defaultValue={editando?.descripcion ?? ""} className={inputClass} />
            </Campo>
          </div>

          <label className="flex items-center gap-2.5">
            <input type="checkbox" name="activo" defaultChecked={editando?.activo ?? true} className={casillaClass} />
            <span className="text-sm text-texto">Activo</span>
          </label>

          <fieldset className="flex flex-col gap-3">
            <legend className="mb-1 flex w-full flex-wrap items-center justify-between gap-2 text-sm font-medium text-texto">
              <span>
                Permisos ({seleccionados.length} de {catalogo.length})
              </span>
              <span className="flex gap-3 text-xs font-normal">
                <button type="button" className="text-acento hover:underline" onClick={() => alternar(catalogo.map((p) => p.codigo), true)}>
                  Marcar todos
                </button>
                <button type="button" className="text-acento hover:underline" onClick={() => setSeleccionados([])}>
                  Quitar todos
                </button>
              </span>
            </legend>
            <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
              {porModulo.map(([modulo, items]) => {
                const codigos = items.map((i) => i.codigo);
                const todos = codigos.every((c) => seleccionados.includes(c));
                return (
                  <div key={modulo} className="rounded-md border border-borde p-3">
                    <label className="flex items-center gap-2.5 border-b border-borde pb-2">
                      <input
                        type="checkbox"
                        checked={todos}
                        onChange={(e) => alternar(codigos, e.target.checked)}
                        className={casillaClass}
                      />
                      <span className="text-sm font-medium text-texto">{etiquetaModulo(modulo)}</span>
                    </label>
                    <div className="mt-2 flex flex-col gap-1">
                      {items.map((permiso) => (
                        <label key={permiso.codigo} className="flex items-center gap-2.5 py-1">
                          <input
                            type="checkbox"
                            name="permisos"
                            value={permiso.codigo}
                            checked={seleccionados.includes(permiso.codigo)}
                            onChange={(e) => alternar([permiso.codigo], e.target.checked)}
                            className={casillaClass}
                          />
                          <span className="text-sm text-texto">{permiso.descripcion ?? permiso.codigo}</span>
                        </label>
                      ))}
                    </div>
                  </div>
                );
              })}
            </div>
          </fieldset>

          {editando ? (
            <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
              Si cambias los permisos o el estado, los usuarios con este perfil deberán iniciar sesión de nuevo
              (incluido tú, si es tu perfil).
            </p>
          ) : null}
          {errorFormulario ? <p className="text-sm text-peligro">{errorFormulario}</p> : null}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={cerrar} className={botonSecundarioClass}>
              Cancelar
            </button>
            <button type="submit" disabled={guardando} className={botonPrimarioClass}>
              {guardando ? "Guardando…" : "Guardar"}
            </button>
          </div>
        </form>
      </Dialogo>

      <DialogoConfirmacion
        abierto={eliminandoPerfil !== null}
        titulo="Eliminar perfil"
        mensaje={
          <>
            Se eliminará el perfil <strong>{eliminandoPerfil?.nombre}</strong>. Esta acción no se puede deshacer; el historial
            se conserva.
          </>
        }
        textoConfirmar="Eliminar"
        peligro
        procesando={eliminando}
        error={errorEliminar}
        onConfirmar={eliminar}
        onCerrar={() => setEliminandoPerfil(null)}
      />

      <Dialogo
        titulo={historialDe ? `Historial de ${historialDe.nombre}` : "Historial"}
        abierto={historialDe !== null}
        onCerrar={() => setHistorialDe(null)}
      >
        {historialDe ? (
          <HistorialRemoto ruta={`/api/seguridad/perfiles/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL} />
        ) : null}
      </Dialogo>
    </div>
  );
}
