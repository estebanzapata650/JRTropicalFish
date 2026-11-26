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

## AUNAP y documentos para autoridades

- **Listado AUNAP** (Resolución 1924 de 2015, 598 especies; `docs/excelBD/Listado de la Aunap.xlsx`):
  `npm run aunap:importar -- "<archivo>"`. El listado trae cuatro grupos de códigos: 1–522, 600–611,
  700–739 y 1*–24*. Se importan todos tal cual, porque **falta confirmar qué implica cada grupo**.
- **Código AUNAP de cada producto** (Exportaciones → AUNAP): el enlace automático solo se hace cuando el
  nombre científico es idéntico; así quedaron 411 de 794 productos. Los sinónimos taxonómicos se enlazan a mano,
  por ejemplo *Ariopsis seemanni* = *Sciades seemanni* (365). **Nunca se enlaza una especie con otra distinta.**
  Un producto sin código no se puede declarar ni exportar. Al guardar las especies de un embarque, el sistema
  avisa cuáles están en ese caso.
- **Paso «2 Trámites» del embarque**, en el orden del despacho:
  1. **Proforma.** La preinvoice es la cotización al cliente. Cuando la aprueba (embarque *confirmado*), se genera
     la proforma, con consecutivo propio `PRO-001` y fecha. Es la base de la VUCE y sale de las especies y
     cantidades reales del pedido. No se genera antes de confirmar (409), y su PDF exige que esté generada.
  2. **VUCE.** Todo despacho tiene una: n.º de trámite TM, fecha de solicitud, fecha de aprobación y cantidad
     aprobada por especie, con un botón para copiar lo pedido. La comparación por especie muestra pedido, aprobado y
     empacado. Estados: sin VUCE, no autorizada, excede o dentro.
  3. **DEX y factura DIAN** (la factura electrónica de Siigo, por ejemplo JRE437; no es la factura comercial 12187…).
  4. **CITES** (n.º y fecha): solo cuando el despacho lleva rayas (*Potamotrygon*, *Paratrygon*), reconocidas por el nombre
     científico, porque el grupo PT también trae lenguados. Si no lleva rayas, la sección dice «No aplica». El informe
     mensual avisa si van rayas sin CITES.
  5. **Aeropuerto y aduana:** packing list y custom invoice. El **ICA** queda para una fase posterior.

  Se guarda en `embarque_permisos_aunap` (`numero` = TM, `fecha` = aprobación) y todo cambio queda en la
  auditoría.
- **Documentos** (PDF):

  | Documento | De dónde sale |
  |---|---|
  | **Proforma** (`PRO-…`) | Las especies y cantidades del pedido, con los precios de la preinvoice |
  | **Packing list de aduana y aeropuerto** | El empaque real, caja por caja |
  | **Custom invoice** | La factura real agrupada por especie AUNAP, con los mismos totales que la factura comercial |

  Todos usan el código y el nombre de la resolución.
- **Regla (`ReglasAunap`)**: lo declarado sale de lo real. Nunca hay factores, especies sustituidas ni
  cantidades o valores distintos a los del pedido, el empaque o la factura. Si alguna especie no tiene código,
  el documento no se genera.

## Empaque (`/exportaciones/empaque`)

- **Estándar por producto** (especie y talla):
  - tipo de bolsa (los 14 de la macro comercial) y peces por bolsa (mínimo y máximo);
  - litros de agua (vacío = los del tipo de bolsa);
  - aditivos del agua y notas para bodega;
  - «revisado con el equipo».

  Sin estándar se usan los peces por bolsa del catálogo.
- **Aditivos:** los medicamentos de los reportes AQUAVET que se dosifican por volumen (12; el mebendazol va en el
  alimento y no aplica). La dosis es la de la ficha del medicamento **por litro de agua**:
  cantidad = dosis ÷ base (L) × litros de la bolsa × bolsas.
- **Costos** (pestaña Costos): heat pack (1.470 COP de la hoja Parametros), oxígeno por bolsa, litros de agua por tipo de
  bolsa y costo por unidad de cada aditivo. Las cajas y las bolsas siguen en Configuración. Ningún costo se inventa: lo
  vacío queda «por definir».
- **Costo de empaque del despacho** (paso Empaque del embarque): sale del empaque real:
  - cajas reales × costo de materiales;
  - bolsas × costo de cada tipo;
  - heat packs (uno por piso) y oxígeno por bolsa;
  - aditivos del estándar de cada especie.

  Muestra el total y el costo por pez, lista lo que falta por cargar (no lo suma) y avisa las filas fuera del estándar
  (otra bolsa, o más o menos peces por bolsa).
- **DOA por empaque:** DOA + ADD + FINAL sobre los peces enviados, por especie × tipo de bolsa × peces por bolsa, de los
  embarques facturados o cerrados (por defecto, los últimos 12 meses). Marca la forma con menor DOA de cada especie,
  para ajustar el estándar con datos.
- **Código:** reglas en `ReglasEmpaque` (con pruebas) y funciones en la migración `FuncionesEmpaque`. Los estándares y
  los costos se editan con `exportaciones:configurar`; todo cambio queda en la auditoría.

## Informe mensual AUNAP

Reemplaza el «Formato Informe Mensual» de la DTIV (`docs/excelBD/10 - OCUBRE 2026 JR TROPICAL FISH LTDA.xlsx`).
Está en **Exportaciones → Informe AUNAP** (`/exportaciones/aunap/informe?mes=2026-10`); por defecto muestra el mes
anterior. Se descarga en Excel con las mismas celdas del formato: encabezado en B3:G11, títulos en las filas 15–16
y datos desde la fila 17, columnas B…S.

- **Unidades:** solo lo que **salió de bodega**, es decir, los despachos del pedido de cliente del embarque.
  Nunca salen de la proforma, del pedido ni de lo aprobado en la VUCE.
- **Filas:** una por exportación × especie AUNAP × municipio de origen.
- **Origen:** lote despachado → centro de acopio → municipio AUNAP. El listado oficial de municipios
  (`municipios_aunap`) tiene 1.121 filas de la hoja oculta ESPECIES y trae la cuenca de cada municipio. Se
  administra en **Orígenes y encabezado** (`/exportaciones/aunap/origenes`).
- **Criadero:** un centro marcado «no se reporta» (Medellín) queda fuera del informe; el total excluido se muestra
  aparte.
- **Fecha de exportación:** la salida del vuelo en hora de Bogotá. Si no hay vuelo, la fecha de despacho; y si
  tampoco hay fecha de despacho, la del último despacho. El mes del informe sale de esta fecha.
- **Avisos** antes de enviar:
  - falta VUCE, DEX o factura DIAN;
  - un producto sin código AUNAP (no se declara);
  - un centro sin municipio;
  - salió más de lo aprobado en la VUCE;
  - lo despachado difiere de lo empacado.
- **Encabezado:** representante legal, resolución, vigencia, dirección, correo y teléfono, en `parametros_aunap`.
  Si la vigencia ya terminó, la pantalla lo advierte.
- **Código:** reglas en `ReglasInformeAunap` (con pruebas), datos en `fn_informe_aunap` y Excel en
  `GeneradorInformeAunapExcel` (ClosedXML, MIT; solo se usa ahí).

## Pendiente

- **Con gerencia**: el informe ICA y el resumen para Siigo. Saldrán de los mismos datos reales. Falta confirmar el
  significado de los grupos de códigos AUNAP 600–611, 700–739 y 1*–24*.
- **Orígenes por definir**: los centros Bogotá y Esmeraldas no tienen municipio. En Barranquilla, San Felipe y San
  José del Guaviare, la cuenca del catálogo no coincide con la del listado AUNAP.
- **Productos sin código AUNAP** (383): revisar sinónimos y, para las especies que no están en la resolución,
  decidir si se retiran de la oferta de exportación o se tramita su autorización.
- **Por aclarar**: significado de las columnas ADD y FINAL de la factura JRCH (hoy se suman al DOA como en el
  Excel); hoja «Parametros agua» (rota en el Excel).
- Factura electrónica DIAN: se sigue digitando en Siigo.
