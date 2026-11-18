# 0001 · Monolito modular con Clean Architecture

- Estado: Aceptado
- Fecha: 2026-11-18

## Contexto

JR Tropical Fish necesita inventario, importaciones y exportaciones, módulos muy acoplados entre sí (una
importación crea lotes de inventario; una exportación los consume). El equipo es pequeño y el volumen de
usuarios bajo. ProyectoTransporte ya validó una estructura en cuatro capas con .NET + Next.js.

## Decisión

Una sola API ASP.NET Core (.NET 10) organizada en `Domain`, `Application`, `Infrastructure` y `Api`, con un
folder por módulo en cada capa. Un solo despliegue.

## Consecuencias

- Transacciones locales entre módulos (recibir una importación y crear el lote es una sola transacción).
- Un despliegue, un log, una base: operación simple en un VPS.
- La disciplina modular depende de convenciones (un folder por módulo, sin referencias cruzadas entre
  servicios salvo por interfaces de `Application`), no de fronteras de red.

## Alternativas consideradas

- **Microservicios**: complejidad operativa (red, consistencia eventual, varios despliegues) sin beneficio
  para este tamaño.
- **Capas planas sin separación**: más rápido al inicio, pero mezcla SQL y reglas de negocio, y dificulta
  probar las reglas.
