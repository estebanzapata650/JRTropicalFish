# JR Tropical Fish

Sistema de gestión para una empresa comercializadora de peces tropicales: **inventario**, **importaciones**
y **exportaciones**.

> Estado: esqueleto de infraestructura listo (login, usuarios/perfiles/permisos, auditoría, archivos S3,
> PDF, correo). Los módulos de negocio están documentados en [docs/dominio](docs/dominio/modulos.md) y se
> construyen por fases.

## Stack

| Capa | Tecnología |
|---|---|
| Web | Next.js 16 · React 19 · Tailwind CSS v4 · TypeScript |
| API | ASP.NET Core .NET 10 · Clean Architecture |
| Datos | PostgreSQL 16 · Dapper + PL/pgSQL · EF Core (migraciones) |
| Archivos | API S3 (Garage en desarrollo, R2/S3 en producción) |
| PDF / Correo | QuestPDF · SendGrid |
| Producción | VPS · Docker Compose · Caddy (HTTPS automático) |

## Requisitos

- .NET SDK 10
- Node.js 24 y npm
- Docker Desktop (o Docker Engine + Compose)

## Arranque rápido

```bash
cp .env.example .env
npm install                    # instala concurrently (orquestador)
npm install --prefix apps/web
npm run dev                    # Postgres + Garage + API + web
```

- Web: http://localhost:3100. Usuario de desarrollo: `admin@jrtropicalfish.local` / `Admin123!`
- API: http://localhost:5377, documentación interactiva en http://localhost:5377/scalar/v1
- Health: http://localhost:5377/api/v1/health

`npm run stop` apaga todo.

## Comandos

| Comando | Qué hace |
|---|---|
| `npm run dev` | Levanta infraestructura, API y web |
| `npm run dev:infra` | Solo contenedores (Postgres, Garage) |
| `npm run stop` | Apaga API, web y contenedores |
| `npm run build` | Compila API y web |
| `npm test` | Pruebas de la API (xUnit) |
| `npm run lint` | ESLint del web |
| `npm run migracion:nueva -- <Nombre>` | Nueva migración de EF Core |
| `npm run prod:build` / `prod:migrar` / `prod:up` | Producción en el VPS (ver guía de despliegue) |

## Documentación

| Documento | Contenido |
|---|---|
| [CLAUDE.md](CLAUDE.md) | Guía principal para desarrollar en el repositorio (también para asistentes de IA) |
| [docs/arquitectura/01-infraestructura.md](docs/arquitectura/01-infraestructura.md) | Piezas, puertos, redes, desarrollo vs. producción |
| [docs/arquitectura/02-backend.md](docs/arquitectura/02-backend.md) | Capas, convenciones, auth, receta para un módulo nuevo |
| [docs/arquitectura/03-frontend.md](docs/arquitectura/03-frontend.md) | Patrón BFF, sesión, rutas, reglas de diseño |
| [docs/arquitectura/04-base-de-datos.md](docs/arquitectura/04-base-de-datos.md) | Esquema, PL/pgSQL, migraciones |
| [docs/arquitectura/05-despliegue-vps.md](docs/arquitectura/05-despliegue-vps.md) | Producción, respaldos, rollback, seguridad |
| [docs/arquitectura/adr](docs/arquitectura/adr/README.md) | Decisiones de arquitectura |
| [docs/dominio](docs/dominio/modulos.md) | Glosario y módulos: inventario, importaciones, exportaciones |
| [docs/design-thinking](docs/design-thinking/00-overview.md) | Descubrimiento con el negocio |

## Estructura

```
apps/
  api/        Solución .NET (Domain, Application, Infrastructure, Api, Tests)
  web/        Next.js (BFF + interfaz)
infra/
  docker/     Dockerfiles de API y web
  caddy/      Proxy inverso de producción
  garage/     S3 local de desarrollo
  backup/     Imagen y scripts de respaldo de PostgreSQL
scripts/      Utilidades de desarrollo (stop, s3-init)
docs/         Arquitectura, dominio, design thinking
```
