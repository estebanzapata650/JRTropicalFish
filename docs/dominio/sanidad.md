# Sanidad: medicación, cuarentena y alimentación

> Estado: **fase 1 implementada** (2026-11-22). Fase 2: **fichas técnicas importadas del PDF** (2026-11-26);
> pendiente el planificador semanal.

## De dónde salen los datos

- **Ficha técnica por especie** (`docs/excelBD/Ficha Técnica por Especie.pdf`, formato FT.CA.01, unas 300
  fichas). Trae:
  - código interno (AP005…), nombres, procedencia, características y dimorfismo;
  - **pH, GH y temperatura**;
  - etología y empaque.

  **No trae** dieta, frecuencia de alimentación, sensibilidad química ni protocolos de cuarentena. Esos campos
  existen en el sistema y los llena el equipo.

  **Importación** (`npm run sanidad:importar-fichas -- "<pdf>" [--simular] [--sobrescribir]`):
  - `scripts/extraer-palabras-pdf` (PdfPig, Apache-2.0; necesita el SDK de .NET) vuelca cada palabra con su posición;
    `scripts/lib/fichas-pdf.mjs` arma las 303 fichas: 4 por fila, varias filas por página y algunas partidas entre
    páginas. Cada valor se asigna a la etiqueta más cercana a su centro, porque Excel centra el texto en la celda.
  - **Asignación a productos:** por código interno, y solo si el nombre de la ficha comparte alguna palabra con el
    nombre científico del producto. El PDF trae 21 códigos repetidos y varios equivocados; si hay dos fichas con el
    mismo código, gana la que más coincide.
  - **No sobrescribe** una ficha que ya exista, salvo con `--sobrescribir`.
  - **Revisión:** deja en `docs/temp/fichas-revision.csv` lo que no cargó.
  - **Resultado del 2026-11-26:** 320 de los 794 productos quedaron con ficha. Quedaron sin cargar, para revisar a
    mano, 14 fichas cuyo nombre no coincide con el producto de ese código (por ejemplo, CF110 *Microglanis* trae la
    ficha de *Pseudoplatystoma*) y 11 códigos que no existen en el catálogo (rayas PT…, LO128/129/418, CC078).
  - **GH:** muchas fichas lo traen vacío. «<5º» se guarda como máximo 5, sin mínimo.
- **Reportes AQUAVET** (15 PDF, ene–feb 2026): necropsia de un grupo de peces y el tratamiento sugerido. De ahí
  salen los **13 medicamentos sembrados**, cada uno con su dosis de referencia y su **fuente**.
  - Cada reporte dice que vale **solo para el grupo muestreado**. Por eso ningún protocolo se aplica solo: lo
    aprueba el veterinario o gerencia.
  - Recomendaciones que se repiten en todos los reportes:
    - 24 h de ayuno antes de tratar;
    - cambio de agua del 50 % antes y del 80 % al terminar;
    - sal marina 1 g/L;
    - no alimentar durante la medicación.

    Se sembraron como el borrador **PRT-001 «Profilaxis de llegada»**, sin días de cuarentena: el veterinario
    los fija al aprobarlo.
- **Regla del usuario (firme)**: los acuarios que salen en un vuelo no se alimentan desde **48 h antes** de la
  salida. El parámetro puede subir, nunca bajar de 48 (`CHECK` en base y validación en la API).
- **Tiempos de retiro**: no vienen en los reportes. Cada medicamento lo tiene en 0 hasta que el veterinario lo fije.

## Dosis (`Application/Sanidad/ReglasSanidad.cs`)

- **Por volumen**: cantidad × litros del acuario ÷ base. Ejemplos con 80 L:
  - Levamisol 0,6 mL/100 L → 0,48 mL;
  - Praziquantel 250 mg/100 L → 200 mg.

  La base es `acuarios.capacidad_litros`. Sin ese dato, la dosis queda sin calcular y aparece un aviso.
- **Por alimento** (Mebendazol 200 mg/100 g): no depende del acuario. Se indica en la nota y el semáforo de
  alimentación muestra «Usar alimento medicado».
- **Fija**: la cantidad de referencia por acuario.
- **Redondeo por unidad**:

  | Unidad | Redondeo |
  |---|---|
  | mL | 0,01 por debajo de 1 mL; 0,1 desde 1 mL |
  | g | 0,01 |
  | mg | 0,1 por debajo de 10 mg; entero desde 10 mg |
  | gotas | entero |
  | tabletas | media tableta |

- El operario puede registrar la cantidad realmente aplicada. A veces el veterinario ajusta la dosis: para
  80 L indicó 1,8 mL de Fenlak, y la proporción da 1,6.

## Estado del acuario (columna del tablero)

Se **deriva** de los datos; nunca se guarda. Por prioridad:

1. **Ayuno pre-vuelo**: el acuario tiene peces reservados para un pedido cuyo embarque sale en 48 h o menos
   (también si el vuelo ya pasó y no se ha despachado). Si al embarque le falta la hora de salida, se usa las
   00:00 (Bogotá) del día de despacho, que es lo más conservador.
2. **Cuarentena / medicación**: hay un tratamiento activo, un tratamiento terminado dentro de su tiempo de
   retiro, o peces `enfermo`.
3. **Aclimatación**: hay stock por ubicar (tarjeta por lote), o un lote llegado hace menos que las horas de
   ayuno de llegada de su ficha (24 por defecto).
4. **Estable**.

La columna **Por llegar** muestra las líneas de pedidos a proveedor enviados o en tránsito que aún no se reciben.

## Semáforo de alimentación

Un acuario va en **rojo** si se cumple alguno de estos motivos:

| Motivo | Cuándo |
|---|---|
| Vuelo | Está en ayuno pre-vuelo. |
| Tratamiento | Tiene un tratamiento activo con un medicamento que requiere ayuno o con un paso de ayuno. |
| Llegada | Está dentro del ayuno de llegada. |

En cualquier otro caso va en **verde**, con la ración de la dieta.

**Dieta**: la del producto, si tiene; si no, la de su grupo. Cantidad por toma = cantidad por cada 100 peces ×
peces ÷ 100. «Alimentado» registra la toma (quién y cuándo).

## Tratamientos

- Se inician desde un **protocolo aprobado** (Mi día lo sugiere al recibir o ubicar un lote; tablero) o como
  **prescripción manual** con sus pasos. La prescripción manual exige `sanidad:gestionar`.
- Un acuario tiene **un solo tratamiento activo**.
- Las aplicaciones se generan por día con la dosis calculada. Cada una se marca **Aplicado** (con la cantidad
  real) o **No se hizo** (con nota obligatoria).
- **Terminar** exige todo registrado. **Cancelar** solo si nada se aplicó todavía; si ya se aplicó algo, se
  termina, para que cuente el retiro.
- **Liberación** = fin + 1 día + el mayor tiempo de retiro de lo usado. Hasta esa fecha el acuario **no se
  reserva ni se despacha**: `sp_reserva_fijar` y `sp_pedido_cliente_despachar` responden `JR004` → 409.
- Se puede adjuntar el PDF del reporte del veterinario (`DetectorTipoArchivo`; S3).

## Protocolos

- Estados: `borrador → aprobado`; `borrador`/`aprobado` → `inactivo` → `borrador`.
- Aprobar exige `sanidad:aprobar-protocolo`, días de cuarentena y al menos un paso.
- **Editar uno aprobado lo devuelve a borrador.**
- Criterios opcionales: producto, grupo, centro de acopio (procedencia) y cuenca. Un criterio fijado que no
  coincide descarta el protocolo. Entre los que quedan se sugiere el más específico
  (producto > grupo > centro > cuenca). Un protocolo sin criterios aplica a todo.

## Reservas de despacho (cierra el hueco «confirmar no reserva»)

- En el **picking del pedido de cliente**, bodega escribe cuántos salen de cada lote y acuario y pulsa
  **Reservar acuarios**. El embarque enlaza a ese picking.
- La reserva no mueve stock: lo disponible para otros pedidos = sano − reservado.
  - `sp_reserva_fijar` valida, con la fila bloqueada, el pedido vigente, el producto, el acuario libre de
    tratamiento, lo pendiente de la línea y lo disponible.
  - El despacho consume la reserva y no deja tomar lo reservado para otro pedido. Al quedar despachado el
    pedido, sus reservas se borran.
- Solo cuentan las reservas de pedidos confirmados o en preparación.

## Pantallas (`/sanidad`)

| Pantalla | Para qué |
|---|---|
| **Mi día** | Celular del operario, botones de al menos 48 px. Tiene cuatro bloques: llegadas (ubicar e iniciar la profilaxis sugerida), medicación con la dosis ya calculada, ronda de alimentación en verde y rojo, y salidas de hoy y mañana. |
| **Tablero de acuarios** | Kanban `components/Tablero.tsx` en escritorio; chips por columna en móvil. Arrastrar solo hace la operación real: iniciar un protocolo, ubicar o terminar un tratamiento. |
| **Tratamientos** | Ficha con las aplicaciones, terminar y cancelar, reporte PDF e historial. |
| **Protocolos** | Pasos por día, aprobación. |
| **Medicamentos** | Catálogo con dosis de referencia, fuente e incompatibles. |
| **Alimentos y dietas** | Alimentos y dietas por grupo o por especie. |
| **Fichas técnicas** | Ficha por especie. |
| **Configuración** | Horas de ayuno pre-vuelo y responsable de cada acuario. |

## Permisos

| Permiso | Qué permite |
|---|---|
| `sanidad:leer` | Ver todo el módulo |
| `sanidad:operar` | Aplicar dosis, alimentar, iniciar un protocolo aprobado, terminar o cancelar tratamientos |
| `sanidad:gestionar` | Maestros, prescripciones manuales, protocolos en borrador, reporte PDF, parámetros, responsables |
| `sanidad:aprobar-protocolo` | Aprobar protocolos (veterinario o gerencia) |

Las reservas usan `pedidos:despachar` (bodega).

Plantillas de perfil:

| Perfil | Permisos de sanidad |
|---|---|
| Bodega | leer y operar |
| Operativo | leer, operar y gestionar |
| Comercial | leer |
| Consulta | leer |
| Administrador | todos (incluye aprobar) |

## Pendiente

- **Fase 2**:
  - planificador semanal (acuarios × lunes a domingo con alimentación, cambios de agua, medicación y ayunos
    proyectados por los vuelos);
  - inventario y costo de insumos.
- **Con el veterinario**: días y pasos del protocolo de llegada (y por procedencia, p. ej. Leticia/Inírida),
  tiempos de retiro, incompatibles y sensibilidad química por especie.
- **Con bodega**: litraje de los acuarios (hoy solo 3 de los activos lo tienen), dietas por grupo y responsables.
- Los pedidos nacionales sin embarque no tienen hora de salida: se reservan, pero no generan ayuno.
