"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import {
  Boxes,
  ChevronLeft,
  ChevronRight,
  Fish,
  LayoutDashboard,
  LogOut,
  Menu,
  PackageCheck,
  PlaneLanding,
  PlaneTakeoff,
  ShieldCheck,
  UserRound,
  X,
} from "lucide-react";
import type { IconoNavegacion, ItemNavegacion } from "@/lib/navegacion";
import { BotonTema } from "./TemaProvider";

const iconos: Record<IconoNavegacion, typeof LayoutDashboard> = {
  panel: LayoutDashboard,
  catalogos: Boxes,
  inventario: PackageCheck,
  importaciones: PlaneLanding,
  exportaciones: PlaneTakeoff,
  seguridad: ShieldCheck,
};

const NOMBRE = "JR Tropical Fish";

const CLAVE_COLAPSADO = "jr_sidebar_colapsado";

export function AppShell({
  navegacion,
  usuario,
  children,
}: {
  navegacion: ItemNavegacion[];
  usuario: { nombre: string; perfil: string };
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const [colapsado, setColapsado] = useState(false);
  const [menuMovilAbierto, setMenuMovilAbierto] = useState(false);

  useEffect(() => {
    setColapsado(window.localStorage.getItem(CLAVE_COLAPSADO) === "true");
  }, []);

  useEffect(() => {
    setMenuMovilAbierto(false);
  }, [pathname]);

  const alternarColapsado = () => {
    setColapsado((previo) => {
      window.localStorage.setItem(CLAVE_COLAPSADO, String(!previo));
      return !previo;
    });
  };

  const nav = (mostrarEtiquetas: boolean) => (
    <nav className="flex flex-1 flex-col gap-1 overflow-y-auto p-2">
      {navegacion.map((item) => {
        const Icono = iconos[item.icono];
        const activo = item.hijos
          ? item.hijos.some((h) => pathname.startsWith(h.href))
          : pathname === item.href;

        return (
          <div key={item.href} className="flex flex-col gap-1">
            <Link
              // Un padre con hijos no tiene página propia: lleva al primero visible.
              href={item.hijos?.[0]?.href ?? item.href}
              title={mostrarEtiquetas ? undefined : item.label}
              className={`flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium transition-colors ${
                activo ? "bg-acento text-acento-texto" : "text-texto hover:bg-acento-suave"
              }`}
            >
              <Icono className="size-5 shrink-0" aria-hidden />
              {mostrarEtiquetas ? <span className="truncate">{item.label}</span> : null}
            </Link>

            {mostrarEtiquetas && item.hijos && activo ? (
              <div className="ml-8 flex flex-col gap-0.5">
                {item.hijos.map((hijo) => (
                  <Link
                    key={hijo.href}
                    href={hijo.href}
                    className={`rounded-md px-3 py-2 text-sm transition-colors ${
                      pathname.startsWith(hijo.href)
                        ? "font-medium text-texto"
                        : "text-texto-suave hover:text-texto"
                    }`}
                  >
                    {hijo.label}
                  </Link>
                ))}
              </div>
            ) : null}
          </div>
        );
      })}
    </nav>
  );

  const pie = (mostrarEtiquetas: boolean) => (
    <div className="border-t border-borde p-2">
      {mostrarEtiquetas ? (
        <div className="px-3 py-2">
          <p className="truncate text-sm font-medium text-texto">{usuario.nombre}</p>
          <p className="truncate text-xs text-texto-suave">{usuario.perfil}</p>
        </div>
      ) : null}
      <Link
        href="/cuenta"
        title={mostrarEtiquetas ? undefined : "Mi cuenta"}
        className={`flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors hover:bg-acento-suave hover:text-texto ${
          pathname === "/cuenta" ? "font-medium text-texto" : "text-texto-suave"
        }`}
      >
        <UserRound className="size-5 shrink-0" aria-hidden />
        {mostrarEtiquetas ? "Mi cuenta" : <span className="sr-only">Mi cuenta</span>}
      </Link>
      <BotonTema mostrarEtiqueta={mostrarEtiquetas} />
      {/* POST clásico (navegación completa), no Server Action: ver app/logout/route.ts. */}
      <form method="post" action="/logout">
        <button
          type="submit"
          title={mostrarEtiquetas ? undefined : "Cerrar sesión"}
          className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-left text-sm text-texto-suave hover:bg-acento-suave hover:text-texto"
        >
          <LogOut className="size-5 shrink-0" aria-hidden />
          {mostrarEtiquetas ? "Cerrar sesión" : <span className="sr-only">Cerrar sesión</span>}
        </button>
      </form>
    </div>
  );

  return (
    <div className="flex min-h-dvh">
      {/* Escritorio: barra lateral fija, colapsable a solo iconos. */}
      <aside
        className={`sticky top-0 hidden h-dvh shrink-0 flex-col border-r border-borde bg-superficie transition-[width] duration-200 md:flex ${
          colapsado ? "w-16" : "w-60"
        }`}
      >
        <div className="flex h-14 items-center gap-2 border-b border-borde px-3">
          {!colapsado ? (
            <span className="flex items-center gap-2 truncate text-sm font-semibold text-texto">
              <Fish className="size-5 shrink-0 text-acento" aria-hidden />
              {NOMBRE}
            </span>
          ) : null}
          <button
            type="button"
            onClick={alternarColapsado}
            aria-label={colapsado ? "Expandir menú" : "Contraer menú"}
            className="ml-auto rounded-md p-1.5 text-texto-suave hover:bg-acento-suave hover:text-texto"
          >
            {colapsado ? <ChevronRight className="size-4" /> : <ChevronLeft className="size-4" />}
          </button>
        </div>
        {nav(!colapsado)}
        {pie(!colapsado)}
      </aside>

      {/* min-w-0 es indispensable: sin él las tablas anchas estiran el contenedor
          y su scroll horizontal interno nunca se activa. */}
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-10 flex h-14 items-center gap-3 border-b border-borde bg-superficie/90 px-4 backdrop-blur md:hidden">
          <button
            type="button"
            onClick={() => setMenuMovilAbierto(true)}
            aria-label="Abrir menú"
            className="rounded-md p-2 text-texto hover:bg-acento-suave"
          >
            <Menu className="size-5" />
          </button>
          <span className="truncate text-sm font-semibold text-texto">{NOMBRE}</span>
        </header>

        {menuMovilAbierto ? (
          <div className="fixed inset-0 z-50 md:hidden">
            {/* Capa de fondo: cierra al tocar, pero no se anuncia como control aparte —
                el botón X es el único con nombre accesible, para no duplicar la etiqueta. */}
            <button
              type="button"
              aria-hidden
              tabIndex={-1}
              onClick={() => setMenuMovilAbierto(false)}
              className="absolute inset-0 bg-black/40"
            />
            <div className="absolute inset-y-0 left-0 flex w-64 flex-col bg-superficie shadow-xl">
              <div className="flex h-14 items-center justify-between border-b border-borde px-3">
                <span className="truncate text-sm font-semibold text-texto">{NOMBRE}</span>
                <button
                  type="button"
                  onClick={() => setMenuMovilAbierto(false)}
                  aria-label="Cerrar menú"
                  className="rounded-md p-1.5 text-texto-suave hover:bg-acento-suave"
                >
                  <X className="size-4" />
                </button>
              </div>
              {nav(true)}
              {pie(true)}
            </div>
          </div>
        ) : null}

        <main className="flex-1 px-4 py-6 md:px-8 md:py-8">{children}</main>
      </div>
    </div>
  );
}
