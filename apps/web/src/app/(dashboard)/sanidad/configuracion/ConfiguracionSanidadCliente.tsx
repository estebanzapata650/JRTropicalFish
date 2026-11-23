"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import {
  Campo,
  EncabezadoTabla,
  Etiqueta,
  TablaResponsive,
} from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import { botonPrimarioClass, inputClass } from "@/lib/estilos";
import { num } from "@/lib/sanidad";
import { asignarResponsableAction, guardarParametrosAction } from "../actions";

export type AcuarioSanidad = {
  id: string;
  codigo: string;
  sector: string;
  litros: number | null;
  responsableId: string | null;
  responsableNombre: string | null;
};

/** Horas de ayuno antes del vuelo (mínimo 48, regla firme) y el operario responsable de cada acuario. */
export function ConfiguracionSanidadCliente({
  horasAyunoVuelo,
  acuarios,
  usuarios,
}: {
  horasAyunoVuelo: number;
  acuarios: AcuarioSanidad[];
  usuarios: { id: string; nombre: string }[];
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [horas, setHoras] = useState(String(horasAyunoVuelo));
  const [mensaje, setMensaje] = useState<{
    tipo: "ok" | "error";
    texto: string;
  } | null>(null);
  const sinLitraje = acuarios.filter((a) => a.litros === null).length;

  const correr = (fn: () => Promise<{ error?: string }>, exito: string) =>
    iniciar(async () => {
      const r = await fn();
      setMensaje(
        r.error
          ? { tipo: "error", texto: r.error }
          : { tipo: "ok", texto: exito },
      );
      if (!r.error) router.refresh();
    });

  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Configuración de sanidad" />
      {mensaje ? (
        <p
          role="status"
          className={`rounded-md px-3 py-2 text-sm ${mensaje.tipo === "ok" ? "bg-exito-fondo text-exito-texto" : "bg-peligro/15 text-peligro"}`}
        >
          {mensaje.texto}
        </p>
      ) : null}

      <form
        className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-4 sm:flex-row sm:items-end"
        onSubmit={(e) => {
          e.preventDefault();
          correr(
            () => guardarParametrosAction(Number(horas)),
            "Parámetros guardados.",
          );
        }}
      >
        <Campo
          label="Ayuno antes del vuelo (horas)"
          htmlFor="horas-ayuno"
          hint="Mínimo 48 h: no se alimenta un acuario reservado para un vuelo que sale antes."
        >
          <input
            id="horas-ayuno"
            type="number"
            min={48}
            max={120}
            className={`${inputClass} sm:w-40`}
            value={horas}
            onChange={(e) => setHoras(e.target.value)}
          />
        </Campo>
        <button
          type="submit"
          disabled={pendiente}
          className={botonPrimarioClass}
        >
          Guardar
        </button>
      </form>

      <section className="flex flex-col gap-3">
        <h2 className="text-base font-semibold text-texto">
          Responsable de cada acuario
        </h2>
        {sinLitraje > 0 ? (
          <p className="rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto">
            {sinLitraje} acuarios sin litraje: sus dosis no se pueden calcular.
            Regístralo en{" "}
            <Link href="/inventario/acuarios" className="underline">
              Acuarios
            </Link>
            .
          </p>
        ) : null}
        {usuarios.length === 0 ? (
          <p className="text-sm text-texto-suave">
            Para elegir responsables necesitas el permiso de consultar usuarios.
          </p>
        ) : null}
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Acuario</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Litros
              </th>
              <th className="px-3 py-2 font-medium">Responsable</th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {acuarios.map((a) => (
              <tr key={a.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2">
                  <span className="font-medium text-texto">{a.codigo}</span>
                  <span className="block text-xs text-texto-suave md:hidden">
                    {a.litros ? `${num(a.litros, 0)} L` : "sin litraje"}
                  </span>
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  {a.litros ? (
                    `${num(a.litros, 0)} L`
                  ) : (
                    <Etiqueta texto="Sin litraje" tono="advertencia" />
                  )}
                </td>
                <td className="px-3 py-2">
                  {usuarios.length > 0 ? (
                    <SelectorBuscable
                      etiqueta={`Responsable de ${a.codigo}`}
                      opciones={usuarios.map((u) => ({
                        id: u.id,
                        nombre: u.nombre,
                      }))}
                      valor={a.responsableId ?? ""}
                      onChange={(v) =>
                        correr(
                          () => asignarResponsableAction(a.id, v || null),
                          `Responsable de ${a.codigo} actualizado.`,
                        )
                      }
                      vacio="Sin responsable"
                      disabled={pendiente}
                    />
                  ) : (
                    (a.responsableNombre ?? "—")
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      </section>
    </div>
  );
}
