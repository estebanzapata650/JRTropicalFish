using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Archivos;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Exportaciones;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Configuración de los documentos de exportación: datos y logo de la empresa, tarifas de flete por defecto,
/// tipos de caja y bolsa, cargos (certificados, airport fee, heat pack) y los datos de exportación de cada
/// cliente (dirección, EORI, formato de factura, lista de precios y descuento).
/// </summary>
public class ConfiguracionExportacionService(
    IConfiguracionExportacionRepository repositorio,
    IClienteRepository clientes,
    IPrecioRepository precios,
    IAlmacenArchivos almacen,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadConfiguracion = "ConfiguracionExportacion";
    public const string EntidadClienteExportacion = "ClienteExportacion";
    public const long TamanoMaximoLogoBytes = 2 * 1024 * 1024;

    /// <summary>Id fijo para auditar la configuración (fila única).</summary>
    public static readonly Guid IdConfiguracion = new("00000000-0000-0000-0000-000000000003");

    private const string CarpetaLogo = "empresa";
    private static readonly string[] TiposLogo = ["image/png", "image/jpeg"];

    public async Task<ConfiguracionExportacion> ObtenerAsync(CancellationToken ct = default) =>
        new(
            await repositorio.DatosEmpresaAsync(ct),
            await repositorio.TarifasAsync(ct),
            await repositorio.TiposCajaAsync(ct),
            await repositorio.TiposBolsaAsync(ct),
            await repositorio.CargosAsync(ct));

    public async Task<Resultado> ActualizarEmpresaAsync(GuardarDatosEmpresaRequest r, CancellationToken ct = default)
    {
        var normalizado = r with
        {
            RazonSocial = Limpiar(r.RazonSocial),
            Nit = Limpiar(r.Nit),
            Direccion = Limpiar(r.Direccion),
            CiudadPais = Limpiar(r.CiudadPais),
            Telefono = Limpiar(r.Telefono),
            Email = Limpiar(r.Email)?.ToLowerInvariant(),
            SitioWeb = Limpiar(r.SitioWeb),
            Banco = Limpiar(r.Banco),
            Swift = Limpiar(r.Swift)?.ToUpperInvariant(),
            CuentaBancaria = Limpiar(r.CuentaBancaria),
            Beneficiario = Limpiar(r.Beneficiario),
            DireccionBanco = Limpiar(r.DireccionBanco),
            FirmanteNombre = Limpiar(r.FirmanteNombre),
            FirmanteCargo = Limpiar(r.FirmanteCargo),
            FirmanteTelefono = Limpiar(r.FirmanteTelefono),
        };
        var error = Primero(
            Requerido(normalizado.RazonSocial, "Razón social", 200),
            Opcional(normalizado.Nit, "NIT", 30),
            Opcional(normalizado.Direccion, "Dirección", 250),
            Opcional(normalizado.CiudadPais, "Ciudad y país", 150),
            Opcional(normalizado.Telefono, "Teléfono", 60),
            normalizado.Email is { } correo && !EmailValido(correo) ? Error.Validacion("El correo no es válido.") : null,
            Opcional(normalizado.SitioWeb, "Sitio web", 200),
            Opcional(normalizado.Banco, "Banco", 100),
            Opcional(normalizado.Swift, "SWIFT", 20),
            Opcional(normalizado.CuentaBancaria, "Cuenta bancaria", 60),
            Opcional(normalizado.Beneficiario, "Beneficiario", 200),
            Opcional(normalizado.DireccionBanco, "Dirección del banco", 250),
            Opcional(normalizado.FirmanteNombre, "Firmante", 150),
            Opcional(normalizado.FirmanteCargo, "Cargo del firmante", 100),
            Opcional(normalizado.FirmanteTelefono, "Teléfono del firmante", 60));
        if (error is not null)
        {
            return error;
        }

        var anterior = await repositorio.DatosEmpresaAsync(ct);
        await repositorio.ActualizarDatosEmpresaAsync(normalizado, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadConfiguracion, IdConfiguracion,
            ComparadorAuditoria.Comparar(EmpresaAuditable(anterior), normalizado), ct);
        return Resultado.Ok();
    }

    /// <summary>Reemplaza el logo. El tipo se decide por los bytes; solo PNG o JPG (QuestPDF no dibuja WEBP).</summary>
    public async Task<Resultado> CambiarLogoAsync(Stream contenido, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer, ct);
        if (buffer.Length == 0)
        {
            return Error.Validacion("El archivo del logo está vacío.");
        }

        if (buffer.Length > TamanoMaximoLogoBytes)
        {
            return Error.Validacion("El logo no puede pesar más de 2 MB.");
        }

        var tipo = DetectorTipoArchivo.Detectar(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, DetectorTipoArchivo.BytesNecesarios)));
        if (tipo is null || !TiposLogo.Contains(tipo))
        {
            return Error.Validacion("El logo debe ser una imagen PNG o JPG.");
        }

        var anterior = (await repositorio.DatosEmpresaAsync(ct)).LogoClave;
        buffer.Position = 0;
        var clave = ClaveArchivo.ParaDocumento(CarpetaLogo, IdConfiguracion, Guid.CreateVersion7(), tipo);
        await almacen.SubirAsync(clave, buffer, tipo, ct);
        await repositorio.ActualizarLogoAsync(clave, reloj.AhoraUtc, ct);
        if (anterior is not null)
        {
            await almacen.EliminarAsync(anterior, ct);
        }

        await auditoria.CambiosAsync(EntidadConfiguracion, IdConfiguracion, [new CambioAuditado("Logo", anterior is null ? null : "Anterior", "Actualizado")], ct);
        return Resultado.Ok();
    }

    /// <summary>El logo en bytes para los PDF (nulo si no hay o no se puede leer).</summary>
    public async Task<byte[]?> LogoAsync(CancellationToken ct = default)
    {
        if ((await repositorio.DatosEmpresaAsync(ct)).LogoClave is not { } clave || await almacen.DescargarAsync(clave, ct) is not { } archivo)
        {
            return null;
        }

        await using var contenido = archivo.Contenido;
        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    public async Task<Resultado<ArchivoDescargado>> DescargarLogoAsync(CancellationToken ct = default) =>
        (await repositorio.DatosEmpresaAsync(ct)).LogoClave is { } clave && await almacen.DescargarAsync(clave, ct) is { } archivo
            ? archivo
            : Error.NoEncontrado("No hay logo cargado.");

    public async Task<Resultado> ActualizarTarifasAsync(TarifasFleteInfo t, CancellationToken ct = default)
    {
        if (ValidarTarifas(t) is { } error)
        {
            return Error.Validacion(error);
        }

        var anterior = await repositorio.TarifasAsync(ct);
        await repositorio.ActualizarTarifasAsync(t, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadConfiguracion, IdConfiguracion, ComparadorAuditoria.Comparar(anterior, t), ct);
        return Resultado.Ok();
    }

    public static string? ValidarTarifas(TarifasFleteInfo t) =>
        new[]
        {
            t.TarifaKgJr, t.TarifaKgCliente, t.FuelKg, t.JpFeeKg, t.VscKg, t.VscMinimo, t.DbcMinimo, t.DfMinimo, t.DueAgentJr,
            t.DueAgentCliente, t.DueCarrier,
        }.Any(v => v is < 0 or > 100_000)
            ? "Las tarifas y cargos van de 0 a 100.000 USD."
            : t.DbcPorcentaje is < 0 or > 100 || t.DfPorcentaje is < 0 or > 100
                ? "DBC y DF son porcentajes de 0 a 100."
                : null;

    public async Task<Resultado<Guid>> GuardarTipoCajaAsync(GuardarTipoCaja r, CancellationToken ct = default)
    {
        var nombre = Limpiar(r.Nombre);
        var descripcion = Limpiar(r.DescripcionFactura);
        var error = Primero(
            Requerido(nombre, "Nombre", 80),
            Requerido(descripcion, "Descripción en factura", 150),
            r.CostoCop is < 0 or > 10_000_000 ? Error.Validacion("El costo va de 0 a 10.000.000 COP.") : null,
            r.PrecioUsd is < 0 or > 10_000 ? Error.Validacion("El precio va de 0 a 10.000 USD.") : null,
            r.KgPorCaja is < 0 or > 500 ? Error.Validacion("El peso va de 0 a 500 kg.") : null,
            r.Pisos is < 1 or > 5 ? Error.Validacion("Los pisos van de 1 a 5.") : null);
        if (error is not null)
        {
            return error;
        }

        var existentes = await repositorio.TiposCajaAsync(ct);
        if (r.Id is { } id && existentes.All(c => c.Id != id))
        {
            return Error.NoEncontrado("El tipo de caja no existe.");
        }

        if (existentes.Any(c => c.Id != r.Id && string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflicto($"Ya existe el tipo de caja «{nombre}».");
        }

        var guardarId = r.Id ?? Guid.CreateVersion7();
        var limpio = r with { Nombre = nombre, DescripcionFactura = descripcion };
        await repositorio.GuardarTipoCajaAsync(guardarId, limpio, reloj.AhoraUtc, ct);
        var anterior = existentes.FirstOrDefault(c => c.Id == r.Id);
        await auditoria.CambiosAsync(
            EntidadConfiguracion, IdConfiguracion,
            ConPrefijo(
                $"Caja {nombre}",
                anterior is null
                    ? ComparadorAuditoria.Instantanea(NuevaCaja(limpio))
                    : ComparadorAuditoria.Comparar(CajaAuditable(anterior), NuevaCaja(limpio))),
            ct);
        return guardarId;
    }

    public async Task<Resultado<Guid>> GuardarTipoBolsaAsync(GuardarTipoBolsa r, CancellationToken ct = default)
    {
        var nombre = Limpiar(r.Nombre);
        var error = Primero(
            Requerido(nombre, "Nombre", 40),
            Opcional(Limpiar(r.Tamano), "Tamaño", 20),
            r.Porcion is <= 0 or > 1 ? Error.Validacion("La porción de caja va de más de 0 a 1.") : null,
            r.CostoCop is < 0 or > 1_000_000 ? Error.Validacion("El costo va de 0 a 1.000.000 COP.") : null);
        if (error is not null)
        {
            return error;
        }

        var existentes = await repositorio.TiposBolsaAsync(ct);
        if (r.Id is { } id && existentes.All(b => b.Id != id))
        {
            return Error.NoEncontrado("El tipo de bolsa no existe.");
        }

        if (existentes.Any(b => b.Id != r.Id && string.Equals(b.Nombre, nombre, StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflicto($"Ya existe el tipo de bolsa «{nombre}».");
        }

        var guardarId = r.Id ?? Guid.CreateVersion7();
        await repositorio.GuardarTipoBolsaAsync(guardarId, r with { Nombre = nombre, Tamano = Limpiar(r.Tamano) }, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadConfiguracion, IdConfiguracion, [new CambioAuditado($"Bolsa {nombre}", r.Id is null ? null : "Anterior", "Guardada")], ct);
        return guardarId;
    }

    public async Task<Resultado> ActualizarCargoAsync(GuardarCargo r, CancellationToken ct = default)
    {
        var nombre = Limpiar(r.Nombre);
        var error = Primero(
            r.Codigo is null || !CodigosCargo.Todos.Contains(r.Codigo) ? Error.Validacion("El cargo no existe.") : null,
            Requerido(nombre, "Nombre", 120),
            r.PrecioUsd is < 0 or > 100_000 ? Error.Validacion("El precio va de 0 a 100.000 USD.") : null);
        if (error is not null)
        {
            return error;
        }

        var anterior = (await repositorio.CargosAsync(ct)).First(c => c.Codigo == r.Codigo);
        await repositorio.ActualizarCargoAsync(r.Codigo!, nombre!, r.PrecioUsd, ct);
        await auditoria.CambiosAsync(
            EntidadConfiguracion, IdConfiguracion,
            ConPrefijo($"Cargo {r.Codigo}", ComparadorAuditoria.Comparar(new CargoAuditable(anterior.Nombre, anterior.PrecioUsd), new CargoAuditable(nombre!, r.PrecioUsd))),
            ct);
        return Resultado.Ok();
    }

    // --- Datos de exportación del cliente ---

    /// <summary>Los datos de exportación del cliente, o los valores por defecto si aún no tiene.</summary>
    public async Task<Resultado<ClienteExportacionInfo>> ClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        await clientes.ObtenerAsync(clienteId, ct) is null
            ? Error.NoEncontrado("El cliente no existe.")
            : await repositorio.ClienteAsync(clienteId, ct) ?? new ClienteExportacionInfo(clienteId, null, null, FormatosFactura.Jrch, null, 0);

    public async Task<Resultado> GuardarClienteAsync(Guid clienteId, GuardarClienteExportacionRequest r, CancellationToken ct = default)
    {
        if (await clientes.ObtenerAsync(clienteId, ct) is null)
        {
            return Error.NoEncontrado("El cliente no existe.");
        }

        var info = new ClienteExportacionInfo(
            clienteId, Limpiar(r.Direccion), Limpiar(r.Eori)?.ToUpperInvariant(), r.FormatoFactura ?? FormatosFactura.Jrch,
            r.ListaPreciosId, r.DescuentoPeces);
        var error = Primero(
            Opcional(info.Direccion, "Dirección", 300),
            Opcional(info.Eori, "EORI", 40),
            FormatosFactura.Todos.Contains(info.FormatoFactura) ? null : Error.Validacion("El formato de factura no es válido."),
            info.DescuentoPeces is < 0 or > 100 ? Error.Validacion("El descuento va de 0 a 100 %.") : null);
        if (error is not null)
        {
            return error;
        }

        if (info.ListaPreciosId is { } lista && await precios.ObtenerListaAsync(lista, ct) is not { EsCosto: false })
        {
            return Error.Validacion("Elige una lista de precios de venta existente.");
        }

        var anterior = await repositorio.ClienteAsync(clienteId, ct);
        await repositorio.GuardarClienteAsync(info, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadClienteExportacion, clienteId,
            anterior is null ? ComparadorAuditoria.Instantanea(info) : ComparadorAuditoria.Comparar(anterior, info), ct);
        return Resultado.Ok();
    }

    private static GuardarDatosEmpresaRequest EmpresaAuditable(DatosEmpresaInfo d) =>
        new(d.RazonSocial, d.Nit, d.Direccion, d.CiudadPais, d.Telefono, d.Email, d.SitioWeb, d.Banco, d.Swift, d.CuentaBancaria,
            d.Beneficiario, d.DireccionBanco, d.FirmanteNombre, d.FirmanteCargo, d.FirmanteTelefono);

    private static CajaAuditableRec NuevaCaja(GuardarTipoCaja c) =>
        new(c.Nombre!, c.DescripcionFactura!, c.CostoCop, c.PrecioUsd, c.KgPorCaja, c.Pisos, c.Activo);

    /// <summary>Antepone el elemento al nombre del campo ("Caja Holandesa · PrecioUsd") en el historial común.</summary>
    private static IReadOnlyList<CambioAuditado> ConPrefijo(string prefijo, IReadOnlyList<CambioAuditado> cambios) =>
        [.. cambios.Select(c => c with { Campo = $"{prefijo} · {c.Campo}" })];

    private static CajaAuditableRec CajaAuditable(TipoCajaFila c) =>
        new(c.Nombre, c.DescripcionFactura, c.CostoCop, c.PrecioUsd, c.KgPorCaja, c.Pisos, c.Activo);

    private sealed record CajaAuditableRec(string Nombre, string DescripcionFactura, decimal CostoCop, decimal PrecioUsd, decimal KgPorCaja, int Pisos, bool Activo);

    private sealed record CargoAuditable(string Nombre, decimal PrecioUsd);
}
