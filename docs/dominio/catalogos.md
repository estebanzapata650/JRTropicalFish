# Catálogo de productos, precios, centros de acopio y proveedores

Reemplaza el Excel **«JR MAIN STOCK LIST»** (hoja *Listado*) y toma de **«BASES_TROPICAL FISH»**
las hojas *Centros_acopio* y *Proveedores*. El stock real (lotes, acuarios, movimientos) vive en [`inventario.md`](inventario.md); el producto
conserva además su **disponibilidad publicada** (lo que ve el cliente en el stocklist).

| Pantalla | Qué contiene |
|---|---|
| `/catalogos/productos` | Productos con grupo, tipo, nombres, talla, peces por bolsa, disponibilidad, hasta 3 centros de origen y precios por lista |
| `/catalogos/precios` | Listas de precios, su regla de cálculo, recálculo en bloque y parámetros (IVA, pasarela, TRM de referencia) |
| `/catalogos/centros-acopio` | Centros con su cuenca y cuántos productos y proveedores tienen |
| `/catalogos/proveedores` | Proveedores (CC/NIT), teléfono con WhatsApp/llamada, centros de acopio |
| Maestros → *Grupos de producto*, *Cuencas* | Listas de valores editables |

## Producto

- **Código**: prefijo del grupo + número (`AP020`, `PC0165`). Si se crea sin código, se asigna el
  siguiente del grupo **de 5 en 5** y con el mismo ancho (`PC0160` → `PC0165`). El código y el grupo
  no cambian después.
- **Grupos** (por prefijo): AP Apistogramas · CC Cíclidos especiales · CF Bagres · CH Carácidos
  especiales · CO Corydoras · CR Crustáceos · LO Loricáridos · OT Otros · PC Criadero · PT Rayas ·
  ST Estrigatas · TE Tetras. Editables en Maestros.
- **Tipo**: salvaje, criadero o especial. Decide el factor de precio nacional.
- **Talla**: mínima/máxima en cm, o una etiqueta (small, medium, big) cuando no hay medida.
- **Disponibilidad publicada**: texto que ve el cliente en el stocklist (`1 - 2 Bags`, `Not available`…).

## Listas de precios

Cada producto tiene **un precio por lista** (sin fila = sin precio). Las 8 listas del Excel:

| Lista | Moneda | Regla deducida del Excel |
|---|---|---|
| Standard | USD | Manual (base del exterior) |
| Wholesale Asian | USD | Manual |
| Glaser | USD | Standard × 1,1 |
| Minoristas del exterior | USD | Standard × 1,2 |
| **Costo de compra** | COP | Manual · **reservada** (`catalogos:ver-costos`) |
| Mayorista nacional | COP | Costo × (salvaje 2 · criadero 1,3 · especial 2) × (1 + IVA) × (1 + Wompi), redondeo $50 |
| David Ramírez (cliente) | COP | Mayorista × 1,2, redondeo $50 |
| Al detal nacional | COP | Mayorista × (salvaje 2 · criadero 1,5 · especial 2), redondeo $50 |

Las reglas reproducen **2.756 de 2.760** precios del Excel. La diferencia real es **TE130**: el Excel
tiene mayorista $200 y la regla da $350 (excepción manual o error de digitación); recalcular esa
lista lo cambiaría. Las otras tres eran redondeos de mi verificación en JavaScript, no de la API.

- Cada precio se puede editar a mano. **Recalcular** aplica la regla a todos los productos con precio
  en la lista base (los que no tienen base quedan igual) y deja constancia en el historial de la lista.
- El formulario del producto muestra el precio que daría la regla con lo escrito en la lista base y
  ofrece «Usar». La regla está en `Application/Catalogos/ReglasPrecio.cs` (probada) y su espejo en
  `web/src/lib/catalogos.ts`.
- Una lista base debe estar en la **misma moneda**; no se permiten ciclos.
- Código, moneda y «es costo» de una lista no cambian.

## Costo protegido

Sin `catalogos:ver-costos`, las listas de costo **no existen** para el usuario: no aparecen en
`/precios/listas`, no viajan en los precios del producto, se quitan del historial y fijarlas da 403.
Los precios en general requieren `precios:leer`; editarlos, `precios:editar`.

## Permisos

| Código | Descripción |
|---|---|
| `catalogos:leer` / `catalogos:editar` | Productos y centros de acopio |
| `catalogos:ver-costos` | Ver y editar el costo de compra |
| `precios:leer` / `precios:editar` | Ver precios / editarlos, listas, reglas, parámetros y recalcular |
| `proveedores:leer` / `proveedores:editar` | Proveedores |

Plantillas: Operativo y Consulta leen catálogo, precios y proveedores; Comercial lee catálogo y
precios. Nadie salvo Administrador ve costos por defecto.

## Importación

```bash
IMPORTAR_EMAIL=... IMPORTAR_PASSWORD=... npm run catalogo:importar -- "<JR MAIN STOCK LIST.xlsx>" "<BASES_TROPICAL FISH.xlsx>" [--simular]
```

Orden: cuencas → centros → grupos → productos con precios → proveedores. Pasa por la API (valida y
audita) y es idempotente. Detalles del mapeo:

- *Listado* manda (trae precios); *Listado Productos APP* completa tallas numéricas, bolsa y centros.
  Se importan 794 productos (789 del Listado + 5 que solo están en APP).
- Talla `"5,0 - 7,0"` → 5–7 cm; `"small"` → etiqueta. Bolsa `"40 ~ 50"` → 40–50.
- Precios en 0 → sin precio. Identificaciones en notación científica (`1.121707658E9`) → enteras.
- Un "teléfono" sin dígitos (`No requiere`) queda vacío y el texto va a las notas.
- Centros sin cuenca en el Excel (Esmeraldas) quedan en la cuenca **«Sin definir»**: asígnala.
