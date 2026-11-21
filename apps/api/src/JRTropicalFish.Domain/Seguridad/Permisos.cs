namespace JRTropicalFish.Domain.Seguridad;

/// <summary>
/// Catálogo maestro de permisos, en formato <c>modulo:accion</c>. Es la única fuente de verdad:
/// se siembra en la base por migración, se usa en <c>[RequierePermiso]</c> y el sitio web tiene un
/// espejo en <c>apps/web/src/lib/permisos.ts</c>.
///
/// <para>
/// Al agregar un módulo: (1) constantes aquí, (2) entrada en <see cref="Todos"/>, (3) asignación en
/// <see cref="PerfilesPlantilla"/>, (4) migración nueva cuyo <c>Up</c> llame a
/// <c>SembradoPermisos.Aplicar(migrationBuilder)</c>, (5) espejo en el web. Ver CLAUDE.md, «Agregar un permiso».
/// </para>
/// </summary>
public static class Permisos
{
    public const string UsuariosLeer = "usuarios:leer";
    public const string UsuariosCrear = "usuarios:crear";
    public const string UsuariosEditar = "usuarios:editar";
    public const string UsuariosEliminar = "usuarios:eliminar";

    public const string PerfilesLeer = "perfiles:leer";
    public const string PerfilesCrear = "perfiles:crear";
    public const string PerfilesEditar = "perfiles:editar";
    public const string PerfilesEliminar = "perfiles:eliminar";

    public const string MaestrosLeer = "maestros:leer";
    public const string MaestrosEditar = "maestros:editar";

    public const string EmpleadosLeer = "empleados:leer";
    public const string EmpleadosCrear = "empleados:crear";
    public const string EmpleadosEditar = "empleados:editar";
    public const string EmpleadosEliminar = "empleados:eliminar";

    /// <summary>Ver y editar el salario. Sin este permiso el salario no viaja en ninguna respuesta.</summary>
    public const string EmpleadosVerSalario = "empleados:ver-salario";

    // --- CRM comercial ---
    public const string ClientesLeer = "clientes:leer";
    public const string ClientesEditar = "clientes:editar";

    /// <summary>Ver pipeline, ventas, vuelos y tareas. Un comercial sin <see cref="CrmVerTodo"/> solo ve lo suyo.</summary>
    public const string CrmLeer = "crm:leer";

    /// <summary>
    /// Marca al usuario como comercial: aparece en los selectores y en las metas, y crea y gestiona
    /// oportunidades y tareas.
    /// </summary>
    public const string CrmVender = "crm:vender";

    /// <summary>Un comercial con este permiso ve y gestiona lo de todos los comerciales (gerencia).</summary>
    public const string CrmVerTodo = "crm:ver-todo";

    public const string VentasGestionar = "ventas:gestionar";
    public const string TrackingGestionar = "tracking:gestionar";
    public const string MetasLeer = "metas:leer";
    public const string MetasEditar = "metas:editar";

    // --- Catálogos ---
    /// <summary>Consultar productos, centros de acopio y grupos.</summary>
    public const string CatalogosLeer = "catalogos:leer";
    public const string CatalogosEditar = "catalogos:editar";

    /// <summary>Ver y editar las listas de costo (costo de compra). Sin él, no viajan en ninguna respuesta.</summary>
    public const string CatalogosVerCostos = "catalogos:ver-costos";

    public const string PreciosLeer = "precios:leer";

    /// <summary>Editar precios, listas, sus reglas y parámetros, y recalcular en bloque.</summary>
    public const string PreciosEditar = "precios:editar";

    public const string ProveedoresLeer = "proveedores:leer";
    public const string ProveedoresEditar = "proveedores:editar";

    // --- Inventario, compras y pedidos de cliente ---
    /// <summary>Existencias, lotes, acuarios, movimientos, dashboard, ofertas y pedidos.</summary>
    public const string InventarioLeer = "inventario:leer";

    /// <summary>Ubicar, trasladar y registrar novedades (enfermo, aliviado, mortalidad).</summary>
    public const string InventarioOperar = "inventario:operar";

    /// <summary>Ajustes manuales con motivo, carga de saldo inicial y catálogo de acuarios.</summary>
    public const string InventarioAjustar = "inventario:ajustar";

    public const string ComprasGestionar = "compras:gestionar";
    public const string ComprasRecibir = "compras:recibir";
    public const string PedidosGestionar = "pedidos:gestionar";
    public const string PedidosDespachar = "pedidos:despachar";

    // --- Exportaciones: documentos comerciales del despacho ---
    /// <summary>Consultar embarques y descargar sus documentos.</summary>
    public const string ExportacionesLeer = "exportaciones:leer";

    /// <summary>Preinvoice, flete, factura, nota crédito y balance; enviar documentos al cliente.</summary>
    public const string ExportacionesGestionar = "exportaciones:gestionar";

    /// <summary>Registrar el empaque real caja por caja (bodega).</summary>
    public const string ExportacionesEmpacar = "exportaciones:empacar";

    /// <summary>Datos de la empresa, tipos de caja y bolsa, cargos y tarifas de flete.</summary>
    public const string ExportacionesConfigurar = "exportaciones:configurar";

    // Módulos de negocio (Catálogos, Inventario, Importaciones, Exportaciones): sus permisos se
    // agregan con cada módulo. El mapa previsto está en docs/dominio/modulos.md.

    /// <summary>Todos los permisos vigentes, con su descripción para la pantalla de perfiles.</summary>
    public static readonly IReadOnlyDictionary<string, string> Todos = new Dictionary<string, string>
    {
        [UsuariosLeer] = "Consultar usuarios",
        [UsuariosCrear] = "Crear usuarios",
        [UsuariosEditar] = "Editar usuarios",
        [UsuariosEliminar] = "Desactivar y reactivar usuarios",
        [PerfilesLeer] = "Consultar perfiles",
        [PerfilesCrear] = "Crear perfiles",
        [PerfilesEditar] = "Editar perfiles",
        [PerfilesEliminar] = "Eliminar perfiles",
        [MaestrosLeer] = "Consultar maestros",
        [MaestrosEditar] = "Crear, editar y eliminar valores de maestros",
        [EmpleadosLeer] = "Consultar empleados",
        [EmpleadosCrear] = "Crear empleados",
        [EmpleadosEditar] = "Editar empleados",
        [EmpleadosEliminar] = "Desactivar y eliminar empleados",
        [EmpleadosVerSalario] = "Ver y editar el salario de los empleados",
        [ClientesLeer] = "Consultar el directorio de clientes",
        [ClientesEditar] = "Crear, editar y eliminar clientes",
        [CrmLeer] = "Consultar pipeline, ventas, vuelos y tareas",
        [CrmVender] = "Ser comercial: gestionar sus oportunidades y tareas",
        [CrmVerTodo] = "Ver y gestionar lo de todos los comerciales",
        [VentasGestionar] = "Registrar y editar ventas (pago, preparación, guía)",
        [TrackingGestionar] = "Editar el seguimiento de vuelos y el DOA",
        [MetasLeer] = "Ver el dashboard comercial, metas y comisiones",
        [MetasEditar] = "Editar metas y parámetros comerciales",
        [CatalogosLeer] = "Consultar el catálogo de productos y centros de acopio",
        [CatalogosEditar] = "Crear y editar productos y centros de acopio",
        [CatalogosVerCostos] = "Ver y editar el costo de compra de los productos",
        [PreciosLeer] = "Consultar las listas de precios",
        [PreciosEditar] = "Editar precios, listas de precios y recalcular",
        [ProveedoresLeer] = "Consultar proveedores",
        [ProveedoresEditar] = "Crear y editar proveedores",
        [InventarioLeer] = "Consultar existencias, lotes, acuarios, ofertas y pedidos",
        [InventarioOperar] = "Ubicar, trasladar y registrar enfermos, aliviados y muertos",
        [InventarioAjustar] = "Ajustes de inventario, saldo inicial y acuarios",
        [ComprasGestionar] = "Registrar disponibilidad de proveedores y pedidos a proveedor",
        [ComprasRecibir] = "Recibir pedidos de proveedor (crea los lotes)",
        [PedidosGestionar] = "Crear y editar pedidos de clientes",
        [PedidosDespachar] = "Picking y despacho de pedidos de clientes",
        [ExportacionesLeer] = "Consultar embarques de exportación y sus documentos",
        [ExportacionesGestionar] = "Preinvoice, factura, nota crédito y envío de documentos al cliente",
        [ExportacionesEmpacar] = "Registrar el empaque real de un embarque (cajas y bolsas)",
        [ExportacionesConfigurar] = "Datos de la empresa, cajas, bolsas, cargos y tarifas de flete",
    };

    /// <summary>
    /// Códigos que existieron y ya no: el sembrado los borra de <c>permisos</c> y
    /// <c>perfil_permisos</c>. Necesario porque el sembrado es de solo inserción.
    /// </summary>
    public static readonly IReadOnlyList<string> Retirados = [];
}

/// <summary>Perfiles sembrados por migración. Editables, pero no eliminables.</summary>
public static class PerfilesPlantilla
{
    public const string Administrador = "Administrador";
    public const string Operativo = "Operativo";
    public const string Consulta = "Consulta";
    public const string Comercial = "Comercial";
    public const string Bodega = "Bodega";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Definicion =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Administrador] = [.. Permisos.Todos.Keys],
            // Operativo: logística y despachos (no es comercial, así que ve todas las ventas).
            [Operativo] =
            [
                Permisos.ClientesLeer, Permisos.CrmLeer, Permisos.VentasGestionar, Permisos.TrackingGestionar,
                Permisos.MetasLeer, Permisos.CatalogosLeer, Permisos.PreciosLeer, Permisos.ProveedoresLeer,
                Permisos.InventarioLeer, Permisos.InventarioOperar, Permisos.ComprasGestionar, Permisos.ComprasRecibir,
                Permisos.PedidosDespachar, Permisos.ExportacionesLeer, Permisos.ExportacionesGestionar, Permisos.ExportacionesEmpacar,
            ],
            [Consulta] =
            [
                Permisos.UsuariosLeer, Permisos.PerfilesLeer, Permisos.MaestrosLeer, Permisos.EmpleadosLeer,
                Permisos.ClientesLeer, Permisos.CrmLeer, Permisos.MetasLeer, Permisos.CatalogosLeer, Permisos.PreciosLeer,
                Permisos.ProveedoresLeer, Permisos.InventarioLeer, Permisos.ExportacionesLeer,
            ],
            // Comercial: ve y gestiona solo lo suyo (sin crm:ver-todo).
            [Comercial] =
            [
                Permisos.ClientesLeer, Permisos.ClientesEditar, Permisos.CrmLeer, Permisos.CrmVender, Permisos.MetasLeer,
                Permisos.CatalogosLeer, Permisos.PreciosLeer, Permisos.InventarioLeer, Permisos.PedidosGestionar,
                Permisos.ExportacionesLeer, Permisos.ExportacionesGestionar,
            ],
            // Bodega (rol del Excel de inventario): recibe, ubica, registra novedades y despacha.
            [Bodega] =
            [
                Permisos.CatalogosLeer, Permisos.InventarioLeer, Permisos.InventarioOperar, Permisos.ComprasRecibir,
                Permisos.PedidosDespachar, Permisos.ExportacionesLeer, Permisos.ExportacionesEmpacar,
            ],
        };
}
