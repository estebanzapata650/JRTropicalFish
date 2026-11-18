# 0002 · Una sola empresa, una sola base de datos

- Estado: Aceptado
- Fecha: 2026-11-18

## Contexto

ProyectoTransporte es multi-tenant: una base PostgreSQL por empresa, slug en el JWT, middleware de
resolución de tenant, aprovisionamiento con `CREATE DATABASE` y migraciones aplicadas a cada base.
JR Tropical Fish es **una sola empresa**.

## Decisión

Una sola base de datos (`jrtropicalfish`). Se elimina todo el kit de tenancy: `TenantContext`,
`TenantResolutionMiddleware`, `TenantDatabaseProvisioner`, `TenantMigrationRunner`, el claim
`empresa_slug`, el header `x-tenant-slug` y las rutas web `/[slug]/**`.

Si la empresa maneja varias bodegas o granjas, se modela como **ubicaciones** dentro de la misma base.

## Consecuencias

- Mucho menos código e infraestructura; migraciones con `dotnet ef` estándar y un solo `DbContext`.
- URLs limpias (`/inventario` en vez de `/<slug>/inventario`).
- Si algún día se quisiera vender como SaaS a otras empresas, habría que reintroducir tenancy
  (discriminador por columna o base por empresa). Se acepta ese costo futuro improbable.

## Alternativas consideradas

- **Copiar el multi-tenant completo**: complejidad desde el día uno sin un segundo cliente a la vista.
