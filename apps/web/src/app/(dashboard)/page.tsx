import { Boxes, PackageCheck, PlaneLanding, PlaneTakeoff } from "lucide-react";
import { PageHeader } from "@/components/PageHeader";

/** Módulos previstos. Cada tarjeta se convierte en enlace cuando su módulo exista. */
const MODULOS = [
  { titulo: "Catálogos", detalle: "Especies, tallas, proveedores y clientes", Icono: Boxes },
  { titulo: "Inventario", detalle: "Tanques, lotes, movimientos y mortalidad", Icono: PackageCheck },
  { titulo: "Importaciones", detalle: "Compras al exterior, recepción y cuarentena", Icono: PlaneLanding },
  { titulo: "Exportaciones", detalle: "Pedidos, proformas, packing list y despacho", Icono: PlaneTakeoff },
];

export default function PanelPage() {
  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <PageHeader titulo="Panel" descripcion="JR Tropical Fish" />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {MODULOS.map(({ titulo, detalle, Icono }) => (
          <div key={titulo} className="flex flex-col gap-2 rounded-md border border-borde bg-superficie px-4 py-4">
            <Icono className="size-5 text-acento" aria-hidden />
            <span className="font-medium text-texto">{titulo}</span>
            <span className="text-sm text-texto-suave">{detalle}</span>
            <span className="mt-1 w-fit rounded-full bg-neutro-fondo px-2.5 py-0.5 text-xs font-medium text-neutro-texto">
              Próximamente
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
