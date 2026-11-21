/**
 * Foto del empleado o, si no tiene, sus iniciales. La imagen sale del route handler que hace
 * streaming desde la API; `?v=` cambia con cada foto nueva y permite cachearla.
 */
export function FotoEmpleado({
  empleado,
  tamano = "sm",
}: {
  empleado: { id: string; nombres: string; apellidos: string; fotoVersion: string | null };
  tamano?: "sm" | "lg";
}) {
  const clases = tamano === "lg" ? "size-28 text-2xl" : "size-10 text-sm";
  const nombre = `${empleado.nombres} ${empleado.apellidos}`;

  if (empleado.fotoVersion) {
    return (
      // eslint-disable-next-line @next/next/no-img-element -- imagen privada servida por un route handler propio
      <img
        src={`/archivos/empleados/${empleado.id}/foto?v=${empleado.fotoVersion}`}
        alt={`Foto de ${nombre}`}
        className={`${clases} shrink-0 rounded-full border border-borde bg-superficie-alterna object-cover`}
        loading="lazy"
      />
    );
  }

  const iniciales = `${empleado.nombres.charAt(0)}${empleado.apellidos.charAt(0)}`.toUpperCase();
  return (
    <span
      aria-hidden
      className={`${clases} inline-flex shrink-0 items-center justify-center rounded-full bg-acento-suave font-semibold text-texto`}
    >
      {iniciales}
    </span>
  );
}
