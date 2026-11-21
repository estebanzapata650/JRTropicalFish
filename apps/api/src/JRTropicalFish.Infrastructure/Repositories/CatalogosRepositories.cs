using System.Text.Json;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Domain.Catalogos;
using JRTropicalFish.Infrastructure.Persistence;

namespace JRTropicalFish.Infrastructure.Repositories;

public class CentroAcopioRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), ICentroAcopioRepository
{
    public Task<IReadOnlyList<CentroAcopioFila>> ListarAsync(CancellationToken ct = default) =>
        ListarAsync<CentroAcopioFila>("SELECT * FROM fn_centros_acopio_listar()", null, ct);

    public Task<CentroAcopioFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<CentroAcopioFila>("SELECT * FROM fn_centro_acopio_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default) =>
        EscalarAsync<bool>(
            "SELECT fn_centro_acopio_nombre_existe(@Nombre::text, @ExcluirId::uuid)", new { Nombre = nombre, ExcluirId = excluirId }, ct);

    public Task GuardarAsync(CentroAcopio c, bool esNuevo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_centro_acopio_guardar(@Id::uuid, @EsNuevo::boolean, @Nombre::text, @CuencaId::uuid, @Activo::boolean, @Fecha::timestamptz)",
            new { c.Id, EsNuevo = esNuevo, c.Nombre, c.CuencaId, c.Activo, Fecha = fecha }, ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_centro_acopio_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);
}

public class ProductoRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IProductoRepository
{
    public Task<IReadOnlyList<ProductoFila>> ListarAsync(CancellationToken ct = default) =>
        ListarAsync<ProductoFila>("SELECT * FROM fn_productos_listar()", null, ct);

    public Task<ProductoFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<ProductoFila>("SELECT * FROM fn_producto_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<ProductoCentroFila>> CentrosAsync(Guid? productoId, CancellationToken ct = default) =>
        ListarAsync<ProductoCentroFila>("SELECT * FROM fn_producto_centros(@Id::uuid)", new { Id = productoId }, ct);

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        EscalarAsync<bool>("SELECT fn_producto_codigo_existe(@Codigo::text, @ExcluirId::uuid)", new { Codigo = codigo, ExcluirId = excluirId }, ct);

    public Task<string> SiguienteCodigoAsync(string prefijo, CancellationToken ct = default) =>
        EscalarAsync<string>("SELECT fn_producto_siguiente_codigo(@Prefijo::text)", new { Prefijo = prefijo }, ct);

    public Task GuardarAsync(
        Producto p,
        bool esNuevo,
        IReadOnlyList<Guid> centros,
        IReadOnlyDictionary<Guid, decimal?> precios,
        DateTime fecha,
        CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_producto_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @GrupoId::uuid, @Tipo::text,
                                     @NombreCientifico::text, @NombreComun::text, @NombreEspanol::text, @TallaMinCm::numeric,
                                     @TallaMaxCm::numeric, @TallaEtiqueta::text, @BolsaMin::integer, @BolsaMax::integer,
                                     @Disponibilidad::text, @Activo::boolean, @Notas::text, @Centros::uuid[], @Precios::jsonb,
                                     @Fecha::timestamptz)
            """,
            new
            {
                p.Id, EsNuevo = esNuevo, p.Codigo, p.GrupoId, p.Tipo, p.NombreCientifico, p.NombreComun, p.NombreEspanol,
                p.TallaMinCm, p.TallaMaxCm, p.TallaEtiqueta, p.BolsaMin, p.BolsaMax, p.Disponibilidad, p.Activo, p.Notas,
                Centros = centros.ToArray(),
                Precios = JsonSerializer.Serialize(precios.Select(x => new { lista = x.Key, precio = x.Value })),
                Fecha = fecha,
            },
            ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_producto_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);
}

public class PrecioRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IPrecioRepository
{
    public Task<IReadOnlyList<ListaPreciosFila>> ListarListasAsync(CancellationToken ct = default) =>
        ListarAsync<ListaPreciosFila>("SELECT * FROM fn_listas_precios_listar()", null, ct);

    public Task<ListaPreciosFila?> ObtenerListaAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<ListaPreciosFila>("SELECT * FROM fn_lista_precios_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<bool> ExisteListaAsync(string campo, string valor, Guid? excluirId, CancellationToken ct = default) =>
        EscalarAsync<bool>(
            "SELECT fn_lista_precios_valor_existe(@Campo::text, @Valor::text, @ExcluirId::uuid)",
            new { Campo = campo, Valor = valor, ExcluirId = excluirId }, ct);

    public Task GuardarListaAsync(ListaPrecios l, bool esNueva, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_lista_precios_guardar(@Id::uuid, @EsNueva::boolean, @Codigo::text, @Nombre::text, @Moneda::text,
                                          @EsCosto::boolean, @ClienteId::uuid, @Orden::integer, @Activo::boolean,
                                          @ListaBaseId::uuid, @FactorSalvaje::numeric, @FactorCriadero::numeric,
                                          @FactorEspecial::numeric, @AplicaImpuestos::boolean, @Redondeo::numeric,
                                          @Fecha::timestamptz)
            """,
            new
            {
                l.Id, EsNueva = esNueva, l.Codigo, l.Nombre, l.Moneda, l.EsCosto, l.ClienteId, l.Orden, l.Activo, l.ListaBaseId,
                l.FactorSalvaje, l.FactorCriadero, l.FactorEspecial, l.AplicaImpuestos, l.Redondeo, Fecha = fecha,
            },
            ct);

    public Task<IReadOnlyList<PrecioFila>> PreciosAsync(Guid? productoId, Guid? listaId, CancellationToken ct = default) =>
        ListarAsync<PrecioFila>(
            "SELECT * FROM fn_precios_listar(@ProductoId::uuid, @ListaId::uuid)", new { ProductoId = productoId, ListaId = listaId }, ct);

    public Task GuardarLoteAsync(Guid listaId, IReadOnlyDictionary<Guid, decimal> precios, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_precios_guardar_lote(@ListaId::uuid, @Precios::jsonb, @Fecha::timestamptz)",
            new
            {
                ListaId = listaId,
                Precios = JsonSerializer.Serialize(precios.Select(x => new { producto = x.Key, precio = x.Value })),
                Fecha = fecha,
            },
            ct);

    public async Task<ParametrosPreciosInfo> ObtenerParametrosAsync(CancellationToken ct = default) =>
        await UnoAsync<ParametrosPreciosInfo>("SELECT * FROM fn_parametros_precios()", null, ct)
        ?? throw new InvalidOperationException("Falta la fila de parametros_precios (la siembra la migración FuncionesCatalogos).");

    public Task ActualizarParametrosAsync(GuardarParametrosPreciosRequest p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_parametros_precios_actualizar(@Iva::numeric, @Pasarela::numeric, @TrmReferencia::numeric, @Fecha::timestamptz)",
            new { p.Iva, p.Pasarela, p.TrmReferencia, Fecha = fecha }, ct);
}

public class ProveedorRepository(PostgresConnectionFactory conexiones) : RepositorioCrm(conexiones), IProveedorRepository
{
    public Task<IReadOnlyList<ProveedorFila>> ListarAsync(CancellationToken ct = default) =>
        ListarAsync<ProveedorFila>("SELECT * FROM fn_proveedores_listar()", null, ct);

    public Task<ProveedorFila?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        UnoAsync<ProveedorFila>("SELECT * FROM fn_proveedor_por_id(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<ProveedorCentroFila>> CentrosAsync(Guid? proveedorId, CancellationToken ct = default) =>
        ListarAsync<ProveedorCentroFila>("SELECT * FROM fn_proveedor_centros(@Id::uuid)", new { Id = proveedorId }, ct);

    public Task<bool> ExisteIdentificacionAsync(Guid tipoId, string numero, Guid? excluirId, CancellationToken ct = default) =>
        EscalarAsync<bool>(
            "SELECT fn_proveedor_identificacion_existe(@TipoId::uuid, @Numero::text, @ExcluirId::uuid)",
            new { TipoId = tipoId, Numero = numero, ExcluirId = excluirId }, ct);

    public Task GuardarAsync(Proveedor p, bool esNuevo, IReadOnlyList<Guid> centros, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_proveedor_guardar(@Id::uuid, @EsNuevo::boolean, @Nombre::text, @TipoIdentificacionId::uuid,
                                      @NumeroIdentificacion::text, @Telefono::text, @Contacto::text, @Email::text, @Notas::text,
                                      @Activo::boolean, @Centros::uuid[], @Fecha::timestamptz)
            """,
            new
            {
                p.Id, EsNuevo = esNuevo, p.Nombre, p.TipoIdentificacionId, p.NumeroIdentificacion, p.Telefono, p.Contacto, p.Email,
                p.Notas, p.Activo, Centros = centros.ToArray(), Fecha = fecha,
            },
            ct);

    public Task EliminarAsync(Guid id, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_proveedor_eliminar(@Id::uuid, @Fecha::timestamptz)", new { Id = id, Fecha = fecha }, ct);
}
