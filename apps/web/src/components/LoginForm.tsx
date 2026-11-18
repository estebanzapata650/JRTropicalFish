"use client";

import { useActionState } from "react";

export type LoginActionState = { error?: string };

type LoginFormProps = {
  action: (state: LoginActionState, formData: FormData) => Promise<LoginActionState>;
  titulo: string;
  subtitulo?: string;
};

const initialState: LoginActionState = {};

export function LoginForm({ action, titulo, subtitulo }: LoginFormProps) {
  const [state, formAction, pending] = useActionState(action, initialState);

  return (
    <div className="flex min-h-dvh flex-col justify-center px-4 py-8 sm:items-center">
      <div className="w-full sm:max-w-sm">
        <h1 className="text-xl font-semibold text-texto sm:text-2xl">{titulo}</h1>
        {subtitulo ? <p className="mt-1 text-sm text-texto-suave">{subtitulo}</p> : null}

        <form action={formAction} className="mt-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="email" className="text-sm font-medium text-texto">
              Correo electrónico
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              required
              className="h-11 rounded-md border border-borde bg-superficie px-3 text-base text-texto outline-none focus:border-acento focus:ring-1 focus:ring-acento"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label htmlFor="password" className="text-sm font-medium text-texto">
              Contraseña
            </label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              required
              className="h-11 rounded-md border border-borde bg-superficie px-3 text-base text-texto outline-none focus:border-acento focus:ring-1 focus:ring-acento"
            />
          </div>

          {state.error ? <p className="text-sm text-peligro">{state.error}</p> : null}

          <button
            type="submit"
            disabled={pending}
            className="mt-2 h-11 rounded-md bg-acento text-base font-medium text-acento-texto transition-colors hover:opacity-90 disabled:opacity-60"
          >
            {pending ? "Ingresando..." : "Ingresar"}
          </button>
        </form>
      </div>
    </div>
  );
}
