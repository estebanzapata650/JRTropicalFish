"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useState } from "react";
import {
  Campo,
  CampoAncho,
  Casilla,
  RejillaCampos,
  Seccion,
} from "@/components/campos";
import {
  DISPONIBILIDADES,
  TIPOS_PRODUCTO,
  calcularPrecio,
  type ListaPrecios,
  type ParametrosPrecios,
  type Producto,
} from "@/lib/catalogos";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
  textareaClass,
} from "@/lib/estilos";
import { dinero } from "@/lib/formato";
import { SelectMapa } from "../../crm/comun";

export type GrupoOpcion = { id: string; codigo: string; nombre: string };
export type CentroOpcion = { id: string; nombre: string; activo: boolean };

const MAXIMO_CENTROS = 3;
const numero = (t: string) =>
  t.trim() === "" ? null : Number(t.replace(",", "."));
const texto = (v: number | null | undefined) => (v == null ? "" : String(v));

/**
 * Crear o editar un producto. Los precios solo aparecen si el usuario puede verlos; las listas
 * derivadas muestran el valor que da su regla con lo que hay escrito en la lista base ("Usar").
 * Solo se envían los precios que cambiaron: así un usuario sin costos nunca toca la lista de costo.
 */
export function FormularioProducto({
  producto,
  listas,
  parametros,
  grupos,
  centros,
  puedeEditarPrecios,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  producto: Producto | null;
  listas: ListaPrecios[];
  parametros: ParametrosPrecios | null;
  grupos: GrupoOpcion[];
  centros: CentroOpcion[];
  puedeEditarPrecios: boolean;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [codigo, setCodigo] = useState(producto?.codigo ?? "");
  const [grupoId, setGrupoId] = useState(producto?.grupoId ?? "");
  const [tipo, setTipo] = useState(producto?.tipo ?? "salvaje");
  const [cientifico, setCientifico] = useState(
    producto?.nombreCientifico ?? "",
  );
  const [comun, setComun] = useState(producto?.nombreComun ?? "");
  const [espanol, setEspanol] = useState(producto?.nombreEspanol ?? "");
  const [tallaMin, setTallaMin] = useState(texto(producto?.tallaMinCm));
  const [tallaMax, setTallaMax] = useState(texto(producto?.tallaMaxCm));
  const [tallaEtiqueta, setTallaEtiqueta] = useState(
    producto?.tallaEtiqueta ?? "",
  );
  const [bolsaMin, setBolsaMin] = useState(texto(producto?.bolsaMin));
  const [bolsaMax, setBolsaMax] = useState(texto(producto?.bolsaMax));
  const [disponibilidad, setDisponibilidad] = useState(
    producto?.disponibilidad ?? "",
  );
  const [activo, setActivo] = useState(producto?.activo ?? true);
  const [notas, setNotas] = useState(producto?.notas ?? "");
  const [centrosElegidos, setCentrosElegidos] = useState<string[]>(
    producto?.centrosAcopio.map((c) => c.id) ?? [],
  );
  const [precios, setPrecios] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      listas.map((l) => [l.id, texto(producto?.precios[l.id])]),
    ),
  );

  const grupo = grupos.find((g) => g.id === grupoId);
  // El centro actual siempre se puede ver aunque esté inactivo.
  const opcionesCentro = centros.filter(
    (c) => c.activo || centrosElegidos.includes(c.id),
  );

  const guardar = () => {
    // Solo los precios que cambiaron (vacío = quitar el precio).
    const cambios: Record<string, number | null> = {};
    for (const l of listas) {
      const antes = producto?.precios[l.id] ?? null;
      const ahora = numero(precios[l.id] ?? "");
      if ((ahora ?? null) !== antes) cambios[l.id] = ahora;
    }
    onGuardar({
      codigo: producto ? null : codigo.trim() || null,
      grupoId,
      tipo,
      nombreCientifico: cientifico,
      nombreComun: comun,
      nombreEspanol: espanol,
      tallaMinCm: numero(tallaMin),
      tallaMaxCm: numero(tallaMax),
      tallaEtiqueta,
      bolsaMin: numero(bolsaMin),
      bolsaMax: numero(bolsaMax),
      disponibilidad,
      activo,
      notas,
      centrosAcopio: centrosElegidos,
      precios: puedeEditarPrecios ? cambios : {},
    });
  };

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        guardar();
      }}
      className="flex flex-col gap-6"
    >
      <Seccion titulo="Identificación">
        <RejillaCampos>
          <Campo
            label="Grupo"
            htmlFor="grupo"
            hint={
              producto
                ? "El grupo no cambia: el código lleva su prefijo."
                : undefined
            }
          >
            <SelectorBuscable
              id="grupo"
              opciones={(producto &&
              !grupos.some((g) => g.id === producto.grupoId)
                ? [
                    ...grupos,
                    {
                      id: producto.grupoId,
                      codigo: producto.grupoCodigo,
                      nombre: producto.grupoNombre,
                    },
                  ]
                : grupos
              ).map((g) => ({ id: g.id, nombre: `${g.codigo} · ${g.nombre}` }))}
              valor={grupoId}
              onChange={(v) => setGrupoId(v)}
              disabled={!!producto}
              placeholder="Escribe para buscar…"
            />
          </Campo>
          <Campo
            label={producto ? "Código" : "Código (opcional)"}
            htmlFor="codigo"
            hint={
              producto
                ? undefined
                : `Vacío = el siguiente del grupo${grupo ? ` (${grupo.codigo}…)` : ""}.`
            }
          >
            <input
              id="codigo"
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
              readOnly={!!producto}
              maxLength={20}
              className={`${inputClass} font-mono uppercase read-only:bg-superficie-alterna`}
            />
          </Campo>
          <Campo label="Tipo" htmlFor="tipo">
            <SelectMapa
              id="tipo"
              valor={tipo}
              onChange={setTipo}
              mapa={TIPOS_PRODUCTO}
            />
          </Campo>
          <Campo label="Nombre científico" htmlFor="cientifico">
            <input
              id="cientifico"
              value={cientifico}
              onChange={(e) => setCientifico(e.target.value)}
              maxLength={200}
              className={`${inputClass} italic`}
            />
          </Campo>
          <Campo
            label="Nombre común (inglés)"
            htmlFor="comun"
            hint="Varios nombres: sepáralos con « | »."
          >
            <input
              id="comun"
              value={comun}
              onChange={(e) => setComun(e.target.value)}
              maxLength={300}
              className={inputClass}
            />
          </Campo>
          <Campo label="Nombre en español" htmlFor="espanol">
            <input
              id="espanol"
              value={espanol}
              onChange={(e) => setEspanol(e.target.value)}
              maxLength={300}
              className={inputClass}
            />
          </Campo>
        </RejillaCampos>
      </Seccion>

      <Seccion titulo="Talla, empaque y disponibilidad">
        <RejillaCampos>
          <Campo label="Talla mínima (cm)" htmlFor="tallaMin">
            <input
              id="tallaMin"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.1"
              value={tallaMin}
              onChange={(e) => setTallaMin(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo label="Talla máxima (cm)" htmlFor="tallaMax">
            <input
              id="tallaMax"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.1"
              value={tallaMax}
              onChange={(e) => setTallaMax(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo
            label="Talla sin medida (opcional)"
            htmlFor="tallaEtiqueta"
            hint="Ej. small, medium, big."
          >
            <input
              id="tallaEtiqueta"
              value={tallaEtiqueta}
              onChange={(e) => setTallaEtiqueta(e.target.value)}
              maxLength={30}
              className={inputClass}
            />
          </Campo>
          <Campo label="Peces por bolsa (mín.)" htmlFor="bolsaMin">
            <input
              id="bolsaMin"
              type="number"
              inputMode="numeric"
              min={1}
              step={1}
              value={bolsaMin}
              onChange={(e) => setBolsaMin(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo label="Peces por bolsa (máx.)" htmlFor="bolsaMax">
            <input
              id="bolsaMax"
              type="number"
              inputMode="numeric"
              min={1}
              step={1}
              value={bolsaMax}
              onChange={(e) => setBolsaMax(e.target.value)}
              className={inputClass}
            />
          </Campo>
          <Campo
            label="Disponibilidad publicada"
            htmlFor="disponibilidad"
            hint="Lo que ve el cliente en el stocklist."
          >
            <input
              id="disponibilidad"
              list="disponibilidades"
              value={disponibilidad}
              onChange={(e) => setDisponibilidad(e.target.value)}
              maxLength={50}
              className={inputClass}
            />
            <datalist id="disponibilidades">
              {DISPONIBILIDADES.map((d) => (
                <option key={d} value={d} />
              ))}
            </datalist>
          </Campo>
          <CampoAncho>
            <fieldset className="flex flex-col gap-1.5">
              <legend className="mb-1.5 text-sm font-medium text-texto">
                Centros de acopio de origen (hasta {MAXIMO_CENTROS}, en orden)
              </legend>
              <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
                {Array.from({ length: MAXIMO_CENTROS }, (_, i) => (
                  <select
                    key={i}
                    aria-label={`Centro de acopio ${i + 1}`}
                    value={centrosElegidos[i] ?? ""}
                    onChange={(e) => {
                      const nuevos = [...centrosElegidos];
                      nuevos[i] = e.target.value;
                      setCentrosElegidos(
                        nuevos
                          .filter(Boolean)
                          .filter((v, j, a) => a.indexOf(v) === j),
                      );
                    }}
                    disabled={i > centrosElegidos.length}
                    className={selectClass}
                  >
                    <option value="">{i === 0 ? "Sin centro" : "—"}</option>
                    {opcionesCentro.map((c) => (
                      <option
                        key={c.id}
                        value={c.id}
                        disabled={
                          centrosElegidos.includes(c.id) &&
                          centrosElegidos[i] !== c.id
                        }
                      >
                        {c.nombre}
                      </option>
                    ))}
                  </select>
                ))}
              </div>
            </fieldset>
          </CampoAncho>
          <Casilla
            label="Producto activo (se ofrece en el catálogo)"
            checked={activo}
            onChange={setActivo}
          />
        </RejillaCampos>
      </Seccion>

      {listas.length > 0 ? (
        <Seccion titulo="Precios">
          <RejillaCampos>
            {listas.map((l) => {
              const base = l.listaBaseId
                ? numero(precios[l.listaBaseId] ?? "")
                : null;
              const sugerido =
                parametros && l.listaBaseId
                  ? calcularPrecio(base, tipo, l, parametros)
                  : null;
              const actual = numero(precios[l.id] ?? "");
              return (
                <Campo
                  key={l.id}
                  label={`${l.nombre} (${l.moneda})${l.activo ? "" : " · inactiva"}`}
                  htmlFor={`precio-${l.id}`}
                  hint={
                    sugerido != null && sugerido !== actual
                      ? `Según la regla: ${dinero(sugerido, l.moneda)}`
                      : l.esCosto
                        ? "Dato reservado: solo lo ven quienes tienen permiso de costos."
                        : undefined
                  }
                >
                  <div className="flex gap-2">
                    <input
                      id={`precio-${l.id}`}
                      type="number"
                      inputMode="decimal"
                      min={0}
                      step={l.moneda === "COP" ? 50 : 0.01}
                      value={precios[l.id] ?? ""}
                      onChange={(e) =>
                        setPrecios((p) => ({ ...p, [l.id]: e.target.value }))
                      }
                      disabled={!puedeEditarPrecios}
                      className={inputClass}
                    />
                    {puedeEditarPrecios &&
                    sugerido != null &&
                    sugerido !== actual ? (
                      <button
                        type="button"
                        onClick={() =>
                          setPrecios((p) => ({
                            ...p,
                            [l.id]: String(sugerido),
                          }))
                        }
                        className={`${botonSecundarioClass} shrink-0`}
                        aria-label={`Usar el precio sugerido para ${l.nombre}`}
                      >
                        Usar
                      </button>
                    ) : null}
                  </div>
                </Campo>
              );
            })}
          </RejillaCampos>
          {!puedeEditarPrecios ? (
            <p className="text-sm text-texto-suave">
              No tienes permiso para editar precios.
            </p>
          ) : null}
        </Seccion>
      ) : null}

      <Campo label="Notas (opcional)" htmlFor="notasProducto">
        <textarea
          id="notasProducto"
          value={notas}
          onChange={(e) => setNotas(e.target.value)}
          maxLength={2000}
          className={textareaClass}
        />
      </Campo>

      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <button
          type="button"
          onClick={onCancelar}
          className={botonSecundarioClass}
        >
          Cancelar
        </button>
        <button
          type="submit"
          disabled={guardando}
          className={botonPrimarioClass}
        >
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}
