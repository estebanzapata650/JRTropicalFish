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

// --- Maestros ---

export type MaestroInfo = { clave: string; nombre: string; nombreSingular: string; descripcion: string };

export type ValorMaestro = { id: string; codigo: string; nombre: string; activo: boolean; creadoEn: string };

/** Opción de un `<select>` alimentado por un maestro. */
export type OpcionMaestro = { id: string; codigo: string; nombre: string };

// --- Empleados ---

export type Empleado = {
  id: string;
  tipoIdentificacionId: string;
  tipoIdentificacionCodigo: string;
  numeroIdentificacion: string;
  nombres: string;
  apellidos: string;
  genero: string | null;
  /** `yyyy-MM-dd` (DateOnly). */
  fechaNacimiento: string | null;
  telefono: string | null;
  email: string | null;
  direccion: string | null;
  ciudad: string | null;
  cargoId: string;
  cargoNombre: string;
  areaId: string | null;
  areaNombre: string | null;
  tipoContratoId: string;
  tipoContratoNombre: string;
  fechaIngreso: string;
  fechaRetiro: string | null;
  /** `null` si el usuario no tiene `empleados:ver-salario`. */
  salario: number | null;
  activo: boolean;
  /** Cambia con cada foto nueva; `null` si no tiene foto. */
  fotoVersion: string | null;
  creadoEn: string;
};
