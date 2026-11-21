"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { History, Power, PowerOff, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, Casilla, EncabezadoTabla, EtiquetaEstado, TablaResponsive } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import type { MaestroInfo, ValorMaestro } from "@/lib/types";
import { cambiarEstadoValorMaestroAction, eliminarValorMaestroAction, guardarValorMaestroAction } from "../actions";

const ETIQUETAS_HISTORIAL = { Codigo: "Código", Nombre: "Nombre", Activo: "Activo" };

type Confirmacion = { tipo: "estado" | "eliminar"; valor: ValorMaestro };

/**
 * Pantalla genérica de maestros: el mismo componente sirve para cualquier maestro registrado en la
 * API (`RegistroMaestros`). La página lo monta con `key` = clave del maestro, así que cambiar de
 * maestro reinicia búsqueda y diálogos sin efectos.
 */
export function MaestrosCliente({
  maestros,
  actual,
  valores,
  puedeEditar,
}: {
  maestros: MaestroInfo[];
  actual: MaestroInfo | undefined;
  valores: ValorMaestro[];
  puedeEditar: boolean;
}) {
  const router = useRouter();

  const [busqueda, setBusqueda] = useState("");
  const [editando, setEditando] = useState<ValorMaestro | null>(null);
  const [creando, setCreando] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [guardando, iniciarGuardado] = useTransition();

  const [historialDe, setHistorialDe] = useState<ValorMaestro | null>(null);
  const [confirmacion, setConfirmacion] = useState<Confirmacion | null>(null);
  const [errorConfirmacion, setErrorConfirmacion] = useState<string | null>(null);
  const [procesando, iniciarProceso] = useTransition();

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    if (!texto) return valores;
    return valores.filter((v) => v.codigo.toLowerCase().includes(texto) || v.nombre.toLowerCase().includes(texto));
  }, [valores, busqueda]);

  if (!actual) {
    return <p className="text-sm text-texto-suave">No hay maestros configurados.</p>;
  }

  const singular = actual.nombreSingular;
  const rutaMaestro = (clave: string) => `/administracion/maestros?tipo=${encodeURIComponent(clave)}`;

  const cerrarFormulario = () => {
    setCreando(false);
    setEditando(null);
    setErrorFormulario(null);
  };

  const guardar = (formData: FormData) =>
    iniciarGuardado(async () => {
      const resultado = await guardarValorMaestroAction(actual.clave, editando?.id ?? null, formData);
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
    const { valor } = confirmacion;
    iniciarProceso(async () => {
      const resultado =
        confirmacion.tipo === "estado"
          ? await cambiarEstadoValorMaestroAction(actual.clave, valor.id, !valor.activo)
          : await eliminarValorMaestroAction(actual.clave, valor.id);
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
        titulo="Maestros"
        descripcion="Listas de valores que usan los demás módulos"
        acciones={
          puedeEditar ? (
            <button
              type="button"
              onClick={() => {
                setEditando(null);
                setCreando(true);
              }}
              className={botonPrimarioClass}
            >
              Nuevo {singular}
            </button>
          ) : null
        }
      />

      {/* Selector del maestro: lista desplegable en móvil, pestañas desde md. */}
      <div className="md:hidden">
        <Campo label="Maestro" htmlFor="maestro">
          <select
            id="maestro"
            value={actual.clave}
            onChange={(e) => router.push(rutaMaestro(e.target.value))}
            className={selectClass}
          >
            {maestros.map((m) => (
              <option key={m.clave} value={m.clave}>
                {m.nombre}
              </option>
            ))}
          </select>
        </Campo>
      </div>
      <nav aria-label="Maestros" className="hidden flex-wrap gap-1 border-b border-borde md:flex">
        {maestros.map((m) => {
          const seleccionado = m.clave === actual.clave;
          return (
            <Link
              key={m.clave}
              href={rutaMaestro(m.clave)}
              aria-current={seleccionado ? "page" : undefined}
              className={`-mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors ${
                seleccionado ? "border-acento text-texto" : "border-transparent text-texto-suave hover:text-texto"
              }`}
            >
              {m.nombre}
            </Link>
          );
        })}
      </nav>

      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-texto-suave">{actual.descripcion}</p>
        <div className="relative w-full sm:max-w-sm">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input
            type="search"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Buscar por código o nombre"
            aria-label={`Buscar en ${actual.nombre}`}
            className={`${inputClass} pl-9`}
          />
        </div>
      </div>

      <TablaResponsive>
        <EncabezadoTabla>
          <tr>
            <th className="px-4 py-3 font-medium text-texto">Código</th>
            <th className="px-4 py-3 font-medium text-texto">Nombre</th>
            <th className="hidden px-4 py-3 font-medium text-texto md:table-cell">Estado</th>
            <th className="px-4 py-3">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-4 py-6 text-center text-sm text-texto-suave">
                {valores.length === 0 ? `Aún no hay valores en ${actual.nombre}.` : "Ningún valor coincide con la búsqueda."}
              </td>
            </tr>
          ) : null}
          {filtrados.map((valor) => (
            <tr key={valor.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3 font-mono text-sm text-texto">{valor.codigo}</td>
              <td className="px-4 py-3">
                <div className="flex min-w-0 flex-col gap-1">
                  <span className="text-texto">{valor.nombre}</span>
                  <span className="md:hidden">
                    <EtiquetaEstado activo={valor.activo} />
                  </span>
                </div>
              </td>
              <td className="hidden px-4 py-3 md:table-cell">
                <EtiquetaEstado activo={valor.activo} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-1">
                  {puedeEditar ? (
                    <>
                      <BotonIcono
                        accion="editar"
                        etiqueta={`Editar ${valor.nombre}`}
                        onClick={() => {
                          setCreando(false);
                          setEditando(valor);
                        }}
                      />
                      <BotonIcono
                        accion="otra"
                        icono={valor.activo ? PowerOff : Power}
                        etiqueta={`${valor.activo ? "Desactivar" : "Activar"} ${valor.nombre}`}
                        onClick={() => setConfirmacion({ tipo: "estado", valor })}
                      />
                      <BotonIcono
                        accion="eliminar"
                        etiqueta={`Eliminar ${valor.nombre}`}
                        onClick={() => setConfirmacion({ tipo: "eliminar", valor })}
                      />
                    </>
                  ) : null}
                  <BotonIcono
                    accion="otra"
                    icono={History}
                    etiqueta={`Historial de ${valor.nombre}`}
                    onClick={() => setHistorialDe(valor)}
                  />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <Dialogo
        titulo={editando ? `Editar ${editando.nombre}` : `Nuevo ${singular}`}
        abierto={creando || editando !== null}
        onCerrar={cerrarFormulario}
        ancho="max-w-lg"
      >
        <form key={editando?.id ?? "nuevo"} action={guardar} className="flex flex-col gap-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-[10rem_1fr]">
            <Campo label="Código" htmlFor="codigo" hint="Se guarda en mayúsculas.">
              <input
                id="codigo"
                name="codigo"
                required
                maxLength={20}
                pattern="[A-Za-z0-9Ññ_\-]+"
                title="Letras, números, guion o guion bajo, sin espacios"
                autoComplete="off"
                defaultValue={editando?.codigo ?? ""}
                className={`${inputClass} font-mono uppercase`}
              />
            </Campo>
            <Campo label="Nombre" htmlFor="nombre">
              <input
                id="nombre"
                name="nombre"
                required
                minLength={2}
                maxLength={150}
                autoComplete="off"
                defaultValue={editando?.nombre ?? ""}
                className={inputClass}
              />
            </Campo>
          </div>
          <Casilla name="activo" label="Activo (se ofrece en los formularios)" defaultChecked={editando?.activo ?? true} />

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
          confirmacion?.tipo === "eliminar"
            ? `Eliminar ${singular}`
            : confirmacion?.valor.activo
              ? `Desactivar ${singular}`
              : `Activar ${singular}`
        }
        mensaje={
          confirmacion?.tipo === "eliminar" ? (
            <>
              <strong>{confirmacion.valor.nombre}</strong> dejará de aparecer en esta lista y en los formularios. Los
              registros que ya lo usan lo conservan.
            </>
          ) : confirmacion?.valor.activo ? (
            <>
              <strong>{confirmacion.valor.nombre}</strong> dejará de ofrecerse en los formularios. Los registros que ya
              lo usan lo conservan.
            </>
          ) : (
            <>
              <strong>{confirmacion?.valor.nombre}</strong> volverá a ofrecerse en los formularios.
            </>
          )
        }
        textoConfirmar={confirmacion?.tipo === "eliminar" ? "Eliminar" : confirmacion?.valor.activo ? "Desactivar" : "Activar"}
        peligro={confirmacion?.tipo === "eliminar" || confirmacion?.valor.activo}
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
          <HistorialRemoto
            ruta={`/api/maestros/${encodeURIComponent(actual.clave)}/${historialDe.id}/historial`}
            etiquetas={ETIQUETAS_HISTORIAL}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}
