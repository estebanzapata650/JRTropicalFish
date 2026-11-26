"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Copy, Plus, Trash2, Wand2 } from "lucide-react";
import {
  cifra,
  type EmbarqueDetalle,
  type TipoBolsa,
} from "@/lib/exportaciones";
import type { ProductoOpcion } from "@/lib/inventario";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
} from "@/lib/estilos";
import { SelectorProducto } from "../../inventario/comun";
import { guardarEmpaqueAction } from "../actions";
import { EncabezadoTabla, TablaResponsive } from "@/components/campos";
import { Aviso, BotonesDocumento, CampoFila, numero } from "./comun";
import { CostoEmpaque } from "./CostoEmpaque";

type Fila = {
  clave: string;
  caja: string;
  productoId: string;
  nombreFactura: string;
  tipoBolsaId: string;
  bolsas: string;
  peces: string;
  precio: string;
};

const nueva = (parcial: Partial<Fila> = {}): Fila => ({
  clave: crypto.randomUUID(),
  caja: "1",
  productoId: "",
  nombreFactura: "",
  tipoBolsaId: "",
  bolsas: "1",
  peces: "",
  precio: "",
  ...parcial,
});

/** Cambia cuando cambia el empaque guardado o las cajas reales: el costo de empaque se recalcula. */
const versionCosto = (d: EmbarqueDetalle) =>
  [...JSON.stringify([d.empaque, d.cajasReales])].reduce((h, c) => (h * 31 + c.charCodeAt(0)) | 0, 0);

/**
 * Paso 4: empaque real caja por caja (lo llena bodega). Cada fila: caja, especie, tipo de bolsa, bolsas ×
 * peces por bolsa. De aquí salen la lista de empaque y la factura. «Desde la orden de cargue» propone las
 * filas a partir de las especies.
 */
export function PasoEmpaque({
  d,
  productos,
  bolsas,
  puede,
  puedeEnviar,
}: {
  d: EmbarqueDetalle;
  productos: ProductoOpcion[];
  bolsas: TipoBolsa[];
  puede: boolean;
  puedeEnviar: boolean;
}) {
  const router = useRouter();
  const [filas, setFilas] = useState<Fila[]>(() =>
    d.empaque.map((f) =>
      nueva({
        caja: String(f.numeroCaja),
        productoId: f.productoId,
        nombreFactura: f.nombreFactura ?? "",
        tipoBolsaId: f.tipoBolsaId ?? "",
        bolsas: String(f.bolsas),
        peces: String(f.pecesPorBolsa),
        precio: String(f.precio),
      }),
    ),
  );
  const [aviso, setAviso] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const cambiar = (clave: string, cambio: Partial<Fila>) =>
    setFilas((fs) =>
      fs.map((f) => (f.clave === clave ? { ...f, ...cambio } : f)),
    );
  const bolsasActivas = bolsas.filter((b) => b.activo);
  const export_ = bolsasActivas.find((b) => b.nombre === "Export")?.id ?? "";

  const cajas = new Set(filas.map((f) => f.caja).filter(Boolean)).size;
  const peces = filas.reduce(
    (s, f) => s + (numero(f.bolsas) ?? 0) * (numero(f.peces) ?? 0),
    0,
  );
  const costoBolsas = filas.reduce(
    (s, f) =>
      s +
      (bolsas.find((b) => b.id === f.tipoBolsaId)?.costoCop ?? 0) *
        (numero(f.bolsas) ?? 0),
    0,
  );

  // Propuesta desde la orden de cargue: una fila por especie en su primera caja, toda la cantidad en una bolsa Export.
  const proponer = () =>
    setFilas(
      d.calculos.ordenCargue.map((o) => {
        const linea = d.lineas.find((l) => l.orden === o.orden)!;
        return nueva({
          caja: o.cajas === "—" ? "1" : o.cajas.split(" - ")[0],
          productoId: linea.productoId,
          tipoBolsaId: export_,
          bolsas: "1",
          peces: String(o.cantidad),
        });
      }),
    );

  return (
    <div className="flex flex-col gap-5">
      <Aviso texto={aviso} />
      <CostoEmpaque embarqueId={d.embarque.id} version={versionCosto(d)} />
      {!puede ? (
        <p className="text-sm text-texto-suave">
          {d.embarque.facturaNumero
            ? "La factura ya se emitió: el empaque no cambia."
            : "Solo bodega (exportaciones:empacar) registra el empaque."}
        </p>
      ) : null}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-texto">
          <strong>{cajas}</strong> cajas ·{" "}
          <strong>{peces.toLocaleString("es-CO")}</strong> peces
          <span className="text-texto-suave">
            {" "}
            · bolsas {costoBolsas.toLocaleString("es-CO")} COP (interno)
          </span>
        </p>
        {puede && d.lineas.length > 0 ? (
          <button
            type="button"
            onClick={proponer}
            className={`${botonSecundarioClass} gap-2`}
          >
            <Wand2 className="size-4" aria-hidden /> Desde la orden de cargue
          </button>
        ) : null}
      </div>

      {!puede && d.empaque.length > 0 ? (
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Caja</th>
              <th className="px-3 py-2 font-medium">Especie</th>
              <th className="hidden px-3 py-2 font-medium sm:table-cell">
                Empaque
              </th>
              <th className="px-3 py-2 text-right font-medium">Peces</th>
              <th className="hidden px-3 py-2 text-right font-medium md:table-cell">
                Precio
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {d.empaque.map((f) => (
              <tr key={f.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2 font-medium text-texto">
                  {f.numeroCaja}
                </td>
                <td className="px-3 py-2 text-texto">
                  <span className="mr-1 font-mono text-xs text-texto-suave">
                    {f.productoCodigo}
                  </span>
                  <span className="italic">{f.nombreCientifico}</span>
                  <p className="text-xs text-texto-suave sm:hidden">
                    {f.empaqueTexto}
                  </p>
                </td>
                <td className="hidden px-3 py-2 text-texto sm:table-cell">
                  {f.empaqueTexto}
                </td>
                <td className="px-3 py-2 text-right tabular-nums text-texto">
                  {f.cantidad.toLocaleString("es-CO")}
                </td>
                <td className="hidden px-3 py-2 text-right tabular-nums text-texto md:table-cell">
                  {cifra(f.precio, 4)}
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      ) : null}
      <ul className={puede ? "flex flex-col gap-3" : "hidden"}>
        {filas.map((f, i) => {
          const cantidad = (numero(f.bolsas) ?? 0) * (numero(f.peces) ?? 0);
          return (
            <li
              key={f.clave}
              className="grid grid-cols-2 gap-2 rounded-md border border-borde p-3 lg:grid-cols-[4.5rem_2.5fr_7rem_4.5rem_5.5rem_6rem_auto] lg:items-center"
            >
              <CampoFila etiqueta="Caja">
                <input
                  aria-label={`Caja de la fila ${i + 1}`}
                  type="number"
                  inputMode="numeric"
                  min={1}
                  value={f.caja}
                  onChange={(ev) => cambiar(f.clave, { caja: ev.target.value })}
                  className={inputClass}
                />
              </CampoFila>
              <CampoFila
                etiqueta="Especie"
                className="col-span-2 row-start-2 lg:col-span-1 lg:row-start-auto"
              >
                <SelectorProducto
                  productos={productos}
                  valor={f.productoId}
                  onChange={(id) => cambiar(f.clave, { productoId: id })}
                  etiqueta={`Especie de la fila ${i + 1}`}
                />
              </CampoFila>
              <CampoFila etiqueta="Bolsa">
                <SelectorBuscable
                  etiqueta={`Bolsa de la fila ${i + 1}`}
                  opciones={bolsasActivas.map((b) => ({
                    id: b.id,
                    nombre: b.nombre,
                  }))}
                  valor={f.tipoBolsaId}
                  onChange={(v) => cambiar(f.clave, { tipoBolsaId: v })}
                  vacio="Bolsa…"
                />
              </CampoFila>
              <CampoFila etiqueta="Bolsas">
                <input
                  aria-label={`Bolsas de la fila ${i + 1}`}
                  type="number"
                  inputMode="numeric"
                  min={1}
                  placeholder="Bolsas"
                  value={f.bolsas}
                  onChange={(ev) =>
                    cambiar(f.clave, { bolsas: ev.target.value })
                  }
                  className={inputClass}
                />
              </CampoFila>
              <CampoFila etiqueta="Peces por bolsa">
                <input
                  aria-label={`Peces por bolsa de la fila ${i + 1}`}
                  type="number"
                  inputMode="numeric"
                  min={1}
                  placeholder="Peces/bolsa"
                  value={f.peces}
                  onChange={(ev) =>
                    cambiar(f.clave, { peces: ev.target.value })
                  }
                  className={inputClass}
                />
              </CampoFila>
              <CampoFila etiqueta="Precio USD">
                <input
                  aria-label={`Precio USD de la fila ${i + 1}`}
                  type="number"
                  inputMode="decimal"
                  min={0}
                  step="0.0001"
                  placeholder="Precio"
                  value={f.precio}
                  onChange={(ev) =>
                    cambiar(f.clave, { precio: ev.target.value })
                  }
                  className={inputClass}
                />
              </CampoFila>
              <div className="col-span-2 flex items-center justify-between gap-2 lg:col-span-1 lg:justify-end">
                <span className="text-sm tabular-nums text-texto-suave lg:hidden">
                  = {cantidad.toLocaleString("es-CO")} peces
                </span>
                {puede ? (
                  <span className="flex gap-1">
                    <button
                      type="button"
                      onClick={() =>
                        setFilas((fs) => [
                          ...fs.slice(0, i + 1),
                          nueva({
                            caja: f.caja,
                            tipoBolsaId: f.tipoBolsaId,
                            bolsas: f.bolsas,
                          }),
                          ...fs.slice(i + 1),
                        ])
                      }
                      aria-label={`Otra especie en la caja de la fila ${i + 1}`}
                      className="inline-flex size-10 items-center justify-center rounded-md text-texto-suave hover:bg-acento-suave"
                    >
                      <Copy className="size-4" aria-hidden />
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        setFilas((fs) => fs.filter((x) => x.clave !== f.clave))
                      }
                      aria-label={`Quitar la fila ${i + 1}`}
                      className="inline-flex size-10 items-center justify-center rounded-md text-peligro hover:bg-acento-suave"
                    >
                      <Trash2 className="size-4" aria-hidden />
                    </button>
                  </span>
                ) : null}
              </div>
              {puede ? (
                <input
                  aria-label={`Nombre en factura de la fila ${i + 1} (opcional)`}
                  placeholder="Nombre en factura (opcional)"
                  maxLength={200}
                  value={f.nombreFactura}
                  onChange={(ev) =>
                    cambiar(f.clave, { nombreFactura: ev.target.value })
                  }
                  className={`${inputClass} col-span-2 lg:col-span-7`}
                />
              ) : null}
            </li>
          );
        })}
      </ul>
      {filas.length === 0 ? (
        <p className="text-sm text-texto-suave">Sin empaque registrado.</p>
      ) : null}

      {puede ? (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <button
            type="button"
            onClick={() =>
              setFilas((fs) => [
                ...fs,
                nueva({
                  caja: String(
                    (Math.max(0, ...fs.map((x) => numero(x.caja) ?? 0)) || 0) +
                      1,
                  ),
                  tipoBolsaId: export_,
                }),
              ])
            }
            className={`${botonSecundarioClass} w-fit gap-2`}
          >
            <Plus className="size-4" aria-hidden /> Nueva caja
          </button>
          <button
            type="button"
            disabled={guardando}
            onClick={() =>
              iniciar(async () => {
                const r = await guardarEmpaqueAction(
                  d.embarque.id,
                  filas
                    .filter((f) => f.productoId)
                    .map((f) => ({
                      numeroCaja: numero(f.caja) ?? 0,
                      productoId: f.productoId,
                      nombreFactura: f.nombreFactura || null,
                      tipoBolsaId: f.tipoBolsaId || null,
                      bolsas: numero(f.bolsas) ?? 0,
                      pecesPorBolsa: numero(f.peces) ?? 0,
                      precio: numero(f.precio),
                      nota: null,
                    })),
                );
                if (r.error) {
                  setError(r.error);
                  return;
                }
                setError(null);
                setAviso(
                  `Empaque guardado: ${cajas} cajas, ${peces.toLocaleString("es-CO")} peces.`,
                );
                router.refresh();
              })
            }
            className={botonPrimarioClass}
          >
            {guardando ? "Guardando…" : "Guardar empaque"}
          </button>
        </div>
      ) : null}
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <p className="text-xs text-texto-suave">
        Precio vacío = el de la especie en el paso 1 (o el de la lista). Total
        del empaque: {cifra(d.calculos.factura.subtotalPeces)} USD guardados.
      </p>
      <BotonesDocumento
        embarqueId={d.embarque.id}
        tipo="empaque"
        correoCliente={d.clienteEmail}
        puedeEnviar={puedeEnviar}
        onEnviado={setAviso}
      />
    </div>
  );
}
