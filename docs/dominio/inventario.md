# Inventario, compras y pedidos de clientes

> Estado: **implementado** (2026-11-21). Reemplaza la app de AppSheet montada sobre el Excel
> **«BASES_TROPICAL FISH»** (hoja *Inicio*: proceso de 10 pasos, 7 módulos y 10 indicadores).
> Reutiliza Productos, Proveedores y Centros de acopio ([`catalogos.md`](catalogos.md)) y Clientes y
> Ventas del CRM ([`crm.md`](crm.md)).

## Proceso (los 10 pasos del Excel)

| Paso | Dónde se hace |
|---|---|
| 1. Disponibilidad de inventario | `/inventario` (tablero Bodega) y `/inventario/existencias` |
| 2. Disponibilidad del proveedor | `/compras/ofertas` |
| 3. Tabla de disponibilidad al cliente | Disponibilidad publicada del producto (catálogo) |
| 4–5. Pedido del cliente | `/crm/pedidos`: desde la venta del CRM (botón en Ventas) o suelto |
| 6. Agrupar por referencia y centro | `/compras/necesidades` |
| 7. Pedido a proveedor | `/compras/pedidos` (o un clic desde Necesidades) |
| 8. Llegada y recibo | «Registrar llegada» en el pedido a proveedor → `/compras/recepciones` |
| 9. Ubicación en acuarios | `/inventario/existencias` (ubicar) y `/inventario/acuarios` (mapa) |
| 10. Picking y despacho | «Picking y despacho» en el pedido de cliente |

## Modelo

Las cantidades son **piezas enteras**. Los costos van en COP y solo viajan con `catalogos:ver-costos`.

| Entidad | Código | Qué es |
|---|---|---|
| **Acuario** | `A101` | Sector, nivel, posición, capacidad en litros, activo |
| **OfertaProveedor** + líneas | `DISP-0001` | Lo que un proveedor ofrece (especie y cantidad, sin precio). Abierta / cerrada |
| **PedidoProveedor** + líneas | `PP-0001` | Pedido a un proveedor; línea con cantidad, costo unitario y cantidad recibida |
| **Recepcion** + líneas | `REC-0001` | Una llegada: por línea, recibida (total), enferma, muerta, costo y costo total calculado |
| **Lote** | `LOT-0001` | Producto, proveedor, centro, fecha de ingreso, costo, recibidos, muertos al llegar. Origen `recepcion` o `saldo_inicial` |
| **Existencia** | — | Saldo materializado lote × acuario × condición. Acuario nulo = **por ubicar**. Nunca se edita a mano |
| **MovimientoInventario** | — | Kardex **inmutable**: tipo, lote, origen y destino (acuario, condición), cantidad, referencias, usuario, fecha |
| **PedidoCliente** + líneas | `PC-0001` | Cliente, comercial, venta del CRM (opcional, una por venta), ciudad; línea con cantidad, precio y despachada |

`existencias` tiene índice único (lote, acuario, condición) con `NULLS NOT DISTINCT` y `CHECK cantidad >= 0`.

### Condiciones y vendibilidad

- **Sano** y en un acuario: vendible. La **aclimatación es solo informativa**: se muestran los días
  desde el ingreso y la alerta de **más de 20 días**.
- **Aclimatación** (recién recibido, sin acuario; en el modelo, existencia con acuario nulo): no se
  despacha hasta ubicarlo. En el Excel era el «acuario» virtual `ACLIMATACION`.
- **Enfermo**: **nunca** se despacha. Se alivia (vuelve a sano) o muere.

## Motor de stock

Todo cambio de stock pasa por una sola primitiva, `sp_inventario_mover_uno(origen → destino)`:

1. bloquea la existencia de origen con `SELECT … FOR UPDATE`;
2. si quedaría negativa, lanza `SQLSTATE JR002` (→ 409 «No hay suficiente stock»);
3. descuenta, suma al destino (crea la fila si no existe) e inserta el movimiento.

`sp_inventario_mover(jsonb)` aplica varias operaciones **todo o nada**.

| Tipo | Origen → destino | Permiso |
|---|---|---|
| `entrada` (recepción) · `saldo_inicial` | nada → por ubicar (sano o enfermo) | `compras:recibir` · `inventario:ajustar` |
| `ubicacion` | por ubicar → acuario | `inventario:operar` |
| `traslado` | acuario A → acuario B (misma condición) | `inventario:operar` |
| `enfermo` · `aliviado` | sano → enfermo · enfermo → sano | `inventario:operar` |
| `mortalidad` | origen → nada (los muertos al llegar también) | `inventario:operar` |
| `despacho` | acuario sano → nada, contra una línea de pedido | `pedidos:despachar` |
| `ajuste` | cualquier sentido, **con motivo obligatorio** | `inventario:ajustar` |

Las reglas de cada tipo (qué origen/destino exige) están en `ReglasStock` (Application) y probadas.

## Reglas

### Pedido a proveedor

- Estados: `borrador → enviado → en_transito → recibido_parcial / recibido`; `cancelado`. Los cambios
  manuales son «comparar y escribir» (`JR001` → 409). **Recibido** y **recibido parcial** solo los
  pone una recepción, nunca a mano.
- Solo se edita sin recepciones y en borrador, enviado o en tránsito.
- Sin `catalogos:ver-costos` se puede editar el pedido y los costos existentes se conservan.

### Recepción

- Se recibe un pedido enviado, en tránsito o recibido parcial. «Llegó» es el total de la especie;
  enfermos + muertos no pueden superarlo.
- **Cada línea crea un lote** (`LOT-xxxx`); los vivos quedan **por ubicar** (sanos y enfermos aparte) y
  los muertos quedan como movimiento de `mortalidad` (% de muertos del lote).
- Se permite **recibir más de lo pedido** (los proveedores mandan extra): pasa con un aviso.
- Si todas las líneas quedan completas, el pedido pasa a **recibido**; si no, a **recibido parcial**.

### Pedido de cliente

- Estados: `borrador → confirmado → en_preparacion → despachado`; `cancelado`.
  «Cerrar con faltantes» pasa de en preparación a despachado sin completar.
- **Desde la venta**: hereda cliente, comercial y ciudad; una venta tiene un solo pedido (409).
  **Suelto**: se elige cliente y, si se puede asignar, comercial.
- **Cada talla es un producto**: precio, stock, picking y reservas cuelgan del producto, así que el pedido no lleva
  talla libre. El selector muestra código · nombre · talla (y busca también por nombre científico e inglés).
- **Precio**: elegir la especie trae el precio de lista según la moneda del pedido (USD = `STANDARD`, COP =
  `MAYORISTA_NACIONAL`) sin pisar lo escrito a mano. Moneda = la de la venta; en un pedido suelto, Colombia = COP y los
  demás = USD.
- Visibilidad como el CRM: el comercial sin `crm:ver-todo` solo ve y edita los suyos.
- **Despacho**: solo confirmado o en preparación; solo desde existencias sanas en acuario; no supera lo
  pendiente de la línea (`JR003` → 400). El primer despacho pasa el pedido a **en preparación** y la
  venta del CRM a `en_empaque`; al completar, el pedido queda **despachado** y la venta en
  `listo_despacho`.
- Una línea no baja de lo ya despachado ni se quita si tiene despachos.

### Necesidades de compra

Por especie: pendiente de los pedidos de cliente **confirmados o en preparación** − stock sano y por
ubicar − en tránsito (pedidos a proveedor pendientes) = **faltante**. Se marcan especies, se elige el
proveedor y se crea el pedido con el faltante como cantidad.

## Tableros (reemplazan los 4 de Looker)

Cuatro pestañas bajo **Inventario → Tableros**. Proveedores y Clientes tienen periodo en la URL
(`?periodo=30d|mes|mes-anterior|90d|anio`, por defecto 30 días); lo que está en camino o pendiente no
depende del periodo.

| Pestaña | Ruta | Qué muestra |
|---|---|---|
| **Bodega** | `/inventario` | En acuarios, en aclimatación, enfermos, ocupación de acuarios (ocupados / activos, vacíos), lotes activos y de más de 20 días; disponibilidad por especie (acuarios, aclimatación, enfermos, total, pedido por clientes, en tránsito, última oferta, faltante); ubicaciones por acuario; acuarios con enfermos |
| **Proveedores** | `/inventario/proveedores` | Pedidos y peces en camino (con «atrasado»), cumplimiento *in full*, recibidos y mortalidad del periodo; cumplimiento y mortalidad por proveedor; compras por proveedor; recibidos por semana; cumplimiento por pedido |
| **Clientes** | `/inventario/clientes` | Pedidos y peces por despachar con **si el stock sano alcanza**; despachado y cumplimiento del periodo; cumplimiento y despachos por cliente; 20 especies más vendidas |
| **Trazabilidad** | `/inventario/lotes` y `/inventario/lotes/{id}` | Por lote: dónde está cada pez (barra), días en aclimatación y en cuarentena, clientes. El **recorrido** del lote: llegada (proveedor, centro, muertos al llegar), aclimatación, acuarios, cuarentena, envíos a clientes y mortalidad, más el kardex |

**Qué se corrigió respecto a Looker**

- *Cumplimiento in full* nunca pasa de 100 %: por línea se cuenta lo recibido (o despachado) hasta lo
  pedido; recibir de más en una especie no compensa lo que faltó en otra. Looker mostraba 1536 % y
  pendientes negativos.
- Las tortas pasaron a **barras ordenadas** (comparar partes sobre una base común es más exacto).
- El evolutivo recibidos/muertos con dos escalas pasó a **columnas por semana** con los muertos en el
  detalle y la mortalidad por proveedor aparte.
- «Pendiente por despachar» dice si el stock sano de la especie alcanza (el stock es total por especie:
  varios pedidos pueden competir por él).

### Aclimatación y cuarentena (cómo se miden)

Salen del kardex (`fn_lotes_tiempos`), sin estados nuevos:

- **Aclimatación**: tiempo en que el lote tuvo al menos un pez sin acuario. «En curso» si todavía tiene.
- **Cuarentena** = **tiempo enfermo**: tiempo en que el lote tuvo al menos un pez enfermo, sumando
  episodios (de enfermo a aliviado o muerto). «En curso» si hoy hay enfermos.
- Lotes del saldo inicial: la aclimatación se cuenta desde la fecha de ingreso del Excel; la cuarentena,
  desde la migración (el Excel no traía esa historia).
- La línea de tiempo del recorrido está pensada para sumar después la **bitácora** de enfermedad y
  alimentación como eventos del mismo lote.

## Permisos

| Permiso | Qué permite |
|---|---|
| `inventario:leer` | Existencias, lotes, acuarios, movimientos, dashboard, ofertas y pedidos |
| `inventario:operar` | Ubicar, trasladar, enfermo, aliviado, mortalidad |
| `inventario:ajustar` | Ajustes con motivo, saldo inicial y CRUD de acuarios |
| `compras:gestionar` | Ofertas de proveedor y pedidos a proveedor |
| `compras:recibir` | Recepciones (crean los lotes) |
| `pedidos:gestionar` | Crear y editar pedidos de cliente |
| `pedidos:despachar` | Picking y despacho |

Plantillas: **Bodega** (nuevo) leer, operar, recibir, despachar y `catalogos:leer`; **Operativo**
leer, operar, compras y despachar; **Comercial** leer y `pedidos:gestionar`; **Consulta** leer.

## Migración desde el Excel

```bash
IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run inventario:importar -- "<BASES_TROPICAL FISH.xlsx>" [--simular]
```

- **Acuarios** de *Base acuarios* (sector, nivel, posición, capacidad), más los que aparezcan en
  movimientos.
- **Saldo inicial por lote**: de *Detalle_lotes* (producto, proveedor, centro, fecha; **conserva el
  código `LOT-xxxx`**) y de *Movimientos* el neto por acuario y condición (Entrada suma, Salida resta,
  estado «Enfermo» = enfermo). El acuario virtual `ACLIMATACION` es **por ubicar**.
- Los negativos se dejan en 0 y se reportan; se contrasta con *Base acuarios · Cantidad actual*.
- Pasa por `POST /inventario/saldo-inicial` (audita, deja movimientos `saldo_inicial`) y es
  idempotente: lo que ya existe se omite. Requiere `npm run catalogo:importar` antes.
- No se migra el historial de pedidos.

### Resultado de la migración (2026-11-21)

63 lotes con stock, 13.528 peces (12.835 sanos en acuario, 693 por ubicar). Inconsistencias del Excel:

- 3 saldos negativos corregidos a 0: LOT-0539 en A108 (−100), LOT-0537 en A108 (−80), LOT-0570 en
  I203 (−1). Por eso A108 queda con 700 y no con los 520 del Excel.
- 13 acuarios no cuadran con *Cantidad actual*.
- `ACLIMATACION` dice 2.235 en el Excel, pero los movimientos dan 693.
- C101 no estaba en *Base acuarios*; se dedujo de los movimientos.

## Pendiente

- Conteos físicos y ajustes con aprobación (`inventario:aprobar-ajuste`).
- Valorización del inventario y costo landed (llega con Importaciones).
- Reserva de stock al confirmar un pedido (hoy el stock se descuenta solo al despachar).
- Reportes en PDF (existencias, acta de recepción, packing list).
