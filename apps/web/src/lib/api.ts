import "server-only";

export const API_URL = process.env.API_URL ?? "http://localhost:5377";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type ApiFetchOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  token?: string;
};

/** Lee el `{ error }` que devuelve la API en cualquier respuesta no exitosa. */
async function errorDeRespuesta(response: Response): Promise<ApiError> {
  let message = `Error ${response.status}`;
  try {
    const data = (await response.json()) as { error?: string };
    if (data.error) message = data.error;
  } catch {
    // el cuerpo no era JSON: se deja el mensaje genérico
  }
  return new ApiError(response.status, message);
}

/**
 * Llama a la API .NET desde el servidor (Server Actions, Server Components, route handlers).
 * Nunca se usa desde el navegador: por eso `API_URL` no lleva `NEXT_PUBLIC_` y el JWT no sale de
 * la cookie httpOnly.
 */
export async function apiFetch<T>(path: string, options: ApiFetchOptions = {}): Promise<T> {
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (options.token) headers.Authorization = `Bearer ${options.token}`;

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
    cache: "no-store",
  });

  if (!response.ok) throw await errorDeRespuesta(response);

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

/**
 * Sube un archivo a la API. Se separa de `apiFetch` porque aquí NO se fija `Content-Type`: lo pone
 * `FormData` con su propio boundary, y ponerlo a mano rompe el multipart.
 */
export async function apiUpload<T>(path: string, formData: FormData, token: string): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: formData,
    cache: "no-store",
  });

  if (!response.ok) throw await errorDeRespuesta(response);

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}
