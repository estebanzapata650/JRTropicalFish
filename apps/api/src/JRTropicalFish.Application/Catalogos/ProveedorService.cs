using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Catalogos;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Catalogos;

/// <summary>Proveedores nacionales (pescadores, acopiadores) y sus centros de acopio.</summary>
public partial class ProveedorService(
    IProveedorRepository proveedores,
    ICentroAcopioRepository centros,
    IMaestroRepository maestros,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Proveedor";

    private static readonly string TablaTiposId = RegistroMaestros.Obtener(RegistroMaestros.TiposIdentificacion).Tabla;

    public async Task<IReadOnlyList<ProveedorDetalle>> ListarAsync(CancellationToken ct = default)
    {
        var porProveedor = (await proveedores.CentrosAsync(null, ct)).ToLookup(c => c.ProveedorId);
        return [.. (await proveedores.ListarAsync(ct)).Select(p => Detalle(p, porProveedor[p.Id]))];
    }

    public async Task<Resultado<ProveedorDetalle>> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        await proveedores.ObtenerAsync(id, ct) is { } p ? Detalle(p, await proveedores.CentrosAsync(id, ct)) : NoExiste();

    public async Task<Resultado<Guid>> CrearAsync(GuardarProveedorRequest r, CancellationToken ct = default)
    {
        var datos = await ValidarAsync(r, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (proveedor, tipo, centrosElegidos) = datos.Valor;
        await proveedores.GuardarAsync(proveedor, esNuevo: true, [.. centrosElegidos.Select(c => c.Id)], reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(Entidad, proveedor.Id, Auditable(proveedor, tipo, centrosElegidos), ct: ct);
        return proveedor.Id;
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarProveedorRequest r, CancellationToken ct = default)
    {
        if (await proveedores.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var datos = await ValidarAsync(r, existente, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (proveedor, tipo, centrosElegidos) = datos.Valor;
        proveedor.Id = id;
        var centrosAntes = (await proveedores.CentrosAsync(id, ct)).Select(c => new CentroRef(c.CentroAcopioId, c.Nombre));
        await proveedores.GuardarAsync(proveedor, esNuevo: false, [.. centrosElegidos.Select(c => c.Id)], reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            ComparadorAuditoria.Comparar(Auditable(existente, centrosAntes), Auditable(proveedor, tipo, centrosElegidos)),
            ct);
        return id;
    }

    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await proveedores.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var centrosAntes = (await proveedores.CentrosAsync(id, ct)).Select(c => new CentroRef(c.CentroAcopioId, c.Nombre));
        await proveedores.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(Entidad, id, Auditable(existente, centrosAntes), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(Guid id, CancellationToken ct = default) =>
        await proveedores.ObtenerAsync(id, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    /// <summary>Sin espacios ni puntos, en mayúsculas (igual que en empleados): «8.028.183-5» → «8028183-5».</summary>
    public static string NormalizarIdentificacion(string numero) =>
        numero.Replace(" ", "", StringComparison.Ordinal).Replace(".", "", StringComparison.Ordinal).ToUpperInvariant();

    private async Task<Resultado<(Proveedor Proveedor, string Tipo, IReadOnlyList<CentroRef> Centros)>> ValidarAsync(
        GuardarProveedorRequest r, ProveedorFila? existente, CancellationToken ct)
    {
        var nombre = Limpiar(r.Nombre);
        var numero = Limpiar(r.NumeroIdentificacion) is { } n ? NormalizarIdentificacion(n) : null;
        var telefono = Limpiar(r.Telefono);
        var contacto = Limpiar(r.Contacto);
        var email = Limpiar(r.Email)?.ToLowerInvariant();
        var notas = LimpiarTextoLargo(r.Notas);

        var error = Primero(
            Requerido(nombre, "Nombre", 200, 2),
            Requerido(numero, "Número de identificación", 30, 3),
            numero is not null && !IdentificacionRegex().IsMatch(numero)
                ? Error.Validacion("El número de identificación solo admite letras, números y guion.")
                : null,
            Opcional(telefono, "Teléfono", 40),
            telefono is not null && !TelefonoValido(telefono) ? Error.Validacion("El teléfono no es válido.") : null,
            Opcional(contacto, "Contacto", 150),
            Opcional(email, "Correo", 320),
            email is not null && !EmailValido(email) ? Error.Validacion("El correo no es válido.") : null,
            Opcional(notas, "Notas", 2000));
        if (error is not null)
        {
            return error;
        }

        if (await maestros.ObtenerAsync(TablaTiposId, r.TipoIdentificacionId, ct) is not { } tipo
            || (!tipo.Activo && existente?.TipoIdentificacionId != tipo.Id))
        {
            return Error.Validacion("Elige un tipo de identificación activo.");
        }

        if (await proveedores.ExisteIdentificacionAsync(tipo.Id, numero!, existente?.Id, ct))
        {
            return Error.Conflicto($"Ya existe un proveedor con {tipo.Codigo} {numero}.");
        }

        var actuales = existente is null
            ? []
            : (await proveedores.CentrosAsync(existente.Id, ct)).Select(c => c.CentroAcopioId).ToHashSet();
        var elegidos = new List<CentroRef>();
        foreach (var id in (r.CentrosAcopio ?? []).Distinct())
        {
            var centro = await centros.ObtenerAsync(id, ct);
            if (centro is null || (!centro.Activo && !actuales.Contains(id)))
            {
                return Error.Validacion("Elige centros de acopio activos.");
            }

            elegidos.Add(new CentroRef(centro.Id, centro.Nombre));
        }

        var proveedor = new Proveedor
        {
            Nombre = nombre!,
            TipoIdentificacionId = tipo.Id,
            NumeroIdentificacion = numero!,
            Telefono = telefono,
            Contacto = contacto,
            Email = email,
            Notas = notas,
            Activo = r.Activo,
        };
        return (proveedor, tipo.Codigo, elegidos);
    }

    private static ProveedorDetalle Detalle(ProveedorFila p, IEnumerable<ProveedorCentroFila> centrosProveedor) =>
        new(p.Id, p.Nombre, p.TipoIdentificacionId, p.TipoIdentificacionCodigo, p.NumeroIdentificacion, p.Telefono, p.Contacto,
            p.Email, p.Notas, p.Activo, p.CreadoEn, [.. centrosProveedor.Select(c => new CentroRef(c.CentroAcopioId, c.Nombre))]);

    private static ProveedorAuditable Auditable(Proveedor p, string tipo, IEnumerable<CentroRef> centrosProveedor) =>
        new(p.Nombre, tipo, p.NumeroIdentificacion, p.Telefono, p.Contacto, p.Email, p.Notas, p.Activo, Unir(centrosProveedor));

    private static ProveedorAuditable Auditable(ProveedorFila p, IEnumerable<CentroRef> centrosProveedor) =>
        new(p.Nombre, p.TipoIdentificacionCodigo, p.NumeroIdentificacion, p.Telefono, p.Contacto, p.Email, p.Notas, p.Activo,
            Unir(centrosProveedor));

    private static string? Unir(IEnumerable<CentroRef> lista)
    {
        var nombres = lista.Select(c => c.Nombre).OrderBy(n => n, StringComparer.Ordinal).ToList();
        return nombres.Count == 0 ? null : string.Join(", ", nombres);
    }

    private static Error NoExiste() => Error.NoEncontrado("El proveedor no existe.");

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]*$")]
    private static partial Regex IdentificacionRegex();
}
