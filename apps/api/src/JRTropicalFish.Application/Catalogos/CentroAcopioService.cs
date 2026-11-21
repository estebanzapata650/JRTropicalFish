using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Catalogos;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Catalogos;

/// <summary>Centros de acopio (origen de los peces) con su cuenca.</summary>
public class CentroAcopioService(
    ICentroAcopioRepository centros,
    IMaestroRepository maestros,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "CentroAcopio";

    private static readonly string TablaCuencas = RegistroMaestros.Obtener(RegistroMaestros.Cuencas).Tabla;

    public Task<IReadOnlyList<CentroAcopioFila>> ListarAsync(CancellationToken ct = default) => centros.ListarAsync(ct);

    public async Task<Resultado<Guid>> CrearAsync(GuardarCentroAcopioRequest r, CancellationToken ct = default)
    {
        var datos = await ValidarAsync(r, null, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (centro, cuenca) = datos.Valor;
        await centros.GuardarAsync(centro, esNuevo: true, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(Entidad, centro.Id, new CentroAcopioAuditable(centro.Nombre, cuenca, centro.Activo), ct: ct);
        return centro.Id;
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarCentroAcopioRequest r, CancellationToken ct = default)
    {
        if (await centros.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        var datos = await ValidarAsync(r, existente, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (centro, cuenca) = datos.Valor;
        centro.Id = id;
        await centros.GuardarAsync(centro, esNuevo: false, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            ComparadorAuditoria.Comparar(
                new CentroAcopioAuditable(existente.Nombre, existente.CuencaNombre, existente.Activo),
                new CentroAcopioAuditable(centro.Nombre, cuenca, centro.Activo)),
            ct);
        return id;
    }

    /// <summary>Un centro con productos o proveedores no se elimina: se desactiva.</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await centros.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (existente.Productos > 0 || existente.Proveedores > 0)
        {
            return Error.Conflicto(
                $"«{existente.Nombre}» tiene {existente.Productos} productos y {existente.Proveedores} proveedores: desactívalo en vez de eliminarlo.");
        }

        await centros.EliminarAsync(id, reloj.AhoraUtc, ct);
        await auditoria.EliminacionAsync(
            Entidad, id, new CentroAcopioAuditable(existente.Nombre, existente.CuencaNombre, existente.Activo), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(Guid id, CancellationToken ct = default) =>
        await centros.ObtenerAsync(id, ct) is null ? NoExiste() : Resultado.Ok(await auditoria.HistorialAsync(Entidad, id, ct));

    private async Task<Resultado<(CentroAcopio Centro, string Cuenca)>> ValidarAsync(
        GuardarCentroAcopioRequest r, CentroAcopioFila? existente, CancellationToken ct)
    {
        var nombre = Limpiar(r.Nombre);
        if (Requerido(nombre, "Nombre", 100, 2) is { } error)
        {
            return error;
        }

        if (await centros.ExisteNombreAsync(nombre!, existente?.Id, ct))
        {
            return Error.Conflicto($"Ya existe el centro de acopio «{nombre}».");
        }

        if (await maestros.ObtenerAsync(TablaCuencas, r.CuencaId, ct) is not { } cuenca
            || (!cuenca.Activo && existente?.CuencaId != cuenca.Id))
        {
            return Error.Validacion("Elige una cuenca activa.");
        }

        return (new CentroAcopio { Nombre = nombre!, CuencaId = cuenca.Id, Activo = r.Activo }, cuenca.Nombre);
    }

    private static Error NoExiste() => Error.NoEncontrado("El centro de acopio no existe.");
}
