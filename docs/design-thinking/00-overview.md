# Design Thinking — JR Tropical Fish

## Objetivo del proyecto

Construir un sistema para **JR Tropical Fish**, empresa que comercializa peces ornamentales y tropicales
vivos, que soporte sus tres procesos principales:

- **Inventario** — peces vivos en tanques, por especie, talla, lote y ubicación, con mortalidad, cuarentena
  y conteos.
- **Importaciones** — compras a proveedores del exterior, recepción con conteo y DOA, cuarentena, costos de
  nacionalización y reclamos.
- **Exportaciones** — venta a clientes del exterior por vía aérea: disponibilidad, proforma, reserva,
  empaque, documentos, despacho y reclamos.

Actores identificados (hipótesis iniciales):

- **Gerente / dueño** — decide, aprueba y necesita visibilidad del negocio.
- **Encargado de bodega / acuarios** — cuida los peces y ejecuta los movimientos físicos.
- **Coordinador de importaciones** — compra al exterior y recibe.
- **Coordinador de exportaciones / comercial** — vende al exterior y despacha.
- **Cliente del exterior (importador mayorista)** — actor externo que compra y reclama.

## Metodología

Se sigue el modelo clásico de **Design Thinking** (Stanford d.school / IDEO) para entender al negocio antes
de construir los módulos. Las fases son secuenciales pero iterativas: es normal volver a una fase anterior
si las entrevistas o pruebas revelan algo nuevo.

> **Nota sobre el alcance de esta iteración**: los documentos de cada fase parten de **supuestos
> iniciales** del equipo sobre cómo opera un negocio de este tipo, no de entrevistas. Su propósito es
> llegar a las entrevistas con hipótesis concretas para confirmar o descartar. Todo lo que aparece en los
> mapas de empatía, personas y problem statements debe leerse como **hipótesis por validar**.

| Fase | Carpeta | Objetivo | Estado |
|---|---|---|---|
| 1. Empatizar | [`01-empatizar/`](01-empatizar/README.md) | Entender a los actores: cómo operan hoy, qué herramientas usan, qué les duele. | Por iniciar — supuestos iniciales |
| 2. Definir | [`02-definir/`](02-definir/README.md) | Sintetizar en personas y problemas concretos a resolver. | Por iniciar — supuestos iniciales |
| 3. Idear | [`03-idear/`](03-idear/README.md) | Generar y priorizar soluciones; definir el alcance del MVP. | Por iniciar — supuestos iniciales |
| 4. Prototipar | [`04-prototipar/`](04-prototipar/README.md) | Representar los flujos críticos (recepción de importación, proforma y despacho). | Por iniciar — supuestos iniciales |
| 5. Testear | [`05-testear/`](05-testear/README.md) | Validar flujos y supuestos con usuarios reales del negocio. | Por iniciar — supuestos iniciales |

## Estado actual

**Por iniciar / supuestos iniciales.** Todas las fases tienen un primer borrador basado en supuestos:

- Fase 1: guía de entrevistas y 5 mapas de empatía hipotéticos, uno por actor.
- Fase 2: 3 personas y 6 problem statements (PS-1 a PS-6).
- Fase 3: ideas priorizadas por problem statement y alcance del MVP.
- Fase 4: 2 flujos conceptuales (recepción de importación; proforma y despacho de exportación).
- Fase 5: plan de pruebas y supuestos clave a validar.

**Siguiente paso**: agendar las entrevistas de la fase 1 con el dueño y el equipo (ver
[`01-empatizar/entrevistas/README.md`](01-empatizar/entrevistas/README.md)) y actualizar los documentos con
lo aprendido.

## Relación con el dominio y el sistema

- El diseño funcional de los módulos está en [`../dominio/`](../dominio/modulos.md): empezar por
  [`../dominio/modulos.md`](../dominio/modulos.md) (mapa de módulos, permisos y fases del MVP) y el
  [`../dominio/glosario.md`](../dominio/glosario.md).
- Cada documento de dominio tiene una sección de **Preguntas abiertas**; las entrevistas de la fase 1 son
  el lugar natural para resolverlas.
- El sistema vive en `apps/` (`apps/api`: ASP.NET Core .NET 10; `apps/web`: Next.js; base de datos
  PostgreSQL). Hoy solo existe el esqueleto: login, usuarios, perfiles, permisos, auditoría, archivos en
  S3, generación de PDF y correo.
