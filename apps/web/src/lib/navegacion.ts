import { PERMISOS, type Permiso } from "./permisos";

export type IconoNavegacion =
  | "panel"
  | "catalogos"
  | "inventario"
  | "importaciones"
  | "exportaciones"
  | "sanidad"
  | "administracion"
  | "comercial"
  | "seguridad";

export type ItemNavegacion = {
  href: string;
  label: string;
  icono: IconoNavegacion;
  /** Si se indica, el ítem solo aparece cuando el usuario tiene ese permiso. */
  permiso?: Permiso;
  hijos?: { href: string; label: string; permiso?: Permiso }[];
};

/**
 * Menú declarativo: cada ítem lleva su permiso en un campo, sin casos especiales por etiqueta.
 * Un ítem con hijos desaparece si el usuario no puede ver ninguno de ellos.
 *
 * Al crear un módulo (Catálogos, Inventario, Importaciones, Exportaciones) se agrega aquí su
 * entrada con el permiso `modulo:leer` correspondiente.
 */
export function construirNavegacion(): ItemNavegacion[] {
  return [
    { href: "/", label: "Panel", icono: "panel" },
    {
      href: "/crm",
      label: "Comercial",
      icono: "comercial",
      hijos: [
        { href: "/crm", label: "Dashboard", permiso: PERMISOS.METAS_LEER },
        { href: "/crm/pipeline", label: "Pipeline", permiso: PERMISOS.CRM_LEER },
        { href: "/crm/ventas", label: "Ventas", permiso: PERMISOS.CRM_LEER },
        { href: "/crm/pedidos", label: "Pedidos de clientes", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/crm/vuelos", label: "Torre de vuelos", permiso: PERMISOS.CRM_LEER },
        { href: "/crm/tareas", label: "Tareas", permiso: PERMISOS.CRM_LEER },
        { href: "/crm/clientes", label: "Clientes", permiso: PERMISOS.CLIENTES_LEER },
        { href: "/crm/metas", label: "Metas", permiso: PERMISOS.METAS_EDITAR },
      ],
    },
    {
      href: "/inventario",
      label: "Inventario",
      icono: "inventario",
      hijos: [
        { href: "/inventario", label: "Tableros", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/inventario/existencias", label: "Existencias", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/inventario/lotes", label: "Trazabilidad", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/inventario/acuarios", label: "Acuarios", permiso: PERMISOS.INVENTARIO_LEER },
      ],
    },
    {
      href: "/sanidad",
      label: "Sanidad",
      icono: "sanidad",
      hijos: [
        { href: "/sanidad", label: "Mi día", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/tablero", label: "Tablero de acuarios", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/tratamientos", label: "Tratamientos", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/protocolos", label: "Protocolos", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/medicamentos", label: "Medicamentos", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/alimentacion", label: "Alimentos y dietas", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/fichas", label: "Fichas técnicas", permiso: PERMISOS.SANIDAD_LEER },
        { href: "/sanidad/configuracion", label: "Configuración", permiso: PERMISOS.SANIDAD_GESTIONAR },
      ],
    },
    {
      href: "/exportaciones",
      label: "Exportaciones",
      icono: "exportaciones",
      hijos: [
        { href: "/exportaciones", label: "Embarques", permiso: PERMISOS.EXPORTACIONES_LEER },
        { href: "/exportaciones/aunap", label: "AUNAP", permiso: PERMISOS.EXPORTACIONES_LEER },
        { href: "/exportaciones/configuracion", label: "Configuración", permiso: PERMISOS.EXPORTACIONES_CONFIGURAR },
      ],
    },
    {
      href: "/compras",
      label: "Compras",
      icono: "importaciones",
      hijos: [
        { href: "/compras/necesidades", label: "Necesidades", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/compras/ofertas", label: "Disponibilidad proveedores", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/compras/pedidos", label: "Pedidos a proveedor", permiso: PERMISOS.INVENTARIO_LEER },
        { href: "/compras/recepciones", label: "Recepciones", permiso: PERMISOS.INVENTARIO_LEER },
      ],
    },
    {
      href: "/catalogos",
      label: "Catálogos",
      icono: "catalogos",
      hijos: [
        { href: "/catalogos/productos", label: "Productos", permiso: PERMISOS.CATALOGOS_LEER },
        { href: "/catalogos/precios", label: "Listas de precios", permiso: PERMISOS.PRECIOS_LEER },
        { href: "/catalogos/centros-acopio", label: "Centros de acopio", permiso: PERMISOS.CATALOGOS_LEER },
        { href: "/catalogos/proveedores", label: "Proveedores", permiso: PERMISOS.PROVEEDORES_LEER },
      ],
    },
    {
      href: "/administracion",
      label: "Administración",
      icono: "administracion",
      hijos: [
        { href: "/administracion/empleados", label: "Empleados", permiso: PERMISOS.EMPLEADOS_LEER },
        { href: "/administracion/maestros", label: "Maestros", permiso: PERMISOS.MAESTROS_LEER },
      ],
    },
    {
      href: "/seguridad",
      label: "Seguridad",
      icono: "seguridad",
      hijos: [
        { href: "/seguridad/usuarios", label: "Usuarios", permiso: PERMISOS.USUARIOS_LEER },
        { href: "/seguridad/perfiles", label: "Perfiles", permiso: PERMISOS.PERFILES_LEER },
      ],
    },
  ];
}

export function filtrarNavegacion(items: ItemNavegacion[], permisos: string[]): ItemNavegacion[] {
  return items
    .map((item) => ({
      ...item,
      hijos: item.hijos?.filter((h) => !h.permiso || permisos.includes(h.permiso)),
    }))
    .filter((item) => {
      if (item.hijos) return item.hijos.length > 0;
      return !item.permiso || permisos.includes(item.permiso);
    });
}
