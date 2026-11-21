# Exportaciones: documentos comerciales del despacho

> Estado: **implementado** (2026-11-21) para los documentos comerciales. Reemplaza el libro
> **«PM-COM-VTA. Formatos Comerciales V5.0»** (`docs/excelBD/`) con sus fórmulas, probado contra un despacho
> real: la factura **12186 a Kamihata Tokyo** (6.174 peces, USD 8.033,05; DOA USD 335,22).
> Los documentos para autoridades quedan **pendientes de revisión con gerencia** (ver al final).

## Qué es un embarque

El **embarque** (`EXP-001`) es el despacho documental de **una venta de exportación del CRM**. Nace desde la
venta (ícono de avión en Ventas o «Nuevo embarque») y hereda cliente, comercial, aerolínea y AWB. Del
cliente toma su lista de precios (por defecto `STANDARD`), descuento y formato de factura; de Configuración,
una copia de las tarifas de flete y de los cargos (editables en el embarque).

Sus **especies** se sincronizan con el **pedido de cliente** de la venta (lo crea si no existe): así bodega
hace el picking y el despacho de stock contra las mismas líneas (ver [`inventario.md`](inventario.md)).

## Pasos y documentos

| Paso | Qué se hace | Documento (PDF) |
|---|---|---|
| 1 Pedido y preinvoice | Especies, cantidad, precio (vacío = lista), fracción de caja y empaque; despacho (vuelo, AWB, destino); cajas estimadas; certificados y cargos | **Preinvoice** (n.º año-día, como el Excel: `2026-240`), con anticipo y datos bancarios |
| 2 Flete | Tarifas del embarque y desglose | **Estimated freight costs** |
| 3 Orden de cargue | Qué especie va en qué caja ("13 - 14") y cómo se empaca | **Orden de cargue** (español, para bodega) |
| 4 Empaque | Bodega registra caja por caja: especie, tipo de bolsa, bolsas × peces | **Packing list** |
| 5 Factura | Cajas reales por tipo, valor real del AWB; **emitir** | **Invoice** (formato JRCH o V2 según el cliente) |
| 6 DOA y nota crédito | Muertos que reporta el cliente por fila: DOA + ADD + FINAL | **Credit note** (`12187-01`) |
| 7 Balance | Anticipo recibido | **Balance** (factura − nota crédito − anticipo) |

Cada documento se ve, se descarga o **se envía por correo** al cliente (PDF adjunto; queda registro). Con el
proveedor de correo `Disco` (desarrollo) el correo y el PDF se guardan en disco, no salen a internet.

## Cálculos (`Application/Exportaciones/ReglasEmbarque.cs`, mismas fórmulas del Excel)

- **Preinvoice** = Σ cantidad × precio − descuento % + cajas × precio del tipo + heat packs (uno por piso de
  caja) × precio + certificados y cargos + AWB estimado. **Anticipo** = 50 % del valor de los peces + 100 % del AWB.
- **Flete** (hoja *Est. Freight Costs*): kg = Σ cajas × kg/caja; cargo por peso = kg × (tarifa cliente + fuel
  + JP fee); VSC = kg × tarifa con mínimo; DBC % sobre el cargo por peso; DF % sobre el due agent; + due agent
  + due carrier.
- **Orden de cargue**: acumulado de fracciones; caja inicio = ⌊anterior⌋ + 1, fin = ⌈acumulado⌉.
- **Factura**: por fila del empaque real (bolsas × peces × precio); descuento; cajas reales con heat packs;
  cargos; AWB real (o el estimado mientras no exista).
- **Nota crédito**: (DOA + ADD + FINAL) × precio, agrupado por especie, menos el descuento.

## Reglas

- Estados: `borrador → preinvoice_enviada → confirmado → empacado → facturado → cerrado`; `cancelado` antes
  de facturar. Enviar la preinvoice al cliente la deja «enviada». «Facturado» solo lo pone la emisión.
- **Emitir factura** exige estar empacado, AWB y su valor, y cajas reales. Asigna el siguiente número (consecutivo
  `FACT`, arrancó en 12187 para seguir el Excel), guarda el PDF en el almacén y **no cambia más**: después solo
  se editan el anticipo, las notas y el DOA.
- La factura actualiza la **venta** (total USD, cajas, aerolínea, AWB) y su **tracking** (n.º de factura, FOB =
  peces − descuento, flete = AWB, cajas). El DOA actualiza el tracking (DOA = nota crédito, n.º de nota crédito).
- Un comercial sin `crm:ver-todo` solo ve sus embarques.

## Configuración (`/exportaciones/configuracion`)

Datos de la empresa (encabezado), **logo** (PNG/JPG), datos bancarios y firma; tarifas de flete por defecto;
tipos de caja (precio, kg, pisos, costo de materiales); tipos de bolsa (porción de caja, costo); precios de
certificados, airport fee y heat pack. Sembrados con la hoja *Parametros*; **los datos bancarios y la firma se
llenan aquí** (no se sembraron). Los datos de exportación de cada cliente (dirección, EORI, formato de factura,
lista y descuento) se editan desde el embarque («Datos del cliente»).

## Permisos

| Permiso | Qué permite |
|---|---|
| `exportaciones:leer` | Ver embarques y descargar documentos |
| `exportaciones:gestionar` | Preinvoice, flete, factura, DOA, balance, estados y envío de documentos |
| `exportaciones:empacar` | Registrar el empaque y marcar «empacado» (bodega) |
| `exportaciones:configurar` | Configuración de exportaciones |

Plantillas: Comercial leer + gestionar; Bodega leer + empacar; Operativo leer + gestionar + empacar; Consulta leer.

## Pendiente

- **Con gerencia (no se construyó)**: proforma AUNAP (solicitud de permiso), lista de empaque de aeropuerto,
  custom invoice, informe AUNAP y resumen para Siigo. En el despacho 12186 esos documentos declaran 4.578 peces
  contra 6.174 facturados y la proforma ≈ 40 % del precio; en el sistema saldrán **de la lista de empaque real**
  cuando se defina la regla. Requieren el catálogo AUNAP (575 especies) y el código AUNAP por producto.
- **Por aclarar**: significado de las columnas ADD y FINAL de la factura JRCH (hoy se suman al DOA como en el
  Excel); hoja «Parametros agua» (rota en el Excel).
- Factura electrónica DIAN: se sigue digitando en Siigo.
