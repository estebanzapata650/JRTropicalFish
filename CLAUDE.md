# CLAUDE.md

Guía principal para trabajar en este repositorio. Léela completa antes de cambiar código. El detalle de
cada tema está en `docs/`; aquí va lo necesario para no romper nada.

## Estado del proyecto

**Esqueleto de infraestructura listo y verificado de punta a punta** (2026-11-18):

- Monorepo con API .NET 10 (4 capas + tests) y web Next.js 16. `npm run dev` levanta todo.
- Login con JWT en cookie httpOnly, `/auth/yo`, menú filtrado por permisos, cierre de sesión.
- **Módulo de Seguridad completo** (2026-11-18): pantallas de **Usuarios** (crear, editar, restablecer
  contraseña, activar/desactivar, desbloquear, historial) y **Perfiles** (crear, editar permisos por
  módulo, eliminar, historial), y **Mi cuenta** (cambiar la propia contraseña). Bloqueo tras 5 intentos
  fallidos (15 min), revocación inmediata de sesiones, auditoría de todo cambio y salvaguarda de "último
  administrador". Detalle en `docs/arquitectura/02-backend.md#autenticación-y-autorización`.
- **Administración** (2026-11-20): pantallas de **Empleados** (datos básicos, contacto, laborales, foto,
  salario protegido por `empleados:ver-salario`) y **Maestros** genéricos (tipos de identificación, cargos,
  áreas, tipos de contrato; una tabla por maestro, un solo juego de funciones). Primer endpoint de archivos
  (foto). Detalle en `docs/dominio/modulos.md` y `docs/arquitectura/02-backend.md#maestros`.
- **CRM comercial** (2026-11-21): Dashboard ejecutivo (ventas vs meta, despachos, clientes nuevos,
  pool de comisiones, embudo, alertas DOA, tareas vencidas), **Pipeline** Kanban (solo avanza; Ganar
  crea venta + tracking), **Ventas** (formulario dinámico nacional/exportación, TRM por venta),
  **Torre de vuelos** (6 fases, % DOA generado), **Tareas** Kanban (semáforo de vencidas),
  **Clientes** con ficha (WhatsApp/llamada) y **Metas**. 53 clientes importados del Excel con
  `npm run crm:importar-clientes`. Reglas en `docs/dominio/crm.md`.
- **Catálogos** (2026-11-21): **Productos** (794, código por grupo, tipo, talla, bolsa, disponibilidad
  publicada, centros de origen), **Listas de precios** (8 listas con regla deducida del Excel,
  recálculo en bloque; costo protegido por `catalogos:ver-costos`), **Centros de acopio**, **Proveedores**
  y maestros *Grupos de producto* y *Cuencas*. Importados con `npm run catalogo:importar`. Reglas en
  `docs/dominio/catalogos.md`.
- **Inventario, compras y pedidos de clientes** (2026-11-21): migrada la app de AppSheet del Excel
  «BASES». Dashboard (10 indicadores), **Existencias** (ubicar, trasladar, enfermo, aliviado, mortalidad),
  **Lotes** con kardex, **Acuarios** (mapa), **Necesidades de compra**, **Disponibilidad de proveedores**,
  **Pedidos a proveedor** con **Recepción** (crea lotes) y **Pedidos de clientes** enlazados a la venta del
  CRM con picking y despacho. Saldo inicial (63 lotes, 13.528 peces) con `npm run inventario:importar`.
  **Tableros** (reemplazan Looker): Bodega, Proveedores, Clientes y Trazabilidad con el recorrido del lote
  (aclimatación y cuarentena medidas del kardex). Gráficos en `components/Graficos.tsx` (CSS + tokens
  `serie-*`, sin librería). Reglas en `docs/dominio/inventario.md`.
- **Exportaciones** (2026-11-21): embarque por venta de exportación con los documentos del libro «Formatos
  Comerciales»: preinvoice, flete, orden de cargue, empaque por caja, factura (JRCH/V2, n.º desde 12187),
  DOA → nota crédito y balance, en PDF y por correo; sincroniza venta, tracking y pedido de cliente.
  Configuración de empresa/logo/banco, cajas, bolsas, cargos y tarifas. Documentos para autoridades (AUNAP,
  aeropuerto, aduana, Siigo) **pendientes con gerencia**. Reglas en `docs/dominio/exportaciones.md`.
- **AUNAP** (2026-11-23): listado de la Resolución 1924 de 2015 (598 especies, `npm run aunap:importar`), código
  AUNAP por producto (Exportaciones → AUNAP), permiso por embarque con la comparación pedido / autorizado /
  empacado, y **solicitud AUNAP, packing list de aduana y custom invoice** generados **solo desde lo real**. No hay
  factores ni sustitución de especies; una especie sin código no se declara.
- **Buscador en los formularios** (`components/SelectorBuscable.tsx`): toda lista de registros (clientes,
  proveedores, productos, acuarios, usuarios…) filtra mientras se escribe, por texto o por iniciales.
- **Sanidad** (2026-11-22, fase 1): **Mi día** del operario (llegadas con profilaxis sugerida, medicación con
  la dosis calculada por litraje, ronda de alimentación con semáforo verde/rojo, salidas), **Tablero de acuarios**
  (Kanban: por llegar, aclimatación, cuarentena, estable, ayuno pre-vuelo), **Tratamientos**, **Protocolos**
  (solo los aprobados por el veterinario se sugieren), **Medicamentos** (13 de los reportes AQUAVET),
  **Alimentos y dietas**, **Fichas técnicas** y **Reservas de despacho** en el picking. Ayuno firme de 48 h antes
  del vuelo; acuario en tratamiento o retiro no se reserva ni despacha (`JR004`). Fase 2: planificador semanal e
  importación del PDF de fichas. Reglas en `docs/dominio/sanidad.md`.
- Transversales listos para usar: auditoría (`auditoria` + `ComparadorAuditoria`), archivos S3
  (`IAlmacenArchivos`, `ClaveArchivo`, `DetectorTipoArchivo`), PDF (`IGeneradorInformePdf`), correo
  (`IServicioCorreo`, proveedores `Disco` / `SendGrid`, modo pruebas), `Resultado<T>`, `IReloj`.
- Health check con Postgres en `/api/v1/health`. 210 pruebas unitarias en verde.
- Producción: `docker-compose.prod.yml` (Caddy, web, api, postgres, backup), Dockerfiles, `.env.prod.example`.

**Pendiente (por fases, ver `docs/dominio/modulos.md`)**: resto de Catálogos (aerolíneas…), Inventario (conteos, ajustes con aprobación, PDFs), Sanidad fase 2 (planificador semanal, importar fichas), Importaciones,
Exportaciones (documentos AUNAP/aduana pendientes con gerencia), Reportes. Recuperación de contraseña por correo. CI/CD (fase 2).

## Descripción del proyecto

JR Tropical Fish compra, mantiene y vende peces tropicales vivos. El sistema cubre:

- **Inventario**: stock vivo por especie, talla, lote y tanque/ubicación; movimientos (entradas, salidas,
  traslados, ajustes, mortalidad); cuarentena.
- **Importaciones**: compras a proveedores del exterior, documentos (factura, certificados, CITES, guía
  aérea), recepción con conteo de DOA, cuarentena, costos de nacionalización.
- **Exportaciones**: pedidos de clientes del exterior, proformas, reserva de stock, packing list, factura
  comercial, documentos sanitarios, despacho y reclamos.

Es **una sola empresa** (sin multi-tenant). Glosario en `docs/dominio/glosario.md`.

## Arquitectura

Resumen; detalle en `docs/arquitectura/`.

- **Backend**: ASP.NET Core .NET 10, Clean Architecture: `Domain` ← `Application` ← `Infrastructure` ← `Api`.
  Servicios concretos scoped inyectados en controladores; sin MediatR/AutoMapper.
- **Datos**: PostgreSQL 16, una base `jrtropicalfish`. **EF Core solo para migraciones**; en ejecución
  **Dapper** contra funciones `fn_*` y procedimientos `sp_*` PL/pgSQL creados en migraciones.
- **Frontend**: Next.js 16 como **BFF**. El navegador nunca llama a la API: Server Components / Server
  Actions usan `apiFetch`; componentes de cliente usan el proxy `app/api/[...path]`; binarios por route
  handlers con streaming.
- **Auth**: JWT HMAC-SHA256 propio; claims `perfil` y un `permiso` por permiso (`modulo:accion`).
- **Archivos**: S3 (`ForcePathStyle`), Garage en desarrollo, R2/S3 en producción.
- **PDF**: QuestPDF (licencia Community) detrás de `IGeneradorInformePdf`.
- **Correo**: `Disco` por defecto (no envía), `SendGrid` por configuración.
- **IDs**: UUID v7 (`Guid.CreateVersion7()`). **Hora**: UTC en base; "hoy" de negocio en `America/Bogota`.
- **Producción**: VPS + Docker Compose + Caddy. Migraciones como paso explícito (`--migrar`).

Decisiones registradas en `docs/arquitectura/adr/`.

## Reglas de diseño (web)

Obligatorias:

- **Ninguna pantalla se da por terminada sin verla a ~390 px** con viewport móvil real.
- `min-w-0` en la columna de contenido (ya en `AppShell`); contenido hasta `max-w-7xl`.
- Formularios con `RejillaCampos` (1/2/3 columnas), `CampoAncho`, `Seccion` (plegable solo en móvil, por CSS).
- Espaciado: `gap-1.5` etiqueta→campo, `gap-4` entre campos, `gap-6` entre secciones.
- Tablas reales (`TablaResponsive`), columnas secundarias `hidden md:table-cell`; acciones de fila con
  `BotonIcono` y `aria-label` que incluya el nombre del registro.
- **Todo color sale de un token** de `globals.css` (`bg-superficie`, `text-texto-suave`, `bg-acento`…);
  nunca paleta fija de Tailwind. Tema con clase `dark` en `<html>`; `BotonTema` sin estado de React.
- Overlays de diálogo/drawer con `aria-hidden` y `tabIndex={-1}`.

## Estructura del repositorio

```
JRTropicalFish/
├── CLAUDE.md · README.md
├── package.json              # orquestador: dev, stop, build, test, migraciones, prod:*
├── docker-compose.yml        # DESARROLLO: postgres (5433), garage (3910/3913)
├── docker-compose.prod.yml   # PRODUCCIÓN: caddy, web, api, postgres, backup
├── .env.example · .env.prod.example · .editorconfig · .dockerignore · .gitignore
├── apps/
│   ├── api/
│   │   ├── JRTropicalFish.slnx · Directory.Build.props (net10.0, warnings = errores) · global.json
│   │   ├── src/JRTropicalFish.Domain/
│   │   │   ├── Comun/            EntidadBase, CatalogoSimple
│   │   │   ├── Seguridad/        Usuario, Perfil, Permiso, Permisos (catálogo + PerfilesPlantilla)
│   │   │   ├── Maestros/         TipoIdentificacion, Cargo, Area, TipoContrato (CatalogoSimple)
│   │   │   ├── Administracion/   Empleado
│   │   │   ├── Crm/              Cliente, Oportunidad, Venta, TrackingVuelo, Tarea, Metas; EnumsCrm
│   │   │   ├── Catalogos/        Producto, CentroAcopio, ListaPrecios, PrecioProducto, Proveedor
│   │   │   ├── Exportaciones/    DatosEmpresa, TarifasFlete, TipoCaja, TipoBolsa, CargoExportacion, Embarque (+líneas, empaque)
│   │   │   ├── Inventario/       Acuario, Lote, Existencia, MovimientoInventario, Oferta/PedidoProveedor, Recepcion, PedidoCliente
│   │   │   ├── Sanidad/          Medicamento, Alimento, Dieta, FichaTecnica, Protocolo, Tratamiento, ReservaDespacho
│   │   │   └── Auditoria/        RegistroAuditoria
│   │   ├── src/JRTropicalFish.Application/
│   │   │   ├── Abstractions/     interfaces: repositorios, IReloj, IAlmacenArchivos, IServicioCorreo…
│   │   │   ├── Comun/            Resultado<T>, Error, TipoError
│   │   │   ├── Auth/             AuthService, IJwtTokenService
│   │   │   ├── Archivos/         ClaveArchivo, DetectorTipoArchivo
│   │   │   ├── Auditoria/        ComparadorAuditoria, ComparadorColecciones
│   │   │   └── Informes/         InformeModelo, IGeneradorInformePdf
│   │   ├── src/JRTropicalFish.Infrastructure/
│   │   │   ├── InfrastructureServiceCollectionExtensions.cs   ← registro único de DI
│   │   │   ├── Persistence/      AppDbContext, factory, MigradorBaseDatos, Migrations/, Sembrado/
│   │   │   ├── Repositories/     Dapper
│   │   │   ├── Auth/ · Almacenamiento/ · Correo/ · Informes/ · Comun/
│   │   ├── src/JRTropicalFish.Api/
│   │   │   ├── Program.cs · Controllers/ · Auth/ (RequierePermiso) · Comun/ (ResultadoHttp) · Startup/
│   │   └── tests/JRTropicalFish.Tests/
│   └── web/
│       └── src/
│           ├── proxy.ts          # redirige a /login sin cookie (Next 16: antes middleware.ts)
│           ├── app/              # login, logout, api/[...path], (dashboard)/...
│           ├── components/       # AppShell, PageHeader, LoginForm, campos, TemaProvider
│           └── lib/              # api, session, permisos, navegacion, estilos
├── infra/  docker/ · caddy/ · garage/ · backup/
├── scripts/  stop.mjs · s3-init.mjs · importar-clientes.mjs · importar-catalogo.mjs · importar-inventario.mjs · lib/xlsx.mjs (Excel → API)
└── docs/  arquitectura/ (+ adr/) · dominio/ · design-thinking/ · temp/ (ignorado)
```

## Comandos

```bash
cp .env.example .env               # una vez
npm install && npm install --prefix apps/web
npm run dev                        # todo el stack; API aplica migraciones y siembra admin
npm run stop

npm test                           # dotnet test
npm run lint                       # eslint web
npm run build                      # API + web

# Migraciones (solo DDL; ver "Acceso a datos")
npm run migracion:nueva -- NombreDeLaMigracion
dotnet ef migrations remove --project apps/api/src/JRTropicalFish.Infrastructure --startup-project apps/api/src/JRTropicalFish.Api

# Producción (en el VPS)
npm run prod:build && npm run prod:migrar && npm run prod:up
```

### Desarrollo local

| Qué | Valor |
|---|---|
| Web | http://localhost:3100 |
| API / Scalar | http://localhost:5377 · http://localhost:5377/scalar/v1 |
| Postgres | `localhost:5433`, base/usuario/clave `jrtropicalfish` |
| Garage S3 | `http://localhost:3910`, bucket `jr-documentos` (credenciales en `.env.example`) |
| Usuario sembrado | `admin@jrtropicalfish.local` / `Admin123!` (solo Development) |
| Correos | `apps/api/src/JRTropicalFish.Api/bin/.../correos-enviados/` (proveedor Disco) |

## Guía para trabajo futuro

### Agregar un módulo

Seguir la receta de `docs/arquitectura/02-backend.md#agregar-un-módulo-receta`. En resumen: entidades y
permisos (Domain) → `AppDbContext` + migración de esquema **revisada a mano** → migración de funciones PL/pgSQL
+ `SembradoPermisos.Aplicar` → interfaz + servicio (Application) → repositorio Dapper + registro en
`AddInfrastructure()` → controlador con `[RequierePermiso]` → pruebas → permisos, menú y páginas en el web.

### Agregar un permiso

1. Constante en `Domain/Seguridad/Permisos.cs` y entrada en `Permisos.Todos`.
2. Asignarlo en `PerfilesPlantilla.Definicion` (Operativo/Consulta según corresponda; Administrador los recibe todos).
3. Migración nueva cuyo `Up` llame a `SembradoPermisos.Aplicar(migrationBuilder)`. Los usuarios de los perfiles
   que reciben o pierden permisos quedan con la sesión revocada: vuelven al login y entran con los permisos al día.
4. Espejo en `apps/web/src/lib/permisos.ts` y uso en `lib/navegacion.ts`.
5. Retirar uno: moverlo a `Permisos.Retirados` y volver a sembrar.

Separar "registrar" de "aprobar/cerrar" cuando las responsabilidades difieren (ej. `inventario:ajustar` vs
`inventario:aprobar-ajuste`).

### Acceso a datos (trampas conocidas)

- **Revisar siempre la migración que autogenera EF Core** antes de aplicarla (borrado de columnas antes de
  migrar datos, `0001-01-01` en fechas, columnas vacías antes de índices únicos, `Cascade` por defecto).
- FK con `OnDelete(Restrict)`; tablas puente con `Cascade`.
- **En un `CALL`, castear todos los parámetros** (`@X::uuid`, `@Y::jsonb`): un nulo sin cast hace que
  Postgres diga que el procedimiento no existe.
- En `RETURNS TABLE`, devolver `varchar` como `::text`.
- **Dapper construye records por posición**: el orden de columnas del `SELECT` = orden del constructor, **y el
  nombre de cada columna debe coincidir con el del parámetro** (`incompatibles_texto` ↔ `IncompatiblesTexto`) y su
  tipo ser exacto: un `uuid[]` no mapea a `Guid[]` (devolverlo como texto y convertir en C#).
- Al renombrar/eliminar una columna, **recrear en la misma migración** toda función que la use (Postgres no
  valida hasta ejecutar). `CREATE OR REPLACE` no cambia `RETURNS TABLE`: `DROP` + `CREATE`.
- Dapper no mapea `DateOnly` sin `DateOnlyTypeHandler` (ya registrado).
- Sembrados idempotentes (`ON CONFLICT DO NOTHING`); nunca sobrescribir datos editados por el negocio.
- Borrado lógico con índice único parcial (`HasFilter("eliminado_en IS NULL")`).
- **Normalizar en C# igual que en SQL** (ej. `lower(email)`), o la auditoría registra cambios falsos.
- Transiciones de estado (etapa, fase, estado de tarea): "comparar y escribir" con `SQLSTATE JR001` →
  409 (ver `docs/arquitectura/02-backend.md#endpoints-del-crm`). Enums del CRM: texto + `CHECK`,
  constantes en `Domain/Crm/EnumsCrm.cs` y etiquetas en `web/src/lib/crm.ts`.
- Datos reservados (salario, costo de compra): sin el permiso no viajan en ninguna respuesta ni en el
  historial, y fijarlos da 403. Seguir el patrón de `EmpleadoService` / `ProductoService`.
- Si la API del usuario está corriendo, compilar con `-c Release` (otra carpeta) en vez de detenerla.
- Inventario: toda operación que descuente stock debe bloquear la fila (`SELECT ... FOR UPDATE`) dentro del
  procedimiento y validar que no quede negativo. Nunca calcular el stock en C# y escribirlo. Ya existe la
  primitiva `sp_inventario_mover_uno` / `sp_inventario_mover` (`JR002` sin stock, `JR003` despacho de más):
  usarla en vez de escribir `existencias` a mano. Lo reservado (`reservas_despacho`) no mueve stock: lo libre es
  sano − `fn_reservado(...)`; un acuario con `fn_acuario_liberacion(...)` no nula no se reserva ni despacha (`JR004`).

### API

- Errores de negocio con `Resultado<T>` + `ResultadoHttp.ARespuesta()`; siempre `{ error }`.
- Todo endpoint exige sesión por defecto; lo público lleva `[AllowAnonymous]` explícito.
- **Revocación de sesiones**: cada petición valida el token contra `usuarios.sesion_invalida_antes`
  (`Api/Auth/ValidacionSesion.cs` → `fn_usuario_sesion_valida`). Cualquier operación que cambie lo que un
  usuario puede hacer (desactivarlo, cambiar su contraseña, cambiarle el perfil, cambiar permisos o estado
  de su perfil) **debe mover esa marca**; si no, el usuario conserva los permisos viejos hasta que el
  token expire (8 h). Al agregar una operación así, seguir el patrón de `sp_usuario_actualizar` /
  `sp_perfil_actualizar`.
- La auditoría se escribe con `ServicioAuditoria` (`CreacionAsync`, `CambiosAsync`, `EliminacionAsync`).
  Se audita una proyección legible (`UsuarioAuditable` con el *nombre* del perfil, no su id) y nunca
  contraseñas ni hashes: solo "Contraseña: cambiada".
- Regla del "último administrador": ninguna operación puede dejar el sistema sin un usuario activo con
  `usuarios:editar` en un perfil activo (`fn_administradores_contar`).
- Los perfiles base (`es_plantilla`) no se renombran ni se eliminan: el sembrado de migraciones y el
  seeder los ubican por nombre.
- Usar `IReloj` en vez de `DateTime.Now` en reglas.
- `TreatWarningsAsErrors` está activo: un warning rompe el build.

### Archivos y PDF

- Nunca aceptar un archivo por extensión o `Content-Type`: `DetectorTipoArchivo` (magic bytes) y la clave
  sale de `ClaveArchivo` (sin datos del usuario). Máximo 10 MB.
- S3 autoalojado: `UseChunkEncoding = false` (ya en `AlmacenArchivosS3`).
- PDF: el módulo arma un `InformeModelo`; nunca usar QuestPDF fuera de `GeneradorInformePdfQuestPdf`.
  En Linux necesita `libfontconfig1` (ya en `api.Dockerfile`).

### Web

- Desde un componente de cliente, el proxy se llama **`/api/<ruta>` sin `v1`** (`/api/compras/pedidos/…`):
  el proxy ya antepone `/api/v1/`. Con `/api/v1/…` responde 404. `apiFetch` (servidor) sí usa `/api/v1/…`.
- Un diálogo que nace abierto (`?id=` en la URL) es seguro: `Dialogo` no pinta hasta hidratar.
- Binarios por route handlers con streaming, nunca por el proxy `app/api/[...path]` (hace `text()`).
- Subidas por Server Action + `apiUpload` sin fijar `Content-Type`; límite de Server Actions en
  `next.config.ts` (12 MB).
- Constantes de clases en `lib/estilos.ts`, no en `components/campos.tsx` (`"use client"`).
- Para elegir un registro (cliente, proveedor, producto, acuario, usuario…) usar `SelectorBuscable`, no `<select>`;
  el `<select>` queda para listas cortas de valores fijos (estado, moneda). Con FormData: `valorInicial` + `name`.
- Un Server Component solo pasa a un cliente datos serializables o Server Actions (no funciones normales).
- React: nada de `setState` durante el render ni en `useEffect` para reaccionar a una acción; usar
  `useTransition` y cerrar/refrescar en el handler.
- "Se ve pero ningún botón funciona" = no hidrató: revisar 403 en `/_next/static` (abrir `next dev` por IP
  requiere `allowedDevOrigins`, ya automático en `next.config.ts`).
- Next.js 16 tiene cambios incompatibles: ver `apps/web/AGENTS.md` y `node_modules/next/dist/docs/`.

### Correo

- Proveedor por defecto `Disco` (no envía). SendGrid exige dominio autenticado: sin eso responde 202 y no
  entrega. `ResultadoCorreo.LlegoAlDestinatario` distingue "aceptado" de "llegó al destinatario real".
- `Correo:RedirigirTodoA` activa el modo pruebas (todo el correo a una dirección).

### Infraestructura

- Puertos de desarrollo distintos a ProyectoTransporte (3100/5377/5433/3910) para correr ambos.
- `api.Dockerfile` copia `JRTropicalFish.slnx` (no `*.sln`): la referencia tenía ese bug.
- La API no se expone en producción; solo Caddy publica 80/443.
- Antes de un despliegue con migraciones, respaldo manual. Guía completa: `docs/arquitectura/05-despliegue-vps.md`.
