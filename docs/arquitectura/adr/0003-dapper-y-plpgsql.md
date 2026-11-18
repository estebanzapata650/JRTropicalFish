# 0003 · Dapper + PL/pgSQL en ejecución, EF Core solo para migraciones

- Estado: Aceptado
- Fecha: 2026-11-18

## Contexto

Se necesita control fino del SQL (reportes de inventario, kardex, reservas de stock con bloqueos) y a la vez
un esquema versionado. ProyectoTransporte usa este enfoque con éxito.

## Decisión

- **Esquema**: EF Core 10 + Npgsql genera y aplica migraciones (`AppDbContext`, snake_case).
- **Ejecución**: Dapper llama funciones `fn_*` (lecturas) y procedimientos `sp_*` (escrituras) PL/pgSQL que
  se crean en las migraciones con `migrationBuilder.Sql`.

## Consecuencias

- El SQL de negocio queda versionado junto al esquema y se puede revisar y optimizar en Postgres.
- Operaciones de inventario (descontar stock con `SELECT ... FOR UPDATE`, validar no negativos) viven cerca
  de los datos.
- Costo: cambiar una columna obliga a recrear las rutinas que la usan; Dapper mapea records por posición.
  Ambas trampas están documentadas en [04-base-de-datos.md](../04-base-de-datos.md).

## Alternativas consideradas

- **EF Core también en ejecución**: más productivo para CRUD, menos control en consultas de inventario y
  doble modelo mental con el SQL de reportes.
- **SQL en strings de C#**: sin versionado propio y difícil de revisar.
