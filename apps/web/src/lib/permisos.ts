/** Espejo de `apps/api/src/JRTropicalFish.Domain/Seguridad/Permisos.cs`. Mantener sincronizado. */
export const PERMISOS = {
  USUARIOS_LEER: "usuarios:leer",
  USUARIOS_CREAR: "usuarios:crear",
  USUARIOS_EDITAR: "usuarios:editar",
  USUARIOS_ELIMINAR: "usuarios:eliminar",
  PERFILES_LEER: "perfiles:leer",
  PERFILES_CREAR: "perfiles:crear",
  PERFILES_EDITAR: "perfiles:editar",
  PERFILES_ELIMINAR: "perfiles:eliminar",
  // Catálogos, Inventario, Importaciones y Exportaciones se agregan con cada módulo
  // (ver docs/dominio/modulos.md).
} as const;

export type Permiso = (typeof PERMISOS)[keyof typeof PERMISOS];

/** Lo que devuelve `GET /api/v1/auth/yo`. */
export type UsuarioActual = {
  id: string;
  nombre: string;
  email: string;
  perfil: string;
  permisos: string[];
};

/** Nombre legible de cada módulo de permisos (la clave es lo que va antes de `:`). */
export const ETIQUETAS_MODULO: Record<string, string> = {
  usuarios: "Usuarios",
  perfiles: "Perfiles",
};

export const etiquetaModulo = (modulo: string) =>
  ETIQUETAS_MODULO[modulo] ?? modulo.charAt(0).toUpperCase() + modulo.slice(1).replace(/-/g, " ");

export const tienePermiso = (permisos: string[], permiso: Permiso) => permisos.includes(permiso);
