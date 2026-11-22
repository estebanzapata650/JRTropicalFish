"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState, useTransition } from "react";
import { History, Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { BotonIcono, Campo, CampoAncho, EncabezadoTabla, Etiqueta, RejillaCampos, TablaResponsive } from "@/components/campos";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { botonPrimarioClass, botonSecundarioClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { ESTADOS_PROTOCOLO, fechaCorta, type Medicamento, type PasoProtocolo, type Protocolo } from "@/lib/sanidad";
import { cambiarEstadoProtocoloAction, guardarProtocoloAction } from "../actions";
import { EditorPasos, pasoParaApi, pasoVacio, type PasoEditable } from "../EditorPasos";

type Opcion = { id: string; nombre: string };

const tono = (e: string) => (e === "aprobado" ? "exito" : e === "borrador" ? "advertencia" : "neutro");

const criterios = (p: Protocolo) =>
  [p.productoCodigo, p.grupoNombre, p.centroNombre, p.cuencaNombre].filter(Boolean).join(" · ") || "Todas las especies";

/**
 * Plantillas de cuarentena y profilaxis. Solo un protocolo aprobado (por el veterinario o gerencia) se sugiere
 * al recibir un lote; editar uno aprobado lo devuelve a borrador.
 */
export function ProtocolosCliente({
  protocolos,
  medicamentos,
  productos,
  grupos,
  cuencas,
  centros,
  gestionar,
  aprobar,
}: {
  protocolos: Protocolo[];
  medicamentos: Medicamento[];
  productos: Opcion[];
  grupos: Opcion[];
  cuencas: Opcion[];
  centros: Opcion[];
  gestionar: boolean;
  aprobar: boolean;
}) {
  const [editando, setEditando] = useState<Protocolo | "nuevo" | null>(null);
  const [historial, setHistorial] = useState<Protocolo | null>(null);

  return (
    <div className="flex flex-col gap-4">
      <PageHeader
        titulo="Protocolos de cuarentena"
        descripcion="Qué aplicar a un lote al llegar, día por día. Se sugieren solo los aprobados por el veterinario o gerencia."
        acciones={
          gestionar ? (
            <button type="button" className={botonPrimarioClass} onClick={() => setEditando("nuevo")}>
              <Plus className="mr-1 size-4" aria-hidden /> Nuevo
            </button>
          ) : null
        }
      />
      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr>
            <th className="px-3 py-2 font-medium">Protocolo</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Aplica a</th>
            <th className="hidden px-3 py-2 font-medium md:table-cell">Días</th>
            <th className="hidden px-3 py-2 font-medium lg:table-cell">Aprobado</th>
            <th className="px-3 py-2 font-medium">Estado</th>
            <th className="px-3 py-2">
              <span className="sr-only">Acciones</span>
            </th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {protocolos.map((p) => (
            <tr key={p.id} className="border-b border-borde align-top last:border-0">
              <td className="px-3 py-2">
                <span className="font-medium text-texto">{p.nombre}</span>
                <span className="block text-xs text-texto-suave">
                  {p.codigo} · {p.pasos} pasos
                </span>
                <span className="block text-xs text-texto-suave md:hidden">{criterios(p)}</span>
              </td>
              <td className="hidden px-3 py-2 md:table-cell">{criterios(p)}</td>
              <td className="hidden px-3 py-2 md:table-cell">{p.diasCuarentena ?? <Etiqueta texto="Sin definir" tono="advertencia" />}</td>
              <td className="hidden px-3 py-2 text-xs lg:table-cell">{p.aprobadoPor ? `${p.aprobadoPor} · ${fechaCorta(p.aprobadoEn)}` : "—"}</td>
              <td className="px-3 py-2">
                <Etiqueta texto={ESTADOS_PROTOCOLO[p.estado]} tono={tono(p.estado)} />
              </td>
              <td className="px-3 py-2 text-right whitespace-nowrap">
                <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${p.nombre}`} onClick={() => setHistorial(p)} />
                <BotonIcono accion="editar" etiqueta={`${gestionar ? "Editar" : "Ver"} ${p.nombre}`} onClick={() => setEditando(p)} />
              </td>
            </tr>
          ))}
        </tbody>
      </TablaResponsive>

      <Dialogo titulo={editando === "nuevo" ? "Nuevo protocolo" : (editando?.nombre ?? "")} abierto={editando !== null} onCerrar={() => setEditando(null)} ancho="max-w-4xl">
        {editando ? (
          <FormProtocolo
            key={editando === "nuevo" ? "nuevo" : editando.id}
            protocolo={editando === "nuevo" ? null : editando}
            medicamentos={medicamentos}
            productos={productos}
            grupos={grupos}
            cuencas={cuencas}
            centros={centros}
            gestionar={gestionar}
            aprobar={aprobar}
            onListo={() => setEditando(null)}
          />
        ) : null}
      </Dialogo>
      <Dialogo titulo={`Historial de ${historial?.nombre ?? ""}`} abierto={historial !== null} onCerrar={() => setHistorial(null)}>
        {historial ? <HistorialRemoto ruta={`/api/sanidad/historial/Protocolo/${historial.id}`} etiquetas={{}} /> : null}
      </Dialogo>
    </div>
  );
}

function FormProtocolo({
  protocolo,
  medicamentos,
  productos,
  grupos,
  cuencas,
  centros,
  gestionar,
  aprobar,
  onListo,
}: {
  protocolo: Protocolo | null;
  medicamentos: Medicamento[];
  productos: Opcion[];
  grupos: Opcion[];
  cuencas: Opcion[];
  centros: Opcion[];
  gestionar: boolean;
  aprobar: boolean;
  onListo: () => void;
}) {
  const router = useRouter();
  const p = protocolo;
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [f, setF] = useState({
    nombre: p?.nombre ?? "",
    descripcion: p?.descripcion ?? "",
    productoId: p?.productoId ?? "",
    grupoId: p?.grupoId ?? "",
    centroAcopioId: p?.centroAcopioId ?? "",
    cuencaId: p?.cuencaId ?? "",
    diasCuarentena: p?.diasCuarentena ? String(p.diasCuarentena) : "",
    ayunoInicialHoras: String(p?.ayunoInicialHoras ?? 24),
  });
  const [pasos, setPasos] = useState<PasoEditable[] | null>(p ? null : [pasoVacio()]);
  const editable = gestionar && p?.estado !== "inactivo";
  const set = (cambio: Partial<typeof f>) => setF((x) => ({ ...x, ...cambio }));

  // Los pasos de un protocolo existente se cargan al abrirlo.
  useEffect(() => {
    if (!p) return;
    let vigente = true;
    fetch(`/api/sanidad/protocolos/${p.id}`, { cache: "no-store" })
      .then((r) => r.json() as Promise<{ pasos: PasoProtocolo[] }>)
      .then(
        (d) =>
          vigente &&
          setPasos(
            d.pasos.map((s) => ({
              clave: s.id,
              dia: s.dia,
              accion: s.accion,
              medicamentoId: s.medicamentoId ?? "",
              porcentajeAgua: s.porcentajeAgua ? String(s.porcentajeAgua) : "",
              nota: s.nota ?? "",
            })),
          ),
      )
      .catch(() => vigente && setError("No se pudieron cargar los pasos."));
    return () => {
      vigente = false;
    };
  }, [p]);

  const terminar = (r: { error?: string }) => {
    if (r.error) setError(r.error);
    else {
      onListo();
      router.refresh();
    }
  };

  const guardar = () =>
    iniciar(async () => {
      setError(null);
      terminar(
        await guardarProtocoloAction(p?.id ?? null, {
          nombre: f.nombre,
          descripcion: f.descripcion || null,
          productoId: f.productoId || null,
          grupoId: f.grupoId || null,
          centroAcopioId: f.centroAcopioId || null,
          cuencaId: f.cuencaId || null,
          diasCuarentena: f.diasCuarentena ? Number(f.diasCuarentena) : null,
          ayunoInicialHoras: Number(f.ayunoInicialHoras) || 0,
          pasos: (pasos ?? []).map(pasoParaApi),
        }),
      );
    });

  const estado = (nuevo: string) =>
    iniciar(async () => {
      setError(null);
      terminar(await cambiarEstadoProtocoloAction(p!.id, p!.estado, nuevo));
    });

  const select = (id: string, label: string, valor: string, opciones: Opcion[], cambiar: (v: string) => void) => (
    <Campo label={label} htmlFor={id}>
      <select id={id} className={selectClass} value={valor} onChange={(e) => cambiar(e.target.value)} disabled={!editable}>
        <option value="">Cualquiera</option>
        {opciones.map((o) => (
          <option key={o.id} value={o.id}>
            {o.nombre}
          </option>
        ))}
      </select>
    </Campo>
  );

  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        guardar();
      }}
    >
      {p?.estado === "aprobado" && editable ? (
        <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">Guardar cambios lo devuelve a borrador: habrá que volver a aprobarlo.</p>
      ) : null}
      <RejillaCampos>
        <Campo label="Nombre" htmlFor="prt-nombre">
          <input id="prt-nombre" className={inputClass} value={f.nombre} onChange={(e) => set({ nombre: e.target.value })} maxLength={150} required disabled={!editable} />
        </Campo>
        <Campo label="Días de cuarentena" htmlFor="prt-dias" hint="Obligatorio para aprobar">
          <input id="prt-dias" type="number" min={1} max={120} className={inputClass} value={f.diasCuarentena} onChange={(e) => set({ diasCuarentena: e.target.value })} disabled={!editable} />
        </Campo>
        <Campo label="Ayuno inicial (horas)" htmlFor="prt-ayuno">
          <input id="prt-ayuno" type="number" min={0} max={168} className={inputClass} value={f.ayunoInicialHoras} onChange={(e) => set({ ayunoInicialHoras: e.target.value })} disabled={!editable} />
        </Campo>
        {select("prt-grupo", "Grupo de producto", f.grupoId, grupos, (v) => set({ grupoId: v }))}
        {select("prt-producto", "Especie (producto)", f.productoId, productos, (v) => set({ productoId: v }))}
        {select("prt-centro", "Centro de acopio (procedencia)", f.centroAcopioId, centros, (v) => set({ centroAcopioId: v }))}
        {select("prt-cuenca", "Cuenca", f.cuencaId, cuencas, (v) => set({ cuencaId: v }))}
        <CampoAncho>
          <Campo label="Descripción" htmlFor="prt-descripcion">
            <textarea id="prt-descripcion" className={textareaClass} value={f.descripcion} onChange={(e) => set({ descripcion: e.target.value })} maxLength={2000} disabled={!editable} />
          </Campo>
        </CampoAncho>
      </RejillaCampos>

      <section className="flex flex-col gap-2">
        <h3 className="text-sm font-semibold text-texto">Pasos por día</h3>
        {pasos === null ? <p className="text-sm text-texto-suave">Cargando pasos…</p> : <EditorPasos pasos={pasos} onChange={setPasos} medicamentos={medicamentos} deshabilitado={!editable} />}
      </section>

      {error ? <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">{error}</p> : null}
      <div className="flex flex-wrap gap-2">
        {editable ? (
          <button type="submit" disabled={pendiente || pasos === null} className={botonPrimarioClass}>
            Guardar
          </button>
        ) : null}
        {p?.estado === "borrador" && aprobar ? (
          <button type="button" disabled={pendiente} className={botonSecundarioClass} onClick={() => estado("aprobado")}>
            Aprobar
          </button>
        ) : null}
        {p && p.estado !== "inactivo" && gestionar ? (
          <button type="button" disabled={pendiente} className={botonSecundarioClass} onClick={() => estado("inactivo")}>
            Inactivar
          </button>
        ) : null}
        {p?.estado === "inactivo" && gestionar ? (
          <button type="button" disabled={pendiente} className={botonSecundarioClass} onClick={() => estado("borrador")}>
            Reactivar (a borrador)
          </button>
        ) : null}
      </div>
    </form>
  );
}
