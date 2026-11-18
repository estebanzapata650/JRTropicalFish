namespace JRTropicalFish.Application.Comun;

/// <summary>Categoría del error, que la API traduce a un código HTTP.</summary>
public enum TipoError
{
    /// <summary>400: los datos no cumplen una regla.</summary>
    Validacion,

    /// <summary>404: el recurso no existe.</summary>
    NoEncontrado,

    /// <summary>409: choca con el estado actual (duplicado, stock insuficiente, estado no válido).</summary>
    Conflicto,

    /// <summary>403: el usuario no puede hacer esto aunque tenga sesión.</summary>
    Prohibido,

    /// <summary>401: credenciales inválidas o sesión no reconocida.</summary>
    NoAutenticado,
}

public sealed record Error(TipoError Tipo, string Mensaje)
{
    public static Error Validacion(string mensaje) => new(TipoError.Validacion, mensaje);
    public static Error NoEncontrado(string mensaje) => new(TipoError.NoEncontrado, mensaje);
    public static Error Conflicto(string mensaje) => new(TipoError.Conflicto, mensaje);
    public static Error Prohibido(string mensaje) => new(TipoError.Prohibido, mensaje);
    public static Error NoAutenticado(string mensaje) => new(TipoError.NoAutenticado, mensaje);
}

/// <summary>
/// Resultado de una operación de negocio que puede fallar por una regla (no por una excepción).
///
/// <para>
/// Los servicios de Application devuelven esto en vez de lanzar excepciones para los casos
/// esperados («la especie ya existe», «no hay stock»). Las excepciones quedan para lo inesperado y
/// las atrapa el manejador global de la API. Los controladores lo traducen con
/// <c>ResultadoHttp.ARespuesta()</c> a la forma <c>{ error }</c> que espera el web.
/// </para>
/// </summary>
public class Resultado
{
    protected Resultado(Error? error) => Error = error;

    public Error? Error { get; }

    public bool Exitoso => Error is null;

    public static Resultado Ok() => new(null);

    public static Resultado Falla(Error error) => new(error);

    public static Resultado<T> Ok<T>(T valor) => Resultado<T>.Ok(valor);

    public static implicit operator Resultado(Error error) => Falla(error);
}

public sealed class Resultado<T> : Resultado
{
    private readonly T? _valor;

    private Resultado(T? valor, Error? error) : base(error) => _valor = valor;

    /// <summary>El valor. Lanza si se lee en un resultado fallido: es un error de programación.</summary>
    public T Valor => Exitoso
        ? _valor!
        : throw new InvalidOperationException($"No hay valor en un resultado fallido: {Error!.Mensaje}");

    public static Resultado<T> Ok(T valor) => new(valor, null);

    public static new Resultado<T> Falla(Error error) => new(default, error);

    public static implicit operator Resultado<T>(T valor) => Ok(valor);

    public static implicit operator Resultado<T>(Error error) => Falla(error);
}
