"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import { Campo, Casilla, RejillaCampos } from "@/components/campos";
import { piezas, type Acuario, type Existencia } from "@/lib/inventario";
import { botonPrimarioClass, botonSecundarioClass, inputClass } from "@/lib/estilos";
import { guardarAcuarioAction } from "../actions";

/**
 * Mapa de acuarios por sector: una fila por nivel y una celda por posición. Vacío, con peces o con
 * enfermos (rojo: indicador 5 de la app anterior). Tocar un acuario muestra lo que tiene.
 */
export function AcuariosCliente({ acuarios, puedeEditar }: { acuarios: Acuario[]; puedeEditar: boolean }) {
  const router = useRouter();
  const sectores = useMemo(() => [...new Set(acuarios.map((a) => a.sector))].sort(), [acuarios]);
  const [sector, setSector] = useState(sectores[0] ?? "");
  const [soloEnfermos, setSoloEnfermos] = useState(false);
  const [elegido, setElegido] = useState<Acuario | null>(null);
  const [editando, setEditando] = useState<Acuario | "nuevo" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();

  const delSector = acuarios.filter((a) => a.sector === sector);
  const niveles = [...new Set(delSector.map((a) => a.nivel))].sort((x, y) => y - x);
  const conEnfermos = acuarios.filter((a) => a.enfermos > 0);
  const ocupados = acuarios.filter((a) => a.sanos + a.enfermos > 0).length;

  const guardar = (datos: Record<string, unknown>) =>
    iniciar(async () => {
      const r = await guardarAcuarioAction(editando === "nuevo" || editando === null ? null : editando.id, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      setEditando(null);
      setError(null);
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Acuarios"
        descripcion={`${acuarios.length} acuarios · ${ocupados} con peces · ${conEnfermos.length} con enfermos`}
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setEditando("nuevo")} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nuevo acuario
            </button>
          ) : null
        }
      />

      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div role="tablist" aria-label="Sector" className="-mx-4 flex gap-1 overflow-x-auto px-4 md:mx-0 md:px-0">
          {sectores.map((s) => {
            const enfermosSector = acuarios.some((a) => a.sector === s && a.enfermos > 0);
            return (
              <button
                key={s}
                type="button"
                role="tab"
                aria-selected={sector === s && !soloEnfermos}
                onClick={() => {
                  setSector(s);
                  setSoloEnfermos(false);
                }}
                className={`relative h-10 min-w-11 shrink-0 rounded-md px-3 text-sm font-medium ${
                  sector === s && !soloEnfermos ? "bg-acento text-acento-texto" : "border border-borde text-texto hover:bg-acento-suave"
                }`}
              >
                {s}
                {enfermosSector ? <span aria-label="con enfermos" className="absolute -top-1 -right-1 size-2.5 rounded-full bg-peligro" /> : null}
              </button>
            );
          })}
        </div>
        <button
          type="button"
          aria-pressed={soloEnfermos}
          onClick={() => setSoloEnfermos((v) => !v)}
          className={`inline-flex h-10 w-fit items-center rounded-full border px-4 text-sm ${soloEnfermos ? "border-peligro bg-peligro/15 font-medium text-peligro" : "border-borde text-peligro hover:bg-acento-suave"}`}
        >
          Acuarios con enfermos ({conEnfermos.length})
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-4 text-xs text-texto-suave">
        <Leyenda clase="border-borde bg-superficie" texto="Vacío" />
        <Leyenda clase="border-acento bg-acento-suave" texto="Con peces" />
        <Leyenda clase="border-peligro bg-peligro/15" texto="Con enfermos" />
      </div>

      {soloEnfermos ? (
        <ul className="grid grid-cols-2 gap-2 sm:grid-cols-4 lg:grid-cols-6">
          {conEnfermos.map((a) => (
            <li key={a.id}>
              <Celda a={a} onClick={() => setElegido(a)} />
            </li>
          ))}
          {conEnfermos.length === 0 ? <li className="col-span-full text-sm text-texto-suave">Ningún acuario tiene enfermos.</li> : null}
        </ul>
      ) : (
        <div className="flex flex-col gap-2 overflow-x-auto">
          {niveles.map((nivel) => (
            <div key={nivel} className="flex items-stretch gap-2">
              <span className="flex w-14 shrink-0 items-center text-xs font-medium text-texto-suave">Nivel {nivel}</span>
              <ul className="flex gap-2">
                {delSector
                  .filter((a) => a.nivel === nivel)
                  .sort((x, y) => x.posicion - y.posicion)
                  .map((a) => (
                    <li key={a.id} className="w-20 shrink-0">
                      <Celda a={a} onClick={() => setElegido(a)} />
                    </li>
                  ))}
              </ul>
            </div>
          ))}
        </div>
      )}

      <Dialogo titulo={elegido ? `Acuario ${elegido.codigo}` : ""} abierto={elegido !== null} onCerrar={() => setElegido(null)} ancho="max-w-xl">
        {elegido ? (
          <DetalleAcuario
            acuario={elegido}
            puedeEditar={puedeEditar}
            onEditar={() => {
              setEditando(elegido);
              setElegido(null);
            }}
          />
        ) : null}
      </Dialogo>

      <Dialogo
        titulo={editando === "nuevo" ? "Nuevo acuario" : editando ? `Editar ${editando.codigo}` : ""}
        abierto={editando !== null}
        onCerrar={() => {
          setEditando(null);
          setError(null);
        }}
        ancho="max-w-xl"
      >
        {editando ? (
          <FormularioAcuario
            key={editando === "nuevo" ? "nuevo" : editando.id}
            acuario={editando === "nuevo" ? null : editando}
            sectorPorDefecto={sector}
            guardando={guardando}
            error={error}
            onGuardar={guardar}
            onCancelar={() => {
              setEditando(null);
              setError(null);
            }}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function Leyenda({ clase, texto }: { clase: string; texto: string }) {
  return (
    <span className="flex items-center gap-1.5">
      <span className={`size-3.5 rounded border ${clase}`} /> {texto}
    </span>
  );
}

function Celda({ a, onClick }: { a: Acuario; onClick: () => void }) {
  const total = a.sanos + a.enfermos;
  const estilo = !a.activo
    ? "border-dashed border-borde bg-fondo-sutil text-texto-tenue"
    : a.enfermos > 0
      ? "border-peligro bg-peligro/15 text-texto"
      : total > 0
        ? "border-acento bg-acento-suave text-texto"
        : "border-borde bg-superficie text-texto-suave";
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={`Acuario ${a.codigo}: ${total} peces${a.enfermos ? `, ${a.enfermos} enfermos` : ""}${a.activo ? "" : ", inactivo"}`}
      className={`flex h-16 w-full flex-col items-center justify-center rounded-md border text-xs transition-colors hover:border-acento ${estilo}`}
    >
      <span className="font-mono font-medium">{a.codigo}</span>
      <span className="tabular-nums">{total > 0 ? piezas(total) : "—"}</span>
    </button>
  );
}

/** Contenido del acuario, traído bajo demanda por el proxy (no viene con la página). */
function DetalleAcuario({ acuario, puedeEditar, onEditar }: { acuario: Acuario; puedeEditar: boolean; onEditar: () => void }) {
  const [estado, setEstado] = useState<{ id: string; existencias?: Existencia[]; error?: string } | null>(null);

  useEffect(() => {
    let vigente = true;
    fetch(`/api/inventario/existencias?acuarioId=${acuario.id}`, { cache: "no-store" })
      .then(async (r) => {
        if (!r.ok) throw new Error(`Error ${r.status}`);
        return (await r.json()) as Existencia[];
      })
      .then((existencias) => vigente && setEstado({ id: acuario.id, existencias }))
      .catch((e: Error) => vigente && setEstado({ id: acuario.id, error: e.message }));
    return () => {
      vigente = false;
    };
  }, [acuario.id]);

  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-texto-suave">
        Sector {acuario.sector} · nivel {acuario.nivel} · posición {acuario.posicion}
        {acuario.capacidadLitros ? ` · ${acuario.capacidadLitros} L` : ""}
        {acuario.activo ? "" : " · inactivo"}
      </p>
      {!estado || estado.id !== acuario.id ? (
        <p className="text-sm text-texto-suave">Cargando…</p>
      ) : estado.error ? (
        <p className="text-sm text-peligro">{estado.error}</p>
      ) : estado.existencias!.length === 0 ? (
        <p className="text-sm text-texto-suave">Acuario vacío.</p>
      ) : (
        <ul className="flex flex-col divide-y divide-borde">
          {estado.existencias!.map((e) => (
            <li key={e.id} className="flex items-center justify-between gap-3 py-2">
              <div className="min-w-0">
                <p className="truncate text-sm text-texto">
                  <span className="mr-1 font-mono text-xs text-texto-suave">{e.productoCodigo}</span>
                  <span className="italic">{e.nombreCientifico}</span>
                </p>
                <Link href={`/inventario/lotes/${e.loteId}`} className="text-xs text-acento hover:underline">
                  {e.loteCodigo}
                </Link>
                <span className="text-xs text-texto-suave"> · {e.diasEnBodega} días</span>
              </div>
              <span className={`shrink-0 text-sm font-semibold tabular-nums ${e.condicion === "enfermo" ? "text-peligro" : "text-texto"}`}>
                {piezas(e.cantidad)}
                {e.condicion === "enfermo" ? " enf." : ""}
              </span>
            </li>
          ))}
        </ul>
      )}
      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Link href={`/inventario/existencias?q=${encodeURIComponent(acuario.codigo)}`} className={botonSecundarioClass}>
          Ver en existencias
        </Link>
        {puedeEditar ? (
          <button type="button" onClick={onEditar} className={botonPrimarioClass}>
            Editar acuario
          </button>
        ) : null}
      </div>
    </div>
  );
}

function FormularioAcuario({
  acuario,
  sectorPorDefecto,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  acuario: Acuario | null;
  sectorPorDefecto: string;
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [codigo, setCodigo] = useState(acuario?.codigo ?? "");
  const [sector, setSector] = useState(acuario?.sector ?? sectorPorDefecto);
  const [nivel, setNivel] = useState(String(acuario?.nivel ?? 1));
  const [posicion, setPosicion] = useState(String(acuario?.posicion ?? 1));
  const [capacidad, setCapacidad] = useState(acuario?.capacidadLitros ? String(acuario.capacidadLitros) : "");
  const [activo, setActivo] = useState(acuario?.activo ?? true);

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({ codigo, sector, nivel: Number(nivel), posicion: Number(posicion), capacidadLitros: capacidad ? Number(capacidad) : null, activo });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <Campo label="Código" htmlFor="codigoAcuario" hint={acuario ? "No cambia." : "Ej. A101 (sector, nivel, posición)."}>
          <input id="codigoAcuario" value={codigo} readOnly={!!acuario} onChange={(e) => setCodigo(e.target.value.toUpperCase())} maxLength={20} className={`${inputClass} font-mono read-only:bg-superficie-alterna`} />
        </Campo>
        <Campo label="Sector" htmlFor="sector">
          <input id="sector" value={sector} onChange={(e) => setSector(e.target.value.toUpperCase())} maxLength={10} className={inputClass} />
        </Campo>
        <Campo label="Nivel" htmlFor="nivel">
          <input id="nivel" type="number" min={0} value={nivel} onChange={(e) => setNivel(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Posición" htmlFor="posicion">
          <input id="posicion" type="number" min={0} value={posicion} onChange={(e) => setPosicion(e.target.value)} className={inputClass} />
        </Campo>
        <Campo label="Capacidad (litros, opcional)" htmlFor="capacidad">
          <input id="capacidad" type="number" min={0} step="0.1" value={capacidad} onChange={(e) => setCapacidad(e.target.value)} className={inputClass} />
        </Campo>
        <Casilla label="Activo" checked={activo} onChange={setActivo} />
      </RejillaCampos>
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
