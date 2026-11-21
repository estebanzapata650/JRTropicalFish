"use client";

import { useMemo, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus, Search } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import { ESTADOS_CLIENTE, LICENCIAS, TIPOS_CLIENTE, type Cliente } from "@/lib/crm";
import { botonPrimarioClass, inputClass, selectClass } from "@/lib/estilos";
import { guardarClienteAction } from "../actions";
import { BotonesContacto } from "../comun";
import { FormularioCliente, type DatosCliente } from "./FormularioCliente";

const tonoEstado = (estado: string) => (estado === "activo" ? "exito" : estado === "prospecto" ? "info" : "neutro");
const tonoLicencia = (l: string) => (l === "activa" ? "exito" : l === "vencida" ? "peligro" : l === "en_tramite" ? "advertencia" : "neutro");

export function ClientesCliente({ clientes, puedeEditar }: { clientes: Cliente[]; puedeEditar: boolean }) {
  const router = useRouter();
  const [busqueda, setBusqueda] = useState("");
  const [tipo, setTipo] = useState("");
  const [estado, setEstado] = useState("");
  const [editando, setEditando] = useState<Cliente | null>(null);
  const [creando, setCreando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();

  const filtrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();
    return clientes.filter(
      (c) =>
        (!tipo || c.tipoCliente === tipo) &&
        (!estado || c.estado === estado) &&
        (!texto ||
          [c.codigo, c.nombreEmpresa, c.contactoPrincipal, c.pais, c.ciudad, c.aeropuertoIata, c.email]
            .some((v) => v?.toLowerCase().includes(texto))),
    );
  }, [clientes, busqueda, tipo, estado]);

  const cerrar = () => {
    setCreando(false);
    setEditando(null);
    setError(null);
  };

  const guardar = (datos: DatosCliente) =>
    iniciar(async () => {
      const r = await guardarClienteAction(editando?.id ?? null, datos);
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
        titulo="Clientes"
        descripcion="Directorio único de clientes nacionales e internacionales."
        acciones={
          puedeEditar ? (
            <button type="button" onClick={() => setCreando(true)} className={`${botonPrimarioClass} gap-2`}>
              <Plus className="size-4" aria-hidden /> Nuevo cliente
            </button>
          ) : null
        }
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-[1fr_auto_auto]">
        <label className="relative">
          <span className="sr-only">Buscar cliente</span>
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-texto-tenue" aria-hidden />
          <input
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            placeholder="Empresa, contacto, país, IATA…"
            className={`${inputClass} pl-9`}
          />
        </label>
        <select aria-label="Filtrar por tipo" value={tipo} onChange={(e) => setTipo(e.target.value)} className={selectClass}>
          <option value="">Todos los tipos</option>
          {Object.entries(TIPOS_CLIENTE).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
        <select aria-label="Filtrar por estado" value={estado} onChange={(e) => setEstado(e.target.value)} className={selectClass}>
          <option value="">Todos los estados</option>
          {Object.entries(ESTADOS_CLIENTE).map(([v, e]) => (
            <option key={v} value={v}>
              {e}
            </option>
          ))}
        </select>
      </div>

      <TablaResponsive compactaEnMovil>
        <EncabezadoTabla>
          <tr className="text-texto-suave">
            <th className="px-4 py-3 font-medium">Cliente</th>
            <th className="hidden px-4 py-3 font-medium md:table-cell">País / IATA</th>
            <th className="hidden px-4 py-3 font-medium lg:table-cell">Tipo</th>
            <th className="hidden px-4 py-3 font-medium lg:table-cell">Licencia</th>
            <th className="px-4 py-3 font-medium">Estado</th>
            <th className="px-4 py-3 text-right font-medium">Acciones</th>
          </tr>
        </EncabezadoTabla>
        <tbody>
          {filtrados.map((c) => (
            <tr key={c.id} className="border-b border-borde last:border-0">
              <td className="px-4 py-3">
                <Link href={`/crm/clientes/${c.id}`} className="font-medium text-texto hover:text-acento hover:underline">
                  {c.nombreEmpresa}
                </Link>
                <p className="text-xs text-texto-suave">
                  {c.codigo}
                  {c.contactoPrincipal ? ` · ${c.contactoPrincipal}` : ""}
                </p>
              </td>
              <td className="hidden px-4 py-3 text-texto md:table-cell">
                {c.pais}
                {c.aeropuertoIata ? <span className="ml-1 font-mono text-xs text-texto-suave">{c.aeropuertoIata}</span> : null}
              </td>
              <td className="hidden px-4 py-3 text-texto lg:table-cell">{TIPOS_CLIENTE[c.tipoCliente]}</td>
              <td className="hidden px-4 py-3 lg:table-cell">
                <Etiqueta texto={LICENCIAS[c.licenciaImportacion]} tono={tonoLicencia(c.licenciaImportacion)} />
              </td>
              <td className="px-4 py-3">
                <Etiqueta texto={ESTADOS_CLIENTE[c.estado]} tono={tonoEstado(c.estado)} />
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <BotonesContacto telefono={c.telefonoWhatsapp} nombre={c.nombreEmpresa} compacto />
                  {puedeEditar ? (
                    <BotonIcono accion="editar" etiqueta={`Editar ${c.nombreEmpresa}`} onClick={() => setEditando(c)} />
                  ) : null}
                </div>
              </td>
            </tr>
          ))}
          {filtrados.length === 0 ? (
            <tr>
              <td colSpan={6} className="px-4 py-8 text-center text-texto-suave">
                {clientes.length === 0 ? "Aún no hay clientes." : "Ningún cliente coincide con el filtro."}
              </td>
            </tr>
          ) : null}
        </tbody>
      </TablaResponsive>
      <p className="text-xs text-texto-suave">
        {filtrados.length} de {clientes.length} clientes
      </p>

      <Dialogo
        titulo={editando ? `Editar ${editando.nombreEmpresa}` : "Nuevo cliente"}
        abierto={creando || editando !== null}
        onCerrar={cerrar}
        ancho="max-w-4xl"
      >
        <FormularioCliente key={editando?.id ?? "nuevo"} cliente={editando} guardando={guardando} error={error} onGuardar={guardar} onCancelar={cerrar} />
      </Dialogo>
    </div>
  );
}
