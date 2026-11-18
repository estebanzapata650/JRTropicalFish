import { redirect } from "next/navigation";
import { AppShell } from "@/components/AppShell";
import { apiFetch } from "@/lib/api";
import { construirNavegacion, filtrarNavegacion } from "@/lib/navegacion";
import type { UsuarioActual } from "@/lib/permisos";
import { getSession } from "@/lib/session";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();
  if (!session) redirect("/login");

  let usuario: UsuarioActual;
  try {
    usuario = await apiFetch<UsuarioActual>("/api/v1/auth/yo", { token: session.token });
  } catch {
    // Sesión inválida o expirada: se vuelve al login en vez de romper el shell.
    redirect("/login");
  }

  const navegacion = filtrarNavegacion(construirNavegacion(), usuario.permisos);

  return (
    <AppShell navegacion={navegacion} usuario={{ nombre: usuario.nombre, perfil: usuario.perfil }}>
      {children}
    </AppShell>
  );
}
