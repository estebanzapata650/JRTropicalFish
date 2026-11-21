import Link from "next/link";
import { hoyNegocio } from "@/lib/formato";

/**
 * Piezas comunes de los tableros de inventario (reemplazan los 4 de Looker): pestañas, periodo y rango
 * de fechas. El periodo vive en la URL (`?periodo=mes`) para poder compartir el enlace.
 */

const PESTANAS = [
  { href: "/inventario", texto: "Bodega" },
  { href: "/inventario/proveedores", texto: "Proveedores" },
  { href: "/inventario/clientes", texto: "Clientes" },
  { href: "/inventario/lotes", texto: "Trazabilidad" },
] as const;

export function PestanasTableros({ actual }: { actual: (typeof PESTANAS)[number]["href"] }) {
  return (
    <nav aria-label="Tableros de inventario" className="-mx-4 overflow-x-auto px-4 sm:mx-0 sm:px-0">
      <ul className="flex min-w-max gap-1 border-b border-borde">
        {PESTANAS.map((p) => {
          const activa = p.href === actual;
          return (
            <li key={p.href}>
              <Link
                href={p.href}
                aria-current={activa ? "page" : undefined}
                className={`-mb-px inline-flex min-h-11 items-center border-b-2 px-3 text-sm font-medium ${
                  activa ? "border-acento text-texto" : "border-transparent text-texto-suave hover:text-texto"
                }`}
              >
                {p.texto}
              </Link>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}

export const PERIODOS = {
  "30d": "Últimos 30 días",
  mes: "Este mes",
  "mes-anterior": "Mes anterior",
  "90d": "Últimos 90 días",
  anio: "Este año",
} as const;

export type Periodo = keyof typeof PERIODOS;

const iso = (d: Date) => d.toISOString().slice(0, 10);

/** Rango `yyyy-MM-dd` (ambos inclusive) del periodo, contra el "hoy" de negocio (Bogotá). */
export function rangoPeriodo(periodo: string | undefined): { periodo: Periodo; desde: string; hasta: string } {
  const p: Periodo = periodo && periodo in PERIODOS ? (periodo as Periodo) : "30d";
  const [a, m, d] = hoyNegocio().split("-").map(Number);
  const hoy = new Date(Date.UTC(a, m - 1, d));
  const dias = (n: number) => new Date(Date.UTC(a, m - 1, d - n));
  switch (p) {
    case "mes":
      return { periodo: p, desde: iso(new Date(Date.UTC(a, m - 1, 1))), hasta: iso(hoy) };
    case "mes-anterior":
      return { periodo: p, desde: iso(new Date(Date.UTC(a, m - 2, 1))), hasta: iso(new Date(Date.UTC(a, m - 1, 0))) };
    case "90d":
      return { periodo: p, desde: iso(dias(89)), hasta: iso(hoy) };
    case "anio":
      return { periodo: p, desde: iso(new Date(Date.UTC(a, 0, 1))), hasta: iso(hoy) };
    default:
      return { periodo: p, desde: iso(dias(29)), hasta: iso(hoy) };
  }
}

/** Selector de periodo como enlaces (sin JS): cada opción es una URL. */
export function SelectorPeriodo({ ruta, actual }: { ruta: string; actual: Periodo }) {
  return (
    <div role="group" aria-label="Periodo" className="flex flex-wrap gap-1.5">
      {(Object.keys(PERIODOS) as Periodo[]).map((p) => (
        <Link
          key={p}
          href={p === "30d" ? ruta : `${ruta}?periodo=${p}`}
          aria-current={p === actual ? "true" : undefined}
          className={`inline-flex min-h-9 items-center rounded-full border px-3 text-sm ${
            p === actual ? "border-acento bg-acento-suave font-medium text-texto" : "border-borde text-texto-suave hover:bg-acento-suave hover:text-texto"
          }`}
        >
          {PERIODOS[p]}
        </Link>
      ))}
    </div>
  );
}

/** Tarjeta de sección de tablero: título, ayuda corta y contenido. */
export function Panel({
  titulo,
  ayuda,
  accion,
  children,
  className = "",
}: {
  titulo: string;
  ayuda?: string;
  accion?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <section className={`flex min-w-0 flex-col gap-4 rounded-md border border-borde bg-superficie p-4 ${className}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="text-base font-semibold text-texto">{titulo}</h2>
          {ayuda ? <p className="text-sm text-texto-suave">{ayuda}</p> : null}
        </div>
        {accion ? <div className="shrink-0 text-sm">{accion}</div> : null}
      </div>
      {children}
    </section>
  );
}
