# 04 · Base de datos

## Modelo

- **PostgreSQL 16**, una sola base: `jrtropicalfish` ([ADR 0002](adr/0002-una-sola-base-de-datos.md)).
- **EF Core solo para el esquema** (migraciones). En tiempo de ejecución, **Dapper** llama funciones y
  procedimientos PL/pgSQL ([ADR 0003](adr/0003-dapper-y-plpgsql.md)).
- Contexto: `Infrastructure/Persistence/AppDbContext.cs`. Migraciones: `Infrastructure/Persistence/Migrations/`.

## Convenciones de esquema

| Tema | Regla |
|---|---|
| Nombres | `snake_case` (`UseSnakeCaseNamingConvention`): tabla `usuarios`, columna `password_hash`, índice `ix_usuarios_email` |
| Llave primaria | `id uuid`, valor UUID v7 generado en C# |
| Auditoría de fila | `creado_en timestamptz`, `actualizado_en timestamptz NULL` |
| Borrado lógico | `eliminado_en timestamptz NULL` en catálogos; índice único **parcial** `WHERE eliminado_en IS NULL` |
| Llaves foráneas | `OnDelete(Restrict)` siempre, salvo tablas puente (`Cascade`). EF pone `Cascade` por defecto: corregirlo |
| Dinero | `numeric(18,2)`; cantidades de peces `integer`; tasas/porcentajes `numeric(9,4)` |
| Enums | Guardados como texto (`HasConversion<string>()`), legibles en SQL y en el historial |
| JSON | `jsonb` (p. ej. `auditoria.cambios_json`) |

## Funciones y procedimientos

- `fn_<entidad>_<accion>` para lecturas: `SELECT * FROM fn_usuario_por_email(@Email)`.
- `sp_<entidad>_<accion>` para escrituras: `CALL sp_usuario_crear(@Id::uuid, @Nombre::text, ...)`.
- **En un `CALL` se castea todo parámetro** (`@X::uuid`, `@Y::jsonb`). Sin el cast, un nulo llega como
  `unknown` y Postgres responde que *el procedimiento no existe*.
- En `RETURNS TABLE`, las columnas `varchar` se devuelven con `::text`.
- Dapper construye un `record` **por posición**: el orden de las columnas del `SELECT` debe coincidir con el
  del constructor. Agregar un campo al record exige revisar la función.
- Cada objeto creado en `Up` tiene su `DROP` en `Down`.
- **Al renombrar o eliminar una columna, recrear en la misma migración toda rutina que la use.** Postgres no
  valida las columnas al crear la función, solo al ejecutarla. Para encontrarlas:
  `SELECT proname FROM pg_proc WHERE pg_get_functiondef(oid) ~ '\mcolumna\M';`
  `CREATE OR REPLACE` no cambia `RETURNS TABLE`: se hace `DROP` + `CREATE`.

## Sembrado

- `Persistence/Sembrado/SembradoPermisos.Aplicar(migrationBuilder)` siembra permisos y perfiles plantilla
  desde `Domain/Seguridad/Permisos.cs`. Es idempotente: cada migración que agregue permisos lo vuelve a llamar.
- Los catálogos se siembran con `ON CONFLICT DO NOTHING` / `WHERE NOT EXISTS`: nunca sobrescribir datos que el
  negocio ya editó.

## Migraciones: flujo

```bash
# 1. Cambiar entidades y AppDbContext
# 2. Generar (desde la raíz)
npm run migracion:nueva -- AgregarInventario
# 3. REVISAR el archivo generado (ver abajo)
# 4. Aplicar: en desarrollo, al arrancar la API; en producción, `npm run prod:migrar`
```

**Revisar siempre la migración autogenerada.** Errores reales vistos en ProyectoTransporte: borrar columnas
antes de migrar sus datos, `activo = false` en filas existentes, `0001-01-01` como `creado_en`, agregar una
columna vacía antes de un índice único (revienta con más de una fila).

## Tablas actuales

| Tabla | Propósito |
|---|---|
| `usuarios` | Cuentas de acceso (email único, perfil, activo) |
| `perfiles` | Agrupación de permisos (`es_plantilla` para los sembrados) |
| `permisos` | Catálogo `modulo:accion` |
| `perfil_permisos` | Puente perfil ↔ permiso |
| `auditoria` | Historial de cambios por entidad (`cambios_json` jsonb) |
| `__EFMigrationsHistory` | Control de EF Core |

Las tablas de negocio previstas (especies, lotes, tanques, movimientos, importaciones, exportaciones…) están
descritas en [docs/dominio](../dominio/modulos.md).

## Acceso local

```
Host: localhost   Puerto: 5433   Base: jrtropicalfish
Usuario: jrtropicalfish   Contraseña: jrtropicalfish   (solo desarrollo)
```

`docker exec -it jrtropicalfish-postgres psql -U jrtropicalfish -d jrtropicalfish`
