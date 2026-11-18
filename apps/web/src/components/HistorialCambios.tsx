"use client";

import { useEffect, useState } from "react";
import { fechaHora } from "@/lib/formato";
import type { CambioAuditado, RegistroAuditoria } from "@/lib/types";

const etiquetasAccion: Record<string, string> = {
  Crear: "Creación",
  Actualizar: "Modificación",
  Eliminar: "Eliminación",
};

/**
 * Los cambios de colecciones hijas llegan como `Contacto[Ana Ruiz].Telefono`: el prefijo dice a qué
 * elemento pertenecen y el sufijo es el campo. Se separan para traducir el campo y mostrar el
 * elemento como contexto.
 */
function partirCampo(campo: string, etiquetas: Record<string, string>) {
  const coincidencia = /^(.+?)\[(.+)\]\.(.+)$/.exec(campo);
  if (!coincidencia) {
    return { contexto: null, etiqueta: etiquetas[campo] ?? campo };
  }
  const [, prefijo, elemento, sufijo] = coincidencia;
  return { contexto: `${prefijo} · ${elemento}`, etiqueta: etiquetas[sufijo] ?? sufijo };
}

function valorLegible(valor: string | null) {
  if (valor === null || valor === "") return "—";
  if (valor === "true") return "Sí";
  if (valor === "false") return "No";
  // Fechas ISO (bloqueos, marcas de tiempo) en hora de Bogotá.
  if (/^\d{4}-\d{2}-\d{2}T/.test(valor)) return fechaHora(valor);
  if (/^Bloqueado hasta \d{4}-/.test(valor)) return `Bloqueado hasta ${fechaHora(valor.slice(16))}`;
  return valor;
}

function leerCambios(json: string): CambioAuditado[] {
  try {
    return JSON.parse(json) as CambioAuditado[];
  } catch {
    return [];
  }
}

/** Lista de cambios ya cargada. El backend audita por nombre de propiedad; cada módulo pasa sus etiquetas. */
export function HistorialCambios({
  registros,
  etiquetas,
}: {
  registros: RegistroAuditoria[];
  etiquetas: Record<string, string>;
}) {
  if (registros.length === 0) {
    return <p className="text-sm text-texto-suave">Sin cambios registrados.</p>;
  }

  return (
    <ul className="flex flex-col gap-3">
      {registros.map((registro) => {
        const cambios = leerCambios(registro.cambiosJson);
        return (
          <li key={registro.id} className="rounded-md border border-borde px-4 py-3">
            <div className="flex flex-col gap-0.5">
              <span className="text-sm font-medium text-texto">
                {etiquetasAccion[registro.accion] ?? registro.accion}
              </span>
              <span className="text-xs text-texto-suave">
                {fechaHora(registro.fechaUtc)} · {registro.usuarioNombre ?? "Sistema"}
              </span>
            </div>

            {cambios.length > 0 ? (
              <ul className="mt-2 flex flex-col gap-1.5">
                {cambios.map((cambio) => {
                  const { contexto, etiqueta } = partirCampo(cambio.Campo, etiquetas);
                  return (
                    <li key={cambio.Campo} className="text-sm break-words">
                      {contexto ? <span className="text-texto-suave">{contexto} → </span> : null}
                      <span className="text-texto">{etiqueta}: </span>
                      {registro.accion === "Actualizar" ? (
                        <>
                          <span className="text-texto-suave line-through">{valorLegible(cambio.Anterior)}</span>
                          <span className="text-texto-tenue"> → </span>
                        </>
                      ) : null}
                      <span className="text-texto">
                        {valorLegible(registro.accion === "Eliminar" ? cambio.Anterior : cambio.Nuevo)}
                      </span>
                    </li>
                  );
                })}
              </ul>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

/**
 * Carga el historial bajo demanda por el proxy BFF (`/api/...`) y lo pinta. Se usa dentro de un
 * diálogo: el historial no se trae con la página porque casi nunca se abre.
 */
export function HistorialRemoto({ ruta, etiquetas }: { ruta: string; etiquetas: Record<string, string> }) {
  const [estado, setEstado] = useState<{ ruta: string; registros?: RegistroAuditoria[]; error?: string } | null>(null);

  useEffect(() => {
    let vigente = true;
    fetch(ruta, { cache: "no-store" })
      .then(async (r) => {
        if (!r.ok) throw new Error(((await r.json().catch(() => ({}))) as { error?: string }).error ?? `Error ${r.status}`);
        return (await r.json()) as RegistroAuditoria[];
      })
      .then((registros) => vigente && setEstado({ ruta, registros }))
      .catch((e: Error) => vigente && setEstado({ ruta, error: e.message }));
    return () => {
      vigente = false;
    };
  }, [ruta]);

  // Mientras llega la respuesta de esta ruta (o si quedó la de otra), se muestra "cargando".
  if (!estado || estado.ruta !== ruta) return <p className="text-sm text-texto-suave">Cargando historial…</p>;
  if (estado.error) return <p className="text-sm text-peligro">{estado.error}</p>;
  return <HistorialCambios registros={estado.registros ?? []} etiquetas={etiquetas} />;
}
