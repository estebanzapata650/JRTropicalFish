import "server-only";
import { cookies } from "next/headers";

/** Cookie httpOnly con el JWT de la API. El navegador la envía, pero JavaScript no la puede leer. */
export const SESSION_COOKIE = "jr_session";

type Session = { token: string };

export async function setSession(token: string, expiraEn: string) {
  const store = await cookies();
  store.set(SESSION_COOKIE, JSON.stringify({ token } satisfies Session), {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires: new Date(expiraEn),
  });
}

export async function getSession(): Promise<Session | null> {
  const store = await cookies();
  const raw = store.get(SESSION_COOKIE)?.value;
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as Partial<Session>;
    return typeof parsed.token === "string" ? { token: parsed.token } : null;
  } catch {
    return null;
  }
}
