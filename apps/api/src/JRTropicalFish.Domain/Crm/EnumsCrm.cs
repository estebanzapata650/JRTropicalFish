namespace JRTropicalFish.Domain.Crm;

/// <summary>
/// Valores válidos de los campos tipo lista del CRM. Se guardan como texto con un <c>CHECK</c> en la
/// base (no como maestros) porque las reglas dependen de ellos: la máquina de estados del pipeline,
/// el formulario dinámico de ventas, el semáforo de tareas. Las etiquetas para el usuario
/// ("1. Prospección") viven en el web (<c>apps/web/src/lib/crm.ts</c>).
///
/// <para>Si se agrega un valor: constante aquí, <c>CHECK</c> en una migración y etiqueta en el web.</para>
/// </summary>
public static class TiposCliente
{
    public const string B2BInternacional = "b2b_internacional";
    public const string LocalColombia = "local_colombia";
    public const string Ecommerce = "ecommerce";

    public static readonly IReadOnlyList<string> Todos = [B2BInternacional, LocalColombia, Ecommerce];
}

public static class LicenciasImportacion
{
    public const string Activa = "activa";
    public const string EnTramite = "en_tramite";
    public const string Vencida = "vencida";
    public const string NoAplica = "no_aplica";

    public static readonly IReadOnlyList<string> Todos = [Activa, EnTramite, Vencida, NoAplica];
}

public static class EstadosCliente
{
    public const string Activo = "activo";
    public const string Inactivo = "inactivo";
    public const string Prospecto = "prospecto";

    public static readonly IReadOnlyList<string> Todos = [Activo, Inactivo, Prospecto];
}

public static class TiposVenta
{
    public const string Exportacion = "exportacion";
    public const string Nacional = "nacional";

    public static readonly IReadOnlyList<string> Todos = [Exportacion, Nacional];
}

public static class Monedas
{
    public const string Usd = "USD";
    public const string Cop = "COP";

    public static readonly IReadOnlyList<string> Todos = [Usd, Cop];
}

/// <summary>
/// Etapas del pipeline, en orden. Las cuatro primeras son activas; <see cref="Ganada"/> y
/// <see cref="Perdida"/> son terminales. Las transiciones válidas las decide <c>ReglasPipeline</c>.
/// </summary>
public static class EtapasPipeline
{
    public const string Prospeccion = "prospeccion";
    public const string Stocklist = "stocklist";
    public const string PreInvoice = "pre_invoice";
    public const string AprobacionVuelo = "aprobacion_vuelo";
    public const string Ganada = "ganada";
    public const string Perdida = "perdida";

    /// <summary>En orden de avance. <see cref="Perdida"/> va al final pero no es "posterior" a nada.</summary>
    public static readonly IReadOnlyList<string> Todos = [Prospeccion, Stocklist, PreInvoice, AprobacionVuelo, Ganada, Perdida];

    public static bool EsTerminal(string etapa) => etapa is Ganada or Perdida;
}

public static class OrigenesCanal
{
    public const string B2BDirecto = "b2b_directo";
    public const string WebEcommerce = "web_ecommerce";
    public const string WhatsappDirecto = "whatsapp_directo";
    public const string MostradorBodega = "mostrador_bodega";

    public static readonly IReadOnlyList<string> Todos = [B2BDirecto, WebEcommerce, WhatsappDirecto, MostradorBodega];
}

public static class EstadosPago
{
    public const string Pendiente = "pendiente";
    public const string Anticipo = "anticipo";
    public const string Pagado = "pagado";

    public static readonly IReadOnlyList<string> Todos = [Pendiente, Anticipo, Pagado];
}

public static class EstadosPreparacion
{
    public const string PendientePicking = "pendiente_picking";
    public const string EnEmpaque = "en_empaque";
    public const string ListoDespacho = "listo_despacho";
    public const string Entregado = "entregado";

    public static readonly IReadOnlyList<string> Todos = [PendientePicking, EnEmpaque, ListoDespacho, Entregado];
}

/// <summary>Fases logísticas de un despacho de exportación, en orden.</summary>
public static class FasesTracking
{
    public const string Cuarentena = "cuarentena";
    public const string EmpaqueO2 = "empaque_o2";
    public const string InspeccionIca = "inspeccion_ica";
    public const string VueloTransito = "vuelo_transito";
    public const string Arribo = "arribo";
    public const string ControlDoa = "control_doa";

    public static readonly IReadOnlyList<string> Todos = [Cuarentena, EmpaqueO2, InspeccionIca, VueloTransito, Arribo, ControlDoa];
}

public static class TiposAccion
{
    public const string Prospeccion = "prospeccion";
    public const string StocklistFotos = "stocklist_fotos";
    public const string PreInvoice = "pre_invoice";
    public const string CobroCartera = "cobro_cartera";
    public const string PostVentaDoa = "post_venta_doa";

    public static readonly IReadOnlyList<string> Todos = [Prospeccion, StocklistFotos, PreInvoice, CobroCartera, PostVentaDoa];
}

public static class EstadosTarea
{
    public const string Pendiente = "pendiente";
    public const string EnCurso = "en_curso";
    public const string EsperandoCliente = "esperando_cliente";
    public const string Completada = "completada";

    public static readonly IReadOnlyList<string> Todos = [Pendiente, EnCurso, EsperandoCliente, Completada];
}

public static class Prioridades
{
    public const string Alta = "alta";
    public const string Media = "media";
    public const string Baja = "baja";

    public static readonly IReadOnlyList<string> Todos = [Alta, Media, Baja];
}
