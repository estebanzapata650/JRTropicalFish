using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Correo;

/// <summary>
/// Reescribe un mensaje para que no salga al destinatario real. Estático y puro a propósito: es la
/// regla que impide escribirle a un cliente por error, y tiene que poder probarse sin DI ni base.
/// </summary>
public static class ReescrituraPruebas
{
    public const string Marca = "[PRUEBA";

    /// <summary>
    /// Devuelve el mensaje redirigido, con el destinatario real en el asunto y en el cuerpo.
    ///
    /// <para>
    /// Es <b>idempotente</b>: si el asunto ya trae la marca no la duplica, que es lo que pasaría
    /// si alguien anidara dos decoradores por error.
    /// </para>
    ///
    /// <para>
    /// ⚠️ <b>Descarta la plantilla.</b> Con plantilla el asunto y el cuerpo los pone el proveedor,
    /// así que el prefijo no se vería por ninguna parte y un correo de prueba llegaría idéntico a
    /// uno real — justo lo que esto existe para evitar. <paramref name="mantenerPlantilla"/> es la
    /// vía de escape consciente para quien necesite probar la plantilla misma.
    /// </para>
    /// </summary>
    public static MensajeCorreo Reescribir(
        MensajeCorreo mensaje, string destino, string entorno, string proveedor,
        bool mantenerPlantilla = false)
    {
        var yaMarcado = mensaje.Asunto.StartsWith(Marca, StringComparison.Ordinal);

        var asunto = yaMarcado
            ? mensaje.Asunto
            : $"{Marca} → {mensaje.Para}] {mensaje.Asunto}";

        var encabezado =
            $"""
            ─────────────────────────────────────────────
            CORREO DE PRUEBA — no salió a su destinatario
            Iba para: {mensaje.NombrePara} <{mensaje.Para}>
            Entorno:  {entorno}
            Habría salido por: {proveedor}
            Fecha:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            ─────────────────────────────────────────────

            """;

        var datos = mantenerPlantilla && mensaje.DatosPlantilla is not null
            ? new Dictionary<string, string>(mensaje.DatosPlantilla)
            {
                ["_prueba"] = "true",
                ["_prueba_destino_real"] = mensaje.Para,
            }
            : mensaje.DatosPlantilla;

        return mensaje with
        {
            Para = destino,
            NombrePara = $"[Prueba] {mensaje.NombrePara}",
            Asunto = asunto,
            CuerpoTexto = yaMarcado ? mensaje.CuerpoTexto : encabezado + mensaje.CuerpoTexto,
            CuerpoHtml = mensaje.CuerpoHtml is null || yaMarcado
                ? mensaje.CuerpoHtml
                : $"<pre>{encabezado}</pre>{mensaje.CuerpoHtml}",
            PlantillaId = mantenerPlantilla ? mensaje.PlantillaId : null,
            DatosPlantilla = datos,
        };
    }
}

/// <summary>
/// Decorador que redirige todo el correo a una sola dirección mientras el sistema está en modo
/// pruebas (<c>Correo:RedirigirTodoA</c> con valor).
///
/// <para>
/// Es un decorador y <b>no un `if` dentro de cada proveedor</b> porque la regla «nadie real recibe
/// correo» tiene que valer igual para SendGrid, el de disco y cualquiera que se agregue; con un `if` por proveedor, el
/// que se agregue mañana nacería sin la red puesta.
/// </para>
///
/// <para>
/// ⚠️ <b>Falla cerrado.</b> Sin una dirección de redirección válida <b>no envía nada</b>. Caer al
/// destinatario real es exactamente el fallo que esto existe para evitar.
/// </para>
/// </summary>
public class CorreoModoPruebas(
    IServicioCorreo interno, string destino, string entorno, bool mantenerPlantilla)
    : IServicioCorreo
{
    public string Proveedor => $"{interno.Proveedor} (pruebas)";

    public async Task<ResultadoCorreo> EnviarAsync(
        MensajeCorreo mensaje, CancellationToken ct = default)
    {
        if (!EsDireccion(destino))
        {
            return new ResultadoCorreo(
                false,
                null,
                "El modo pruebas está activo y no hay una dirección de redirección válida "
                + "configurada, así que no se envió nada.");
        }

        var redirigido = ReescrituraPruebas.Reescribir(
            mensaje, destino, entorno, interno.Proveedor, mantenerPlantilla);

        var resultado = await interno.EnviarAsync(redirigido, ct);

        // ⚠️ Aunque el proveedor de adentro lo entregue, **no llegó a quien iba**: fue a la
        // dirección de redirección. Quien pregunte «¿le llegó la proforma al cliente?» tiene que
        // oír que no.
        return resultado with { LlegoAlDestinatario = false };
    }

    /// <summary>Lo mínimo para no mandar a una cadena que no es una dirección.</summary>
    public static bool EsDireccion(string? valor)
    {
        var texto = valor?.Trim() ?? "";
        var arroba = texto.IndexOf('@');

        return arroba > 0 && arroba < texto.Length - 1 && !texto.Contains(' ');
    }
}
