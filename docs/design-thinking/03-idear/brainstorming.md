# Brainstorming

Ideas de solución por cada problem statement de
[`../02-definir/problem-statements.md`](../02-definir/problem-statements.md), con una priorización inicial
razonada (Deseable / Factible / Viable). El negocio puede ajustar cualquier prioridad.

Leyenda: **Sí** / **Parcial** / **No** en cada criterio. Prioridad: **Alta** (entra al MVP), **Media**
(después del MVP), **Baja** (exploratoria).

---

## PS-1 — Dueño: inventario confiable y valorizado

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Stock por especie, talla, lote y tanque calculado solo a partir de movimientos (sin editar saldos) | Sí | Sí | Sí | Alta |
| Carga inicial del inventario actual desde Excel, con un conteo físico de arranque | Sí | Sí | Sí | Alta |
| Reporte de existencias en PDF por bodega/tanque/especie | Sí | Sí | Sí | Alta |
| Inventario valorizado por lote (visible solo con permiso de costos) | Sí | Sí | Sí | Alta |
| Tablero de inicio con stock disponible, en cuarentena, reservado y mortalidad de la semana | Sí | Sí | Sí | Media |
| Alertas por correo: stock bajo, mortalidad alta, cuarentena vencida | Sí | Sí | Sí | Media |
| Sensores de calidad de agua conectados a cada tanque | Sí | Parcial (hardware) | Parcial | Baja |

---

## PS-2 — Dueño: costo real por lote y margen por pedido

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Registro de costos de nacionalización por importación, con soporte adjunto | Sí | Sí | Sí | Media |
| Prorrateo automático configurable (por valor FOB, piezas o cajas) y costo landed por lote | Sí | Sí | Sí | Media |
| Costo provisional al recibir y costo definitivo al liquidar | Sí | Sí | Sí | Media |
| Margen por pedido (venta − costo de los lotes despachados) | Sí | Sí | Sí | Media |
| Consulta automática de la TRM del día | Sí | Parcial (fuente externa por validar) | Sí | Baja |
| Integración con el software contable | Sí | Parcial (depende del software) | Parcial | Baja |

*Prioridad Media porque depende de tener primero inventario por lote e importaciones registradas
(fases 4 y 5 de [`modulos.md`](../../dominio/modulos.md)).*

---

## PS-3 — Encargado de acuarios: registro rápido en la bodega

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Pantallas web adaptadas a celular con pocos campos y botones grandes (mortalidad, traslado, conteo) | Sí | Sí | Sí | Alta |
| Código o QR por tanque que abre directamente su contenido y acciones | Sí | Sí | Sí | Alta |
| Causas de mortalidad como botones (sin escribir) y foto opcional desde la cámara | Sí | Sí | Sí | Alta |
| Conteo físico "a ciegas" por tanque, con ajustes que aprueba el jefe | Sí | Sí | Sí | Media |
| Lista del día: "pedidos por preparar" y "importaciones por recibir" | Sí | Sí | Sí | Media |
| Registro sin conexión que se sincroniza después | Sí | Parcial (complejidad alta) | Sí | Baja |
| Registro por voz | Parcial | Parcial | Parcial | Baja |

---

## PS-4 — Encargado y comercio exterior: recepción con evidencia para reclamar

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Pantalla de recepción por línea del packing list: vivos, DOA, faltantes, sobrantes | Sí | Sí | Sí | Alta |
| Fotos de DOA cargadas desde el celular durante la recepción | Sí | Sí | Sí | Alta |
| Acta de recepción en PDF generada al cerrar | Sí | Sí | Sí | Alta |
| Lotes creados automáticamente en cuarentena al cerrar la recepción | Sí | Sí | Sí | Alta |
| Fecha límite de reclamo visible desde la llegada del vuelo y alerta antes de vencer | Sí | Sí | Sí | Alta |
| Reclamo al proveedor pre-armado con el acta y las fotos, enviado por correo | Sí | Sí | Sí | Media |
| Expediente digital por importación con checklist de documentos | Sí | Sí | Sí | Alta |
| Soporte de video como evidencia | Sí | Parcial (hoy solo PDF/JPG/PNG/WEBP, 10 MB) | Sí | Baja |

---

## PS-5 — Comercio exterior: vender solo lo disponible

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Lista de disponibilidad en PDF generada del inventario disponible (sin cuarentena ni reservado) | Sí | Sí | Sí | Alta |
| Proforma en PDF desde el pedido, enviada por correo, con versiones | Sí | Sí | Sí | Alta |
| Reserva automática por lote al confirmar el pedido; alerta de reserva sin cobertura | Sí | Sí | Sí | Alta |
| Listas de precios por cliente y aprobación de precios especiales | Sí | Sí | Sí | Media |
| Vista de "comprometido vs disponible" por especie y talla | Sí | Sí | Sí | Media |
| Portal del cliente para ver disponibilidad y pedir | Parcial | Sí | Parcial | Baja |

---

## PS-6 — Comercio exterior: documentos de despacho sin retrabajo

| Idea | Deseable | Factible | Viable | Prioridad |
|---|---|---|---|---|
| Registro de empaque por caja (especie, talla, lote, cantidad, bolsas) desde el celular | Sí | Sí | Sí | Alta |
| Packing list y factura comercial en PDF generados del empaque real | Sí | Sí | Sí | Alta |
| Checklist documental por pedido (certificado, CITES si aplica, AWB) que bloquea el despacho | Sí | Sí | Sí | Alta |
| Salida de inventario automática al registrar el despacho | Sí | Sí | Sí | Alta |
| Envío por correo al cliente de todos los documentos del despacho | Sí | Sí | Sí | Alta |
| Sugerencia de bolsas por densidad de especie y talla | Sí | Sí | Sí | Media |
| Registro y aprobación de reclamos del cliente con trazabilidad al lote | Sí | Sí | Sí | Media |
| Integración con facturación electrónica DIAN | Sí | Parcial (proveedor tecnológico por validar) | Parcial | Baja |
| Etiquetas de caja en PDF | Sí | Sí | Sí | Baja |

---

## Síntesis — alcance del MVP

Las ideas de prioridad **Alta** definen el alcance mínimo, organizado según las fases de
[`../../dominio/modulos.md`](../../dominio/modulos.md):

1. **Maestros** (fase 1): especies, tallas, bodegas, tanques con código/QR, causas de mortalidad,
   proveedores, clientes.
2. **Inventario base** (fase 2) — responde a PS-1 y PS-3:
   - Lotes y existencias por tanque calculados desde movimientos; carga inicial.
   - Registro desde el celular de mortalidad (causa con botones, foto opcional) y traslados.
   - Ajustes con aprobación; liberación de cuarentena.
   - Reporte de existencias y valorizado en PDF.
3. **Exportaciones núcleo** (fase 3) — responde a PS-5 y PS-6:
   - Lista de disponibilidad y proforma en PDF, por correo.
   - Confirmación con reserva por lote.
   - Empaque por caja desde el celular; packing list y factura comercial generados del empaque.
   - Checklist documental que bloquea el despacho; salida de inventario al despachar; envío de documentos
     al cliente.
4. **Importaciones núcleo** (fase 4) — responde a PS-4:
   - Orden de compra, expediente de documentos con checklist, embarque con AWB.
   - Recepción por línea con fotos, acta en PDF, lotes en cuarentena, fecha límite de reclamo.

Quedan para después del MVP (prioridad **Media**): costeo landed y liquidación (PS-2), reclamos completos a
proveedor y de cliente, conteos físicos completos, alertas por correo, tablero de inicio, listas de precios
por cliente y margen por pedido. Las ideas de prioridad **Baja** (sensores, modo sin conexión, portal del
cliente, facturación electrónica, voz) se revisan cuando el núcleo esté en uso.
