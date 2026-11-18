# 0004 · Next.js como BFF; el navegador no habla con la API

- Estado: Aceptado
- Fecha: 2026-11-18

## Contexto

Hay que proteger el JWT y no exponer la API a Internet.

## Decisión

El navegador solo habla con Next.js. Server Components y Server Actions llaman a la API con `apiFetch`; los
componentes de cliente usan el proxy `app/api/[...path]`; los binarios van por route handlers con streaming.
El JWT vive en una cookie httpOnly (`jr_session`).

## Consecuencias

- Sin CORS; el token nunca es accesible a JavaScript (mitiga robo por XSS).
- En producción la API vive solo en la red interna de Docker.
- Un salto de red extra por petición (despreciable en la misma máquina).
- Cuidado: el proxy genérico no sirve para binarios.

## Alternativas consideradas

- **SPA llamando a la API directo con token en `localStorage`**: expone el token y obliga a publicar la API.
