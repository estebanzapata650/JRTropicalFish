# 0005 · Producción en VPS con Docker Compose y Caddy

- Estado: Aceptado
- Fecha: 2026-11-18

## Contexto

ProyectoTransporte no definió producción. JR Tropical Fish necesita un despliegue económico, entendible y
reproducible para un equipo pequeño.

## Decisión

Un VPS (Ubuntu 24.04) con Docker Compose (`docker-compose.prod.yml`): `caddy` (TLS automático),
`web`, `api`, `postgres`, `backup`. Archivos en un S3 gestionado (Cloudflare R2 recomendado). Secretos en
`.env.prod` en el servidor. Migraciones como paso explícito (`--migrar`). CI/CD con GitHub Actions en fase 2.

## Consecuencias

- Costo bajo y predecible; el mismo `docker compose` sirve para un servidor de staging.
- La disponibilidad depende de un solo servidor: se mitiga con respaldos diarios fuera del VPS y un
  procedimiento de restauración probado.
- Escalar implica mover Postgres a un servicio gestionado o pasar a un orquestador; los Dockerfiles no cambian.

## Alternativas consideradas

- **Azure / AWS gestionados (App Service, RDS…)**: más resiliencia, más costo y más superficie de
  configuración para el tamaño actual.
- **Kubernetes**: desproporcionado.
