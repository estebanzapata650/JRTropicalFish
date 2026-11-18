"use client";

import { Moon, Sun } from "lucide-react";

export const CLAVE_TEMA = "jr_tema";

/**
 * Script que corre ANTES del primer pintado para aplicar el tema guardado.
 * Sin esto, la página aparece en claro y salta a oscuro (parpadeo).
 */
export const scriptTemaInicial = `
(function () {
  try {
    var t = localStorage.getItem('${CLAVE_TEMA}');
    if (!t) t = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'oscuro' : 'claro';
    if (t === 'oscuro') document.documentElement.classList.add('dark');
  } catch (e) {}
})();
`;

/**
 * Alterna entre tema claro y oscuro.
 *
 * No guarda el tema en estado de React a propósito: el servidor no sabe qué tema tiene el
 * usuario, así que cualquier render condicionado al tema provocaría un desajuste de hidratación.
 * En su lugar se pintan los dos iconos y los dos textos, y CSS decide cuál se ve.
 */
export function BotonTema({ mostrarEtiqueta = true }: { mostrarEtiqueta?: boolean }) {
  const alternar = () => {
    const oscuroAhora = document.documentElement.classList.toggle("dark");
    try {
      localStorage.setItem(CLAVE_TEMA, oscuroAhora ? "oscuro" : "claro");
    } catch {
      // Modo privado o almacenamiento bloqueado: el tema aplica igual, solo no se recuerda.
    }
  };

  return (
    <button
      type="button"
      onClick={alternar}
      aria-label="Cambiar entre tema claro y oscuro"
      title="Cambiar entre tema claro y oscuro"
      className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm text-texto-suave transition-colors hover:bg-acento-suave hover:text-texto"
    >
      <Moon className="size-5 shrink-0 dark:hidden" aria-hidden />
      <Sun className="hidden size-5 shrink-0 dark:block" aria-hidden />
      {mostrarEtiqueta ? (
        <>
          <span className="dark:hidden">Tema oscuro</span>
          <span className="hidden dark:inline">Tema claro</span>
        </>
      ) : null}
    </button>
  );
}
