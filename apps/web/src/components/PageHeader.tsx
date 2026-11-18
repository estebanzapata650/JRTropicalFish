import type { ReactNode } from "react";

/** Cabecera de página: título, descripción opcional y acciones a la derecha. */
export function PageHeader({
  titulo,
  descripcion,
  acciones,
}: {
  titulo: string;
  descripcion?: string;
  acciones?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-col gap-0.5">
        <h1 className="text-lg font-semibold text-texto sm:text-xl">{titulo}</h1>
        {descripcion ? <p className="text-sm text-texto-suave">{descripcion}</p> : null}
      </div>
      {acciones ? <div className="flex shrink-0 gap-2">{acciones}</div> : null}
    </div>
  );
}
