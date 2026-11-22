namespace JRTropicalFish.Domain.Sanidad;

/// <summary>Cómo se calcula la dosis de un medicamento.</summary>
public static class TiposDosis
{
    /// <summary>Cantidad por cada N litros de agua del acuario (baño).</summary>
    public const string PorVolumen = "por_volumen";

    /// <summary>Cantidad por cada N gramos de alimento (alimento medicado).</summary>
    public const string PorAlimento = "por_alimento";

    /// <summary>Cantidad fija por acuario, sin importar el volumen.</summary>
    public const string Fija = "fija";

    public static readonly IReadOnlyList<string> Todos = [PorVolumen, PorAlimento, Fija];
}

/// <summary>Unidad en la que el operario aplica el medicamento o el alimento.</summary>
public static class UnidadesSanidad
{
    public const string Mililitro = "ml";
    public const string Gramo = "g";
    public const string Miligramo = "mg";
    public const string Gota = "gotas";
    public const string Tableta = "tableta";
    public const string Cubo = "cubo";

    public static readonly IReadOnlyList<string> Medicamento = [Mililitro, Gramo, Miligramo, Gota, Tableta];

    public static readonly IReadOnlyList<string> Alimento = [Gramo, Mililitro, Cubo];

    public static readonly IReadOnlyList<string> Todas = [Mililitro, Gramo, Miligramo, Gota, Tableta, Cubo];
}

public static class TiposAlimento
{
    public const string Seco = "seco";
    public const string Vivo = "vivo";
    public const string Congelado = "congelado";
    public const string Papilla = "papilla";

    public static readonly IReadOnlyList<string> Todos = [Seco, Vivo, Congelado, Papilla];
}

/// <summary>Un protocolo solo se sugiere (y se aplica desde el tablero) cuando está aprobado.</summary>
public static class EstadosProtocolo
{
    public const string Borrador = "borrador";
    public const string Aprobado = "aprobado";
    public const string Inactivo = "inactivo";

    public static readonly IReadOnlyList<string> Todos = [Borrador, Aprobado, Inactivo];
}

/// <summary>Lo que se hace en un paso de un protocolo o en una aplicación de un tratamiento.</summary>
public static class AccionesSanidad
{
    public const string Medicar = "medicar";
    public const string CambioAgua = "cambio_agua";
    public const string Ayuno = "ayuno";
    public const string Observacion = "observacion";

    public static readonly IReadOnlyList<string> Todas = [Medicar, CambioAgua, Ayuno, Observacion];
}

public static class MotivosTratamiento
{
    public const string ProfilaxisLlegada = "profilaxis_llegada";
    public const string Prescripcion = "prescripcion";
    public const string Otro = "otro";

    public static readonly IReadOnlyList<string> Todos = [ProfilaxisLlegada, Prescripcion, Otro];
}

public static class EstadosTratamiento
{
    public const string Activo = "activo";
    public const string Terminado = "terminado";
    public const string Cancelado = "cancelado";

    public static readonly IReadOnlyList<string> Todos = [Activo, Terminado, Cancelado];
}

public static class EstadosAplicacion
{
    public const string Pendiente = "pendiente";
    public const string Aplicada = "aplicada";
    public const string Omitida = "omitida";

    public static readonly IReadOnlyList<string> Todos = [Pendiente, Aplicada, Omitida];
}

/// <summary>Columna del tablero de bodega: se deriva de los datos, nunca se guarda.</summary>
public static class EstadosAcuario
{
    public const string PorLlegar = "por_llegar";
    public const string Aclimatacion = "aclimatacion";
    public const string Cuarentena = "cuarentena";
    public const string Estable = "estable";
    public const string AyunoVuelo = "ayuno_vuelo";

    public static readonly IReadOnlyList<string> Todos = [PorLlegar, Aclimatacion, Cuarentena, Estable, AyunoVuelo];
}

/// <summary>Por qué un acuario no se alimenta hoy (semáforo rojo).</summary>
public static class MotivosAyuno
{
    public const string Vuelo = "vuelo";
    public const string Tratamiento = "tratamiento";
    public const string Llegada = "llegada";

    public static readonly IReadOnlyList<string> Todos = [Vuelo, Tratamiento, Llegada];
}
