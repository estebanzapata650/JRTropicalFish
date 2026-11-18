import { AppShell } from "@/components/AppShell";
import { construirNavegacion, filtrarNavegacion } from "@/lib/navegacion";
import { obtenerUsuarioActual } from "@/lib/usuario-actual";

export default async function DashboardLayout({ children }: { children: React.ReactNode }) {
  // Valida la sesión contra la API (expirada o revocada → login) y arma el menú por permisos.
  const usuario = await obtenerUsuarioActual();
  const navegacion = filtrarNavegacion(construirNavegacion(), usuario.permisos);

  return (
    <AppShell navegacion={navegacion} usuario={{ nombre: usuario.nombre, perfil: usuario.perfil }}>
      {children}
    </AppShell>
  );
}
