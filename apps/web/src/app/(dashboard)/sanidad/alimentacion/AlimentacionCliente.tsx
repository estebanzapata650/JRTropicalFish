"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { Plus } from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { DialogoConfirmacion } from "@/components/DialogoConfirmacion";
import {
  BotonIcono,
  Campo,
  CampoAncho,
  Casilla,
  EncabezadoTabla,
  EtiquetaEstado,
  RejillaCampos,
  TablaResponsive,
} from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  inputClass,
  selectClass,
} from "@/lib/estilos";
import {
  TIPOS_ALIMENTO,
  UNIDADES_ALIMENTO,
  num,
  unidad,
  type Alimento,
  type Dieta,
} from "@/lib/sanidad";
import {
  eliminarDietaAction,
  guardarAlimentoAction,
  guardarDietaAction,
} from "../actions";

type Opcion = { id: string; nombre: string };

/**
 * Alimentos y dietas. La dieta de un grupo (Apistogramma, Corydoras…) vale para todas sus especies; una
 * especie con dieta propia no hereda la del grupo. La ración es por cada 100 peces y por toma.
 */
export function AlimentacionCliente({
  alimentos,
  dietas,
  grupos,
  productos,
  gestionar,
}: {
  alimentos: Alimento[];
  dietas: Dieta[];
  grupos: Opcion[];
  productos: Opcion[];
  gestionar: boolean;
}) {
  const router = useRouter();
  const [alimento, setAlimento] = useState<Alimento | "nuevo" | null>(null);
  const [dieta, setDieta] = useState<Dieta | "nueva" | null>(null);
  const [eliminar, setEliminar] = useState<Dieta | null>(null);
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-8">
      <PageHeader
        titulo="Alimentos y dietas"
        descripcion="Qué come cada grupo de especies, cuántas veces al día y cuánto por cada 100 peces."
      />

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-2">
          <h2 className="text-base font-semibold text-texto">Dietas</h2>
          {gestionar ? (
            <button
              type="button"
              className={botonSecundarioClass}
              onClick={() => setDieta("nueva")}
              disabled={alimentos.every((a) => !a.activo)}
            >
              <Plus className="mr-1 size-4" aria-hidden /> Nueva dieta
            </button>
          ) : null}
        </div>
        {alimentos.length === 0 ? (
          <p className="text-sm text-texto-suave">
            Primero crea los alimentos.
          </p>
        ) : null}
        {error ? (
          <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">
            {error}
          </p>
        ) : null}
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Para</th>
              <th className="px-3 py-2 font-medium">Alimento</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Veces al día
              </th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Por cada 100 peces
              </th>
              <th className="px-3 py-2">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {dietas.length === 0 ? (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-6 text-center text-texto-suave"
                >
                  Sin dietas: la ronda de alimentación no sabrá qué dar.
                </td>
              </tr>
            ) : null}
            {dietas.map((d) => {
              const para = d.grupoNombre
                ? `Grupo ${d.grupoNombre}`
                : `${d.productoCodigo} ${d.productoNombre ?? ""}`;
              return (
                <tr key={d.id} className="border-b border-borde last:border-0">
                  <td className="px-3 py-2 text-texto">{para}</td>
                  <td className="px-3 py-2">
                    {d.alimentoNombre}
                    <span className="block text-xs text-texto-suave md:hidden">
                      {d.vecesDia}×/día · {num(d.cantidadPor100)}{" "}
                      {unidad(d.unidad)} por 100
                    </span>
                  </td>
                  <td className="hidden px-3 py-2 md:table-cell">
                    {d.vecesDia}
                  </td>
                  <td className="hidden px-3 py-2 md:table-cell">
                    {num(d.cantidadPor100)} {unidad(d.unidad)}
                  </td>
                  <td className="px-3 py-2 text-right whitespace-nowrap">
                    {gestionar ? (
                      <>
                        <BotonIcono
                          accion="editar"
                          etiqueta={`Editar la dieta de ${para}`}
                          onClick={() => setDieta(d)}
                        />
                        <BotonIcono
                          accion="eliminar"
                          etiqueta={`Eliminar la dieta de ${para}`}
                          onClick={() => setEliminar(d)}
                        />
                      </>
                    ) : null}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </TablaResponsive>
      </section>

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-2">
          <h2 className="text-base font-semibold text-texto">Alimentos</h2>
          {gestionar ? (
            <button
              type="button"
              className={botonSecundarioClass}
              onClick={() => setAlimento("nuevo")}
            >
              <Plus className="mr-1 size-4" aria-hidden /> Nuevo alimento
            </button>
          ) : null}
        </div>
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr>
              <th className="px-3 py-2 font-medium">Alimento</th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Tipo
              </th>
              <th className="hidden px-3 py-2 font-medium md:table-cell">
                Unidad
              </th>
              <th className="px-3 py-2 font-medium">Estado</th>
              <th className="px-3 py-2">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {alimentos.map((a) => (
              <tr key={a.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2">
                  <span className="text-texto">{a.nombre}</span>
                  <span className="block text-xs text-texto-suave">
                    {a.codigo}
                  </span>
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  {TIPOS_ALIMENTO[a.tipo]}
                </td>
                <td className="hidden px-3 py-2 md:table-cell">
                  {unidad(a.unidad)}
                </td>
                <td className="px-3 py-2">
                  <EtiquetaEstado activo={a.activo} />
                </td>
                <td className="px-3 py-2 text-right">
                  {gestionar ? (
                    <BotonIcono
                      accion="editar"
                      etiqueta={`Editar ${a.nombre}`}
                      onClick={() => setAlimento(a)}
                    />
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
      </section>

      <Dialogo
        titulo={
          alimento === "nuevo"
            ? "Nuevo alimento"
            : `Editar ${alimento?.nombre ?? ""}`
        }
        abierto={alimento !== null}
        onCerrar={() => setAlimento(null)}
        ancho="max-w-xl"
      >
        {alimento ? (
          <FormAlimento
            key={alimento === "nuevo" ? "n" : alimento.id}
            alimento={alimento === "nuevo" ? null : alimento}
            onListo={() => setAlimento(null)}
          />
        ) : null}
      </Dialogo>
      <Dialogo
        titulo={dieta === "nueva" ? "Nueva dieta" : "Editar dieta"}
        abierto={dieta !== null}
        onCerrar={() => setDieta(null)}
        ancho="max-w-xl"
      >
        {dieta ? (
          <FormDieta
            key={dieta === "nueva" ? "n" : dieta.id}
            dieta={dieta === "nueva" ? null : dieta}
            alimentos={alimentos}
            grupos={grupos}
            productos={productos}
            onListo={() => setDieta(null)}
          />
        ) : null}
      </Dialogo>
      <DialogoConfirmacion
        abierto={eliminar !== null}
        titulo="Eliminar dieta"
        mensaje={
          eliminar
            ? `¿Eliminar ${eliminar.alimentoNombre} de la dieta de ${eliminar.grupoNombre ?? eliminar.productoCodigo}?`
            : ""
        }
        textoConfirmar="Eliminar"
        peligro
        procesando={pendiente}
        onCerrar={() => setEliminar(null)}
        onConfirmar={() =>
          iniciar(async () => {
            const r = await eliminarDietaAction(eliminar!.id);
            setEliminar(null);
            setError(r.error ?? null);
            if (!r.error) router.refresh();
          })
        }
      />
    </div>
  );
}

function FormAlimento({
  alimento,
  onListo,
}: {
  alimento: Alimento | null;
  onListo: () => void;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [f, setF] = useState({
    codigo: alimento?.codigo ?? "",
    nombre: alimento?.nombre ?? "",
    tipo: alimento?.tipo ?? "seco",
    unidad: alimento?.unidad ?? "g",
    activo: alimento?.activo ?? true,
  });
  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        iniciar(async () => {
          const r = await guardarAlimentoAction(alimento?.id ?? null, f);
          if (r.error) setError(r.error);
          else {
            onListo();
            router.refresh();
          }
        });
      }}
    >
      <RejillaCampos>
        <Campo label="Código" htmlFor="ali-codigo">
          <input
            id="ali-codigo"
            className={inputClass}
            value={f.codigo}
            onChange={(e) => setF({ ...f, codigo: e.target.value })}
            maxLength={20}
            required
          />
        </Campo>
        <Campo label="Nombre" htmlFor="ali-nombre">
          <input
            id="ali-nombre"
            className={inputClass}
            value={f.nombre}
            onChange={(e) => setF({ ...f, nombre: e.target.value })}
            maxLength={120}
            required
          />
        </Campo>
        <Campo label="Tipo" htmlFor="ali-tipo">
          <select
            id="ali-tipo"
            value={f.tipo}
            onChange={(e) => setF({ ...f, tipo: e.target.value })}
            className={selectClass}
          >
            {Object.entries(TIPOS_ALIMENTO).map(([v, t]) => (
              <option key={v} value={v}>
                {t}
              </option>
            ))}
          </select>
        </Campo>
        <Campo label="Unidad" htmlFor="ali-unidad">
          <select
            id="ali-unidad"
            className={selectClass}
            value={f.unidad}
            onChange={(e) => setF({ ...f, unidad: e.target.value })}
          >
            {Object.entries(UNIDADES_ALIMENTO).map(([v, t]) => (
              <option key={v} value={v}>
                {t}
              </option>
            ))}
          </select>
        </Campo>
        <CampoAncho>
          <Casilla
            label="Activo"
            checked={f.activo}
            onChange={(v) => setF({ ...f, activo: v })}
          />
        </CampoAncho>
      </RejillaCampos>
      {error ? (
        <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">
          {error}
        </p>
      ) : null}
      <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
        Guardar
      </button>
    </form>
  );
}

function FormDieta({
  dieta,
  alimentos,
  grupos,
  productos,
  onListo,
}: {
  dieta: Dieta | null;
  alimentos: Alimento[];
  grupos: Opcion[];
  productos: Opcion[];
  onListo: () => void;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [para, setPara] = useState<"grupo" | "producto">(
    dieta?.productoId ? "producto" : "grupo",
  );
  const [f, setF] = useState({
    grupoId: dieta?.grupoId ?? "",
    productoId: dieta?.productoId ?? "",
    alimentoId: dieta?.alimentoId ?? alimentos.find((a) => a.activo)?.id ?? "",
    vecesDia: String(dieta?.vecesDia ?? 2),
    cantidadPor100: dieta ? String(dieta.cantidadPor100) : "",
    nota: dieta?.nota ?? "",
  });
  const alimento = alimentos.find((a) => a.id === f.alimentoId);
  return (
    <form
      className="flex flex-col gap-6"
      onSubmit={(e) => {
        e.preventDefault();
        iniciar(async () => {
          const r = await guardarDietaAction(dieta?.id ?? null, {
            grupoId: para === "grupo" ? f.grupoId || null : null,
            productoId: para === "producto" ? f.productoId || null : null,
            alimentoId: f.alimentoId,
            vecesDia: Number(f.vecesDia) || 0,
            cantidadPor100: Number(f.cantidadPor100.replace(",", ".")) || 0,
            nota: f.nota || null,
          });
          if (r.error) setError(r.error);
          else {
            onListo();
            router.refresh();
          }
        });
      }}
    >
      <RejillaCampos>
        <Campo label="Para" htmlFor="die-para">
          <select
            id="die-para"
            className={selectClass}
            value={para}
            onChange={(e) => setPara(e.target.value as "grupo" | "producto")}
          >
            <option value="grupo">Un grupo de especies</option>
            <option value="producto">
              Una especie (reemplaza la del grupo)
            </option>
          </select>
        </Campo>
        {para === "grupo" ? (
          <Campo label="Grupo" htmlFor="die-grupo">
            <SelectorBuscable
              id="die-grupo"
              opciones={grupos.map((g) => ({ id: g.id, nombre: g.nombre }))}
              valor={f.grupoId}
              onChange={(v) => setF({ ...f, grupoId: v })}
              requerido
              placeholder="Escribe para buscar…"
            />
          </Campo>
        ) : (
          <Campo label="Especie" htmlFor="die-producto">
            <SelectorBuscable
              id="die-producto"
              opciones={productos.map((p) => ({ id: p.id, nombre: p.nombre }))}
              valor={f.productoId}
              onChange={(v) => setF({ ...f, productoId: v })}
              requerido
              placeholder="Escribe para buscar…"
            />
          </Campo>
        )}
        <Campo label="Alimento" htmlFor="die-alimento">
          <SelectorBuscable
            id="die-alimento"
            opciones={alimentos
              .filter((a) => a.activo || a.id === f.alimentoId)
              .map((a) => ({ id: a.id, nombre: a.nombre }))}
            valor={f.alimentoId}
            onChange={(v) => setF({ ...f, alimentoId: v })}
            requerido
          />
        </Campo>
        <Campo label="Veces al día" htmlFor="die-veces">
          <input
            id="die-veces"
            type="number"
            min={1}
            max={12}
            className={inputClass}
            value={f.vecesDia}
            onChange={(e) => setF({ ...f, vecesDia: e.target.value })}
            required
          />
        </Campo>
        <Campo
          label={`Por toma, cada 100 peces (${unidad(alimento?.unidad ?? "g")})`}
          htmlFor="die-cantidad"
        >
          <input
            id="die-cantidad"
            inputMode="decimal"
            className={inputClass}
            value={f.cantidadPor100}
            onChange={(e) => setF({ ...f, cantidadPor100: e.target.value })}
            required
          />
        </Campo>
        <Campo label="Nota" htmlFor="die-nota">
          <input
            id="die-nota"
            className={inputClass}
            value={f.nota}
            onChange={(e) => setF({ ...f, nota: e.target.value })}
            maxLength={300}
          />
        </Campo>
      </RejillaCampos>
      {error ? (
        <p className="rounded-md bg-peligro/15 px-3 py-2 text-sm text-peligro">
          {error}
        </p>
      ) : null}
      <button type="submit" disabled={pendiente} className={botonPrimarioClass}>
        Guardar
      </button>
    </form>
  );
}
