using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JRTropicalFish.Application.Abstractions;

namespace JRTropicalFish.Infrastructure.Correo;

/// <summary>
/// Escribe cada correo en un archivo y no envía nada.
///
/// <para>
/// Es el proveedor por defecto, y no por comodidad: los destinatarios son clientes y proveedores
/// <b>reales</b> del exterior, tomados de la base. Una prueba no puede escribirles. Con esto el
/// flujo completo se verifica sin que salga un solo correo de la máquina.
/// </para>
///
/// <para>
/// Devuelve un <c>MensajeId</c> propio con el prefijo <c>disco-</c> para que el resto del sistema
/// no tenga que distinguir proveedores, y para que en la ficha se vea de un vistazo que ese envío
/// no fue real.
/// </para>
/// </summary>
public class CorreoDisco(IOptions<CorreoOptions> opciones, ILogger<CorreoDisco> log) : IServicioCorreo
{
    private readonly CorreoOptions _opciones = opciones.Value;

    public string Proveedor => "Disco";

    public async Task<ResultadoCorreo> EnviarAsync(MensajeCorreo mensaje, CancellationToken ct = default)
    {
        var carpeta = Path.Combine(AppContext.BaseDirectory, _opciones.CarpetaDisco);
        Directory.CreateDirectory(carpeta);

        var id = $"disco-{Guid.CreateVersion7():N}";
        var archivo = Path.Combine(
            carpeta,
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{Sanear(mensaje.Para)}-{id[^8..]}.txt");

        var texto = new StringBuilder()
            .AppendLine($"Para:    {mensaje.NombrePara} <{mensaje.Para}>")
            .AppendLine($"Asunto:  {mensaje.Asunto}")
            .AppendLine($"Fecha:   {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .AppendLine($"Mensaje: {id}");

        if (mensaje.PlantillaId is { Length: > 0 })
        {
            texto.AppendLine($"Plantilla: {mensaje.PlantillaId}");
            foreach (var (clave, valor) in mensaje.DatosPlantilla ?? new Dictionary<string, string>())
            {
                texto.AppendLine($"  {clave} = {valor}");
            }
        }

        texto.AppendLine(new string('-', 60)).AppendLine(mensaje.CuerpoTexto);

        await File.WriteAllTextAsync(archivo, texto.ToString(), Encoding.UTF8, ct);

        log.LogInformation("Correo escrito en disco: {Archivo}", archivo);
        // ⚠️ `LlegoAlDestinatario` se queda en false: el archivo está en el servidor y nadie
        // recibió nada. Es lo que permite que la pantalla lo diga en vez de cantar «enviado».
        return new ResultadoCorreo(true, id, null);
    }

    /// <summary>El nombre del archivo no puede traer nada del exterior sin filtrar.</summary>
    private static string Sanear(string valor)
    {
        var limpio = new string([.. valor.Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' or '@')]);
        return limpio.Length > 40 ? limpio[..40] : limpio;
    }
}
