namespace JRTropicalFish.Infrastructure.Correo;

/// <summary>Configuración del envío de correo (sección <c>Correo</c>).</summary>
public class CorreoOptions
{
    public const string SectionName = "Correo";

    /// <summary>
    /// <c>SendGrid</c> o <c>Disco</c>.
    ///
    /// <para>
    /// ⚠️ <b>El valor por defecto es <c>Disco</c> a propósito.</b> Los destinatarios (clientes y
    /// proveedores del exterior) son reales: una prueba no puede escribirles ni quemar cupo del
    /// proveedor. En desarrollo el correo se escribe en <see cref="CarpetaDisco"/> y no sale de la
    /// máquina; SendGrid se activa poniendo este valor en configuración.
    /// </para>
    /// </summary>
    public string Proveedor { get; set; } = "Disco";

    /// <summary>La clave del proveedor. ⚠️ <b>Nunca se versiona</b>: va en variables de entorno o user-secrets.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// ⚠️ Tiene que ser una identidad <b>verificada</b> en SendGrid (dominio autenticado o
    /// remitente único). Si no lo está, SendGrid acepta la petición con <c>202</c> y no entrega
    /// nada; es el fallo más difícil de diagnosticar del servicio.
    /// </summary>
    public string RemitenteEmail { get; set; } = "no-responder@jrtropicalfish.local";

    public string RemitenteNombre { get; set; } = "JR Tropical Fish";

    /// <summary>Modo prueba de SendGrid: valida la petición y <b>no entrega nada</b>.</summary>
    public bool SandBox { get; set; }

    /// <summary>
    /// Si tiene valor, TODO el correo se redirige a esta dirección (decorador
    /// <see cref="CorreoModoPruebas"/>). Pensado para QA/staging: el flujo real se ejercita sin
    /// escribirle a un cliente.
    /// </summary>
    public string? RedirigirTodoA { get; set; }

    /// <summary>
    /// En modo pruebas, si se conserva la plantilla dinámica en vez de mandar el cuerpo propio.
    /// Falso por defecto: con plantilla el aviso de «esto es una prueba» no se vería.
    /// </summary>
    public bool MantenerPlantillaEnPruebas { get; set; }

    /// <summary>Dónde escribe el proveedor de disco. Relativa a la raíz de la API.</summary>
    public string CarpetaDisco { get; set; } = "correos-enviados";

    public int TimeoutSegundos { get; set; } = 20;
}
