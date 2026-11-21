"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Plus, Upload } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { BotonIcono, Campo, EncabezadoTabla, EtiquetaEstado, RejillaCampos, Seccion, TablaResponsive } from "@/components/campos";
import { Dialogo } from "@/components/Dialogo";
import { cifra, usd, type Cargo, type Configuracion, type DatosEmpresa, type TipoBolsa, type TipoCaja } from "@/lib/exportaciones";
import { botonPrimarioClass, botonSecundarioClass, casillaClass, inputClass } from "@/lib/estilos";
import { guardarBolsaAction, guardarCajaAction, guardarCargoAction, guardarEmpresaAction, guardarTarifasAction, subirLogoAction } from "../actions";
import { Aviso, numero } from "../[id]/comun";
import { CAMPOS_TARIFA } from "../[id]/PasoFlete";

/**
 * Configuración de los documentos de exportación (hoja «Parametros» del libro de formatos): datos y logo
 * de la empresa, tarifas de flete por defecto, tipos de caja y de bolsa, y precios de los cargos.
 */
export function ConfiguracionCliente({ c }: { c: Configuracion }) {
  const [aviso, setAviso] = useState<string | null>(null);
  return (
    <div className="flex flex-col gap-6">
      <PageHeader titulo="Configuración de exportaciones" descripcion="Lo que encabeza y calcula cada documento: empresa, tarifas de flete, cajas, bolsas y cargos." />
      <Aviso texto={aviso} />
      <Empresa e={c.empresa} onGuardado={setAviso} />
      <TarifasPorDefecto t={c.tarifas} onGuardado={setAviso} />
      <Cajas cajas={c.cajas} onGuardado={setAviso} />
      <Bolsas bolsas={c.bolsas} onGuardado={setAviso} />
      <Cargos cargos={c.cargos} onGuardado={setAviso} />
    </div>
  );
}

const CAMPOS_EMPRESA: { campo: keyof DatosEmpresa; etiqueta: string; grupo: "empresa" | "banco" | "firma" }[] = [
  { campo: "razonSocial", etiqueta: "Razón social", grupo: "empresa" },
  { campo: "nit", etiqueta: "NIT", grupo: "empresa" },
  { campo: "direccion", etiqueta: "Dirección", grupo: "empresa" },
  { campo: "ciudadPais", etiqueta: "Ciudad y país", grupo: "empresa" },
  { campo: "telefono", etiqueta: "Teléfono", grupo: "empresa" },
  { campo: "email", etiqueta: "Correo de ventas", grupo: "empresa" },
  { campo: "sitioWeb", etiqueta: "Sitio web", grupo: "empresa" },
  { campo: "banco", etiqueta: "Banco", grupo: "banco" },
  { campo: "swift", etiqueta: "SWIFT / BIC", grupo: "banco" },
  { campo: "cuentaBancaria", etiqueta: "Cuenta corriente N°", grupo: "banco" },
  { campo: "beneficiario", etiqueta: "Beneficiario", grupo: "banco" },
  { campo: "direccionBanco", etiqueta: "Dirección de la oficina", grupo: "banco" },
  { campo: "firmanteNombre", etiqueta: "Firma: nombre", grupo: "firma" },
  { campo: "firmanteCargo", etiqueta: "Firma: cargo", grupo: "firma" },
  { campo: "firmanteTelefono", etiqueta: "Firma: celular y WhatsApp", grupo: "firma" },
];

function Empresa({ e, onGuardado }: { e: DatosEmpresa; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [valores, setValores] = useState(() => Object.fromEntries(CAMPOS_EMPRESA.map((c) => [c.campo, (e[c.campo] as string | null) ?? ""])));
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const grupos = [
    { clave: "empresa", titulo: "Empresa (encabezado de los documentos)" },
    { clave: "banco", titulo: "Datos bancarios (preinvoice y balance)" },
    { clave: "firma", titulo: "Firma de los documentos" },
  ] as const;

  return (
    <Seccion titulo="Empresa, banco y firma">
      <div className="flex flex-col gap-6 p-4">
        <Logo version={e.actualizadoEn} hayLogo={!!e.logoClave} onGuardado={onGuardado} />
        <form
          onSubmit={(ev) => {
            ev.preventDefault();
            iniciar(async () => {
              const r = await guardarEmpresaAction(Object.fromEntries(CAMPOS_EMPRESA.map((c) => [c.campo, valores[c.campo] || null])));
              if (r.error) {
                setError(r.error);
                return;
              }
              setError(null);
              onGuardado("Datos de la empresa guardados.");
              router.refresh();
            });
          }}
          className="flex flex-col gap-6"
        >
          {grupos.map((g) => (
            <fieldset key={g.clave} className="flex flex-col gap-3">
              <legend className="mb-1 text-sm font-semibold text-texto">{g.titulo}</legend>
              <RejillaCampos>
                {CAMPOS_EMPRESA.filter((c) => c.grupo === g.clave).map((c) => (
                  <Campo key={c.campo} label={c.etiqueta} htmlFor={`emp-${c.campo}`}>
                    <input id={`emp-${c.campo}`} value={valores[c.campo]} onChange={(ev) => setValores((v) => ({ ...v, [c.campo]: ev.target.value }))} maxLength={320} className={inputClass} />
                  </Campo>
                ))}
              </RejillaCampos>
            </fieldset>
          ))}
          {error ? <p className="text-sm text-peligro">{error}</p> : null}
          <div className="flex justify-end">
            <button type="submit" disabled={guardando} className={botonPrimarioClass}>
              {guardando ? "Guardando…" : "Guardar empresa"}
            </button>
          </div>
        </form>
      </div>
    </Seccion>
  );
}

function Logo({ version, hayLogo, onGuardado }: { version: string | null; hayLogo: boolean; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [subiendo, iniciar] = useTransition();
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <div className="flex size-24 shrink-0 items-center justify-center overflow-hidden rounded-md border border-borde bg-fondo">
        {hayLogo ? (
          // eslint-disable-next-line @next/next/no-img-element -- imagen privada servida por route handler, sin optimizador
          <img src={`/archivos/exportaciones/logo?v=${encodeURIComponent(version ?? "")}`} alt="Logo de la empresa" className="max-h-full max-w-full object-contain" />
        ) : (
          <span className="px-2 text-center text-xs text-texto-tenue">Sin logo</span>
        )}
      </div>
      <form
        onSubmit={(ev) => {
          ev.preventDefault();
          const datos = new FormData(ev.currentTarget);
          iniciar(async () => {
            const r = await subirLogoAction(datos);
            if (r.error) {
              setError(r.error);
              return;
            }
            setError(null);
            onGuardado("Logo actualizado: sale en el encabezado de los PDF.");
            router.refresh();
          });
        }}
        className="flex flex-col gap-2"
      >
        <label htmlFor="logo" className="text-sm text-texto">
          Logo para los PDF (PNG o JPG, hasta 2 MB)
        </label>
        <div className="flex flex-wrap gap-2">
          <input id="logo" name="archivo" type="file" accept="image/png,image/jpeg" required className="max-w-full text-sm text-texto file:mr-3 file:rounded-md file:border file:border-borde file:bg-superficie file:px-3 file:py-2 file:text-texto" />
          <button type="submit" disabled={subiendo} className={`${botonSecundarioClass} gap-2`}>
            <Upload className="size-4" aria-hidden /> {subiendo ? "Subiendo…" : "Subir logo"}
          </button>
        </div>
        {error ? <p className="text-sm text-peligro">{error}</p> : null}
      </form>
    </div>
  );
}

function TarifasPorDefecto({ t, onGuardado }: { t: Configuracion["tarifas"]; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [valores, setValores] = useState(() => Object.fromEntries(CAMPOS_TARIFA.map((c) => [c.campo, String(t[c.campo])])));
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  return (
    <Seccion titulo="Tarifas de flete por defecto (USD)">
      <form
        onSubmit={(ev) => {
          ev.preventDefault();
          iniciar(async () => {
            const r = await guardarTarifasAction(Object.fromEntries(CAMPOS_TARIFA.map((c) => [c.campo, numero(valores[c.campo]) ?? 0])));
            if (r.error) {
              setError(r.error);
              return;
            }
            setError(null);
            onGuardado("Tarifas guardadas: aplican a los embarques nuevos.");
            router.refresh();
          });
        }}
        className="flex flex-col gap-4 p-4"
      >
        <p className="text-sm text-texto-suave">Cada embarque las copia al crearse; cambiar aquí no toca los embarques existentes.</p>
        <RejillaCampos>
          {CAMPOS_TARIFA.map((c) => (
            <Campo key={c.campo} label={c.etiqueta} htmlFor={`tar-${c.campo}`} hint={c.ayuda}>
              <input id={`tar-${c.campo}`} type="number" inputMode="decimal" min={0} step="0.0001" value={valores[c.campo]} onChange={(ev) => setValores((v) => ({ ...v, [c.campo]: ev.target.value }))} className={inputClass} />
            </Campo>
          ))}
        </RejillaCampos>
        {error ? <p className="text-sm text-peligro">{error}</p> : null}
        <div className="flex justify-end">
          <button type="submit" disabled={guardando} className={botonPrimarioClass}>
            {guardando ? "Guardando…" : "Guardar tarifas"}
          </button>
        </div>
      </form>
    </Seccion>
  );
}

function Cajas({ cajas, onGuardado }: { cajas: TipoCaja[]; onGuardado: (t: string) => void }) {
  const [editando, setEditando] = useState<TipoCaja | null | undefined>(undefined);
  return (
    <Seccion titulo="Tipos de caja">
      <div className="flex flex-col gap-3 p-4">
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Caja</th>
              <th className="px-3 py-2 text-right font-medium">Precio</th>
              <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Kg · pisos</th>
              <th className="hidden px-3 py-2 text-right font-medium md:table-cell">Costo (COP)</th>
              <th className="px-3 py-2">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {cajas.map((c) => (
              <tr key={c.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2 text-texto">
                  {c.nombre} {c.activo ? null : <EtiquetaEstado activo={false} />}
                  <p className="text-xs text-texto-suave">{c.descripcionFactura}</p>
                </td>
                <td className="px-3 py-2 text-right tabular-nums text-texto">{usd(c.precioUsd)}</td>
                <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">
                  {cifra(c.kgPorCaja)} kg · {c.pisos}
                </td>
                <td className="hidden px-3 py-2 text-right tabular-nums text-texto md:table-cell">{c.costoCop.toLocaleString("es-CO", { maximumFractionDigits: 0 })}</td>
                <td className="px-3 py-2 text-right">
                  <BotonIcono accion="editar" etiqueta={`Editar ${c.nombre}`} onClick={() => setEditando(c)} />
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
        <button type="button" onClick={() => setEditando(null)} className={`${botonSecundarioClass} w-fit gap-2`}>
          <Plus className="size-4" aria-hidden /> Nuevo tipo de caja
        </button>
      </div>
      <Dialogo titulo={editando ? `Editar ${editando.nombre}` : "Nuevo tipo de caja"} abierto={editando !== undefined} onCerrar={() => setEditando(undefined)}>
        {editando !== undefined ? (
          <FormularioCaja
            caja={editando}
            onGuardado={(t) => {
              setEditando(undefined);
              onGuardado(t);
            }}
          />
        ) : null}
      </Dialogo>
    </Seccion>
  );
}

function FormularioCaja({ caja, onGuardado }: { caja: TipoCaja | null; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [v, setV] = useState({
    nombre: caja?.nombre ?? "",
    descripcionFactura: caja?.descripcionFactura ?? "",
    costoCop: String(caja?.costoCop ?? ""),
    precioUsd: String(caja?.precioUsd ?? ""),
    kgPorCaja: String(caja?.kgPorCaja ?? ""),
    pisos: String(caja?.pisos ?? 1),
    activo: caja?.activo ?? true,
  });
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  const sugerido = numero(v.costoCop) ? Math.ceil(((numero(v.costoCop) ?? 0) / 3200) * 1.15) : null;
  return (
    <form
      onSubmit={(ev) => {
        ev.preventDefault();
        iniciar(async () => {
          const r = await guardarCajaAction({
            id: caja?.id ?? null,
            nombre: v.nombre,
            descripcionFactura: v.descripcionFactura,
            costoCop: numero(v.costoCop) ?? 0,
            precioUsd: numero(v.precioUsd) ?? 0,
            kgPorCaja: numero(v.kgPorCaja) ?? 0,
            pisos: numero(v.pisos) ?? 1,
            activo: v.activo,
          });
          if (r.error) {
            setError(r.error);
            return;
          }
          onGuardado(`Caja «${v.nombre}» guardada.`);
          router.refresh();
        });
      }}
      className="flex flex-col gap-4"
    >
      <RejillaCampos>
        <Campo label="Nombre" htmlFor="cajaNombre">
          <input id="cajaNombre" value={v.nombre} onChange={(ev) => setV({ ...v, nombre: ev.target.value })} maxLength={80} required className={inputClass} />
        </Campo>
        <Campo label="Descripción en factura" htmlFor="cajaDesc">
          <input id="cajaDesc" value={v.descripcionFactura} onChange={(ev) => setV({ ...v, descripcionFactura: ev.target.value })} maxLength={150} required className={inputClass} />
        </Campo>
        <Campo label="Costo de materiales (COP)" htmlFor="cajaCosto">
          <input id="cajaCosto" type="number" inputMode="decimal" min={0} value={v.costoCop} onChange={(ev) => setV({ ...v, costoCop: ev.target.value })} className={inputClass} />
        </Campo>
        <Campo label="Precio al cliente (USD)" htmlFor="cajaPrecio" hint={sugerido ? `Excel: costo / 3.200 × 1,15 ≈ ${sugerido}` : undefined}>
          <input id="cajaPrecio" type="number" inputMode="decimal" min={0} step="0.01" value={v.precioUsd} onChange={(ev) => setV({ ...v, precioUsd: ev.target.value })} className={inputClass} />
        </Campo>
        <Campo label="Kg por caja (flete)" htmlFor="cajaKg">
          <input id="cajaKg" type="number" inputMode="decimal" min={0} step="0.1" value={v.kgPorCaja} onChange={(ev) => setV({ ...v, kgPorCaja: ev.target.value })} className={inputClass} />
        </Campo>
        <Campo label="Pisos (heat packs)" htmlFor="cajaPisos">
          <input id="cajaPisos" type="number" inputMode="numeric" min={1} max={5} value={v.pisos} onChange={(ev) => setV({ ...v, pisos: ev.target.value })} className={inputClass} />
        </Campo>
      </RejillaCampos>
      <label className="flex min-h-11 items-center gap-2 text-sm text-texto">
        <input type="checkbox" checked={v.activo} onChange={(ev) => setV({ ...v, activo: ev.target.checked })} className={casillaClass} />
        Activa
      </label>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex justify-end">
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}

function Bolsas({ bolsas, onGuardado }: { bolsas: TipoBolsa[]; onGuardado: (t: string) => void }) {
  const [editando, setEditando] = useState<TipoBolsa | null | undefined>(undefined);
  return (
    <Seccion titulo="Tipos de bolsa">
      <div className="flex flex-col gap-3 p-4">
        <TablaResponsive compactaEnMovil>
          <EncabezadoTabla>
            <tr className="text-texto-suave">
              <th className="px-3 py-2 font-medium">Bolsa</th>
              <th className="px-3 py-2 text-right font-medium">Porción de caja</th>
              <th className="hidden px-3 py-2 text-right font-medium sm:table-cell">Costo (COP)</th>
              <th className="px-3 py-2">
                <span className="sr-only">Acciones</span>
              </th>
            </tr>
          </EncabezadoTabla>
          <tbody>
            {bolsas.map((b) => (
              <tr key={b.id} className="border-b border-borde last:border-0">
                <td className="px-3 py-2 text-texto">
                  {b.nombre} {b.activo ? null : <EtiquetaEstado activo={false} />}
                  <p className="text-xs text-texto-suave">{b.tamano ?? "—"}</p>
                </td>
                <td className="px-3 py-2 text-right tabular-nums text-texto">{cifra(b.porcion, 4)}</td>
                <td className="hidden px-3 py-2 text-right tabular-nums text-texto sm:table-cell">{b.costoCop.toLocaleString("es-CO")}</td>
                <td className="px-3 py-2 text-right">
                  <BotonIcono accion="editar" etiqueta={`Editar bolsa ${b.nombre}`} onClick={() => setEditando(b)} />
                </td>
              </tr>
            ))}
          </tbody>
        </TablaResponsive>
        <button type="button" onClick={() => setEditando(null)} className={`${botonSecundarioClass} w-fit gap-2`}>
          <Plus className="size-4" aria-hidden /> Nuevo tipo de bolsa
        </button>
      </div>
      <Dialogo titulo={editando ? `Editar bolsa ${editando.nombre}` : "Nuevo tipo de bolsa"} abierto={editando !== undefined} onCerrar={() => setEditando(undefined)}>
        {editando !== undefined ? (
          <FormularioBolsa
            bolsa={editando}
            onGuardado={(t) => {
              setEditando(undefined);
              onGuardado(t);
            }}
          />
        ) : null}
      </Dialogo>
    </Seccion>
  );
}

function FormularioBolsa({ bolsa, onGuardado }: { bolsa: TipoBolsa | null; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [v, setV] = useState({
    nombre: bolsa?.nombre ?? "",
    tamano: bolsa?.tamano ?? "",
    porcion: String(bolsa?.porcion ?? ""),
    costoCop: String(bolsa?.costoCop ?? ""),
    activo: bolsa?.activo ?? true,
  });
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  return (
    <form
      onSubmit={(ev) => {
        ev.preventDefault();
        iniciar(async () => {
          const r = await guardarBolsaAction({ id: bolsa?.id ?? null, nombre: v.nombre, tamano: v.tamano || null, porcion: numero(v.porcion) ?? 0, costoCop: numero(v.costoCop) ?? 0, activo: v.activo });
          if (r.error) {
            setError(r.error);
            return;
          }
          onGuardado(`Bolsa «${v.nombre}» guardada.`);
          router.refresh();
        });
      }}
      className="flex flex-col gap-4"
    >
      <RejillaCampos>
        <Campo label="Nombre" htmlFor="bolsaNombre">
          <input id="bolsaNombre" value={v.nombre} onChange={(ev) => setV({ ...v, nombre: ev.target.value })} maxLength={40} required className={inputClass} />
        </Campo>
        <Campo label="Tamaño" htmlFor="bolsaTam" hint='11,5"'>
          <input id="bolsaTam" value={v.tamano} onChange={(ev) => setV({ ...v, tamano: ev.target.value })} maxLength={20} className={inputClass} />
        </Campo>
        <Campo label="Porción de caja (0–1)" htmlFor="bolsaPor" hint="Export = 1, 1/2 = 0,5…">
          <input id="bolsaPor" type="number" inputMode="decimal" min={0} max={1} step="0.000001" value={v.porcion} onChange={(ev) => setV({ ...v, porcion: ev.target.value })} className={inputClass} />
        </Campo>
        <Campo label="Costo (COP)" htmlFor="bolsaCosto">
          <input id="bolsaCosto" type="number" inputMode="decimal" min={0} value={v.costoCop} onChange={(ev) => setV({ ...v, costoCop: ev.target.value })} className={inputClass} />
        </Campo>
      </RejillaCampos>
      <label className="flex min-h-11 items-center gap-2 text-sm text-texto">
        <input type="checkbox" checked={v.activo} onChange={(ev) => setV({ ...v, activo: ev.target.checked })} className={casillaClass} />
        Activa
      </label>
      {error ? <p className="text-sm text-peligro">{error}</p> : null}
      <div className="flex justify-end">
        <button type="submit" disabled={guardando} className={botonPrimarioClass}>
          {guardando ? "Guardando…" : "Guardar"}
        </button>
      </div>
    </form>
  );
}

function Cargos({ cargos, onGuardado }: { cargos: Cargo[]; onGuardado: (t: string) => void }) {
  const router = useRouter();
  const [valores, setValores] = useState(() => Object.fromEntries(cargos.map((c) => [c.codigo, { nombre: c.nombre, precio: String(c.precioUsd) }])));
  const [error, setError] = useState<string | null>(null);
  const [guardando, iniciar] = useTransition();
  return (
    <Seccion titulo="Certificados y cargos (USD)">
      <form
        onSubmit={(ev) => {
          ev.preventDefault();
          iniciar(async () => {
            for (const c of cargos) {
              const v = valores[c.codigo];
              if (v.nombre === c.nombre && numero(v.precio) === c.precioUsd) continue;
              const r = await guardarCargoAction({ codigo: c.codigo, nombre: v.nombre, precioUsd: numero(v.precio) ?? 0 });
              if (r.error) {
                setError(r.error);
                return;
              }
            }
            setError(null);
            onGuardado("Cargos guardados: aplican a los embarques nuevos.");
            router.refresh();
          });
        }}
        className="flex flex-col gap-3 p-4"
      >
        {cargos.map((c) => (
          <div key={c.codigo} className="grid grid-cols-1 gap-2 sm:grid-cols-[2fr_8rem]">
            <input aria-label={`Nombre en factura: ${c.codigo}`} value={valores[c.codigo].nombre} onChange={(ev) => setValores((x) => ({ ...x, [c.codigo]: { ...x[c.codigo], nombre: ev.target.value } }))} maxLength={120} className={inputClass} />
            <input
              aria-label={`Precio USD: ${c.nombre}`}
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={valores[c.codigo].precio}
              onChange={(ev) => setValores((x) => ({ ...x, [c.codigo]: { ...x[c.codigo], precio: ev.target.value } }))}
              className={inputClass}
            />
          </div>
        ))}
        {error ? <p className="text-sm text-peligro">{error}</p> : null}
        <div className="flex justify-end">
          <button type="submit" disabled={guardando} className={botonPrimarioClass}>
            {guardando ? "Guardando…" : "Guardar cargos"}
          </button>
        </div>
      </form>
    </Seccion>
  );
}
