# Plan de pruebas

Plan para validar los flujos de [`../04-prototipar/prototipos/`](../04-prototipar/prototipos/README.md) y
los supuestos de las fases anteriores con las personas reales de JR Tropical Fish. Estado: **por iniciar**.

## Cobertura — ¿los flujos atienden cada problem statement?

Revisión interna inicial (sin usuarios):

| Problem statement | ¿Atendido? | Dónde | Observación |
|---|---|---|---|
| PS-1 — Inventario confiable y valorizado | Parcial | Flujo 1 (lotes creados desde la recepción) y Flujo 2 (reserva y salida) | Falta prototipar la consulta de stock, la carga inicial y el reporte de existencias. |
| PS-2 — Costo real por lote y margen | No | — | Fuera del MVP (prioridad Media). Prototipar el costeo y la liquidación antes de la fase 5 de construcción. |
| PS-3 — Registro rápido en bodega | Parcial | Flujo 1 (conteo en celular) y Flujo 2 (empaque en celular) | Falta prototipar el registro de mortalidad y traslados, el uso diario más frecuente. |
| PS-4 — Recepción con evidencia para reclamar | Sí | [Flujo 1](../04-prototipar/prototipos/flujo-01-recepcion-importacion.md) | El envío del reclamo está simplificado; validar qué evidencia exigen los proveedores. |
| PS-5 — Vender solo lo disponible | Sí | [Flujo 2](../04-prototipar/prototipos/flujo-02-proforma-y-despacho-exportacion.md) | Validar si la reserva debe ocurrir en la proforma o en la confirmación. |
| PS-6 — Documentos de despacho sin retrabajo | Sí | Flujo 2 | Validar la lista de documentos por país destino y la relación con la factura electrónica DIAN. |

## Supuestos clave a validar (riesgos)

| # | Supuesto | Por qué es riesgoso | Cómo validarlo |
|---|---|---|---|
| 1 | La bodega tiene señal suficiente y el equipo usará el celular para registrar. | Si no, el inventario vuelve a depender del papel (PS-3). | Observación en bodega; prueba de la pantalla de mortalidad en sitio. |
| 2 | Contar vivos y DOA línea por línea es viable el día de la llegada. | Si no se puede, la evidencia para reclamar no existe (PS-4). | Observar una recepción real y medir tiempos. |
| 3 | Las reservas al confirmar evitan la sobreventa sin estorbar al comercial. | Si estorban, se volverá a trabajar por fuera del sistema (PS-5). | Simulación con pedidos reales de un mes anterior. |
| 4 | Los requisitos documentales (certificado sanitario, CITES, declaraciones) se pueden parametrizar por país y especie. | Un requisito mal modelado puede bloquear un despacho real o dejar pasar uno incompleto (PS-6). | Revisión con el comercio exterior y la agencia de aduanas **(por validar ICA, AUNAP, DIAN, CITES)**. |
| 5 | Lote = especie + talla + origen es una unidad de control práctica en la bodega. | Si en los tanques se mezclan lotes, la trazabilidad por lote no se sostiene. | Entrevista y observación con el encargado de bodega. |
| 6 | La ventana de reclamo de proveedores y clientes es de 24 a 48 horas y aceptan fotos como evidencia. | Define alertas, formatos de archivo (¿video?) y el diseño del Flujo 1. | Revisar acuerdos y correos reales con proveedores y clientes. |
| 7 | Una sola persona maneja importaciones y exportaciones. | Afecta perfiles, permisos y la pantalla de inicio. | Entrevista con el dueño. |
| 8 | Los clientes aceptan proforma, packing list y factura en el formato PDF del sistema. | Un formato no aceptado obliga a rehacer documentos (PS-6). | Mostrar ejemplos al comercial y, si es posible, a un cliente. |

## Sesiones de prueba propuestas

| Sesión | Persona | Prototipo | Tareas | Criterio de éxito |
|---|---|---|---|---|
| S1 | Wilmer (encargado de acuarios) | Flujo 1, pantallas P1 a P3 | Recibir una importación de 3 líneas con DOA y cerrar la recepción. | Completa sin ayuda; tiempo por línea menor a 2 minutos; entiende la regla de cuadre. |
| S2 | Wilmer | Pantalla de mortalidad y traslado (por prototipar) | Registrar 3 mortalidades con causa y un traslado. | Menos de 30 segundos por registro desde que abre el tanque. |
| S3 | Laura (comercio exterior) | Flujo 1, pantalla P4 | Revisar el expediente, identificar documentos faltantes y crear el reclamo. | Identifica los faltantes y la fecha límite sin ayuda. |
| S4 | Laura | Flujo 2, pantallas P1 y P3 | Registrar un pedido real reciente, generar la proforma, confirmar y despachar. | Detecta la línea sin cobertura; no puede despachar con documentos faltantes; documentos coherentes. |
| S5 | Wilmer | Flujo 2, pantalla P2 | Empacar 4 cajas con 2 especies, una con faltante. | Registra el faltante; respeta densidad; menos de 1 minuto por caja. |
| S6 | Andrés (dueño) | Reportes de existencias y aprobaciones (por prototipar) | Consultar stock de una especie, aprobar un ajuste, revisar un acta. | Encuentra la información sin preguntar; confía en la cifra. |

Recomendación: hacer S1, S2 y S4 primero, porque validan los supuestos 1, 2 y 3, que son los que más
determinan si el sistema se usa en la práctica.

---

## Plantilla para registrar cada sesión

*Un archivo por sesión en esta carpeta, con nombre `AAAA-MM-DD_sesion-SX.md`.*

```markdown
# Sesión de prueba — [SX] — [fecha]

- **Prototipo probado**: (referencia a 04-prototipar/prototipos/)
- **Usuario(s)**: (rol, referencia a la persona en 02-definir/personas.md)
- **Tarea que se le pidió realizar**:
- **Tiempo empleado**:
- **Qué funcionó**:
- **Qué no funcionó / confundió**:
- **Supuestos confirmados / descartados**: (número de la tabla de supuestos)
- **Cambios propuestos a reglas de dominio**: (documento y número de regla)
- **Aprendizajes y siguientes pasos**:
```
