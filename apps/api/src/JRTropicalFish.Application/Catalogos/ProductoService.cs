using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Catalogos;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Catalogos;

/// <summary>
/// Catálogo de productos (stock list). El costo de compra es un dato protegido, como el salario:
/// sin <c>catalogos:ver-costos</c> las listas de costo no viajan en ninguna respuesta ni en el
/// historial, y no se pueden fijar.
/// </summary>
public partial class ProductoService(
    IProductoRepository productos,
    IPrecioRepository precios,
    ICentroAcopioRepository centros,
    IMaestroRepository maestros,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string Entidad = "Producto";
    public const int MaximoCentros = 3;

    private static readonly string TablaGrupos = RegistroMaestros.Obtener(RegistroMaestros.GruposProducto).Tabla;

    public async Task<IReadOnlyList<ProductoDetalle>> ListarAsync(bool verCostos, CancellationToken ct = default)
    {
        var filas = await productos.ListarAsync(ct);
        var centrosPorProducto = (await productos.CentrosAsync(null, ct)).ToLookup(c => c.ProductoId);
        var visibles = await ListasVisiblesAsync(verCostos, ct);
        var preciosPorProducto = (await precios.PreciosAsync(null, null, ct))
            .Where(p => visibles.ContainsKey(p.ListaPreciosId))
            .ToLookup(p => p.ProductoId);

        return [.. filas.Select(p => Detalle(p, centrosPorProducto[p.Id], preciosPorProducto[p.Id]))];
    }

    public async Task<Resultado<ProductoDetalle>> ObtenerAsync(Guid id, bool verCostos, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(id, ct) is not { } fila)
        {
            return NoExiste();
        }

        var visibles = await ListasVisiblesAsync(verCostos, ct);
        return Detalle(
            fila,
            await productos.CentrosAsync(id, ct),
            (await precios.PreciosAsync(id, null, ct)).Where(p => visibles.ContainsKey(p.ListaPreciosId)));
    }

    public async Task<Resultado<Creado>> CrearAsync(GuardarProductoRequest r, bool verCostos, CancellationToken ct = default)
    {
        var datos = await ValidarAsync(r, null, verCostos, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (producto, grupo, centrosElegidos, listas) = datos.Valor;
        var codigoPedido = Limpiar(r.Codigo)?.ToUpperInvariant();
        if (codigoPedido is null)
        {
            producto.Codigo = await productos.SiguienteCodigoAsync(grupo.Codigo, ct);
        }
        else
        {
            if (!Regex.IsMatch(codigoPedido, $"^{Regex.Escape(grupo.Codigo)}[0-9]{{1,6}}$"))
            {
                return Error.Validacion($"El código debe ser el prefijo del grupo ({grupo.Codigo}) seguido de números, ej. {grupo.Codigo}020.");
            }

            if (await productos.ExisteCodigoAsync(codigoPedido, null, ct))
            {
                return Error.Conflicto($"Ya existe un producto con el código {codigoPedido}.");
            }

            producto.Codigo = codigoPedido;
        }

        var preciosPedidos = r.Precios ?? new Dictionary<Guid, decimal?>();
        await productos.GuardarAsync(producto, esNuevo: true, [.. centrosElegidos.Select(c => c.Id)], preciosPedidos, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(
            Entidad, producto.Id, Auditable(producto, grupo.Nombre, centrosElegidos),
            CambiosDePrecio(new Dictionary<Guid, decimal>(), preciosPedidos, listas),
            ct);
        return new Creado(producto.Id, producto.Codigo);
    }

    public async Task<Resultado<Guid>> ActualizarAsync(Guid id, GuardarProductoRequest r, bool verCostos, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        if (r.GrupoId != existente.GrupoId)
        {
            return Error.Validacion("El grupo no se cambia: el código del producto lleva su prefijo. Crea el producto en el grupo correcto.");
        }

        var datos = await ValidarAsync(r, existente, verCostos, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        var (producto, grupo, centrosElegidos, listas) = datos.Valor;
        producto.Id = id;
        producto.Codigo = existente.Codigo;
        var centrosAntes = await productos.CentrosAsync(id, ct);
        var preciosAntes = (await precios.PreciosAsync(id, null, ct)).ToDictionary(p => p.ListaPreciosId, p => p.Precio);
        var preciosPedidos = r.Precios ?? new Dictionary<Guid, decimal?>();

        await productos.GuardarAsync(producto, esNuevo: false, [.. centrosElegidos.Select(c => c.Id)], preciosPedidos, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            Entidad, id,
            [
                .. ComparadorAuditoria.Comparar(
                    Auditable(existente, centrosAntes.Select(c => new CentroRef(c.CentroAcopioId, c.Nombre))),
                    Auditable(producto, grupo.Nombre, centrosElegidos)),
                .. CambiosDePrecio(preciosAntes, preciosPedidos, listas),
            ],
            ct);
        return id;
    }

    /// <summary>Borrado lógico. Sus precios y centros se quedan con él (por si se restaura desde la base).</summary>
    public async Task<Resultado> EliminarAsync(Guid id, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(id, ct) is not { } existente)
        {
            return NoExiste();
        }

        await productos.EliminarAsync(id, reloj.AhoraUtc, ct);
        var centrosAntes = await productos.CentrosAsync(id, ct);
        await auditoria.EliminacionAsync(
            Entidad, id, Auditable(existente, centrosAntes.Select(c => new CentroRef(c.CentroAcopioId, c.Nombre))), ct);
        return Resultado.Ok();
    }

    /// <summary>Sin permiso de costos se quitan del historial los cambios de las listas de costo.</summary>
    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialAsync(
        Guid id, bool verCostos, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(id, ct) is null)
        {
            return NoExiste();
        }

        var registros = await auditoria.HistorialAsync(Entidad, id, ct);
        if (verCostos)
        {
            return Resultado.Ok(registros);
        }

        var marcasCosto = (await precios.ListarListasAsync(ct)).Where(l => l.EsCosto).Select(l => $"({l.Codigo})").ToList();
        var resultado = new List<RegistroAuditoriaLectura>(registros.Count);
        foreach (var registro in registros)
        {
            var cambios = JsonSerializer.Deserialize<List<CambioAuditado>>(registro.CambiosJson) ?? [];
            var visibles = cambios.Where(c => !marcasCosto.Any(m => c.Campo.EndsWith(m, StringComparison.Ordinal))).ToList();
            if (visibles.Count > 0)
            {
                resultado.Add(visibles.Count == cambios.Count ? registro : registro with { CambiosJson = JsonSerializer.Serialize(visibles) });
            }
        }

        return Resultado.Ok<IReadOnlyList<RegistroAuditoriaLectura>>(resultado);
    }

    /// <summary>Campo de auditoría de un precio. El código entre paréntesis permite filtrar las listas de costo.</summary>
    public static string CampoPrecio(ListaPreciosFila lista) => $"Precio {lista.Nombre} ({lista.Codigo})";

    private async Task<Dictionary<Guid, ListaPreciosFila>> ListasVisiblesAsync(bool verCostos, CancellationToken ct) =>
        (await precios.ListarListasAsync(ct)).Where(l => verCostos || !l.EsCosto).ToDictionary(l => l.Id);

    private async Task<Resultado<(Producto Producto, ValorMaestroRef Grupo, IReadOnlyList<CentroRef> Centros, IReadOnlyDictionary<Guid, ListaPreciosFila> Listas)>> ValidarAsync(
        GuardarProductoRequest r, ProductoFila? existente, bool verCostos, CancellationToken ct)
    {
        var cientifico = Limpiar(r.NombreCientifico);
        var comun = Limpiar(r.NombreComun);
        var espanol = Limpiar(r.NombreEspanol);
        var etiqueta = Limpiar(r.TallaEtiqueta);
        var disponibilidad = Limpiar(r.Disponibilidad);
        var notas = LimpiarTextoLargo(r.Notas);

        var error = Primero(
            EnLista(r.Tipo, TiposProducto.Todos, "Tipo"),
            Requerido(cientifico, "Nombre científico", 200, 2),
            Opcional(comun, "Nombre común", 300),
            Opcional(espanol, "Nombre en español", 300),
            r.TallaMinCm is < 0 or > 999 || r.TallaMaxCm is < 0 or > 999 ? Error.Validacion("La talla va de 0 a 999 cm.") : null,
            r.TallaMinCm > r.TallaMaxCm ? Error.Validacion("La talla mínima no puede ser mayor que la máxima.") : null,
            Opcional(etiqueta, "Talla (etiqueta)", 30),
            r.BolsaMin is < 1 || r.BolsaMax is < 1 ? Error.Validacion("Los peces por bolsa deben ser al menos 1.") : null,
            r.BolsaMin > r.BolsaMax ? Error.Validacion("Los peces por bolsa: el mínimo no puede ser mayor que el máximo.") : null,
            Opcional(disponibilidad, "Disponibilidad", 50),
            Opcional(notas, "Notas", 2000));
        if (error is not null)
        {
            return error;
        }

        if (await maestros.ObtenerAsync(TablaGrupos, r.GrupoId, ct) is not { } grupo
            || (!grupo.Activo && existente?.GrupoId != grupo.Id))
        {
            return Error.Validacion("Elige un grupo de producto activo.");
        }

        var centrosElegidos = await ValidarCentrosAsync(r.CentrosAcopio, existente, ct);
        if (!centrosElegidos.Exitoso)
        {
            return centrosElegidos.Error!;
        }

        var listas = (await precios.ListarListasAsync(ct)).ToDictionary(l => l.Id);
        foreach (var (listaId, precio) in r.Precios ?? new Dictionary<Guid, decimal?>())
        {
            if (!listas.TryGetValue(listaId, out var lista) || (lista.EsCosto && !verCostos))
            {
                return listas.ContainsKey(listaId)
                    ? Error.Prohibido("No tienes permiso para ver ni editar el costo de compra.")
                    : Error.Validacion("Una de las listas de precios no existe.");
            }

            if (precio is < 0 or > 10_000_000_000m)
            {
                return Error.Validacion($"El precio en «{lista.Nombre}» no es válido.");
            }
        }

        var producto = new Producto
        {
            Codigo = "",
            GrupoId = grupo.Id,
            Tipo = r.Tipo!,
            NombreCientifico = cientifico!,
            NombreComun = comun,
            NombreEspanol = espanol,
            TallaMinCm = r.TallaMinCm,
            TallaMaxCm = r.TallaMaxCm,
            TallaEtiqueta = etiqueta,
            BolsaMin = r.BolsaMin,
            BolsaMax = r.BolsaMax,
            Disponibilidad = disponibilidad,
            Activo = r.Activo,
            Notas = notas,
        };
        return (producto, new ValorMaestroRef(grupo.Id, grupo.Codigo, grupo.Nombre), centrosElegidos.Valor, listas);
    }

    private async Task<Resultado<IReadOnlyList<CentroRef>>> ValidarCentrosAsync(
        IReadOnlyList<Guid>? pedidos, ProductoFila? existente, CancellationToken ct)
    {
        var ids = pedidos ?? [];
        if (ids.Count > MaximoCentros)
        {
            return Error.Validacion($"Un producto tiene como máximo {MaximoCentros} centros de acopio.");
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            return Error.Validacion("Hay un centro de acopio repetido.");
        }

        var actuales = existente is null ? [] : (await productos.CentrosAsync(existente.Id, ct)).Select(c => c.CentroAcopioId).ToHashSet();
        var resultado = new List<CentroRef>(ids.Count);
        foreach (var id in ids)
        {
            var centro = await centros.ObtenerAsync(id, ct);
            if (centro is null || (!centro.Activo && !actuales.Contains(id)))
            {
                return Error.Validacion("Elige centros de acopio activos.");
            }

            resultado.Add(new CentroRef(centro.Id, centro.Nombre));
        }

        return resultado;
    }

    private static IEnumerable<CambioAuditado> CambiosDePrecio(
        IReadOnlyDictionary<Guid, decimal> antes, IReadOnlyDictionary<Guid, decimal?> pedidos, IReadOnlyDictionary<Guid, ListaPreciosFila> listas)
    {
        foreach (var (listaId, valor) in pedidos)
        {
            var anterior = antes.TryGetValue(listaId, out var a) ? a : (decimal?)null;
            var nuevo = valor is > 0 ? valor : null;
            if (anterior != nuevo && listas.TryGetValue(listaId, out var lista))
            {
                yield return new CambioAuditado(CampoPrecio(lista), Formato(anterior), Formato(nuevo));
            }
        }
    }

    private static string? Formato(decimal? valor) => valor?.ToString("G29", CultureInfo.InvariantCulture);

    private static ProductoDetalle Detalle(ProductoFila p, IEnumerable<ProductoCentroFila> centrosProducto, IEnumerable<PrecioFila> preciosProducto) =>
        new(p.Id, p.Codigo, p.GrupoId, p.GrupoCodigo, p.GrupoNombre, p.Tipo, p.NombreCientifico, p.NombreComun, p.NombreEspanol,
            p.TallaMinCm, p.TallaMaxCm, p.TallaEtiqueta, p.BolsaMin, p.BolsaMax, p.Disponibilidad, p.Activo, p.Notas, p.CreadoEn,
            [.. centrosProducto.OrderBy(c => c.Orden).Select(c => new CentroRef(c.CentroAcopioId, c.Nombre))],
            preciosProducto.ToDictionary(x => x.ListaPreciosId, x => x.Precio));

    private static ProductoAuditable Auditable(Producto p, string grupo, IEnumerable<CentroRef> centrosProducto) =>
        new(p.Codigo, grupo, p.Tipo, p.NombreCientifico, p.NombreComun, p.NombreEspanol, p.TallaMinCm, p.TallaMaxCm, p.TallaEtiqueta,
            p.BolsaMin, p.BolsaMax, p.Disponibilidad, p.Activo, p.Notas, Unir(centrosProducto));

    private static ProductoAuditable Auditable(ProductoFila p, IEnumerable<CentroRef> centrosProducto) =>
        new(p.Codigo, p.GrupoNombre, p.Tipo, p.NombreCientifico, p.NombreComun, p.NombreEspanol, p.TallaMinCm, p.TallaMaxCm,
            p.TallaEtiqueta, p.BolsaMin, p.BolsaMax, p.Disponibilidad, p.Activo, p.Notas, Unir(centrosProducto));

    private static string? Unir(IEnumerable<CentroRef> centrosProducto)
    {
        var nombres = centrosProducto.Select(c => c.Nombre).ToList();
        return nombres.Count == 0 ? null : string.Join(", ", nombres);
    }

    private static Error NoExiste() => Error.NoEncontrado("El producto no existe.");

    private sealed record ValorMaestroRef(Guid Id, string Codigo, string Nombre);
}
