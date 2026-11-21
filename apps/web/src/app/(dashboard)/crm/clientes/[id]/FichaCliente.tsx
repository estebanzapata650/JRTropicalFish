"use client";

import { useState, useTransition, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ArrowLeft, Mail, Pencil, Trash2 } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import { HistorialRemoto } from "@/components/HistorialCambios";
import { EncabezadoTabla, Etiqueta, TablaResponsive } from "@/components/campos";
import {
  ESTADOS_CLIENTE,
  ESTADOS_PAGO,
  ESTADOS_TAREA,
  ETAPAS,
  ETIQUETAS_HISTORIAL_CRM,
  FASES_TRACKING,
  LICENCIAS,
  TIPOS_CLIENTE,
  TIPOS_VENTA,
  tonoEtapa,
  tonoPago,
  type Cliente,
  type ClienteRelacionados,
} from "@/lib/crm";
import { botonSecundarioClass } from "@/lib/estilos";
import { dinero, fecha } from "@/lib/formato";
import { eliminarClienteAction, guardarClienteAction } from "../../actions";
import { BotonesContacto } from "../../comun";
import { FormularioCliente, type DatosCliente } from "../FormularioCliente";

type Pestana = "oportunidades" | "ventas" | "vuelos" | "tareas" | "historial";

export function FichaCliente({
  cliente,
  relacionados,
  puedeEditar,
}: {
  cliente: Cliente;
  relacionados: ClienteRelacionados | null;
  puedeEditar: boolean;
}) {
  const router = useRouter();
  const [pestana, setPestana] = useState<Pestana>(relacionados ? "oportunidades" : "historial");
  const [editando, setEditando] = useState(false);
  const [eliminando, setEliminando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [procesando, iniciar] = useTransition();

  const guardar = (datos: DatosCliente) =>
    iniciar(async () => {
      const r = await guardarClienteAction(cliente.id, datos);
      if (r.error) {
        setError(r.error);
        return;
      }
      setEditando(false);
      setError(null);
      router.refresh();
    });

  const eliminar = () =>
    iniciar(async () => {
      const r = await eliminarClienteAction(cliente.id);
      if (r.error) {
        setError(r.error);
        return;
      }
      router.push("/crm/clientes");
    });

  const pestanas: { id: Pestana; titulo: string; cantidad?: number }[] = [
    ...(relacionados
      ? [
          { id: "oportunidades" as const, titulo: "Oportunidades", cantidad: relacionados.oportunidades.length },
          { id: "ventas" as const, titulo: "Ventas", cantidad: relacionados.ventas.length },
          { id: "vuelos" as const, titulo: "Vuelos", cantidad: relacionados.vuelos.length },
          { id: "tareas" as const, titulo: "Tareas", cantidad: relacionados.tareas.length },
        ]
      : []),
    { id: "historial", titulo: "Historial" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <Link href="/crm/clientes" className="inline-flex w-fit items-center gap-1.5 text-sm text-texto-suave hover:text-texto">
        <ArrowLeft className="size-4" aria-hidden /> Clientes
      </Link>

      <header className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex min-w-0 flex-col gap-1.5">
          <p className="text-xs font-medium text-texto-suave">{cliente.codigo}</p>
          <h1 className="text-xl font-semibold break-words text-texto">{cliente.nombreEmpresa}</h1>
          <div className="flex flex-wrap gap-2">
            <Etiqueta texto={ESTADOS_CLIENTE[cliente.estado]} tono={cliente.estado === "activo" ? "exito" : cliente.estado === "prospecto" ? "info" : "neutro"} />
            <Etiqueta texto={TIPOS_CLIENTE[cliente.tipoCliente]} tono="neutro" />
            <Etiqueta
              texto={`Licencia: ${LICENCIAS[cliente.licenciaImportacion]}`}
              tono={cliente.licenciaImportacion === "activa" ? "exito" : cliente.licenciaImportacion === "vencida" ? "peligro" : "neutro"}
            />
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          <BotonesContacto telefono={cliente.telefonoWhatsapp} nombre={cliente.nombreEmpresa} />
          {cliente.email ? (
            <a href={`mailto:${cliente.email}`} className={`${botonSecundarioClass} gap-2`} aria-label={`Escribir un correo a ${cliente.nombreEmpresa}`}>
              <Mail className="size-4" aria-hidden /> Correo
            </a>
          ) : null}
          {puedeEditar ? (
            <>
              <button type="button" onClick={() => setEditando(true)} className={`${botonSecundarioClass} gap-2`}>
                <Pencil className="size-4" aria-hidden /> Editar
              </button>
              <button
                type="button"
                onClick={() => setEliminando(true)}
                className={`${botonSecundarioClass} gap-2 text-peligro`}
                aria-label={`Eliminar ${cliente.nombreEmpresa}`}
              >
                <Trash2 className="size-4" aria-hidden />
              </button>
            </>
          ) : null}
        </div>
      </header>

      <dl className="grid grid-cols-1 gap-4 rounded-md border border-borde bg-superficie p-4 sm:grid-cols-2 lg:grid-cols-4">
        <Dato titulo="Contacto">{cliente.contactoPrincipal}</Dato>
        <Dato titulo="Teléfono / WhatsApp">{cliente.telefonoWhatsapp}</Dato>
        <Dato titulo="Correo">{cliente.email}</Dato>
        <Dato titulo="Destino">
          {[cliente.ciudad, cliente.pais].filter(Boolean).join(", ")}
          {cliente.aeropuertoIata ? <span className="ml-1 font-mono text-texto-suave">({cliente.aeropuertoIata})</span> : null}
        </Dato>
        {cliente.notas ? (
          <div className="sm:col-span-2 lg:col-span-4">
            <Dato titulo="Notas">
              <span className="whitespace-pre-line">{cliente.notas}</span>
            </Dato>
          </div>
        ) : null}
      </dl>

      <div className="flex flex-col gap-4">
        <div role="tablist" aria-label="Información vinculada" className="-mx-4 flex gap-1 overflow-x-auto border-b border-borde px-4 md:mx-0 md:px-0">
          {pestanas.map((p) => (
            <button
              key={p.id}
              type="button"
              role="tab"
              aria-selected={pestana === p.id}
              onClick={() => setPestana(p.id)}
              className={`shrink-0 border-b-2 px-3 py-2.5 text-sm transition-colors ${
                pestana === p.id ? "border-acento font-medium text-texto" : "border-transparent text-texto-suave hover:text-texto"
              }`}
            >
              {p.titulo}
              {p.cantidad !== undefined ? <span className="ml-1.5 text-xs text-texto-tenue">{p.cantidad}</span> : null}
            </button>
          ))}
        </div>

        {relacionados && pestana === "oportunidades" ? (
          <Lista vacio="Sin oportunidades." columnas={["Oportunidad", "Etapa", "Monto", "Comercial"]}>
            {relacionados.oportunidades.map((o) => (
              <tr key={o.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <span className="font-medium text-texto">{o.codigo}</span>
                  <p className="text-xs text-texto-suave">{TIPOS_VENTA[o.tipoVenta]} · {fecha(o.fechaContactoInicial)}</p>
                </td>
                <td className="px-4 py-3">
                  <Etiqueta texto={ETAPAS[o.etapa]} tono={tonoEtapa(o.etapa)} />
                </td>
                <td className="px-4 py-3 tabular-nums text-texto">{dinero(o.montoEstimado, o.moneda)}</td>
                <td className="px-4 py-3 text-texto">{o.comercialNombre}</td>
              </tr>
            ))}
          </Lista>
        ) : null}

        {relacionados && pestana === "ventas" ? (
          <Lista vacio="Sin ventas." columnas={["Venta", "Total", "Pago", "Guía AWB"]}>
            {relacionados.ventas.map((v) => (
              <tr key={v.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <span className="font-medium text-texto">{v.codigo}</span>
                  <p className="text-xs text-texto-suave">{TIPOS_VENTA[v.tipoVenta]} · {fecha(v.fechaPedido)}</p>
                </td>
                <td className="px-4 py-3 tabular-nums text-texto">{dinero(v.total, v.moneda)}</td>
                <td className="px-4 py-3">
                  <Etiqueta texto={ESTADOS_PAGO[v.estadoPago]} tono={tonoPago(v.estadoPago)} />
                </td>
                <td className="px-4 py-3 font-mono text-xs text-texto">{v.noGuiaAwb ?? "—"}</td>
              </tr>
            ))}
          </Lista>
        ) : null}

        {relacionados && pestana === "vuelos" ? (
          <Lista vacio="Sin despachos de exportación." columnas={["Despacho", "Fase", "AWB / aerolínea", "% DOA"]}>
            {relacionados.vuelos.map((t) => (
              <tr key={t.id} className="border-b border-borde last:border-0">
                <td className="px-4 py-3">
                  <span className="font-medium text-texto">{t.codigo}</span>
                  <p className="text-xs text-texto-suave">{t.ventaCodigo} · {fecha(t.fechaDespachoVuelo)}</p>
                </td>
                <td className="px-4 py-3 text-texto">{FASES_TRACKING[t.fase]}</td>
                <td className="px-4 py-3 text-texto">
                  <span className="font-mono text-xs">{t.noAwb ?? "—"}</span>
                  <p className="text-xs text-texto-suave">{t.aerolinea}</p>
                </td>
                <td className="px-4 py-3 tabular-nums text-texto">{t.porcentajeDoa == null ? "—" : `${t.porcentajeDoa.toLocaleString("es-CO")} %`}</td>
              </tr>
            ))}
          </Lista>
        ) : null}

        {relacionados && pestana === "tareas" ? (
          <Lista vacio="Sin tareas." columnas={["Acción", "Estado", "Vence", "Comercial"]}>
            {relacionados.tareas.map((t) => (
              <tr key={t.id} className={`border-b border-borde last:border-0 ${t.vencida ? "bg-peligro/10" : ""}`}>
                <td className="px-4 py-3 text-texto">{t.accion}</td>
                <td className="px-4 py-3 text-texto">{ESTADOS_TAREA[t.estado]}</td>
                <td className={`px-4 py-3 ${t.vencida ? "font-medium text-peligro" : "text-texto"}`}>
                  {fecha(t.fechaVencimiento)}
                  {t.vencida ? <span className="sr-only"> (vencida)</span> : null}
                </td>
                <td className="px-4 py-3 text-texto">{t.comercialNombre}</td>
              </tr>
            ))}
          </Lista>
        ) : null}

        {pestana === "historial" ? (
          <HistorialRemoto ruta={`/api/crm/clientes/${cliente.id}/historial`} etiquetas={ETIQUETAS_HISTORIAL_CRM} />
        ) : null}
      </div>

      <Dialogo titulo={`Editar ${cliente.nombreEmpresa}`} abierto={editando} onCerrar={() => setEditando(false)} ancho="max-w-4xl">
        <FormularioCliente cliente={cliente} guardando={procesando} error={error} onGuardar={guardar} onCancelar={() => setEditando(false)} />
      </Dialogo>
      <DialogoConfirmacion
        titulo="Eliminar cliente"
        mensaje={
          <>
            ¿Eliminar a <strong>{cliente.nombreEmpresa}</strong>? Solo es posible si no tiene oportunidades, ventas ni tareas;
            si las tiene, márcalo como Inactivo.
          </>
        }
        textoConfirmar="Eliminar"
        peligro
        abierto={eliminando}
        procesando={procesando}
        error={error}
        onConfirmar={eliminar}
        onCerrar={() => {
          setEliminando(false);
          setError(null);
        }}
      />
    </div>
  );
}

function Dato({ titulo, children }: { titulo: string; children: ReactNode }) {
  return (
    <div className="flex min-w-0 flex-col gap-0.5">
      <dt className="text-xs text-texto-suave">{titulo}</dt>
      <dd className="text-sm break-words text-texto">{children || "—"}</dd>
    </div>
  );
}

function Lista({ columnas, vacio, children }: { columnas: string[]; vacio: string; children: ReactNode[] }) {
  if (children.length === 0) return <p className="py-6 text-center text-sm text-texto-suave">{vacio}</p>;
  return (
    <TablaResponsive>
      <EncabezadoTabla>
        <tr className="text-texto-suave">
          {columnas.map((c) => (
            <th key={c} className="px-4 py-3 font-medium">
              {c}
            </th>
          ))}
        </tr>
      </EncabezadoTabla>
      <tbody>{children}</tbody>
    </TablaResponsive>
  );
}
