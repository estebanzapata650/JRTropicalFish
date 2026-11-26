# Revisión: huecos pendientes y guía de pruebas

> Fecha: 22/11/2026. Para revisar con gerencia, el veterinario y bodega, y para probar el sistema de punta a punta
> antes de salir a producción. Cada punto tiene una casilla para marcarlo.
>
> Para ver este archivo con formato en VS Code: clic derecho → **Abrir vista previa** (Ctrl+Shift+V).

---

# Parte A. Huecos pendientes

## A1. Esperan una decisión de gerencia

| ✔ | Hueco | Qué pasa hoy | Qué se necesita |
|---|---|---|---|
| ☐ | **Productos sin código AUNAP** (383 de 794) | La solicitud AUNAP, el packing list de aduana y la custom invoice ya existen y salen de lo real. Un producto sin código no se puede declarar | Revisar sinónimos en Exportaciones → AUNAP. Para las especies que no están en la Resolución 1924, decidir si se retiran de la exportación o se tramita su autorización |
| ☐ | **Grupos de códigos AUNAP** 600–611, 700–739 y 1*–24* | Se importaron tal cual | Confirmar qué implica cada grupo (¿restricciones, especies exóticas, especies peligrosas?) |
| ☐ | **Informe AUNAP posterior y resumen para Siigo** | No existen | Definir el formato; saldrán de los mismos datos reales |
| ☐ | **Columnas ADD y FINAL** de la factura JRCH | Se suman al DOA para la nota crédito, igual que en el Excel | Confirmar qué significa cada una |
| ☐ | **Hoja «Parametros agua»** del libro de formatos | Está rota en el Excel; no se migró | Saber si se usa y con qué datos |

## A2. Esperan al veterinario (Sanidad)

| ✔ | Hueco | Qué pasa hoy | Qué se necesita |
|---|---|---|---|
| ☐ | **Protocolo de llegada** | PRT-001 «Profilaxis de llegada» está en **borrador**, sin días de cuarentena. Mientras no haya un protocolo aprobado, Mi día **no sugiere profilaxis** | Días de cuarentena y pasos (medicamentos por día). Si aplica, protocolos por procedencia (Leticia, Inírida…) o por grupo de especies |
| ☐ | **Tiempos de retiro** | Todos los medicamentos están en 0 días: el acuario se puede despachar al día siguiente de terminar el tratamiento | Días de retiro por medicamento |
| ☐ | **Medicamentos que no se combinan** | No hay ninguno marcado | La lista de incompatibilidades |
| ☐ | **Sensibilidad química por especie** | El campo existe en la ficha técnica, vacío | Qué especies no toleran qué medicamentos (p. ej. cobre) |
| ☐ | **Dosis ajustadas** | El sistema calcula la dosis proporcional a los litros. Para 80 L, el reporte de Fenlak indica 1,8 mL y la proporción da 1,6 | Confirmar si se usa la proporción o una tabla propia. El operario puede registrar la cantidad real |

## A3. Esperan datos de bodega

| ✔ | Hueco | Por qué importa |
|---|---|---|
| ☐ | **Litraje de los acuarios** (Inventario → Acuarios). Solo 3 lo tienen | **Sin litraje no se calcula ninguna dosis.** Es lo más urgente |
| ☐ | **Dietas por grupo de especies** (Sanidad → Alimentos y dietas). No hay ninguna | Sin dieta, la ronda de alimentación no dice qué dar ni cuánto |
| ☐ | **Responsable de cada acuario** (Sanidad → Configuración) | Aparece en el tablero; hoy todos dicen «Sin responsable» |
| ☐ | **Datos bancarios, firmante y logo** (Exportaciones → Configuración) | Salen en la preinvoice y la factura |
| ☐ | **Reimportar el Excel «BASES» actualizado** | Looker iba en el lote LOT-1011 y lo cargado llega a LOT-0695. Hacerlo antes de producción (`npm run inventario:importar`) |

## A3b. Empaque: revisar con el equipo y cargar costos

| ✔ | Qué | Dónde |
|---|---|---|
| ☐ | **Estándar de empaque por especie y talla**: tipo de bolsa, peces por bolsa, aditivos. Hoy ninguno está definido; se usan los peces por bolsa del catálogo. Marcar «Revisado con el equipo» al confirmarlo | Exportaciones → Empaque → Estándares |
| ☐ | **Litros de agua de cada tipo de bolsa** (14). Sin litros no se calcula el costo de los aditivos | Exportaciones → Empaque → Costos |
| ☐ | **Costo por unidad de los aditivos** (Stress Guard, sal, etc.) | Exportaciones → Empaque → Costos |
| ☐ | **Costo del oxígeno por bolsa** y confirmar el del heat pack (1.470 COP del Excel) | Exportaciones → Empaque → Costos |
| ☐ | **Costo de materiales de la caja triple piso** (hoy 0) y revisar los de las demás cajas y bolsas | Exportaciones → Configuración |
| ☐ | **Qué aditivos lleva cada especie** y si la dosis de empaque es la misma de la ficha del medicamento | Con el veterinario |
| ☐ | Después de unos despachos con DOA, revisar **DOA por empaque** y ajustar los estándares | Exportaciones → Empaque → DOA por empaque |

## A4. Huecos de integración (para decidir en las pruebas con el equipo)

| ✔ | Hueco | Qué pasa hoy |
|---|---|---|
| ☐ | Una **venta nacional** no crea su pedido de cliente | La venta de exportación sí lo crea (a través del embarque). En la nacional, el pedido se crea a mano en Comercial → Pedidos de clientes |
| ☐ | **Confirmar un pedido no reserva** stock solo | Bodega reserva en el picking («Reservar acuarios»). Sin reserva, el stock sigue libre para otros pedidos |
| ☐ | **Pedidos nacionales sin hora de salida** | Se pueden reservar, pero no generan el ayuno pre-vuelo (no hay vuelo) |
| ☐ | **Disponibilidad publicada** del catálogo | Se mantiene a mano; no se calcula del stock |
| ☐ | **Total de la venta vs. precios del pedido** | Son independientes: el total de la venta no sale de las líneas del pedido (en exportación sí lo actualiza la factura) |
| ☐ | **«Ubicar» desde Mi día o el tablero** | Envía todo el lote como sano. Si parte del lote llegó enfermo, falla y hay que ubicar desde Inventario → Existencias. Es un ajuste pequeño |

## A5. Funcionalidad aún no construida

| ✔ | Qué | Módulo |
|---|---|---|
| ☐ | Planificador semanal (acuarios × lunes a domingo: alimentación, cambios de agua, medicación y ayunos proyectados) | Sanidad, fase 2 |
| ☐ | Importar el PDF de fichas técnicas (≈ 300 especies) | Sanidad, fase 2 |
| ☐ | Inventario y costo de insumos (medicamentos, alimento) | Sanidad |
| ☐ | Conteos físicos, ajustes con aprobación y PDFs | Inventario |
| ☐ | Aerolíneas y demás maestros | Catálogos |
| ☐ | Compras al exterior: documentos, CITES, guía aérea, DOA de llegada, costos de nacionalización | Importaciones |
| ☐ | Reportes | Reportes |
| ☐ | Recuperar la contraseña por correo | Seguridad |
| ☐ | Despliegue automático (CI/CD) | Infraestructura |

---

# Parte B. Guía de pruebas

## B0. Preparación

| Qué | Valor |
|---|---|
| Levantar todo | En la carpeta del proyecto: `npm run dev` (necesita Docker Desktop abierto) |
| Aplicación web | http://localhost:3100 |
| API (documentación interactiva) | http://localhost:5377/scalar/v1 |
| Salud del sistema | http://localhost:5377/api/v1/health → debe responder `Healthy` |
| Usuario administrador | `admin@jrtropicalfish.local` / `Admin123!` (solo en desarrollo) |
| Correos de prueba | No salen a internet: quedan como archivos (con el PDF adjunto) en `apps/api/src/JRTropicalFish.Api/bin/Debug/net10.0/correos-enviados/`; la carpeta se crea con el primer correo |
| Detener todo | `npm run stop` |

**Recomendaciones**

- Marca todo lo que crees en las pruebas con la palabra **PRUEBA** (en notas u observaciones) para encontrarlo y borrarlo después.
- Repite las pantallas clave **en el celular** (o el navegador a ~390 px de ancho) y en **tema oscuro** (botón «Tema» abajo en el menú).
- Prueba también con usuarios de perfil **Bodega**, **Comercial** y **Consulta**: cada uno debe ver solo su menú y sus botones.

## B1. Pruebas automáticas (técnicas)

Desde la carpeta del proyecto, en una terminal:

| ✔ | Comando | Resultado esperado |
|---|---|---|
| ☐ | `npm test` | `Correctas! … Superado: 200` (reglas de precios, CRM, inventario, exportaciones y sanidad) |
| ☐ | `npm run lint` | Sin errores |
| ☐ | `npm run build` | API y web compilan sin errores ni advertencias. Hacerlo con `npm run stop` antes: si la API está corriendo, sus archivos están en uso |
| ☐ | Abrir http://localhost:5377/api/v1/health | `{"status":"Healthy","checks":{"postgres":"Healthy"}}` |

## B2. Recorrido funcional por módulo

Cada fila: ruta → qué hacer → qué debe pasar.

### 1. Acceso y seguridad

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/login | Entrar con el administrador | Llega al Panel; el menú muestra todos los módulos |
| ☐ | http://localhost:3100/login | Equivocar la contraseña 5 veces con un usuario de prueba | El usuario queda bloqueado 15 minutos |
| ☐ | http://localhost:3100/seguridad/usuarios | Crear un usuario por perfil (Bodega, Comercial, Consulta); restablecer contraseña; desactivar y reactivar | Cada acción queda en el historial; un usuario desactivado no entra |
| ☐ | http://localhost:3100/seguridad/perfiles | Quitar un permiso a un perfil de prueba | Los usuarios de ese perfil vuelven al login y entran ya sin ese permiso |
| ☐ | http://localhost:3100/cuenta | Cambiar la propia contraseña | Pide la actual; con la nueva se puede entrar |
| ☐ | Entrar como **Bodega** | Revisar el menú | Ve Inventario, Sanidad (Mi día, Tablero…), Compras (recibir), Pedidos (despachar) y Exportaciones (empacar); no ve Seguridad ni Metas |

### 2. Administración

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/administracion/maestros | Crear y editar un valor (p. ej. un cargo) | Aparece en el formulario de empleados |
| ☐ | http://localhost:3100/administracion/empleados | Crear un empleado con foto y salario | La foto se ve; el salario solo con el permiso «ver salario» |

### 3. Catálogos

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/catalogos/productos | Buscar «Corydoras»; editar un producto | 794 productos; el código sigue el grupo |
| ☐ | http://localhost:3100/catalogos/precios | Revisar una lista; recalcular | El costo solo se ve con el permiso «ver costos» |
| ☐ | http://localhost:3100/catalogos/centros-acopio | Revisar centros y su cuenca | Listado con productos y proveedores |
| ☐ | http://localhost:3100/catalogos/proveedores | Crear un proveedor PRUEBA | Queda con su historial |

### 4. Comercial (CRM)

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/crm | Revisar el dashboard | Ventas vs. meta, despachos, embudo, alertas DOA, tareas vencidas |
| ☐ | http://localhost:3100/crm/clientes | Abrir la ficha de un cliente | Botones de WhatsApp y llamada |
| ☐ | http://localhost:3100/crm/pipeline | Crear una oportunidad PRUEBA y avanzarla hasta «Ganada» (arrastrando o con los botones) | Solo avanza; al ganar crea la venta y su seguimiento de vuelo |
| ☐ | http://localhost:3100/crm/ventas | Revisar la venta creada (exportación) | Formulario de exportación con TRM; ícono de avión hacia Exportaciones |
| ☐ | http://localhost:3100/crm/vuelos | Revisar la torre de vuelos | 6 fases y % de DOA |
| ☐ | http://localhost:3100/crm/tareas | Crear una tarea vencida | Aparece en rojo (semáforo) |
| ☐ | http://localhost:3100/crm/metas | Ajustar una meta del mes | El dashboard la refleja |

### 5. Compras e inventario

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/inventario | Revisar el tablero de bodega | 10 indicadores; gráficos legibles en claro y oscuro |
| ☐ | http://localhost:3100/compras/necesidades | Revisar qué falta comprar | Demanda de clientes vs. stock y tránsito |
| ☐ | http://localhost:3100/compras/ofertas | Registrar la disponibilidad de un proveedor PRUEBA | Queda abierta |
| ☐ | http://localhost:3100/compras/pedidos | Crear un pedido a proveedor y pasarlo a «enviado» | Aparece en Sanidad → Tablero, columna «Por llegar» |
| ☐ | http://localhost:3100/compras/recepciones | Recibir el pedido con algunos enfermos y muertos | Crea los lotes; los muertos van al kardex |
| ☐ | http://localhost:3100/inventario/existencias | Ubicar el lote, trasladar, marcar enfermos, aliviar, registrar mortalidad | Nunca deja stock negativo; cada movimiento va al kardex |
| ☐ | http://localhost:3100/inventario/acuarios | **Registrar el litraje** de los acuarios de prueba | Necesario para las dosis de Sanidad |
| ☐ | http://localhost:3100/inventario/lotes | Abrir la trazabilidad de un lote | Recorrido del lote con aclimatación y cuarentena medidas |
| ☐ | http://localhost:3100/inventario/proveedores | Revisar el cumplimiento de proveedores | Pedido vs. recibido |
| ☐ | http://localhost:3100/inventario/clientes | Revisar los pedidos de clientes pendientes | Pendiente por especie y stock sano disponible |

### 6. Pedidos de clientes (picking, reserva y despacho)

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/crm/pedidos | Crear un pedido PRUEBA y confirmarlo | Queda «confirmado» |
| ☐ | Mismo pedido → **Picking y despacho** | Escribir cantidades por acuario y pulsar **Reservar acuarios** | Aparece «X reservados» en cada acuario |
| ☐ | Otro pedido de la misma especie | Intentar despachar lo que está reservado para el primero | La API lo rechaza: «quedan N libres… reservados para otros pedidos» |
| ☐ | Primer pedido → **Despachar** | Despachar parte de lo reservado | El stock baja y la reserva queda en lo que falta; al completar, el pedido pasa a «despachado» |

### 7. Exportaciones

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/exportaciones/configuracion | Cargar datos bancarios, firmante y logo | Salen en los PDF |
| ☐ | http://localhost:3100/exportaciones | «Nuevo embarque» desde la venta de exportación PRUEBA | Hereda cliente, comercial, aerolínea y AWB |
| ☐ | Ficha del embarque, paso **1 Pedido y preinvoice** | Agregar especies, vuelo (con hora) y cajas; ver el PDF; enviarlo por correo | Preinvoice con anticipo; el correo queda en la carpeta de correos |
| ☐ | Pasos **2 Flete** y **3 Orden de cargue** | Ver los PDF | Flete con desglose; orden de cargue en español con las cajas «13 – 14» |
| ☐ | Paso **4 Empaque** (como Bodega) | Registrar caja por caja y marcar «empacado» | Packing list en PDF; arriba, el costo de empaque y los avisos de fuera de estándar |
| ☐ | http://localhost:3100/exportaciones/empaque | Definir el estándar de una especie (bolsa, peces, Stress Guard); en Costos, cargar litros y costos | La especie queda «por revisar»; el costo del despacho incluye el aditivo y deja de listar faltantes |
| ☐ | Paso **5 Factura** | Cajas reales y valor del AWB; **Emitir** | Factura n.º 12187 (formato del cliente); la venta y la torre de vuelos se actualizan solas |
| ☐ | Pasos **6 DOA** y **7 Balance** | Registrar muertos y el anticipo | Nota crédito y balance en PDF; el DOA llega a la torre de vuelos |

### 8. Sanidad

| ✔ | Ruta | Qué hacer | Resultado esperado |
|---|---|---|---|
| ☐ | http://localhost:3100/sanidad/medicamentos | Revisar los 13 medicamentos y su fuente; fijar un tiempo de retiro | La dosis de referencia se lee clara («0,6 mL por cada 100 L») |
| ☐ | http://localhost:3100/sanidad/alimentacion | Crear un alimento y una dieta para un grupo (p. ej. Corydoras) | La ronda de alimentación la usa |
| ☐ | http://localhost:3100/sanidad/protocolos | Crear un protocolo PRUEBA con días y pasos; **Aprobar** (requiere el permiso de aprobar) | Sin días no deja aprobar; editarlo lo devuelve a borrador |
| ☐ | http://localhost:3100/sanidad/tratamientos | **Nuevo tratamiento** en un acuario con litraje, desde el protocolo aprobado | Dosis calculada (p. ej. Levamisol en 80 L = 0,48 mL) |
| ☐ | http://localhost:3100/sanidad | **Mi día** en el celular: Aplicado / No se hizo; Alimentado | La dosis aparece calculada; «No se hizo» exige un motivo; el acuario en tratamiento sale en **rojo** |
| ☐ | Pedido del paso 6 | Intentar reservar o despachar el acuario en tratamiento | Lo rechaza: «… está en tratamiento: se puede despachar desde el …» |
| ☐ | Tratamientos → ficha | Registrar todo y **Terminar** | Sale de cuarentena cuando pasa la fecha de liberación |
| ☐ | Embarque con vuelo dentro de **menos de 48 h** y acuario reservado | Abrir el tablero y Mi día | El acuario pasa a «Ayuno pre-vuelo» con **PROHIBIDO ALIMENTAR** y la cuenta regresiva |
| ☐ | http://localhost:3100/sanidad/tablero | En escritorio, arrastrar un acuario estable a «Cuarentena»; en el celular, usar los chips | Arrastrar abre «Iniciar protocolo»; no deja soltar donde no corresponde |
| ☐ | http://localhost:3100/sanidad/fichas | Completar la ficha de una especie (pH, temperatura, ayuno al llegar) | Queda «Con ficha» |
| ☐ | http://localhost:3100/sanidad/configuracion | Intentar poner 24 h de ayuno pre-vuelo; asignar responsables | No acepta menos de 48 h; el responsable aparece en el tablero |

## B3. Revisión de diseño (en cada pantalla anterior)

| ✔ | Qué revisar |
|---|---|
| ☐ | A ~390 px de ancho (celular) nada se sale de la pantalla y los botones se pueden tocar con el dedo |
| ☐ | En tema oscuro todo se lee (textos, etiquetas de color, gráficos) |
| ☐ | Los diálogos se pueden desplazar y cerrar en el celular |
| ☐ | Las tablas esconden columnas secundarias en el celular sin perder las acciones |

## B4. Al terminar las pruebas

| ✔ | Qué hacer |
|---|---|
| ☐ | Anotar aquí lo que falló (ruta, usuario, qué pasó) |
| ☐ | Borrar los datos marcados PRUEBA (o pedirlo para hacerlo por base de datos) |
| ☐ | Antes de producción: respaldo de la base, reimportar BASES y revisar `docs/arquitectura/05-despliegue-vps.md` |

### Hallazgos de la prueba

| Ruta | Usuario | Qué pasó | Gravedad |
|---|---|---|---|
| | | | |
| | | | |
