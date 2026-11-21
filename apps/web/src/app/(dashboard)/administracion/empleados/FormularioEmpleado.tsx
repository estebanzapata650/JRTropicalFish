"use client";

import { useRef, useState } from "react";
import { ImagePlus } from "lucide-react";
import { Campo, CampoAncho, Casilla, RejillaCampos, Seccion } from "@/components/campos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import type { Empleado, OpcionMaestro } from "@/lib/types";
import { FotoEmpleado } from "./FotoEmpleado";

export type OpcionesEmpleado = {
  tiposIdentificacion: OpcionMaestro[];
  cargos: OpcionMaestro[];
  areas: OpcionMaestro[];
  tiposContrato: OpcionMaestro[];
};

const GENEROS = ["Femenino", "Masculino", "Otro"];
const TIPOS_FOTO = ["image/jpeg", "image/png", "image/webp"];
const TAMANO_MAXIMO_FOTO = 10 * 1024 * 1024;

/**
 * Formulario de empleado. Sin `required` nativos a propósito: en móvil las secciones están
 * plegadas (`display: none`) y el navegador bloquea el envío sin decir nada si un campo oculto no
 * pasa la validación. Los obligatorios los valida la API y el mensaje se muestra abajo.
 */
export function FormularioEmpleado({
  empleado,
  opciones,
  puedeVerSalario,
  hoy,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  empleado: Empleado | null;
  opciones: OpcionesEmpleado;
  puedeVerSalario: boolean;
  hoy: string;
  guardando: boolean;
  error: string | null;
  onGuardar: (formData: FormData) => void;
  onCancelar: () => void;
}) {
  const archivoRef = useRef<HTMLInputElement>(null);
  const [vistaPrevia, setVistaPrevia] = useState<string | null>(null);
  const [quitarFoto, setQuitarFoto] = useState(false);
  const [errorFoto, setErrorFoto] = useState<string | null>(null);

  // La vista previa se crea y libera en el handler del input: nada de efectos.
  const elegirFoto = (archivo: File | undefined) => {
    if (vistaPrevia) URL.revokeObjectURL(vistaPrevia);
    setVistaPrevia(null);
    setErrorFoto(null);
    if (!archivo) return;

    if (!TIPOS_FOTO.includes(archivo.type)) {
      setErrorFoto("La foto debe ser JPG, PNG o WEBP.");
      if (archivoRef.current) archivoRef.current.value = "";
      return;
    }
    if (archivo.size > TAMANO_MAXIMO_FOTO) {
      setErrorFoto("La foto no puede pesar más de 10 MB.");
      if (archivoRef.current) archivoRef.current.value = "";
      return;
    }
    setQuitarFoto(false);
    setVistaPrevia(URL.createObjectURL(archivo));
  };

  /** El valor actual del empleado, aunque ya no esté activo, para que el selector no lo pierda. */
  const conActual = (lista: OpcionMaestro[], id: string | null | undefined, nombre: string | null | undefined) =>
    id && !lista.some((o) => o.id === id) ? [...lista, { id, codigo: "", nombre: `${nombre ?? "—"} (inactivo)` }] : lista;

  const tiposIdentificacion = conActual(
    opciones.tiposIdentificacion,
    empleado?.tipoIdentificacionId,
    empleado?.tipoIdentificacionCodigo,
  );
  const cargos = conActual(opciones.cargos, empleado?.cargoId, empleado?.cargoNombre);
  const areas = conActual(opciones.areas, empleado?.areaId, empleado?.areaNombre);
  const tiposContrato = conActual(opciones.tiposContrato, empleado?.tipoContratoId, empleado?.tipoContratoNombre);

  const fotoActual = empleado?.fotoVersion && !quitarFoto ? empleado : null;

  return (
    // onSubmit y no `action`: React 19 reinicia los campos tras un `action`, y si la API rechaza
    // los datos se perdería todo lo escrito (incluida la foto elegida).
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar(new FormData(e.currentTarget));
      }}
      className="flex flex-col gap-6"
    >
      <Seccion titulo="Foto">
        <div className="flex flex-col items-start gap-4 sm:flex-row sm:items-center">
          {vistaPrevia ? (
            // eslint-disable-next-line @next/next/no-img-element -- vista previa local (blob:)
            <img src={vistaPrevia} alt="Vista previa de la foto" className="size-28 shrink-0 rounded-full border border-borde object-cover" />
          ) : fotoActual ? (
            <FotoEmpleado empleado={fotoActual} tamano="lg" />
          ) : (
            <span className="inline-flex size-28 shrink-0 items-center justify-center rounded-full border border-dashed border-borde text-texto-tenue">
              <ImagePlus className="size-8" aria-hidden />
            </span>
          )}
          <div className="flex min-w-0 flex-col gap-1.5">
            <label htmlFor="foto" className="text-sm font-medium text-texto">
              {empleado?.fotoVersion ? "Cambiar foto" : "Subir foto"} <span className="font-normal text-texto-suave">(opcional)</span>
            </label>
            <input
              ref={archivoRef}
              id="foto"
              name="foto"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={(e) => elegirFoto(e.target.files?.[0])}
              className="max-w-full text-sm text-texto-suave file:mr-3 file:h-10 file:rounded-md file:border file:border-borde file:bg-superficie file:px-3 file:text-sm file:font-medium file:text-texto hover:file:bg-acento-suave"
            />
            <p className="text-xs text-texto-suave">JPG, PNG o WEBP, máximo 10 MB.</p>
            {errorFoto ? <p className="text-sm text-peligro">{errorFoto}</p> : null}
            {empleado?.fotoVersion && !vistaPrevia ? (
              <Casilla name="quitarFoto" label="Quitar la foto actual" checked={quitarFoto} onChange={setQuitarFoto} />
            ) : null}
          </div>
        </div>
      </Seccion>

      <Seccion titulo="Identificación">
        <RejillaCampos>
          <Campo label="Tipo de identificación" htmlFor="tipoIdentificacionId">
            <select
              id="tipoIdentificacionId"
              name="tipoIdentificacionId"
              defaultValue={empleado?.tipoIdentificacionId ?? ""}
              className={selectClass}
            >
              <option value="">Selecciona…</option>
              {tiposIdentificacion.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.codigo ? `${o.codigo} · ${o.nombre}` : o.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Número" htmlFor="numeroIdentificacion">
            <input
              id="numeroIdentificacion"
              name="numeroIdentificacion"
              maxLength={30}
              autoComplete="off"
              defaultValue={empleado?.numeroIdentificacion ?? ""}
              className={inputClass}
            />
          </Campo>
          <Campo label="Género (opcional)" htmlFor="genero">
            <select id="genero" name="genero" defaultValue={empleado?.genero ?? ""} className={selectClass}>
              <option value="">Sin especificar</option>
              {GENEROS.map((g) => (
                <option key={g} value={g}>
                  {g}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Nombres" htmlFor="nombres">
            <input id="nombres" name="nombres" maxLength={100} autoComplete="off" defaultValue={empleado?.nombres ?? ""} className={inputClass} />
          </Campo>
          <Campo label="Apellidos" htmlFor="apellidos">
            <input id="apellidos" name="apellidos" maxLength={100} autoComplete="off" defaultValue={empleado?.apellidos ?? ""} className={inputClass} />
          </Campo>
          <Campo label="Fecha de nacimiento (opcional)" htmlFor="fechaNacimiento">
            <input
              id="fechaNacimiento"
              name="fechaNacimiento"
              type="date"
              max={hoy}
              defaultValue={empleado?.fechaNacimiento ?? ""}
              className={inputClass}
            />
          </Campo>
        </RejillaCampos>
      </Seccion>

      <Seccion titulo="Contacto">
        <RejillaCampos>
          <Campo label="Teléfono (opcional)" htmlFor="telefono">
            <input id="telefono" name="telefono" type="tel" maxLength={30} defaultValue={empleado?.telefono ?? ""} className={inputClass} />
          </Campo>
          <Campo label="Correo electrónico (opcional)" htmlFor="email">
            <input id="email" name="email" type="email" maxLength={320} defaultValue={empleado?.email ?? ""} className={inputClass} />
          </Campo>
          <Campo label="Ciudad (opcional)" htmlFor="ciudad">
            <input id="ciudad" name="ciudad" maxLength={100} defaultValue={empleado?.ciudad ?? ""} className={inputClass} />
          </Campo>
          <CampoAncho>
            <Campo label="Dirección (opcional)" htmlFor="direccion">
              <input id="direccion" name="direccion" maxLength={250} defaultValue={empleado?.direccion ?? ""} className={inputClass} />
            </Campo>
          </CampoAncho>
        </RejillaCampos>
      </Seccion>

      <Seccion titulo="Datos laborales">
        <RejillaCampos>
          <Campo label="Cargo" htmlFor="cargoId">
            <select id="cargoId" name="cargoId" defaultValue={empleado?.cargoId ?? ""} className={selectClass}>
              <option value="">Selecciona…</option>
              {cargos.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Área (opcional)" htmlFor="areaId">
            <select id="areaId" name="areaId" defaultValue={empleado?.areaId ?? ""} className={selectClass}>
              <option value="">Sin área</option>
              {areas.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Tipo de contrato" htmlFor="tipoContratoId">
            <select id="tipoContratoId" name="tipoContratoId" defaultValue={empleado?.tipoContratoId ?? ""} className={selectClass}>
              <option value="">Selecciona…</option>
              {tiposContrato.map((o) => (
                <option key={o.id} value={o.id}>
                  {o.nombre}
                </option>
              ))}
            </select>
          </Campo>
          <Campo label="Fecha de ingreso" htmlFor="fechaIngreso">
            <input id="fechaIngreso" name="fechaIngreso" type="date" defaultValue={empleado?.fechaIngreso ?? ""} className={inputClass} />
          </Campo>
          <Campo label="Fecha de retiro (opcional)" htmlFor="fechaRetiro">
            <input id="fechaRetiro" name="fechaRetiro" type="date" defaultValue={empleado?.fechaRetiro ?? ""} className={inputClass} />
          </Campo>
          {puedeVerSalario ? (
            <Campo label="Salario mensual (COP)" htmlFor="salario">
              <input
                id="salario"
                name="salario"
                type="number"
                inputMode="numeric"
                min={0}
                step={1}
                defaultValue={empleado?.salario ?? ""}
                className={inputClass}
              />
            </Campo>
          ) : null}
        </RejillaCampos>
        {(opciones.cargos.length === 0 || opciones.tiposContrato.length === 0) && !empleado ? (
          <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
            Faltan valores en Maestros: crea al menos un cargo y un tipo de contrato activos.
          </p>
        ) : null}
      </Seccion>

      {error ? <p className="text-sm text-peligro">{error}</p> : null}

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button type="button" onClick={onCancelar} className={botonSecundarioClass}>
          Cancelar
        </button>
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
