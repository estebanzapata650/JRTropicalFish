import Link from "next/link";
import { Boxes, Handshake, PackageCheck, PlaneLanding, PlaneTakeoff } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";
import { PERMISOS, type Permiso } from "@/lib/permisos";
import { obtenerUsuarioActual } from "@/lib/usuario-actual";

/** Módulos del sistema. Los que existen son enlaces (si el usuario tiene permiso); los demás, «Próximamente». */
const MODULOS: { titulo: string; detalle: string; Icono: typeof Boxes; href?: string; permiso?: Permiso }[] = [
  { titulo: "Comercial", detalle: "Dashboard, pipeline, ventas, vuelos y tareas", Icono: Handshake, href: "/crm", permiso: PERMISOS.METAS_LEER },
  { titulo: "Catálogos", detalle: "Productos, listas de precios, centros de acopio y proveedores", Icono: Boxes, href: "/catalogos/productos", permiso: PERMISOS.CATALOGOS_LEER },
  { titulo: "Inventario", detalle: "Tanques, lotes, movimientos y mortalidad", Icono: PackageCheck },
  { titulo: "Importaciones", detalle: "Compras al exterior, recepción y cuarentena", Icono: PlaneLanding },
  { titulo: "Exportaciones", detalle: "Pedidos, proformas, packing list y despacho", Icono: PlaneTakeoff },
];

export default async function PanelPage() {
  const { permisos } = await obtenerUsuarioActual();

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <PageHeader titulo="Panel" descripcion="JR Tropical Fish" />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {MODULOS.map(({ titulo, detalle, Icono, href, permiso }) => {
          const contenido = (
            <>
              <Icono className="size-5 text-acento" aria-hidden />
              <span className="font-medium text-texto">{titulo}</span>
              <span className="text-sm text-texto-suave">{detalle}</span>
              {href ? null : (
                <span className="mt-1 w-fit rounded-full bg-neutro-fondo px-2.5 py-0.5 text-xs font-medium text-neutro-texto">Próximamente</span>
              )}
            </>
          );
          const clase = "flex flex-col gap-2 rounded-md border border-borde bg-superficie px-4 py-4";
          return href && (!permiso || permisos.includes(permiso)) ? (
            <Link key={titulo} href={href} className={`${clase} transition-colors hover:border-acento hover:bg-acento-suave`}>
              {contenido}
            </Link>
          ) : (
            <div key={titulo} className={`${clase} ${href ? "opacity-60" : ""}`}>
              {contenido}
            </div>
          );
        })}
      </div>
    </div>
  );
}
