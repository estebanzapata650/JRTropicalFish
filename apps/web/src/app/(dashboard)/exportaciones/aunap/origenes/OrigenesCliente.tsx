"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useMemo, useState, useTransition } from "react";
import { Campo, EncabezadoTabla, Etiqueta, RejillaCampos, Seccion, TablaResponsive } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import type { CentroAunap, MunicipioAunap, ParametrosAunap } from "@/lib/exportaciones";
import { guardarCentroAunapAction, guardarParametrosAunapAction } from "../../actions";

/** Quita tildes y mayúsculas para comparar cuencas ("Pacífico" = "Pacifico"). */
const normal = (s: string) => s.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

/**
 * Orígenes del informe AUNAP: cada centro de acopio se declara con un municipio del listado oficial (que trae su
 * cuenca). Lo de criadero no se reporta. También el encabezado del informe (datos del permisionario).
 */
export function OrigenesCliente({
  centros,
  municipios,
  parametros,
  configurar,
}: {
  centros: CentroAunap[];
  municipios: MunicipioAunap[];
  parametros: ParametrosAunap;
  configurar: boolean;
}) {
  const opciones = useMemo(
    () => municipios.map((m) => ({ id: String(m.id), nombre: `${m.municipio} (${m.departamento})`, detalle: m.cuenca })),
    [municipios],
  );
  const pendientes = centros.filter((c) => !c.excluir && !c.municipioAunapId).length;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Orígenes AUNAP"
        descripcion="De qué municipio se declara lo que llega de cada centro de acopio, y el encabezado del informe mensual."
        acciones={
          <Link href="/exportaciones/aunap/informe" className={botonSecundarioClass}>
            Ver informe mensual
          </Link>
        }
      />

      <section aria-labelledby="titulo-centros" className="flex flex-col gap-3">
        <div>
          <h2 id="titulo-centros" className="text-base font-semibold text-texto">
            Centros de acopio {pendientes > 0 ? <span className="text-advertencia-texto">({pendientes} sin municipio)</span> : null}
          </h2>
          <p className="text-sm text-texto-suave">
            La cuenca sale del listado de municipios de la AUNAP. Si no coincide con la del catálogo, se muestra para revisarla.
          </p>
        </div>
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Centro</th>
              <th className="px-3 py-2 font-medium">Municipio que se declara</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">Cuenca AUNAP</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {centros.map((c) => (
              <FilaCentro key={c.centroAcopioId} centro={c} opciones={opciones} configurar={configurar} />
            ))}
          </tbody>
        </TablaResponsive>
      </section>

      <Seccion titulo="Encabezado del informe">
        <FormParametros parametros={parametros} configurar={configurar} />
      </Seccion>
    </div>
  );
}

function FilaCentro({
  centro,
  opciones,
  configurar,
}: {
  centro: CentroAunap;
  opciones: { id: string; nombre: string; detalle: string }[];
  configurar: boolean;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const difiere = centro.cuenca && !normal(centro.cuencaInterna).startsWith(normal(centro.cuenca).split(" ")[0]);

  const guardar = (municipioId: number | null, excluir: boolean) =>
    iniciar(async () => {
      const r = await guardarCentroAunapAction(centro.centroAcopioId, municipioId, excluir);
      setError(r.error ?? null);
      if (!r.error) router.refresh();
    });

  return (
    <tr className="border-b border-borde align-top last:border-0">
      <td className="px-3 py-2">
        <span className="font-medium text-texto">{centro.centroNombre}</span>
        <span className="block text-xs text-texto-suave">Catálogo: {centro.cuencaInterna}</span>
        <span className="block text-xs text-texto-suave md:hidden">
          {centro.excluir ? "No se reporta" : centro.cuenca ? `AUNAP: ${centro.cuenca}` : ""}
        </span>
      </td>
      <td className="px-3 py-2">
        {centro.excluir ? (
          <div className="flex flex-wrap items-center gap-2">
            <Etiqueta texto="Criadero · no se reporta" tono="neutro" />
            {configurar ? (
              <button type="button" disabled={pendiente} onClick={() => guardar(null, false)} className="text-sm text-acento hover:underline">
                Reportar
              </button>
            ) : null}
          </div>
        ) : configurar ? (
          <div className="flex flex-col gap-1.5">
            <SelectorBuscable
              etiqueta={`Municipio de origen de ${centro.centroNombre}`}
              opciones={opciones}
              valor={centro.municipioAunapId ? String(centro.municipioAunapId) : ""}
              onChange={(id) => guardar(id ? Number(id) : null, false)}
              disabled={pendiente}
              placeholder="Escribe el municipio…"
              className="min-w-0 sm:min-w-64"
            />
            <button type="button" disabled={pendiente} onClick={() => guardar(null, true)} className="w-fit text-xs text-texto-suave hover:underline">
              Es criadero: no reportar
            </button>
          </div>
        ) : centro.municipio ? (
          <span className="text-texto">
            {centro.municipio} ({centro.departamento})
          </span>
        ) : (
          <span className="text-advertencia-texto">Sin definir</span>
        )}
        {error ? <p className="text-xs text-peligro">{error}</p> : null}
      </td>
      <td className="hidden px-3 py-2 md:table-cell">
        {centro.excluir ? (
          <span className="text-texto-suave">—</span>
        ) : centro.cuenca ? (
          <>
            <span className="text-texto">{centro.cuenca}</span>
            {difiere ? <span className="block text-xs text-advertencia-texto">El catálogo dice {centro.cuencaInterna}</span> : null}
          </>
        ) : (
          <span className="text-advertencia-texto">Sin municipio</span>
        )}
      </td>
    </tr>
  );
}

function FormParametros({ parametros, configurar }: { parametros: ParametrosAunap; configurar: boolean }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{ texto: string; error: boolean } | null>(null);
  const vencida = parametros.vigenciaHasta && parametros.vigenciaHasta < new Date().toISOString().slice(0, 10);

  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault();
        const f = new FormData(e.currentTarget);
        const v = (k: string) => (String(f.get(k) ?? "").trim() || null);
        iniciar(async () => {
          const r = await guardarParametrosAunapAction({
            representanteLegal: v("representanteLegal"),
            resolucion: v("resolucion"),
            vigenciaDesde: v("vigenciaDesde"),
            vigenciaHasta: v("vigenciaHasta"),
            direccion: v("direccion"),
            correo: v("correo"),
            telefono: v("telefono"),
          });
          setMensaje(r.error ? { texto: r.error, error: true } : { texto: "Encabezado guardado.", error: false });
          if (!r.error) router.refresh();
        });
      }}
    >
      {vencida ? (
        <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
          La vigencia del permiso de comercialización terminó. Actualiza la resolución y las fechas con la prórroga.
        </p>
      ) : null}
      <RejillaCampos>
        <Campo label="Representante legal" htmlFor="representanteLegal">
          <input id="representanteLegal" name="representanteLegal" defaultValue={parametros.representanteLegal ?? ""} maxLength={150} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="N.º de resolución" htmlFor="resolucion">
          <input id="resolucion" name="resolucion" defaultValue={parametros.resolucion ?? ""} maxLength={100} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Vigencia desde" htmlFor="vigenciaDesde">
          <input id="vigenciaDesde" name="vigenciaDesde" type="date" defaultValue={parametros.vigenciaDesde ?? ""} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Vigencia hasta" htmlFor="vigenciaHasta">
          <input id="vigenciaHasta" name="vigenciaHasta" type="date" defaultValue={parametros.vigenciaHasta ?? ""} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Dirección" htmlFor="direccion">
          <input id="direccion" name="direccion" defaultValue={parametros.direccion ?? ""} maxLength={250} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Correo" htmlFor="correo">
          <input id="correo" name="correo" type="email" defaultValue={parametros.correo ?? ""} maxLength={320} className={inputClass} disabled={!configurar} />
        </Campo>
        <Campo label="Teléfono / celular" htmlFor="telefono">
          <input id="telefono" name="telefono" defaultValue={parametros.telefono ?? ""} maxLength={60} className={inputClass} disabled={!configurar} />
        </Campo>
      </RejillaCampos>
      {mensaje ? (
        <p role="status" className={`rounded-md px-3 py-2 text-sm ${mensaje.error ? "bg-peligro/15 text-peligro" : "bg-exito-fondo text-exito-texto"}`}>
          {mensaje.texto}
        </p>
      ) : null}
      {configurar ? (
        <button type="submit" disabled={pendiente} className={`${botonPrimarioClass} w-full sm:w-fit`}>
          {pendiente ? "Guardando…" : "Guardar encabezado"}
        </button>
      ) : null}
    </form>
  );
}
