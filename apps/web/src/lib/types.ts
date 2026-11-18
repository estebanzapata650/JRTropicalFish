/** Tipos compartidos de las respuestas de la API (espejo de los records de Application). */

// --- Auditoría ---

export type RegistroAuditoria = {
  id: string;
  entidad: string;
  entidadId: string;
  accion: "Crear" | "Actualizar" | "Eliminar" | string;
  cambiosJson: string;
  usuarioId: string | null;
  usuarioNombre: string | null;
  fechaUtc: string;
};

/** Así lo serializa la API (System.Text.Json sin política de nombres: PascalCase). */
export type CambioAuditado = { Campo: string; Anterior: string | null; Nuevo: string | null };

// --- Seguridad ---

export type PermisoCatalogo = { id: string; codigo: string; descripcion: string | null };

export type PerfilResumen = {
  id: string;
  nombre: string;
  descripcion: string | null;
  esPlantilla: boolean;
  activo: boolean;
  totalPermisos: number;
  totalUsuarios: number;
};

export type PerfilDetalle = {
  id: string;
  nombre: string;
  descripcion: string | null;
  esPlantilla: boolean;
  activo: boolean;
  permisos: string[];
};

export type UsuarioResumen = {
  id: string;
  nombre: string;
  email: string;
  perfilId: string;
  perfilNombre: string;
  activo: boolean;
  bloqueadoHasta: string | null;
  ultimoAccesoEn: string | null;
  creadoEn: string;
};

/** Resultado de las Server Actions de formularios. */
export type EstadoFormulario = { error?: string; ok?: boolean };
