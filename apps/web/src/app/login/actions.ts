"use server";

import { redirect } from "next/navigation";
import type { LoginActionState } from "@/components/LoginForm";
import { ApiError, apiFetch } from "@/lib/api";
import { setSession } from "@/lib/session";

type LoginResponse = { token: string; expiraEn: string };

export async function loginAction(_prev: LoginActionState, formData: FormData): Promise<LoginActionState> {
  const email = String(formData.get("email") ?? "").trim();
  const password = String(formData.get("password") ?? "");

  if (!email || !password) {
    return { error: "Ingresa tu correo y contraseña." };
  }

  let resultado: LoginResponse;
  try {
    resultado = await apiFetch<LoginResponse>("/api/v1/auth/login", {
      method: "POST",
      body: { email, password },
    });
  } catch (err) {
    if (err instanceof ApiError && (err.status === 401 || err.status === 400)) {
      return { error: "Correo o contraseña incorrectos." };
    }
    return { error: "No se pudo iniciar sesión. Intenta de nuevo." };
  }

  await setSession(resultado.token, resultado.expiraEn);
  redirect("/");
}
