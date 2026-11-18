# 03 · Frontend (Next.js)

## Patrón BFF (backend for frontend)

El navegador **nunca** llama a la API .NET. Todo pasa por el servidor de Next.js:

| Caso | Mecanismo | Archivo |
|---|---|---|
| Leer datos para una página | Server Component → `apiFetch()` | `src/lib/api.ts` |
| Guardar un formulario | Server Action → `apiFetch()` / `apiUpload()` | `app/**/actions.ts` |
| Componente de cliente que necesita datos | `fetch("/api/<ruta>")` → proxy catch-all | `app/api/[...path]/route.ts` |
| Descargar un PDF o archivo | Route handler dedicado que hace streaming | `app/**/<recurso>/route.ts` |

Consecuencias: no hay CORS, el JWT vive en una cookie **httpOnly** (`jr_session`) que JavaScript no puede
leer, y `API_URL` no lleva prefijo `NEXT_PUBLIC_`. `src/lib/api.ts` y `src/lib/session.ts` importan
`server-only` para que el build falle si alguien los usa desde un componente de cliente.

⚠️ El proxy catch-all hace `response.text()`: **no** sirve para binarios. Un PDF por ahí llega corrupto.

## Sesión

- Login: `app/login/actions.ts` llama a `POST /api/v1/auth/login` y guarda `{ token }` en `jr_session` con la
  expiración del token.
- `src/proxy.ts` (Next.js 16 renombró `middleware.ts` a `proxy.ts`) redirige a `/login` si falta la cookie.
- `app/(dashboard)/layout.tsx` valida de verdad la sesión pidiendo `GET /api/v1/auth/yo`; si falla, redirige
  al login. Con esa respuesta arma el menú filtrado por permisos.
- Logout: `<form method="post" action="/logout">` → route handler que borra la cookie y responde 303. Es una
  navegación completa a propósito (no una Server Action).

## Rutas

```
src/app/
├── layout.tsx              # raíz: fuentes, tema inicial
├── error.tsx               # error global (detecta "API caída")
├── login/                  # pública
├── logout/route.ts
├── api/[...path]/route.ts  # proxy BFF para componentes de cliente
└── (dashboard)/            # todo lo que exige sesión, con AppShell
    ├── page.tsx            # panel
    └── seguridad/{usuarios,perfiles}/
```

Al crecer: `(dashboard)/inventario`, `(dashboard)/importaciones/[id]/{documentos,recepcion,historial}`… Las
fichas de detalle usan **rutas anidadas como pestañas** (cada pestaña carga lo suyo y la URL se comparte).

## Menú y permisos

- `src/lib/permisos.ts` es el espejo de `Domain/Seguridad/Permisos.cs`. Mantener sincronizado.
- `src/lib/navegacion.ts` es declarativo: cada ítem lleva su `permiso`; un ítem con hijos desaparece si el
  usuario no ve ninguno.
- Ocultar un botón no es seguridad: la API valida con `[RequierePermiso]` siempre.

## Diseño

Reglas obligatorias (heredadas de ProyectoTransporte):

- **Ninguna pantalla se da por terminada sin verla a ~390 px** con un viewport móvil real.
- **`min-w-0`** en la columna de contenido (ya está en `AppShell`); sin él las tablas anchas rompen el layout.
- Contenido hasta `max-w-7xl`; formularios con `RejillaCampos` (`grid-cols-1 md:grid-cols-2 xl:grid-cols-3`),
  `CampoAncho` para campos largos, `Seccion` plegable solo en móvil.
- Espaciado: `gap-1.5` etiqueta→campo, `gap-4` entre campos, `gap-6` entre secciones.
- Tablas reales en escritorio (`TablaResponsive`), ocultando columnas secundarias con `hidden md:table-cell`.
- Acciones de fila como iconos (`BotonIcono`) con `aria-label` que incluya el nombre del registro.

### Tema claro / oscuro

- **Todo color sale de un token** de `globals.css` (`bg-fondo`, `bg-superficie`, `border-borde`,
  `text-texto`, `text-texto-suave`, `bg-acento`, `text-peligro`…). Nunca clases de paleta fija
  (`bg-gray-900`, `text-white`).
- Acento de marca: azul petróleo (`#0e7490` claro / `#22d3ee` oscuro).
- El tema se aplica con la clase `dark` en `<html>`; `scriptTemaInicial` corre antes del primer pintado y
  `BotonTema` no guarda el tema en estado de React (evita desajustes de hidratación).
- Constantes de clases compartidas en `lib/estilos.ts`, **no** en `components/campos.tsx` (que es
  `"use client"` y rompe la interpolación desde Server Components).

## Componentes base

| Componente | Uso |
|---|---|
| `AppShell` | Barra lateral colapsable (escritorio) + drawer (móvil), usuario, tema, cerrar sesión |
| `PageHeader` | Título, descripción y acciones de una página |
| `LoginForm` | Formulario de ingreso con `useActionState` |
| `campos.tsx` | `RejillaCampos`, `CampoAncho`, `Campo`, `Casilla`, `Seccion`, `TablaResponsive`, `EncabezadoTabla`, `Etiqueta`, `EtiquetaEstado`, `BotonIcono` |
| `TemaProvider` | `scriptTemaInicial`, `BotonTema` |

Para módulos con documentos e historial, traer de ProyectoTransporte y generalizar (sin copiar lógica de
transporte): `Dialogo`, `Paginacion`, `HistorialCambios`, `GestionDocumental`, `CampoBuscable`, `CampoMoneda`.

## Comandos

```bash
npm run dev --prefix apps/web     # o `npm run dev` en la raíz para todo el stack
npm run lint --prefix apps/web
npm run build --prefix apps/web
```
