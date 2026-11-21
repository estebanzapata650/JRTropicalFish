"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { History, Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, CampoAncho, Casilla, EncabezadoTabla, EtiquetaEstado, RejillaCampos, TablaResponsive } from "@/components/campos";
import { ETIQUETAS_HISTORIAL_CATALOGO, type Proveedor } from "@/lib/catalogos";
import { botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass, selectClass, textareaClass } from "@/lib/estilos";
import { BotonesContacto } from "../../crm/comun";
import { eliminarProveedorAction, guardarProveedorAction } from "../actions";

type CentroOpcion = { id: string; nombre: string; activo: boolean };
type TipoOpcion = { id: string; codigo: string; nombre: string };

export function ProveedoresCliente({
  proveedores,
  centros,
  tipos,
  puedeEditar,
}: {
  proveedores: Proveedor[];
  centros: CentroOpcion[];
  tipos: TipoOpcion[];
  puedeEditar: boolean;
}) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [centro, setCentro] = useState("");
  const [editando, setEditando] = useState<Proveedor | null>(null);
  const [creando, setCreando] = useState(false);
  const [historialDe, setHistorialDe] = useState<Proveedor | null>(null);
  const [eliminando, setEliminando] = useState<Proveedor | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const visibles = useMemo(() => {
    const t = busqueda.trim().toLowerCase();
    return proveedores.filter(
      (p) =>
        (!centro || p.centrosAcopio.some((c) => c.id === centro)) &&
        (!t || [p.nombre, p.numeroIdentificacion, p.contacto, p.telefono].some((v) => v?.toLowerCase().includes(t))),
    );
  }, [proveedores, busqueda, centro]);

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
        titulo="Proveedores"
        descripcion="Pescadores y acopiadores nacionales, por centro de acopio."
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nuevo proveedor
            </button>
          ) : null
        }
      />
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto]">
        <label className="relative">
          <span className="sr-only">Buscar proveedor</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input value={busqueda} onChange={(e) => setBusqueda(e.target.value)} placeholder="Nombre, identificación o teléfono…" className={`${inputClass} pl-9`} />
        </label>
        <select aria-label="Filtrar por centro de acopio" value={centro} onChange={(e) => setCentro(e.target.value)} className={`${selectClass} sm:w-56`}>
          <option value="">Todos los centros</option>
          {centros.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nombre}
            </option>
          ))}
        </select>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Proveedor</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">Centros de acopio</th>
            <th className="hidden px-4 py-3 font-medium sm:table-cell">Estado</th>
            <th className="px-4 py-3 text-right font-medium">Acciones</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {visibles.map((p) => (
            <tr key={p.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <p className="font-medium text-texto">{p.nombre}</p>
                <p className="text-xs text-texto-suave">
                  {p.tipoIdentificacionCodigo} {p.numeroIdentificacion}
                  {p.telefono ? ` · ${p.telefono}` : ""}
                </p>
                <p className="text-xs text-texto-suave md:hidden">{p.centrosAcopio.map((c) => c.nombre).join(", ")}</p>
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">{p.centrosAcopio.map((c) => c.nombre).join(", ") || "—"}</td>
              <td className="hidden px-4 py-3 sm:table-cell">
                <EtiquetaEstado activo={p.activo} />
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <BotonesContacto telefono={p.telefono} nombre={p.nombre} compacto />
                  <BotonIcono accion="otra" icono={History} etiqueta={`Historial de ${p.nombre}`} onClick={() => setHistorialDe(p)} />
                  {puedeEditar ? <BotonIcono accion="editar" etiqueta={`Editar ${p.nombre}`} onClick={() => setEditando(p)} /> : null}
                </div>
              </td>
            </tr>
          ))}
          {visibles.length === 0 ? (
            <tr>
              <td colSpan={4} className="px-4 py-8 text-center text-texto-suave">
                {proveedores.length === 0 ? "Aún no hay proveedores." : "Ningún proveedor coincide con el filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <p className="text-sm text-texto-suave">
        {visibles.length} de {proveedores.length} proveedores
      </p>

      <Dialogo titulo={editando ? `Editar ${editando.nombre}` : "Nuevo proveedor"} abierto={creando || editando !== null} onCerrar={cerrar} ancho="max-w-4xl">
        <FormularioProveedor
          key={editando?.id ?? "nuevo"}
          proveedor={editando}
          centros={centros}
          tipos={tipos}
          guardando={procesando}
          error={error}
          onGuardar={(datos) => correr(() => guardarProveedorAction(editando?.id ?? null, datos))}
          onCancelar={cerrar}
        />
        {editando && puedeEditar ? (
          <div className="mt-6 border-t border-borde pt-4">
            <button type="button" onClick={() => setEliminando(editando)} className="text-sm font-medium text-peligro hover:underline">
              Eliminar este proveedor
            </button>
          </div>
        ) : null}
      </Dialogo>
      <Dialogo titulo={historialDe ? `Historial de ${historialDe.nombre}` : ""} abierto={historialDe !== null} onCerrar={() => setHistorialDe(null)}>
        {historialDe ? (
          <HistorialRemoto ruta={`/api/catalogos/proveedores/${historialDe.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CATALOGO} />
        ) : null}
      </Dialogo>
      <DialogoConfirmacion
        titulo="Eliminar proveedor"
        mensaje={eliminando ? <>¿Eliminar a <strong>{eliminando.nombre}</strong>? Si ya no trabaja con ustedes, mejor márcalo inactivo.</> : null}
        textoConfirmar="Eliminar"
        peligro
        abierto={eliminando !== null}
        procesando={procesando}
        error={error}
        onConfirmar={() => eliminando && correr(() => eliminarProveedorAction(eliminando.id))}
        onCerrar={() => {
          setEliminando(null);
          setError(null);
        }}
      />
    </div>
  );
}

function FormularioProveedor({
  proveedor,
  centros,
  tipos,
  guardando,
  error,
  onGuardar,
  onCancelar,
}: {
  proveedor: Proveedor | null;
  centros: CentroOpcion[];
  tipos: TipoOpcion[];
  guardando: boolean;
  error: string | null;
  onGuardar: (datos: Record<string, unknown>) => void;
  onCancelar: () => void;
}) {
  const [nombre, setNombre] = useState(proveedor?.nombre ?? "");
  const [tipoId, setTipoId] = useState(proveedor?.tipoIdentificacionId ?? tipos.find((t) => t.codigo === "CC")?.id ?? "");
  const [numero, setNumero] = useState(proveedor?.numeroIdentificacion ?? "");
  const [telefono, setTelefono] = useState(proveedor?.telefono ?? "");
  const [contacto, setContacto] = useState(proveedor?.contacto ?? "");
  const [email, setEmail] = useState(proveedor?.email ?? "");
  const [notas, setNotas] = useState(proveedor?.notas ?? "");
  const [activo, setActivo] = useState(proveedor?.activo ?? true);
  const [elegidos, setElegidos] = useState<string[]>(proveedor?.centrosAcopio.map((c) => c.id) ?? []);
  const opcionesCentro = centros.filter((c) => c.activo || elegidos.includes(c.id));

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onGuardar({
          nombre, tipoIdentificacionId: tipoId, numeroIdentificacion: numero, telefono, contacto, email, notas, activo, centrosAcopio: elegidos,
        });
      }}
      className="flex flex-col gap-6"
    >
      <RejillaCampos>
        <CampoAncho>
          <Campo label="Nombre" htmlFor="nombreProveedor">
            <input id="nombreProveedor" value={nombre} onChange={(e) => setNombre(e.target.value)} maxLength={200} className={inputClass} />
          </Campo>
        </CampoAncho>
        <Campo label="Tipo de identificación" htmlFor="tipoId">
          <select id="tipoId" value={tipoId} onChange={(e) => setTipoId(e.target.value)} className={selectClass}>
            {tipos.map((t) => (
              <option key={t.id} value={t.id}>
                {t.codigo} · {t.nombre}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Número" htmlFor="numeroId">
          <input id="numeroId" value={numero} onChange={(e) => setNumero(e.target.value)} maxLength={30} className={inputClass} />
        </Campo>
        <Campo label="Teléfono / WhatsApp (opcional)" htmlFor="telefonoProveedor">
          <input id="telefonoProveedor" type="tel" value={telefono} onChange={(e) => setTelefono(e.target.value)} maxLength={40} className={inputClass} />
        </Campo>
        <Campo label="Contacto (opcional)" htmlFor="contactoProveedor">
          <input id="contactoProveedor" value={contacto} onChange={(e) => setContacto(e.target.value)} maxLength={150} className={inputClass} />
        </Campo>
        <Campo label="Correo (opcional)" htmlFor="emailProveedor">
          <input id="emailProveedor" type="email" value={email} onChange={(e) => setEmail(e.target.value)} maxLength={320} className={inputClass} />
        </Campo>
        <div className="flex items-end">
          <Casilla label="Activo" checked={activo} onChange={setActivo} />
        </div>
        <CampoAncho>
          <fieldset className="flex flex-col gap-1.5">
            <legend className="mb-1.5 text-sm font-medium text-texto">Centros de acopio</legend>
            <div className="grid grid-cols-1 gap-x-4 sm:grid-cols-2 lg:grid-cols-4">
              {opcionesCentro.map((c) => (
                <label key={c.id} className="flex items-center gap-2.5 py-1 text-sm text-texto">
                  <input
                    type="checkbox"
                    checked={elegidos.includes(c.id)}
                    onChange={(e) => setElegidos((x) => (e.target.checked ? [...x, c.id] : x.filter((id) => id !== c.id)))}
                    className={casillaClass}
                  />
                  {c.nombre}
                </label>
              ))}
            </div>
          </fieldset>
        </CampoAncho>
        <CampoAncho>
          <Campo label="Notas (opcional)" htmlFor="notasProveedor">
            <textarea id="notasProveedor" value={notas} onChange={(e) => setNotas(e.target.value)} maxLength={2000} className={textareaClass} />
          </Campo>
        </CampoAncho>
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
