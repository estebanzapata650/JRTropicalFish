namespace JRTropicalFish.Application.Abstractions;

/// <summary>
/// Un correo a enviar. Puede ir con cuerpo propio o delegando el texto a una plantilla del
/// proveedor: lo segundo deja cambiar la redacción sin desplegar.
/// </summary>
/// <param name="Para">Dirección del destinatario.</param>
/// <param name="NombrePara">Nombre para mostrar, cuando se conoce.</param>
/// <param name="Asunto">Ignorado si la plantilla trae el suyo.</param>
/// <param name="CuerpoTexto">Versión en texto plano. Siempre se envía: hay clientes que no pintan HTML.</param>
/// <param name="CuerpoHtml">Versión en HTML, opcional.</param>
/// <param name="PlantillaId">Identificador de la plantilla dinámica del proveedor, si se usa.</param>
/// <param name="DatosPlantilla">Valores que rellenan la plantilla.</param>
/// <param name="Adjuntos">Archivos adjuntos (ej. la proforma o el packing list en PDF).</param>
public record MensajeCorreo(
    string Para,
    string? NombrePara,
    string Asunto,
    string CuerpoTexto,
    string? CuerpoHtml = null,
    string? PlantillaId = null,
    IReadOnlyDictionary<string, string>? DatosPlantilla = null,
    IReadOnlyList<AdjuntoCorreo>? Adjuntos = null);

public record AdjuntoCorreo(string NombreArchivo, string TipoMime, byte[] Contenido);

/// <summary>
/// Qué pasó con el envío.
/// </summary>
/// <param name="Exitoso">Falso solo si el proveedor rechazó la petición.</param>
/// <param name="MensajeId">
/// El <c>X-Message-Id</c> que devuelve el proveedor. ⚠️ Es lo único que permite rastrear un correo
/// después: SendGrid responde <c>202</c> y entrega de forma asíncrona, así que un correo aceptado
/// todavía puede rebotar o caer en la lista de supresión sin que nadie se entere.
/// </param>
/// <param name="Error">Por qué falló, cuando falló.</param>
/// <param name="LlegoAlDestinatario">
/// Si el mensaje salió de verdad <b>hacia la dirección que pidió quien lo mandó</b>.
///
/// <para>
/// ⚠️ No es lo mismo que <paramref name="Exitoso"/>. El proveedor de disco escribe el correo en un
/// archivo del servidor y termina bien; el modo pruebas lo entrega, pero a la dirección de
/// redirección y no al destinatario real. En los dos casos el envío fue exitoso y <b>nadie
/// recibió nada</b>, y la pantalla no debe decir «enviado al cliente».
/// </para>
///
/// <para>
/// Nace en <c>false</c> a propósito: un adaptador nuevo que se olvide de ponerlo reporta «no
/// llegó», que es el error inofensivo.
/// </para>
/// </param>
public record ResultadoCorreo(
    bool Exitoso, string? MensajeId, string? Error, bool LlegoAlDestinatario = false);

/// <summary>
/// Envío de correo. <b>Nadie fuera de Infrastructure conoce al proveedor</b>, igual que
/// <c>GeneradorInformePdfQuestPdf</c> es el único archivo que conoce QuestPDF: se elige por
/// configuración (sección <c>Correo</c>) y cambiar de proveedor no toca código de aplicación.
/// </summary>
public interface IServicioCorreo
{
    /// <summary>Nombre del proveedor activo, para poder decirlo en los diagnósticos.</summary>
    string Proveedor { get; }

    Task<ResultadoCorreo> EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default);
}
