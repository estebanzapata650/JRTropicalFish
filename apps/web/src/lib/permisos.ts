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
  MAESTROS_LEER: "maestros:leer",
  MAESTROS_EDITAR: "maestros:editar",
  EMPLEADOS_LEER: "empleados:leer",
  EMPLEADOS_CREAR: "empleados:crear",
  EMPLEADOS_EDITAR: "empleados:editar",
  EMPLEADOS_ELIMINAR: "empleados:eliminar",
  EMPLEADOS_VER_SALARIO: "empleados:ver-salario",
  CLIENTES_LEER: "clientes:leer",
  CLIENTES_EDITAR: "clientes:editar",
  CRM_LEER: "crm:leer",
  CRM_VENDER: "crm:vender",
  CRM_VER_TODO: "crm:ver-todo",
  VENTAS_GESTIONAR: "ventas:gestionar",
  TRACKING_GESTIONAR: "tracking:gestionar",
  METAS_LEER: "metas:leer",
  METAS_EDITAR: "metas:editar",
  CATALOGOS_LEER: "catalogos:leer",
  CATALOGOS_EDITAR: "catalogos:editar",
  CATALOGOS_VER_COSTOS: "catalogos:ver-costos",
  PRECIOS_LEER: "precios:leer",
  PRECIOS_EDITAR: "precios:editar",
  PROVEEDORES_LEER: "proveedores:leer",
  PROVEEDORES_EDITAR: "proveedores:editar",
  INVENTARIO_LEER: "inventario:leer",
  INVENTARIO_OPERAR: "inventario:operar",
  INVENTARIO_AJUSTAR: "inventario:ajustar",
  COMPRAS_GESTIONAR: "compras:gestionar",
  COMPRAS_RECIBIR: "compras:recibir",
  PEDIDOS_GESTIONAR: "pedidos:gestionar",
  PEDIDOS_DESPACHAR: "pedidos:despachar",
  EXPORTACIONES_LEER: "exportaciones:leer",
  EXPORTACIONES_GESTIONAR: "exportaciones:gestionar",
  EXPORTACIONES_EMPACAR: "exportaciones:empacar",
  EXPORTACIONES_CONFIGURAR: "exportaciones:configurar",
  SANIDAD_LEER: "sanidad:leer",
  SANIDAD_OPERAR: "sanidad:operar",
  SANIDAD_GESTIONAR: "sanidad:gestionar",
  SANIDAD_APROBAR_PROTOCOLO: "sanidad:aprobar-protocolo",
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
  maestros: "Maestros",
  empleados: "Empleados",
  clientes: "Clientes",
  crm: "CRM comercial",
  ventas: "Ventas",
  tracking: "Tracking de vuelos",
  metas: "Metas y comisiones",
  catalogos: "Catálogo de productos",
  precios: "Listas de precios",
  proveedores: "Proveedores",
  inventario: "Inventario",
  compras: "Compras",
  pedidos: "Pedidos de clientes",
};

export const etiquetaModulo = (modulo: string) =>
  ETIQUETAS_MODULO[modulo] ?? modulo.charAt(0).toUpperCase() + modulo.slice(1).replace(/-/g, " ");

export const tienePermiso = (permisos: string[], permiso: Permiso) => permisos.includes(permiso);
