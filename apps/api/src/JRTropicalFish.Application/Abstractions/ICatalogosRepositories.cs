using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Domain.Catalogos;

namespace JRTropicalFish.Application.Abstractions;

public interface ICentroAcopioRepository
{
    Task<IReadOnlyList<CentroAcopioFila>> ListarAsync(CancellationToken ct = default);

    Task<CentroAcopioFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default);

    Task GuardarAsync(CentroAcopio centro, bool esNuevo, DateTime fecha, CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IProductoRepository
{
    Task<IReadOnlyList<ProductoFila>> ListarAsync(CancellationToken ct = default);

    Task<ProductoFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    /// <param name="productoId"><c>null</c> = los de todos los productos.</param>
    Task<IReadOnlyList<ProductoCentroFila>> CentrosAsync(Guid? productoId, CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default);

    Task<string> SiguienteCodigoAsync(string prefijo, CancellationToken ct = default);

    /// <summary>Producto + centros (reemplaza) + los precios indicados, en una transacción.</summary>
    Task GuardarAsync(
        Producto producto,
        bool esNuevo,
        IReadOnlyList<Guid> centros,
        IReadOnlyDictionary<Guid, decimal?> precios,
        DateTime fecha,
        CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}

public interface IPrecioRepository
{
    Task<IReadOnlyList<ListaPreciosFila>> ListarListasAsync(CancellationToken ct = default);

    Task<ListaPreciosFila?> ObtenerListaAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExisteListaAsync(string campo, string valor, Guid? excluirId, CancellationToken ct = default);

    Task GuardarListaAsync(ListaPrecios lista, bool esNueva, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<PrecioFila>> PreciosAsync(Guid? productoId, Guid? listaId, CancellationToken ct = default);

    /// <summary>Upsert de los precios de una lista (recalcular en bloque).</summary>
    Task GuardarLoteAsync(Guid listaId, IReadOnlyDictionary<Guid, decimal> precios, DateTime fecha, CancellationToken ct = default);

    Task<ParametrosPreciosInfo> ObtenerParametrosAsync(CancellationToken ct = default);

    Task ActualizarParametrosAsync(GuardarParametrosPreciosRequest parametros, DateTime fecha, CancellationToken ct = default);
}

public interface IProveedorRepository
{
    Task<IReadOnlyList<ProveedorFila>> ListarAsync(CancellationToken ct = default);

    Task<ProveedorFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ProveedorCentroFila>> CentrosAsync(Guid? proveedorId, CancellationToken ct = default);

    Task<bool> ExisteIdentificacionAsync(Guid tipoId, string numero, Guid? excluirId, CancellationToken ct = default);

    Task GuardarAsync(Proveedor proveedor, bool esNuevo, IReadOnlyList<Guid> centros, DateTime fecha, CancellationToken ct = default);

    Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default);
}
