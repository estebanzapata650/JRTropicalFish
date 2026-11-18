"use client";

import { useEffect } from "react";
import { AlertTriangle } from "lucide-react";

/**
 * Pantalla de error de la aplicación. El caso más frecuente en desarrollo es que la API .NET no
 * esté levantada: sin este aviso la app solo mostraba una pantalla rota sin explicar nada.
 */
export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  const pareceApiCaida =
    error.message.includes("fetch failed") ||
    error.message.includes("ECONNREFUSED") ||
    error.message.includes("Error 5");

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-4 px-4 text-center">
      <AlertTriangle className="size-10 text-peligro" aria-hidden />
      <h1 className="text-lg font-semibold text-texto">
        {pareceApiCaida ? "No se pudo conectar con la API" : "Algo salió mal"}
      </h1>

      {pareceApiCaida ? (
        <div className="flex max-w-md flex-col gap-2 text-sm text-texto-suave">
          <p>La aplicación web está funcionando, pero la API no responde.</p>
          <p>
            Desde la raíz del repositorio ejecuta{" "}
            <code className="rounded bg-acento-suave px-1.5 py-0.5">npm run dev</code>: levanta Postgres, Garage, la
            API y el web.
          </p>
        </div>
      ) : (
        <p className="max-w-md text-sm text-texto-suave">{error.message}</p>
      )}

      <button
        type="button"
        onClick={reset}
        className="inline-flex h-11 items-center justify-center rounded-md bg-acento px-4 text-base font-medium text-acento-texto transition-opacity hover:opacity-90"
      >
        Reintentar
      </button>
    </div>
  );
}
