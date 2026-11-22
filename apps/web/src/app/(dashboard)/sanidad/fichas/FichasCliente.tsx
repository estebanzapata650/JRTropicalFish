"use client";

import { useRouter } from "next/navigation";
import { useEffect, useMemo, useState, useTransition } from "react";
import { Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { BotonIcono, Campo, CampoAncho, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { botonPrimarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { num, type FichaResumen, type FichaTecnica } from "@/lib/sanidad";
import { guardarFichaAction } from "../actions";

const LIMITE = 100;

const rango = (a: number | null, b: number | null, sufijo = "") => (a === null && b === null ? "—" : `${num(a)}–${num(b)}${sufijo}`);

/**
 * Ficha técnica por especie (formato FT.CA.01): agua ideal, comportamiento, empaque, sensibilidad química y
 * horas de ayuno al llegar. La importación del PDF de fichas queda para la fase 2.
 */
export function FichasCliente({ fichas, gestionar }: { fichas: FichaResumen[]; gestionar: boolean }) {
  const [busqueda, setBusqueda] = useState("");
  const [filtro, setFiltro] = useState<"todas" | "con" | "sin">("todas");
  const [abierta, setAbierta] = useState<FichaResumen | null>(null);

  const filtradas = useMemo(() => {
    const q = busqueda.trim().toLowerCase();
    return fichas.filter(
      (f) =>
        (filtro === "todas" || (filtro === "con") === f.tieneFicha) &&
        (!q || f.productoCodigo.toLowerCase().includes(q) || f.nombreCientifico.toLowerCase().includes(q) || (f.nombreComun ?? "").toLowerCase().includes(q)),
    );
  }, [busqueda, filtro, fichas]);
  const con = fichas.filter((f) => f.tieneFicha).length;

  return (
    <div className="flex flex-col gap-4">
      <PageHeader titulo="Fichas técnicas" descripcion={`${con} de ${fichas.length} especies con ficha. Sin ficha, el ayuno de llegada es de 24 h.`} />
      <div className="flex flex-col gap-2 sm:flex-row">
        <div className="relative flex-1">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input type="search" aria-label="Buscar especie" placeholder="Código o nombre" className={`${inputClass} pl-9`} value={busqueda} onChange={(e) => setBusqueda(e.target.value)} />
        </div>
        <select aria-label="Filtrar fichas" className={`${selectClass} sm:w-48`} value={filtro} onChange={(e) => setFiltro(e.target.value as typeof filtro)}>
          <option value="todas">Todas</option>
          <option value="con">Con ficha</option>
          <option value="sin">Sin ficha</option>
        </select>
      </div>
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr>
            <th className="px-3 py-2 font-medium">Especie</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Grupo</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">pH</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Temperatura</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Ayuno al llegar</th>
            <th className="px-3 py-2 font-medium">Ficha</th>
            <th className="px-3 py-2">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtradas.slice(0, LIMITE).map((f) => (
            <tr key={f.productoId} className="border-b border-borde last:border-0">
              <td className="px-3 py-2">
                <span className="font-medium text-texto">{f.productoCodigo}</span> <span className="italic text-texto">{f.nombreCientifico}</span>
                {f.nombreComun ? <span className="block text-xs text-texto-suave">{f.nombreComun}</span> : null}
              </td>
              <td className="hidden px-3 py-2 md:table-cell">{f.grupoNombre ?? "—"}</td>
              <td className="hidden px-3 py-2 md:table-cell">{rango(f.phMin, f.phMax)}</td>
              <td className="hidden px-3 py-2 md:table-cell">{rango(f.temperaturaMin, f.temperaturaMax, " °C")}</td>
              <td className="hidden px-3 py-2 lg:table-cell">{f.ayunoLlegadaHoras} h</td>
              <td className="px-3 py-2">{f.tieneFicha ? <Etiqueta texto="Sí" tono="exito" /> : <Etiqueta texto="No" tono="neutro" />}</td>
              <td className="px-3 py-2 text-right">
                <BotonIcono accion="editar" etiqueta={`${gestionar ? "Editar" : "Ver"} la ficha de ${f.productoCodigo}`} onClick={() => setAbierta(f)} />
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>
      {filtradas.length > LIMITE ? <p className="text-sm text-texto-suave">Mostrando {LIMITE} de {filtradas.length}: busca para acotar.</p> : null}

      <Dialogo titulo={abierta ? `Ficha ${abierta.productoCodigo} · ${abierta.nombreCientifico}` : ""} abierto={abierta !== null} onCerrar={() => setAbierta(null)} ancho="max-w-3xl">
        {abierta ? <FormFicha key={abierta.productoId} productoId={abierta.productoId} gestionar={gestionar} onListo={() => setAbierta(null)} /> : null}
      </Dialogo>
    </div>
  );
}

const CAMPOS_TEXTO: [keyof FichaTecnica, string, number][] = [
  ["procedencia", "Procedencia", 500],
  ["caracteristicas", "Características", 2000],
  ["dimorfismo", "Dimorfismo sexual", 1000],
  ["etologia", "Etología (comportamiento y alimentación)", 2000],
  ["empaque", "Empaque", 500],
  ["sensibilidadQuimica", "Sensibilidad química (medicamentos que no tolera)", 1000],
];

const CAMPOS_NUMERO: [keyof FichaTecnica, string][] = [
  ["phMin", "pH mínimo"],
  ["phMax", "pH máximo"],
  ["ghMin", "GH mínimo (°d)"],
  ["ghMax", "GH máximo (°d)"],
  ["temperaturaMin", "Temperatura mínima (°C)"],
  ["temperaturaMax", "Temperatura máxima (°C)"],
];

function FormFicha({ productoId, gestionar, onListo }: { productoId: string; gestionar: boolean; onListo: () => void }) {
  const router = useRouter();
  const [ficha, setFicha] = useState<Record<string, string> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pendiente, iniciar] = useTransition();

  useEffect(() => {
    let vigente = true;
    fetch(`/api/sanidad/fichas/${productoId}`, { cache: "no-store" })
      .then((r) => r.json() as Promise<FichaTecnica>)
      .then((f) => {
        if (!vigente) return;
        setFicha(Object.fromEntries(Object.entries(f).map(([k, v]) => [k, v === null ? "" : String(v)])));
      })
      .catch(() => vigente && setError("No se pudo cargar la ficha."));
    return () => {
      vigente = false;
    };
  }, [productoId]);

  if (!ficha) return <p className="text-sm text-texto-suave">{error ?? "Cargando…"}</p>;
  const set = (k: string, v: string) => setFicha({ ...ficha, [k]: v });
  const numero = (v: string) => (v.trim() === "" ? null : Number(v.replace(",", ".")));

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        iniciar(async () => {
          const datos: Record<string, unknown> = { ayunoLlegadaHoras: Number(ficha.ayunoLlegadaHoras) || 0 };
          for (const [k] of CAMPOS_TEXTO) datos[k] = ficha[k] || null;
          for (const [k] of CAMPOS_NUMERO) datos[k] = numero(ficha[k]);
          const r = await guardarFichaAction(productoId, datos);
          if (r.error) setError(r.error);
          else {
            onListo();
            router.refresh();
          }
        });
      }}
    >
      <RejillaCampos>
        {CAMPOS_NUMERO.map(([k, label]) => (
          <Campo key={k} label={label} htmlFor={`ficha-${k}`}>
            <input id={`ficha-${k}`} inputMode="decimal" className={inputClass} value={ficha[k]} onChange={(e) => set(k, e.target.value)} disabled={!gestionar} />
          </Campo>
        ))}
        <Campo label="Ayuno al llegar (horas)" htmlFor="ficha-ayuno" hint="Horas sin alimentar después de la llegada">
          <input id="ficha-ayuno" type="number" min={0} max={168} className={inputClass} value={ficha.ayunoLlegadaHoras} onChange={(e) => set("ayunoLlegadaHoras", e.target.value)} disabled={!gestionar} />
        </Campo>
        {CAMPOS_TEXTO.map(([k, label, max]) => (
          <CampoAncho key={k}>
            <Campo label={label} htmlFor={`ficha-${k}`}>
              <textarea id={`ficha-${k}`} className={textareaClass} value={ficha[k]} maxLength={max} onChange={(e) => set(k, e.target.value)} disabled={!gestionar} />
            </Campo>
          </CampoAncho>
        ))}
      </RejillaCampos>
      {error ? <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">{error}</p> : null}
      {gestionar ? (
        <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
          Guardar
        </button>
      ) : null}
    </form>
  );
}
