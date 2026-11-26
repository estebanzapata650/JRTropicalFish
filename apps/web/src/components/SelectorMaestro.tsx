"use client";

import { useMemo } from "react";
import { useDetalle } from "@/app/(dashboard)/compras/useDetalle";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import type { ValorMaestro } from "@/lib/types";

/**
 * Elige un valor de un maestro (aerolíneas, aeropuertos, causas de mortalidad…) con el buscador. Carga los valores
 * activos por el proxy, así no hay que pasarlos desde cada página. El campo sigue guardando texto: `guardar` decide si
 * se guarda el nombre ("Qatar Airways") o el código ("HKG"). Un valor que ya estaba escrito y no está en el maestro se
 * conserva como opción, para no perderlo al editar.
 */
export function SelectorMaestro({
  clave,
  valor,
  onChange,
  guardar = "nombre",
  id,
  etiqueta,
  placeholder,
  vacio,
  disabled,
}: {
  clave: string;
  valor: string;
  onChange: (valor: string) => void;
  guardar?: "nombre" | "codigo";
  id?: string;
  etiqueta?: string;
  placeholder?: string;
  /** Opción vacía con nombre ("Sin aerolínea"). */
  vacio?: string;
  disabled?: boolean;
}) {
  const { datos, cargando, error } = useDetalle<ValorMaestro[]>(`/api/maestros/${clave}?soloActivos=true`);
  const opciones = useMemo(() => {
    const lista = (datos ?? []).map((m) => ({
      id: guardar === "codigo" ? m.codigo : m.nombre,
      nombre: guardar === "codigo" ? `${m.codigo} · ${m.nombre}` : m.nombre,
      detalle: guardar === "codigo" ? null : m.codigo,
    }));
    if (valor && !lista.some((o) => o.id === valor)) lista.unshift({ id: valor, nombre: valor, detalle: "no está en el maestro" });
    return lista;
  }, [datos, guardar, valor]);

  return (
    <SelectorBuscable
      id={id}
      opciones={opciones}
      valor={valor}
      onChange={onChange}
      etiqueta={etiqueta}
      vacio={vacio}
      disabled={disabled}
      placeholder={cargando ? "Cargando…" : error ? "No se pudo cargar la lista" : (placeholder ?? "Escribe para buscar…")}
    />
  );
}
