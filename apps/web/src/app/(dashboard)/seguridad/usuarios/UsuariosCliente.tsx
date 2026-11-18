"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { History, LockOpen, Power, PowerOff, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import { estaBloqueado, fechaHora, haceCuanto } from "@/lib/formato";
import type { UsuarioResumen } from "@/lib/types";
import { cambiarEstadoUsuarioAction, desbloquearUsuarioAction, guardarUsuarioAction } from "../actions";

const ETIQUETAS_HISTORIAL = { Nombre: "Nombre", Email: "Correo", Perfil: "Perfil", Activo: "Activo" };

type Confirmacion = { tipo: "estado"; usuario: UsuarioResumen } | { tipo: "desbloquear"; usuario: UsuarioResumen };

export function UsuariosCliente({
  usuarios,
  perfiles,
  usuarioActualId,
  puedeCrear,
  puedeEditar,
  puedeCambiarEstado,
  ahoraIso,
}: {
  usuarios: UsuarioResumen[];
  perfiles: { id: string; nombre: string }[];
  usuarioActualId: string;
  puedeCrear: boolean;
  puedeEditar: boolean;
  puedeCambiarEstado: boolean;
  /** "Ahora" del servidor: evita que el estado "bloqueado" difiera entre servidor y cliente al hidratar. */
  ahoraIso: string;
}) {
  const router = useRouter();
  const ahora = useMemo(() => new Date(ahoraIso), [ahoraIso]);

  const [busqueda, setBusqueda] = useState("");
  const [editando, setEditando] = useState<UsuarioResumen | null>(null);
  const [creando, setCreando] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [guardando, iniciarGuardado] = useTransition();

  const [historialDe, setHistorialDe] = useState<UsuarioResumen | null>(null);
  const [confirmacion, setConfirmacion] = useState<Confirmacion | null>(null);
  const [errorConfirmacion, setErrorConfirmacion] = useState<string | null>(null);
  const [procesando, iniciarProceso] = useTransition();

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return usuarios;
    return usuarios.filter(
      (u) =>
        u.nombre.toLowerCase().includes(texto) ||
        u.email.toLowerCase().includes(texto) ||
        u.perfilNombre.toLowerCase().includes(texto),
    );
  }, [usuarios, busqueda]);

  const formularioAbierto = creando || editando !== null;

  const cerrarFormulario = () => {
    setCreando(false);
    setEditando(null);
    setErrorFormulario(null);
  };

  // Guardar, cerrar y refrescar ocurren en el handler: nada de reaccionar a un estado en un efecto.
  const guardar = (formData: FormData) =>
    iniciarGuardado(async () => {
      const resultado = await guardarUsuarioAction(editando?.id ?? null, formData);
      if (resultado.error) {
        setErrorFormulario(resultado.error);
        return;
      }
      cerrarFormulario();
      router.refresh();
    });

  const cerrarConfirmacion = () => {
    setConfirmacion(null);
    setErrorConfirmacion(null);
  };

  const confirmar = () => {
    if (!confirmacion) return;
    const { usuario } = confirmacion;
    iniciarProceso(async () => {
      const resultado =
        confirmacion.tipo === "estado"
          ? await cambiarEstadoUsuarioAction(usuario.id, !usuario.activo)
          : await desbloquearUsuarioAction(usuario.id);
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
        titulo="Usuarios"
        descripcion="Quién puede entrar al sistema y con qué perfil"
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
              Nuevo usuario
            </button>
          ) : null
        }
      />

      <div className="relative w-full sm:max-w-sm">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
        <input
          type="search"
          value={busqueda}
          onChange={(e) => setBusqueda(e.target.value)}
          placeholder="Buscar por nombre, correo o perfil"
          aria-label="Buscar usuarios"
          className={`${inputClass} pl-9`}
        />
      </div>

      <TablaResponsive>
        <EncabezadoTabla>
          <tr>
            <th className="px-4 py-3 font-medium text-texto">Usuario</th>
            <th className="hidden px-4 py-3 font-medium text-texto sm:table-cell">Perfil</th>
            <th className="hidden px-4 py-3 font-medium text-texto lg:table-cell">Último acceso</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Estado</th>
            <th className="px-4 py-3">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-4 py-6 text-center text-sm text-texto-suave">
                {usuarios.length === 0 ? "No hay usuarios." : "Ningún usuario coincide con la búsqueda."}
              </td>
            </tr>
          ) : null}
          {filtrados.map((usuario) => {
            const bloqueado = estaBloqueado(usuario.bloqueadoHasta, ahora);
            const esYo = usuario.id === usuarioActualId;
            return (
              <tr key={usuario.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <div className="flex min-w-0 flex-col">
                    <span className="font-medium text-texto">
                      {usuario.nombre}
                      {esYo ? <span className="ml-1.5 text-xs font-normal text-texto-suave">(tú)</span> : null}
                    </span>
                    <span className="truncate text-sm text-texto-suave">{usuario.email}</span>
                    <span className="text-xs text-texto-suave sm:hidden">{usuario.perfilNombre}</span>
                    <span className="mt-1 md:hidden">
                      <EstadoUsuario activo={usuario.activo} bloqueado={bloqueado} />
                    </span>
                  </div>
                </td>
                <td className="hidden px-4 py-3 text-texto sm:table-cell">{usuario.perfilNombre}</td>
                <td className="hidden px-4 py-3 text-sm text-texto-suave lg:table-cell" title={fechaHora(usuario.ultimoAccesoEn)}>
                  {haceCuanto(usuario.ultimoAccesoEn, ahora)}
                </td>
                <td className="hidden px-4 py-3 md:table-cell">
                  <EstadoUsuario activo={usuario.activo} bloqueado={bloqueado} />
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-1">
                    {puedeEditar && bloqueado ? (
                      <BotonIcono
                        accion="otra"
                        icono={LockOpen}
                        etiqueta={`Desbloquear a ${usuario.nombre}`}
                        onClick={() => setConfirmacion({ tipo: "desbloquear", usuario })}
                      />
                    ) : null}
                    {puedeEditar ? (
                      <BotonIcono
                        accion="editar"
                        etiqueta={`Editar a ${usuario.nombre}`}
                        onClick={() => {
                          setCreando(false);
                          setEditando(usuario);
                        }}
                      />
                    ) : null}
                    {puedeCambiarEstado && !esYo ? (
                      <BotonIcono
                        accion="otra"
                        icono={usuario.activo ? PowerOff : Power}
                        etiqueta={`${usuario.activo ? "Desactivar" : "Reactivar"} a ${usuario.nombre}`}
                        onClick={() => setConfirmacion({ tipo: "estado", usuario })}
                      />
                    ) : null}
                    <BotonIcono
                      accion="otra"
                      icono={History}
                      etiqueta={`Historial de ${usuario.nombre}`}
                      onClick={() => setHistorialDe(usuario)}
                    />
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo={editando ? `Editar a ${editando.nombre}` : "Nuevo usuario"}
        abierto={formularioAbierto}
        onCerrar={cerrarFormulario}
        ancho="max-w-xl"
      >
        {/* La clave remonta el formulario al cambiar de usuario, para que los defaultValue se reinicien. */}
        <form key={editando?.id ?? "nuevo"} action={guardar} className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <Campo label="Nombre" htmlFor="nombre">
              <input id="nombre" name="nombre" required minLength={2} maxLength={200} defaultValue={editando?.nombre ?? ""} className={inputClass} />
            </Campo>
            <Campo label="Correo electrónico" htmlFor="email">
              <input
                id="email"
                name="email"
                type="email"
                required
                autoComplete="off"
                defaultValue={editando?.email ?? ""}
                className={inputClass}
              />
            </Campo>
            <Campo
              label="Perfil"
              htmlFor="perfilId"
              hint={editando ? "Cambiar el perfil cierra las sesiones abiertas de este usuario." : undefined}
            >
              <select
                id="perfilId"
                name="perfilId"
                required
                defaultValue={editando?.perfilId ?? ""}
                className={selectClass}
              >
                <option value="" disabled>
                  Selecciona un perfil
                </option>
                {perfiles.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre}
                  </option>
                ))}
              </select>
            </Campo>
            <div className="hidden sm:block" />
            <Campo
              label={editando ? "Nueva contraseña (opcional)" : "Contraseña"}
              htmlFor="password"
              hint="Mínimo 8 caracteres, con letras y números."
            >
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                required={!editando}
                minLength={8}
                className={inputClass}
              />
            </Campo>
            <Campo label="Confirmar contraseña" htmlFor="confirmacion">
              <input
                id="confirmacion"
                name="confirmacion"
                type="password"
                autoComplete="new-password"
                required={!editando}
                className={inputClass}
              />
            </Campo>
          </div>

          {perfiles.length === 0 ? (
            <p className="text-sm text-advertencia-texto">
              No tienes acceso a la lista de perfiles activos: pide el permiso «Consultar perfiles».
            </p>
          ) : null}
          {errorFormulario ? <p className="text-sm text-peligro">{errorFormulario}</p> : null}

          <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={cerrarFormulario} className={botonSecundarioClass}>
              Cancelar
            </button>
            <button type="submit" disabled={guardando} className={botonPrimarioClass}>
              {guardando ? "Guardando…" : "Guardar"}
            </button>
          </div>
        </form>
      </Dialogo>

      <DialogoConfirmacion
        abierto={confirmacion !== null}
        titulo={
          confirmacion?.tipo === "desbloquear"
            ? "Desbloquear usuario"
            : confirmacion?.usuario.activo
              ? "Desactivar usuario"
              : "Reactivar usuario"
        }
        mensaje={
          confirmacion?.tipo === "desbloquear" ? (
            <>
              <strong>{confirmacion.usuario.nombre}</strong> podrá volver a intentar el ingreso de inmediato.
            </>
          ) : confirmacion?.usuario.activo ? (
            <>
              <strong>{confirmacion.usuario.nombre}</strong> no podrá ingresar y sus sesiones abiertas se cerrarán de
              inmediato. Su historial se conserva.
            </>
          ) : (
            <>
              <strong>{confirmacion?.usuario.nombre}</strong> podrá volver a ingresar con su contraseña actual.
            </>
          )
        }
        textoConfirmar={
          confirmacion?.tipo === "desbloquear" ? "Desbloquear" : confirmacion?.usuario.activo ? "Desactivar" : "Reactivar"
        }
        peligro={confirmacion?.tipo === "estado" && confirmacion.usuario.activo}
        procesando={procesando}
        error={errorConfirmacion}
        onConfirmar={confirmar}
        onCerrar={cerrarConfirmacion}
      />

      <Dialogo
        titulo={historialDe ? `Historial de ${historialDe.nombre}` : "Historial"}
        abierto={historialDe !== null}
        onCerrar={() => setHistorialDe(null)}
      >
        {historialDe ? (
          <HistorialRemoto ruta={`/api/seguridad/usuarios/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL} />
        ) : null}
      </Dialogo>
    </div>
  );
}

function EstadoUsuario({ activo, bloqueado }: { activo: boolean; bloqueado: boolean }) {
  if (!activo) return <Etiqueta texto="Inactivo" tono="neutro" />;
  if (bloqueado) return <Etiqueta texto="Bloqueado" tono="advertencia" />;
  return <Etiqueta texto="Activo" tono="exito" />;
}
