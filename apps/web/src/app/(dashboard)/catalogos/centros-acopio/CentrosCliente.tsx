"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, Casilla, EncabezadoTabla, EtiquetaEstado, RejillaCampos, TablaResponsive } from "@/components/campos";
import { ETIQUETAS_HISTORIAL_CATALOGO, type CentroAcopio } from "@/lib/catalogos";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass } from "@/lib/estilos";
import { eliminarCentroAction, guardarCentroAction } from "../actions";

type Opcion = { id: string; nombre: string };

export function CentrosCliente({ centros, cuencas, puedeEditar }: { centros: CentroAcopio[]; cuencas: Opcion[]; puedeEditar: boolean }) {
  const router = useRouter();
  const [cuenca, setCuenca] = useState("");
  const [editando, setEditando] = useState<CentroAcopio | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<CentroAcopio | null>(null);
  const [eliminando, setEliminando] = useState<CentroAcopio | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => centros.filter((c) => !cuenca || c.cuencaId === cuenca), [centros, cuenca]);
  const cuencasConCentros = [...new Map(centros.map((c) => [c.cuencaId, c.cuencaNombre])).entries()];

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setEliminando(null);
    setError(null);
  };

  const correr = (accion: () => Promise<{ error?: string }>) =>
    iniciar(async () => {
      const r = await accion();
      if (r.error) {
        setError(r.error);
        return;
      }
      cerrar();
      router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Centros de acopio"
        descripcion="Lugares de origen de los peces, por cuenca. Las cuencas se administran en Maestros."
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nuevo centro
            </button>
          ) : null
        }
      />
      <select aria-label="Filtrar por cuenca" value={cuenca} onChange={(e) => setCuenca(e.target.value)} className={`${selectClass} sm:w-64`}>
        <option value="">Todas las cuencas</option>
        {cuencasConCentros.map(([id, nombre]) => (
          <option key={id} value={id}>
            {nombre}
          </option>
        ))}
      </select>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Centro</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Productos</th>
            <th className="hidden px-4 py-3 text-right font-medium sm:table-cell">Proveedores</th>
            <th className="px-4 py-3 font-medium">Estado</th>
            <th className="px-4 py-3 text-right font-medium">Acciones</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((c) => (
            <tr key={c.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <p className="font-medium text-texto">{c.nombre}</p>
                <p className="text-xs text-texto-suave">
                  {c.cuencaNombre}
                  <span className="sm:hidden"> · {c.productos} productos · {c.proveedores} proveedores</span>
                </p>
              </td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">{c.productos}</td>
              <td className="hidden px-4 py-3 text-right tabular-nums text-texto sm:table-cell">{c.proveedores}</td>
              <td className="px-4 py-3">
                <EtiquetaEstado activo={c.activo} />
              </td>
              <td className="px-4 py-3">
                <div className="flex justify-end">
                  <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${c.nombre}`} onClick={() => setHistorialDe(c)} />
                  {puedeEditar ? <BotonIcono accion="editar" etiqueta={`Editar ${c.nombre}`} onClick={() => setEditando(c)} /> : null}
                  {puedeEditar ? <BotonIcono accion="eliminar" etiqueta={`Eliminar ${c.nombre}`} onClick={() => setEliminando(c)} /> : null}
                </div>
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={5} className="px-4 py-8 text-center text-texto-suave">
                No hay centros de acopio.
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo={editando ? `Editar ${editando.nombre}` : "Nuevo centro de acopio"} abierto={creando || editando !== null} onCerrar={cerrar} ancho="max-w-xl">
        <FormularioCentro
          key={editando?.id ?? "nuevo"}
          centro={editando}
          cuencas={cuencas}
          guardando={procesando}
          error={error}
          onGuardar={(datos) => correr(() => guardarCentroAction(editando?.id ?? null, datos))}
          onCancelar={cerrar}
        />
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.nombre}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? (
          <HistorialRemoto ruta={`/api/v1/catalogos/centros-acopio/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CATALOGO} />
        ) : null}
      </Dialogo>
      <DialogoConfirmacion
        titulo="Eliminar centro de acopio"
        mensaje={eliminando ? <>¿Eliminar <strong>{eliminando.nombre}</strong>? Solo es posible si no tiene productos ni proveedores.</> : null}
        textoConfirmar="Eliminar"
        peligro
        abierto={eliminando !== null}
        procesando={procesando}
        error={error}
        onConfirmar={() => eliminando && correr(() => eliminarCentroAction(eliminando.id))}
        onCerrar={cerrar}
      />
    </div>
  );
}

function FormularioCentro({
  centro,
  cuencas,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  centro: CentroAcopio | null;
  cuencas: Opcion[];
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [nombre, setNombre] = useState(centro?.nombre ?? "");
  const [cuencaId, setCuencaId] = useState(centro?.cuencaId ?? "");
  const [activo, setActivo] = useState(centro?.activo ?? true);
  const opciones =
    centro && !cuencas.some((c) => c.id === centro.cuencaId) ? [...cuencas, { id: centro.cuencaId, nombre: centro.cuencaNombre }] : cuencas;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({ nombre, cuencaId, activo });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <Campo label="Nombre" htmlFor="nombreCentro">
          <input id="nombreCentro" value={nombre} onChange={(e) => setNombre(e.target.value)} maxLength={100} className={inputClass} />
        </Campo>
        <Campo label="Cuenca" htmlFor="cuencaCentro">
          <select id="cuencaCentro" value={cuencaId} onChange={(e) => setCuencaId(e.target.value)} className={selectClass}>
            <option value="">Selecciona…</option>
            {opciones.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombre}
              </option>
            ))}
          </select>
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
