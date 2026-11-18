using JRTropicalFish.Application.Comun;
using Microsoft.AspNetCore.Mvc;

namespace JRTropicalFish.Api.Comun;

/// <summary>
/// Traduce un <see cref="Resultado"/> de Application a la respuesta HTTP. Los errores salen siempre
/// como <c>{ error }</c>, que es lo que lee <c>apiFetch</c> en el web para mostrar el mensaje.
/// </summary>
public static class ResultadoHttp
{
    public static IActionResult ARespuesta<T>(this Resultado<T> resultado) =>
        resultado.Exitoso ? new OkObjectResult(resultado.Valor) : Error(resultado.Error!);

    public static IActionResult ARespuesta(this Resultado resultado) =>
        resultado.Exitoso ? new NoContentResult() : Error(resultado.Error!);

    public static IActionResult Error(Error error) =>
        new ObjectResult(new { error = error.Mensaje })
        {
            StatusCode = error.Tipo switch
            {
                TipoError.Validacion => StatusCodes.Status400BadRequest,
                TipoError.NoEncontrado => StatusCodes.Status404NotFound,
                TipoError.Conflicto => StatusCodes.Status409Conflict,
                TipoError.Prohibido => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest,
            },
        };
}
