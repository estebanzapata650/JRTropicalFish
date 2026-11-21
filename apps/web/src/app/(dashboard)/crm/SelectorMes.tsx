"use client";

import { useRouter } from "next/navigation";
import { inputClass } from "@/lib/estilos";

/** Mes del dashboard en la URL (`?mes=2026-11`), para poder compartir el enlace. */
export function SelectorMes({ valor }: { valor: string }) {
  const router = useRouter();
  return (
    <label className="flex items-center gap-2 text-sm text-texto-suave">
      Mes
      <input
        type="month"
        value={valor}
        onChange={(e) => e.target.value && router.push(`/crm?mes=${e.target.value}`)}
        className={`${inputClass} w-44`}
      />
    </label>
  );
}
