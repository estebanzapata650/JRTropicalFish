# Registro de decisiones de arquitectura (ADR)

Cada decisión que cambie la forma del sistema se registra aquí, en un archivo numerado que no se reescribe:
si una decisión cambia, se crea un ADR nuevo que **reemplaza** al anterior y se marca el viejo como
*Reemplazado por NNNN*.

| # | Decisión | Estado |
|---|---|---|
| [0001](0001-monolito-modular-clean-architecture.md) | Monolito modular con Clean Architecture | Aceptado |
| [0002](0002-una-sola-base-de-datos.md) | Una sola empresa, una sola base de datos | Aceptado |
| [0003](0003-dapper-y-plpgsql.md) | Dapper + PL/pgSQL en ejecución, EF Core solo para migraciones | Aceptado |
| [0004](0004-bff-nextjs.md) | Next.js como BFF; el navegador no habla con la API | Aceptado |
| [0005](0005-vps-docker-compose.md) | Producción en VPS con Docker Compose y Caddy | Aceptado |

## Plantilla

```markdown
# NNNN · Título

- Estado: Propuesto | Aceptado | Reemplazado por NNNN
- Fecha: AAAA-MM-DD

## Contexto
Qué problema o fuerza obliga a decidir.

## Decisión
Qué se decidió, en una o dos frases.

## Consecuencias
Lo que se gana, lo que se pierde y lo que queda pendiente.

## Alternativas consideradas
Opciones descartadas y por qué.
```
