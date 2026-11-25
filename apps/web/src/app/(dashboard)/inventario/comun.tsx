"use client";

import { useMemo } from "react";
import { Plus, Trash2 } from "lucide-react";
import type { ProductoOpcion } from "@/lib/inventario";
import { SelectorBuscable } from "@/components/SelectorBuscable";
import { botonSecundarioClass, inputClass } from "@/lib/estilos";

/**
 * Elegir una especie entre ~800 escribiendo su código, su nombre o las iniciales: la lista se acorta mientras
 * se escribe. El valor es el id del producto.
 */
export function SelectorProducto({
  productos,
  valor,
  onChange,
  etiqueta,
}: {
  productos: ProductoOpcion[];
  valor: string;
  onChange: (id: string) => void;
  etiqueta: string;
}) {
  const opciones = useMemo(
    () =>
      productos.map((p) => ({
        id: p.id,
        nombre: `${p.codigo} · ${p.nombre}`,
        detalle: p.detalle,
      })),
    [productos],
  );
  return (
    <SelectorBuscable
      opciones={opciones}
      valor={valor}
      onChange={onChange}
      etiqueta={etiqueta}
      placeholder="Código o nombre de la especie…"
    />
  );
}

export type LineaEditable = {
  clave: string;
  id?: string;
  productoId: string;
  cantidad: string;
  valor: string;
  observacion: string;
  /** Mínimo permitido (lo ya despachado o recibido); la línea no se puede quitar si es > 0. */
  minimo?: number;
};

export const nuevaLinea = (
  parcial: Partial<LineaEditable> = {},
): LineaEditable => ({
  clave: crypto.randomUUID(),
  productoId: "",
  cantidad: "",
  valor: "",
  observacion: "",
  ...parcial,
});

/**
 * Valor de la línea al cambiar de especie o de moneda. Conserva lo escrito a mano: solo reemplaza un precio vacío
 * o el que había puesto una lista. Sin moneda (aún no hay cliente) no pone precio.
 */
export function precioSugerido(
  linea: LineaEditable,
  anterior: ProductoOpcion | undefined,
  nuevo: ProductoOpcion | undefined,
  moneda: string | null | undefined,
) {
  const escrito = aNumero(linea.valor);
  if (moneda === undefined) return linea.valor;
  const deLista = Object.values(anterior?.precios ?? {});
  if (escrito !== null && !deLista.includes(escrito)) return linea.valor;
  const precio = moneda ? nuevo?.precios?.[moneda] : undefined;
  return precio != null ? String(precio) : "";
}

/**
 * Editor de líneas por especie (ofertas, pedidos a proveedor, pedidos de cliente). En escritorio es
 * una tabla; en móvil cada línea es una tarjeta con sus campos uno debajo del otro.
 */
export function EditorLineas({
  lineas,
  onChange,
  productos,
  etiquetaValor,
  conObservacion = true,
  moneda,
}: {
  lineas: LineaEditable[];
  onChange: (lineas: LineaEditable[]) => void;
  productos: ProductoOpcion[];
  /** "Costo unitario (COP)", "Precio unitario"… Sin etiqueta, no hay columna de valor. */
  etiquetaValor?: string;
  conObservacion?: boolean;
  /** Pedidos de cliente: elegir la especie trae su precio de la lista de esa moneda. */
  moneda?: string | null;
}) {
  const cambiar = (clave: string, cambio: Partial<LineaEditable>) =>
    onChange(lineas.map((l) => (l.clave === clave ? { ...l, ...cambio } : l)));
  const porId = useMemo(
    () => new Map(productos.map((p) => [p.id, p])),
    [productos],
  );
  /** Cambia la especie; el precio de lista entra solo si no se escribió otro a mano. */
  const elegir = (l: LineaEditable, id: string) =>
    cambiar(l.clave, {
      productoId: id,
      valor: precioSugerido(l, porId.get(l.productoId), porId.get(id), moneda),
    });

  return (
    <div className="flex flex-col gap-3">
      <ul className="flex flex-col gap-3">
        {lineas.map((l, i) => (
          <li
            key={l.clave}
            className="grid grid-cols-1 gap-2 rounded-md border border-borde p-3 md:grid-cols-[2fr_7rem_8rem_1.5fr_auto] md:items-center md:border-0 md:p-0"
          >
            <SelectorProducto
              productos={productos}
              valor={l.productoId}
              onChange={(id) => elegir(l, id)}
              etiqueta={`Especie de la línea ${i + 1}`}
            />
            <input
              aria-label={`Cantidad de la línea ${i + 1}`}
              type="number"
              inputMode="numeric"
              min={l.minimo || 1}
              step={1}
              placeholder="Cantidad"
              value={l.cantidad}
              onChange={(e) => cambiar(l.clave, { cantidad: e.target.value })}
              className={inputClass}
            />
            {etiquetaValor ? (
              <input
                aria-label={`${etiquetaValor} de la línea ${i + 1}`}
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                placeholder={etiquetaValor}
                value={l.valor}
                onChange={(e) => cambiar(l.clave, { valor: e.target.value })}
                className={inputClass}
              />
            ) : (
              <span className="hidden md:block" />
            )}
            {conObservacion ? (
              <input
                aria-label={`Observación de la línea ${i + 1}`}
                placeholder="Observación (opcional)"
                maxLength={500}
                value={l.observacion}
                onChange={(e) =>
                  cambiar(l.clave, { observacion: e.target.value })
                }
                className={inputClass}
              />
            ) : (
              <span className="hidden md:block" />
            )}
            <button
              type="button"
              onClick={() =>
                onChange(lineas.filter((x) => x.clave !== l.clave))
              }
              disabled={(l.minimo ?? 0) > 0}
              title={
                (l.minimo ?? 0) > 0
                  ? "Tiene despachos: no se puede quitar"
                  : "Quitar"
              }
              aria-label={`Quitar la línea ${i + 1}`}
              className="inline-flex size-11 items-center justify-center justify-self-end rounded-md text-peligro hover:bg-acento-suave disabled:opacity-30"
            >
              <Trash2 className="size-4" aria-hidden />
            </button>
            {(l.minimo ?? 0) > 0 ? (
              <p className="text-xs text-texto-suave md:col-span-5">
                Ya despachados: {l.minimo} (mínimo).
              </p>
            ) : null}
          </li>
        ))}
      </ul>
      <button
        type="button"
        onClick={() => onChange([...lineas, nuevaLinea()])}
        className={`${botonSecundarioClass} w-fit gap-2`}
      >
        <Plus className="size-4" aria-hidden /> Agregar especie
      </button>
    </div>
  );
}

/** Líneas del editor → cuerpo para la API. */
export const aNumero = (t: string) =>
  t.trim() === "" ? null : Number(t.replace(",", "."));
