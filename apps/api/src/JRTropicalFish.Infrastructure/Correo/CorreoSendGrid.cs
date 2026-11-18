using JRTropicalFish.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace JRTropicalFish.Infrastructure.Correo;

/// <summary>
/// Envío por SendGrid (Twilio), con el paquete oficial y sus plantillas dinámicas. Es el único
/// archivo que conoce SendGrid.
///
/// <para>
/// ⚠️ <b>Un 202 no significa entregado.</b> SendGrid acepta la petición y entrega después, así que
/// responde lo mismo cuando el remitente no está verificado y cuando el destinatario está en la
/// <i>lista de supresión</i> por un rebote anterior. Por eso se devuelve el <c>X-Message-Id</c> de
/// cada envío: es lo único con lo que después se puede preguntar qué pasó.
/// </para>
/// </summary>
public class CorreoSendGrid : IServicioCorreo
{
    private readonly CorreoOptions _opciones;
    private readonly ILogger<CorreoSendGrid> _log;
    private readonly ISendGridClient? _cliente;

    public CorreoSendGrid(
        IOptions<CorreoOptions> opciones, IHttpClientFactory fabrica, ILogger<CorreoSendGrid> log)
    {
        _opciones = opciones.Value;
        _log = log;

        if (string.IsNullOrWhiteSpace(_opciones.ApiKey))
        {
            // Degradación informada: se dice y no se finge.
            _log.LogWarning("SendGrid está elegido pero no hay clave. No se enviará ningún correo.");
            return;
        }

        _cliente = new SendGridClient(fabrica.CreateClient(nameof(CorreoSendGrid)), _opciones.ApiKey);
    }

    public string Proveedor => "SendGrid";

    public async Task<ResultadoCorreo> EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
    {
        if (_cliente is null)
        {
            return new ResultadoCorreo(false, null, "El envío de correo no está configurado.");
        }

        var de = new EmailAddress(_opciones.RemitenteEmail, _opciones.RemitenteNombre);
        var para = new EmailAddress(mensaje.Para, mensaje.NombrePara);

        // Con plantilla el texto lo pone SendGrid: así se corrige la redacción sin desplegar.
        var sobre = !string.IsNullOrWhiteSpace(mensaje.PlantillaId)
            ? MailHelper.CreateSingleTemplateEmail(de, para, mensaje.PlantillaId, mensaje.DatosPlantilla)
            : MailHelper.CreateSingleEmail(de, para, mensaje.Asunto, mensaje.CuerpoTexto, mensaje.CuerpoHtml);

        foreach (var adjunto in mensaje.Adjuntos ?? [])
        {
            sobre.AddAttachment(adjunto.NombreArchivo, Convert.ToBase64String(adjunto.Contenido), adjunto.TipoMime);
        }

        if (_opciones.SandBox)
        {
            sobre.SetSandBoxMode(true);
        }

        try
        {
            var respuesta = await _cliente.SendEmailAsync(sobre, ct);

            if ((int)respuesta.StatusCode is < 200 or > 299)
            {
                var cuerpo = await respuesta.Body.ReadAsStringAsync(ct);
                _log.LogWarning(
                    "SendGrid rechazó el correo a {Para}: {Estado} {Cuerpo}",
                    mensaje.Para, respuesta.StatusCode, cuerpo);

                return new ResultadoCorreo(
                    false, null, $"El proveedor de correo respondió {(int)respuesta.StatusCode}.");
            }

            // Lo devuelve en la cabecera, no en el cuerpo: la respuesta de un 202 viene vacía.
            var mensajeId = respuesta.Headers.TryGetValues("X-Message-Id", out var valores)
                ? valores.FirstOrDefault()
                : null;

            _log.LogInformation(
                "Correo aceptado por SendGrid para {Para} con id {MensajeId}", mensaje.Para, mensajeId);

            return new ResultadoCorreo(true, mensajeId, null, LlegoAlDestinatario: !_opciones.SandBox);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _log.LogError(ex, "Falló el envío de correo a {Para}", mensaje.Para);
            return new ResultadoCorreo(false, null, "No se pudo contactar al proveedor de correo.");
        }
    }
}
