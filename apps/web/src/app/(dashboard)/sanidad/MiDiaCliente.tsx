"use client";

import { SelectorBuscable } from "@/components/SelectorBuscable";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import {
  Ban,
  CircleCheck,
  Droplets,
  PackageOpen,
  PlaneTakeoff,
  Syringe,
  TriangleAlert,
  Truck,
  Utensils,
} from "lucide-react";
import { Dialogo } from "@/components/Dialogo";
import { Campo } from "@/components/campos";
import { PageHeader } from "@/components/PageHeader";
import {
  botonPrimarioClass,
  botonSecundarioClass,
  textareaClass,
} from "@/lib/estilos";
import {
  ACCIONES,
  MOTIVOS_AYUNO,
  fechaCorta,
  horaCorta,
  num,
  unidad,
  type Aplicacion,
  type MiDia,
  type Ronda,
} from "@/lib/sanidad";
import {
  alimentarAction,
  aplicarAction,
  crearTratamientoAction,
  omitirAction,
  ubicarAction,
} from "./actions";

type AcuarioOpcion = { id: string; codigo: string; litros: number | null };

/** Botón grande para manos mojadas: mínimo 48 px de alto. */
const botonGrande = `${botonPrimarioClass} h-12 min-w-28`;
const botonGrandeSecundario = `${botonSecundarioClass} h-12`;

/**
 * Mi día en bodega: cuatro bloques de acción rápida, pensados para el celular. Cada acción es un toque;
 * la API valida y recalcula, y la pantalla se refresca.
 */
export function MiDiaCliente({
  dia,
  acuarios,
  operar,
  ubicar,
}: {
  dia: MiDia;
  acuarios: AcuarioOpcion[];
  operar: boolean;
  ubicar: boolean;
}) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();
  const [mensaje, setMensaje] = useState<{
    tipo: "ok" | "error";
    texto: string;
  } | null>(null);
  const [omitiendo, setOmitiendo] = useState<Aplicacion | null>(null);

  const correr = (
    accion: () => Promise<{ error?: string; ok?: boolean }>,
    exito: string,
  ) =>
    iniciar(async () => {
      const r = await accion();
      setMensaje(
        r.error
          ? { tipo: "error", texto: r.error }
          : { tipo: "ok", texto: exito },
      );
      if (!r.error) router.refresh();
    });

  const pendientesMedicar = dia.medicacion.filter(
    (a) => a.estado === "pendiente",
  ).length;
  const rojos = dia.alimentacion.filter((r) => !r.alimentar).length;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        titulo="Mi día en bodega"
        descripcion={`${new Date(`${dia.hoy}T12:00:00`).toLocaleDateString("es-CO", { weekday: "long", day: "numeric", month: "long" })} · ayuno pre-vuelo de ${dia.horasAyunoVuelo} h`}
      />

      <nav
        aria-label="Bloques del día"
        className="-mx-4 flex gap-2 overflow-x-auto px-4 md:mx-0 md:px-0"
      >
        {[
          {
            href: "#llegadas",
            texto: "Llegadas",
            n: dia.llegadas.length,
            Icono: PackageOpen,
          },
          {
            href: "#medicacion",
            texto: "Medicar",
            n: pendientesMedicar,
            Icono: Syringe,
          },
          {
            href: "#alimentacion",
            texto: "Alimentar",
            n: dia.alimentacion.length,
            Icono: Utensils,
          },
          {
            href: "#salidas",
            texto: "Salidas",
            n: dia.salidas.length,
            Icono: Truck,
          },
        ].map(({ href, texto, n, Icono }) => (
          <a
            key={href}
            href={href}
            className="inline-flex h-11 shrink-0 items-center gap-2 rounded-full border border-borde bg-superficie px-4 text-sm font-medium text-texto"
          >
            <Icono className="size-4" aria-hidden /> {texto}
            <span className="rounded-full bg-neutro-fondo px-2 text-xs tabular-nums text-neutro-texto">
              {n}
            </span>
          </a>
        ))}
      </nav>

      {dia.avisos.map((a) => (
        <p
          key={a}
          className="flex items-start gap-2 rounded-md bg-advertencia-fondo px-3 py-2 text-sm text-advertencia-texto"
        >
          <TriangleAlert className="mt-0.5 size-4 shrink-0" aria-hidden /> {a}
        </p>
      ))}

      {mensaje ? (
        <p
          role="status"
          className={`rounded-md px-3 py-2 text-sm ${mensaje.tipo === "ok" ? "bg-exito-fondo text-exito-texto" : "bg-peligro/15 text-peligro"}`}
        >
          {mensaje.texto}
        </p>
      ) : null}

      {/* 1. Llegadas */}
      <Bloque
        id="llegadas"
        titulo="1. Llegadas de hoy"
        Icono={PackageOpen}
        vacio="No hay lotes por ubicar ni llegadas hoy."
      >
        {dia.llegadas.map((l) => (
          <li
            key={l.loteId}
            className="flex flex-col gap-3 rounded-md border border-borde bg-superficie p-3"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="text-base font-semibold text-texto">
                {l.productoCodigo}{" "}
                <span className="font-normal text-texto-suave">
                  {l.productoNombre}
                </span>
              </p>
              <span className="text-sm tabular-nums text-texto">
                {num(l.cantidad, 0)} peces
              </span>
            </div>
            <p className="text-xs text-texto-suave">
              {l.loteCodigo} · llegó {horaCorta(l.llegada)} ·{" "}
              {l.porUbicar ? "por ubicar" : `en ${l.acuarioCodigo}`}
            </p>
            {l.porUbicar && ubicar ? (
              <UbicarLote
                acuarios={acuarios}
                disabled={pendiente}
                onUbicar={(acuario) =>
                  correr(
                    () => ubicarAction(l.loteId, acuario, l.cantidad),
                    `${l.loteCodigo} ubicado.`,
                  )
                }
              />
            ) : null}
            {!l.porUbicar && l.acuarioId ? (
              l.tieneTratamiento ? (
                <p className="text-sm text-texto-suave">
                  El acuario ya tiene tratamiento.
                </p>
              ) : l.protocoloSugeridoId ? (
                operar ? (
                  <button
                    type="button"
                    disabled={pendiente}
                    className={botonGrande}
                    onClick={() =>
                      correr(
                        () =>
                          crearTratamientoAction({
                            acuarioId: l.acuarioId,
                            protocoloId: l.protocoloSugeridoId,
                          }),
                        `Profilaxis iniciada en ${l.acuarioCodigo}.`,
                      )
                    }
                  >
                    Iniciar «{l.protocoloSugerido}» en {l.acuarioCodigo}
                  </button>
                ) : (
                  <p className="text-sm text-texto-suave">
                    Sugerido: {l.protocoloSugerido}
                  </p>
                )
              ) : (
                <p className="text-sm text-texto-suave">
                  Sin protocolo aprobado para esta especie.
                </p>
              )
            ) : null}
          </li>
        ))}
      </Bloque>

      {/* 2. Medicación */}
      <Bloque
        id="medicacion"
        titulo="2. Rutina de medicación"
        Icono={Syringe}
        vacio="Hoy no hay nada que medicar."
      >
        {dia.medicacion.map((a) => {
          const hecha = a.estado !== "pendiente";
          const atrasada = !hecha && a.fecha < dia.hoy;
          return (
            <li
              key={a.id}
              className={`flex flex-col gap-3 rounded-md border p-3 sm:flex-row sm:items-center sm:justify-between ${
                hecha
                  ? "border-borde bg-fondo-sutil"
                  : atrasada
                    ? "border-peligro bg-superficie"
                    : "border-borde bg-superficie"
              }`}
            >
              <div className="flex min-w-0 flex-col gap-1">
                <p className="text-xs text-texto-suave">
                  {a.tratamientoCodigo} · Día {a.dia}/{a.diasTotal}
                  {atrasada ? (
                    <span className="font-semibold text-peligro">
                      {" "}
                      · atrasada ({fechaCorta(a.fecha)})
                    </span>
                  ) : null}
                </p>
                <p className="text-lg font-semibold text-texto">
                  {a.acuarioCodigo}{" "}
                  <span className="font-normal">
                    ·{" "}
                    {a.accion === "medicar"
                      ? a.medicamentoNombre
                      : ACCIONES[a.accion]}
                    {a.accion === "cambio_agua" && a.porcentajeAgua
                      ? ` ${a.porcentajeAgua} %`
                      : ""}
                  </span>
                </p>
                {a.accion === "medicar" ? (
                  <p className="text-2xl font-bold tabular-nums text-texto">
                    {a.cantidadCalculada !== null ? (
                      `${num(a.cantidadCalculada)} ${unidad(a.unidad)}`
                    ) : (
                      <span className="text-base font-medium text-advertencia-texto">
                        Dosis sin calcular
                      </span>
                    )}
                  </p>
                ) : null}
                {a.nota ? (
                  <p className="text-sm text-texto-suave">{a.nota}</p>
                ) : null}
                {hecha ? (
                  <p className="flex items-center gap-1 text-sm text-texto-suave">
                    <CircleCheck
                      className="size-4 text-exito-texto"
                      aria-hidden
                    />
                    {a.estado === "aplicada" ? "Aplicado" : "No se aplicó"} por{" "}
                    {a.usuarioNombre ?? "—"} · {horaCorta(a.registradoEn)}
                  </p>
                ) : null}
              </div>
              {!hecha && operar ? (
                <div className="flex shrink-0 gap-2">
                  <button
                    type="button"
                    disabled={pendiente}
                    className={`${botonGrande} flex-1`}
                    onClick={() =>
                      correr(
                        () => aplicarAction(a.id, a.cantidadCalculada),
                        `${a.acuarioCodigo}: registrado.`,
                      )
                    }
                  >
                    {a.accion === "medicar" ? "Aplicado" : "Hecho"}
                  </button>
                  <button
                    type="button"
                    disabled={pendiente}
                    className={botonGrandeSecundario}
                    onClick={() => setOmitiendo(a)}
                  >
                    No se hizo
                  </button>
                </div>
              ) : null}
            </li>
          );
        })}
      </Bloque>

      {/* 3. Alimentación */}
      <Bloque
        id="alimentacion"
        titulo="3. Ronda de alimentación"
        Icono={Utensils}
        vacio="No hay acuarios con peces."
        resumen={rojos > 0 ? `${rojos} en ayuno obligatorio` : undefined}
      >
        {dia.alimentacion.map((r) => (
          <FilaRonda
            key={r.acuarioId}
            r={r}
            operar={operar}
            pendiente={pendiente}
            onAlimentar={(alimento, cantidad) =>
              correr(
                () => alimentarAction(r.acuarioId, alimento, cantidad),
                `${r.acuarioCodigo} alimentado.`,
              )
            }
          />
        ))}
      </Bloque>

      {/* 4. Salidas */}
      <Bloque
        id="salidas"
        titulo="4. Salidas y despachos"
        Icono={Truck}
        vacio="No hay salidas reservadas para hoy ni mañana."
      >
        {dia.salidas.map((s) => (
          <li
            key={s.pedidoId}
            className="flex flex-col gap-2 rounded-md border border-borde bg-superficie p-3"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="text-base font-semibold text-texto">
                {s.embarqueCodigo ?? s.pedidoCodigo}{" "}
                <span className="font-normal text-texto-suave">
                  {s.clienteNombre}
                </span>
              </p>
              <span className="flex items-center gap-1 text-sm text-texto">
                <PlaneTakeoff className="size-4" aria-hidden />{" "}
                {horaCorta(s.salida)}
              </span>
            </div>
            <ul className="flex flex-col gap-1 text-sm">
              {s.acuarios.map((a) => (
                <li
                  key={`${a.acuarioId}-${a.loteId}`}
                  className="flex justify-between gap-2 border-t border-borde pt-1"
                >
                  <span className="font-medium text-texto">
                    {a.acuarioCodigo}
                  </span>
                  <span className="text-texto-suave">{a.productoCodigo}</span>
                  <span className="tabular-nums text-texto">
                    {num(a.cantidad, 0)}
                  </span>
                </li>
              ))}
            </ul>
            <Link
              href={`/crm/pedidos?id=${s.pedidoId}`}
              className={`${botonSecundarioClass} h-12`}
            >
              Picking y despacho de {s.pedidoCodigo}
            </Link>
          </li>
        ))}
      </Bloque>

      <Dialogo
        titulo={`No se hizo: ${omitiendo?.acuarioCodigo ?? ""}`}
        abierto={omitiendo !== null}
        onCerrar={() => setOmitiendo(null)}
        ancho="max-w-md"
      >
        {omitiendo ? (
          <OmitirForm
            pendiente={pendiente}
            onEnviar={(nota) => {
              const a = omitiendo;
              setOmitiendo(null);
              correr(
                () => omitirAction(a.id, nota),
                `${a.acuarioCodigo}: registrado como no hecho.`,
              );
            }}
          />
        ) : null}
      </Dialogo>
    </div>
  );
}

function Bloque({
  id,
  titulo,
  Icono,
  vacio,
  resumen,
  children,
}: {
  id: string;
  titulo: string;
  Icono: typeof Syringe;
  vacio: string;
  resumen?: string;
  children: React.ReactNode[];
}) {
  return (
    <section
      id={id}
      aria-labelledby={`${id}-titulo`}
      className="flex scroll-mt-20 flex-col gap-3"
    >
      <div className="flex items-baseline justify-between gap-2">
        <h2
          id={`${id}-titulo`}
          className="flex items-center gap-2 text-base font-semibold text-texto"
        >
          <Icono className="size-5" aria-hidden /> {titulo}
        </h2>
        {resumen ? (
          <span className="text-sm font-medium text-peligro">{resumen}</span>
        ) : null}
      </div>
      {children.length === 0 ? (
        <p className="rounded-md border border-dashed border-borde px-3 py-6 text-center text-sm text-texto-suave">
          {vacio}
        </p>
      ) : (
        <ul className="flex flex-col gap-2">{children}</ul>
      )}
    </section>
  );
}

function FilaRonda({
  r,
  operar,
  pendiente,
  onAlimentar,
}: {
  r: Ronda;
  operar: boolean;
  pendiente: boolean;
  onAlimentar: (alimento: string | null, cantidad: number | null) => void;
}) {
  const vecesDia = Math.max(0, ...r.raciones.map((x) => x.vecesDia));
  const primera = r.raciones[0];
  return (
    <li
      className={`flex flex-col gap-2 rounded-md border-2 p-3 sm:flex-row sm:items-center sm:justify-between ${
        r.alimentar
          ? "border-exito-texto/40 bg-exito-fondo/40"
          : "border-peligro bg-peligro/10"
      }`}
    >
      <div className="flex min-w-0 items-start gap-3">
        {r.alimentar ? (
          <CircleCheck
            className="mt-0.5 size-7 shrink-0 text-exito-texto"
            aria-label="Alimentar"
          />
        ) : (
          <Ban
            className="mt-0.5 size-7 shrink-0 text-peligro"
            aria-label="No alimentar"
          />
        )}
        <div className="flex min-w-0 flex-col gap-1">
          <p className="text-lg font-semibold text-texto">
            {r.acuarioCodigo}{" "}
            <span className="text-sm font-normal text-texto-suave">
              · {num(r.peces, 0)} peces
            </span>
          </p>
          {r.alimentar ? (
            <>
              {r.raciones.map((x) => (
                <p
                  key={`${x.productoCodigo}-${x.alimento}`}
                  className="text-sm text-texto"
                >
                  <span className="font-semibold tabular-nums">
                    {num(x.cantidadToma)} {unidad(x.unidad)}
                  </span>{" "}
                  {x.alimento} · {x.vecesDia} vez{x.vecesDia === 1 ? "" : "es"}{" "}
                  al día{" "}
                  <span className="text-texto-suave">({x.productoCodigo})</span>
                </p>
              ))}
              {r.alimentoMedicado ? (
                <p className="text-sm font-medium text-advertencia-texto">
                  Usar alimento medicado
                </p>
              ) : null}
              {r.sinDieta ? (
                <p className="text-sm text-texto-suave">
                  Sin dieta definida para alguna especie: revisa Alimentos y
                  dietas.
                </p>
              ) : null}
              {r.tomasHoy > 0 ? (
                <p className="text-xs text-texto-suave">
                  <Droplets className="mr-1 inline size-3" aria-hidden />
                  Hoy: {r.tomasHoy}
                  {vecesDia ? ` de ${vecesDia}` : ""} · última{" "}
                  {horaCorta(r.ultimaToma)}
                </p>
              ) : null}
            </>
          ) : (
            <>
              <p className="text-base font-bold tracking-wide text-peligro">
                NO ALIMENTAR
              </p>
              <p className="text-sm text-texto">
                {r.motivos.map((m) => MOTIVOS_AYUNO[m] ?? m).join(" · ")}
              </p>
              {r.detalleMotivo ? (
                <p className="text-xs text-texto-suave">{r.detalleMotivo}</p>
              ) : null}
            </>
          )}
        </div>
      </div>
      {r.alimentar && operar ? (
        <button
          type="button"
          disabled={pendiente}
          className={botonGrande}
          onClick={() =>
            onAlimentar(
              primera?.alimentoId ?? null,
              primera ? primera.cantidadToma : null,
            )
          }
        >
          Alimentado
        </button>
      ) : null}
    </li>
  );
}

function UbicarLote({
  acuarios,
  disabled,
  onUbicar,
}: {
  acuarios: AcuarioOpcion[];
  disabled: boolean;
  onUbicar: (acuario: string) => void;
}) {
  const [acuario, setAcuario] = useState("");
  return (
    <div className="flex flex-col gap-2 sm:flex-row">
      <SelectorBuscable
        etiqueta="Acuario destino"
        opciones={acuarios.map((a) => ({
          id: a.id,
          nombre: a.litros ? `${a.codigo} · ${num(a.litros, 0)} L` : a.codigo,
        }))}
        valor={acuario}
        onChange={(v) => setAcuario(v)}
        placeholder="Escribe para buscar…"
      />
      <button
        type="button"
        disabled={disabled || !acuario}
        className={botonGrande}
        onClick={() => onUbicar(acuario)}
      >
        Ubicar
      </button>
    </div>
  );
}

function OmitirForm({
  pendiente,
  onEnviar,
}: {
  pendiente: boolean;
  onEnviar: (nota: string) => void;
}) {
  const [nota, setNota] = useState("");
  return (
    <form
      className="flex flex-col gap-4"
      onSubmit={(e) => {
        e.preventDefault();
        if (nota.trim()) onEnviar(nota.trim());
      }}
    >
      <Campo label="¿Por qué no se hizo?" htmlFor="nota-omitir">
        <textarea
          id="nota-omitir"
          className={textareaClass}
          value={nota}
          onChange={(e) => setNota(e.target.value)}
          maxLength={500}
          required
        />
      </Campo>
      <button
        type="submit"
        disabled={pendiente || !nota.trim()}
        className={botonPrimarioClass}
      >
        Registrar
      </button>
    </form>
  );
}
