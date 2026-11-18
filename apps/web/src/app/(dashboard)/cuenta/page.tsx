import type { Metadata } from "next";
import { PageHeader } from "@/components/PageHeader";
import { etiquetaModulo } from "@/lib/permisos";
import { obtenerUsuarioActual } from "@/lib/usuario-actual";
import { CambiarPasswordForm } from "./CambiarPasswordForm";

export const metadata: Metadata = { title: "Mi cuenta" };

/** Datos del propio usuario y cambio de contraseña. No requiere ningún permiso. */
export default async function CuentaPage() {
  const usuario = await obtenerUsuarioActual();

  const porModulo = new Map<string, number>();
  for (const permiso of usuario.permisos) {
    const modulo = permiso.split(":")[0];
    porModulo.set(modulo, (porModulo.get(modulo) ?? 0) + 1);
  }

  return (
    <div className="mx-auto flex w-full max-w-7xl flex-col gap-6">
      <PageHeader titulo="Mi cuenta" descripcion="Tus datos de acceso y tu contraseña" />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <section className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
          <h2 className="text-base font-semibold text-texto">Datos</h2>
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-[8rem_1fr]">
            <dt className="text-texto-suave">Nombre</dt>
            <dd className="text-texto">{usuario.nombre}</dd>
            <dt className="text-texto-suave">Correo</dt>
            <dd className="break-all text-texto">{usuario.email}</dd>
            <dt className="text-texto-suave">Perfil</dt>
            <dd className="text-texto">{usuario.perfil}</dd>
            <dt className="text-texto-suave">Permisos</dt>
            <dd className="flex flex-wrap gap-1.5">
              {[...porModulo.entries()].map(([modulo, cantidad]) => (
                <span key={modulo} className="rounded-full bg-neutro-fondo px-2.5 py-0.5 text-xs text-neutro-texto">
                  {etiquetaModulo(modulo)} · {cantidad}
                </span>
              ))}
              {porModulo.size === 0 ? <span className="text-texto-suave">Sin permisos</span> : null}
            </dd>
          </dl>
          <p className="text-xs text-texto-suave">
            Tu nombre, correo y perfil los administra un usuario con permiso sobre Usuarios.
          </p>
        </section>

        <section className="flex flex-col gap-4 rounded-md border border-borde bg-superficie p-4">
          <h2 className="text-base font-semibold text-texto">Cambiar contraseña</h2>
          <CambiarPasswordForm />
        </section>
      </div>
    </div>
  );
}
