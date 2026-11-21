"use client";

import { useEffect, useState } from "react";

/**
 * Trae un detalle por el proxy BFF cuando cambia la ruta (o la versión, para recargar tras guardar).
 * Mismo patrón que `HistorialRemoto`: el estado guarda la ruta para no mostrar datos de otra.
 */
export function useDetalle<T>(ruta: string | null, version = 0) {
  const [estado, setEstado] = useState<{ clave: string; datos?: T; error?: string } | null>(null);
  const clave = `${ruta}#${version}`;

  useEffect(() => {
    if (!ruta) return;
    let vigente = true;
    fetch(ruta, { cache: "no-store" })
      .then(async (r) => {
        if (!r.ok) throw new Error(((await r.json().catch(() => ({}))) as { error?: string }).error ?? `Error ${r.status}`);
        return (await r.json()) as T;
      })
      .then((datos) => vigente && setEstado({ clave: `${ruta}#${version}`, datos }))
      .catch((e: Error) => vigente && setEstado({ clave: `${ruta}#${version}`, error: e.message }));
    return () => {
      vigente = false;
    };
  }, [ruta, version]);

  if (!ruta || !estado || estado.clave !== clave) return { cargando: !!ruta, datos: undefined, error: undefined };
  return { cargando: false, datos: estado.datos, error: estado.error };
}
