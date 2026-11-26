"use client";

import { useRouter } from "next/navigation";
import { useTransition } from "react";
import { inputClass } from "@/lib/estilos";

/** Mes del informe (`?mes=2026-10`). */
export function SelectorMes({ valor }: { valor: string }) {
  const router = useRouter();
  const [cargando, iniciar] = useTransition();
  return (
    <label className="flex flex-col gap-1.5 sm:w-56">
      <span className="text-sm font-medium text-texto">Mes reportado</span>
      <input
        type="month"
        value={valor}
        aria-busy={cargando}
        onChange={(e) => {
          const v = e.target.value;
          if (v) iniciar(() => router.push(`/exportaciones/aunap/informe?mes=${v}`));
        }}
        className={inputClass}
      />
    </label>
  );
}
