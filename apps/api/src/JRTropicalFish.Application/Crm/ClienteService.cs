using System.Globalization;
using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Crm;

/// <summary>
/// Directorio de clientes. El código (<c>CLI-001</c>) lo asigna el consecutivo; la importación del
/// Excel puede traer el suyo y el consecutivo sube para no repetirlo.
/// </summary>
public partial class ClienteService(
    IClienteRepository clientes,
    IConsecutivoRepository consecutivos,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Cliente";
    public const string Prefijo = "CLI";

    public Task<IReadOnlyList<ClienteFila>> ListarAsync(string? busqueda, CancellationToken ct = default) =>
        clientes.ListarAsync(Limpiar(busqueda), ct);

    public async Task<Resultado<ClienteFila>> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await clientes.ObtenerAsync(id, ct) is { } cliente ? cliente : NoExiste();

    public async Task<Resultado<Creado>> CrearAsync(GuardarClienteRequest request, CancellationToken ct = default)
    {
        var datos = await ValidarAsync(request, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        string codigo;
        var codigoPedido = Limpiar(request.Codigo)?.ToUpperInvariant();
        if (codigoPedido is not null)
        {
            var coincidencia = CodigoRegex().Match(codigoPedido);
            if (!coincidencia.Success)
            {
                return Error.Validacion("El código debe tener la forma CLI-001.");
            }

            if (await clientes.ExisteCodigoAsync(codigoPedido, null, ct))
            {
                return Error.Conflicto($"Ya existe un cliente con el código {codigoPedido}.");
            }

            codigo = codigoPedido;
            await consecutivos.AjustarAsync(Prefijo, int.Parse(coincidencia.Groups[1].Value, CultureInfo.InvariantCulture), ct);
        }
        else
        {
            codigo = await consecutivos.SiguienteAsync(Prefijo, ct);
        }

        var cliente = datos.Valor;
        cliente.Codigo = codigo;
        cliente.CreadoEn = reloj.AhoraUtc;
        await clientes.CrearAsync(cliente, ct);
        await auditoria.CreacionAsync(Entidad, cliente.Id, Auditable(cliente), ct: ct);
        return new Creado(cliente.Id, codigo);
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarClienteRequest request, CancellationToken ct = default)
    {
        if (await clientes.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var datos = await ValidarAsync(request, id, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var cliente = datos.Valor;
        cliente.Id = id;
        cliente.Codigo = existente.Codigo;
        cliente.ActualizadoEn = reloj.AhoraUtc;
        await clientes.ActualizarAsync(cliente, ct);
        await auditoria.CambiosAsync(
            Entidad, id, ComparadorAuditoria.Comparar(Auditable(existente), Auditable(cliente)), ct);
        return id;
    }

    /// <summary>Borrado lógico, solo si no tiene oportunidades, ventas ni tareas (si no, se marca Inactivo).</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await clientes.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (await clientes.TieneMovimientosAsync(id, ct))
        {
            return Error.Conflicto("El cliente tiene oportunidades, ventas o tareas: márcalo como Inactivo en vez de eliminarlo.");
        }

        await clientes.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(Entidad, id, Auditable(existente), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(Guid id, CancellationToken ct = default) =>
        await clientes.ObtenerAsync(id, ct) is null
            ? NoExiste()
            : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    private async Task<Resultado<Cliente>> ValidarAsync(GuardarClienteRequest r, Guid? id, CancellationToken ct)
    {
        var nombre = Limpiar(r.NombreEmpresa);
        var contacto = Limpiar(r.ContactoPrincipal);
        var email = Limpiar(r.Email)?.ToLowerInvariant();
        var telefono = Limpiar(r.TelefonoWhatsapp);
        var pais = Limpiar(r.Pais);
        var ciudad = Limpiar(r.Ciudad);
        var iata = Limpiar(r.AeropuertoIata)?.ToUpperInvariant();
        var notas = LimpiarTextoLargo(r.Notas);

        var error = Primero(
            Requerido(nombre, "Nombre de la empresa", 200, 2),
            Opcional(contacto, "Contacto principal", 150),
            Opcional(email, "Correo", 320),
            email is not null && !EmailValido(email) ? Error.Validacion("El correo no es válido.") : null,
            Opcional(telefono, "Teléfono / WhatsApp", 40),
            telefono is not null && !TelefonoValido(telefono)
                ? Error.Validacion("El teléfono solo admite dígitos, +, espacios, guiones y paréntesis (mínimo 7 dígitos).")
                : null,
            Requerido(pais, "País", 100, 2),
            Opcional(ciudad, "Ciudad", 100),
            iata is not null && !IataRegex().IsMatch(iata)
                ? Error.Validacion("El aeropuerto debe ser un código IATA de 3 letras (ej. NRT, MIA, BOG).")
                : null,
            EnLista(r.TipoCliente, TiposCliente.Todos, "Tipo de cliente"),
            EnLista(r.LicenciaImportacion, LicenciasImportacion.Todos, "Licencia de importación"),
            EnLista(r.Estado, EstadosCliente.Todos, "Estado"),
            Opcional(notas, "Notas", 2000));
        if (error is not null)
        {
            return error;
        }

        if (await clientes.ExisteNombreAsync(nombre!, id, ct))
        {
            return Error.Conflicto($"Ya existe un cliente llamado «{nombre}».");
        }

        return new Cliente
        {
            Codigo = "",
            NombreEmpresa = nombre!,
            ContactoPrincipal = contacto,
            Email = email,
            TelefonoWhatsapp = telefono,
            Pais = pais!,
            Ciudad = ciudad,
            AeropuertoIata = iata,
            TipoCliente = r.TipoCliente!,
            LicenciaImportacion = r.LicenciaImportacion!,
            Estado = r.Estado!,
            Notas = notas,
        };
    }

    private static ClienteAuditable Auditable(Cliente c) =>
        new(c.Codigo, c.NombreEmpresa, c.ContactoPrincipal, c.Email, c.TelefonoWhatsapp, c.Pais, c.Ciudad,
            c.AeropuertoIata, c.TipoCliente, c.LicenciaImportacion, c.Estado, c.Notas);

    private static ClienteAuditable Auditable(ClienteFila c) =>
        new(c.Codigo, c.NombreEmpresa, c.ContactoPrincipal, c.Email, c.TelefonoWhatsapp, c.Pais, c.Ciudad,
            c.AeropuertoIata, c.TipoCliente, c.LicenciaImportacion, c.Estado, c.Notas);

    private static Error NoExiste() => Error.NoEncontrado("El cliente no existe.");

    [GeneratedRegex(@"^CLI-(\d{1,6})$")]
    private static partial Regex CodigoRegex();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex IataRegex();
}
