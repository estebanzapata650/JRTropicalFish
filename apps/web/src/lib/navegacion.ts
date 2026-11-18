import { PERMISOS, type Permiso } from "./permisos";

export type IconoNavegacion =
  | "panel"
  | "catalogos"
  | "inventario"
  | "importaciones"
  | "exportaciones"
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
