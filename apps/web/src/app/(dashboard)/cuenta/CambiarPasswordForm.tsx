"use client";

import { useActionState } from "react";
import { Campo } from "@/components/campos";
import { botonPrimarioClass, inputClass } from "@/lib/estilos";
import type { EstadoFormulario } from "@/lib/types";
import { cambiarPasswordAction } from "./actions";

export function CambiarPasswordForm() {
  const [estado, accion, pendiente] = useActionState(cambiarPasswordAction, {} as EstadoFormulario);

  return (
    // La clave remonta el formulario tras un cambio exitoso para limpiar los campos.
    <form key={estado.ok ? "ok" : "form"} action={accion} className="flex flex-col gap-4">
      <Campo label="Contraseña actual" htmlFor="passwordActual">
        <input id="passwordActual" name="passwordActual" type="password" autoComplete="current-password" required className={inputClass} />
      </Campo>
      <Campo label="Nueva contraseña" htmlFor="passwordNueva" hint="Mínimo 8 caracteres, con letras y números.">
        <input id="passwordNueva" name="passwordNueva" type="password" autoComplete="new-password" required minLength={8} className={inputClass} />
      </Campo>
      <Campo label="Confirmar nueva contraseña" htmlFor="confirmacion">
        <input id="confirmacion" name="confirmacion" type="password" autoComplete="new-password" required className={inputClass} />
      </Campo>

      {estado.error ? <p className="text-sm text-peligro">{estado.error}</p> : null}
      {estado.ok ? (
        <p className="rounded-md bg-exito-fondo px-3 py-2 text-sm text-exito-texto">
          Contraseña actualizada. Las demás sesiones abiertas con tu usuario se cerraron.
        </p>
      ) : null}

      <div className="flex sm:justify-end">
        <button type="submit" disabled={pendiente} className={`${botonPrimarioClass} w-full sm:w-auto`}>
          {pendiente ? "Guardando…" : "Cambiar contraseña"}
        </button>
      </div>
    </form>
  );
}
