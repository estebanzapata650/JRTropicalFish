using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Domain.Catalogos;

namespace JRTropicalFish.Tests.Catalogos;

/// <summary>
/// Catálogo en memoria con las mismas reglas que las funciones PL/pgSQL: precios solo de las listas
/// indicadas (null/0 quita), centros reemplazados, código siguiente del grupo de 5 en 5.
/// Siembra las listas del Excel con sus reglas.
/// </summary>
public sealed class CatalogosEnMemoria :
    IProductoRepository, IPrecioRepository, ICentroAcopioRepository, IMaestroRepository, IClienteRepository,
    IAuditoriaRepository, IReloj, IUsuarioActual
{
    public DateTime Ahora { get; set; } = new(2026, 11, 21, 15, 0, 0, DateTimeKind.Utc);

    public Dictionary<Guid, Producto> Productos { get; } = [];
    public Dictionary<Guid, List<Guid>> CentrosDe { get; } = [];
    public Dictionary<(Guid Producto, Guid Lista), decimal> Precios { get; } = [];
    public Dictionary<Guid, ListaPrecios> Listas { get; } = [];
    public Dictionary<Guid, CentroAcopio> Centros { get; } = [];
    public List<ValorMaestro> Grupos { get; } = [];
    public List<ValorMaestro> Cuencas { get; } = [];
    public List<RegistroAuditoria> Auditoria { get; } = [];
    public ParametrosPreciosInfo Parametros { get; set; } = new(19m, 3.15m, 3150m, null);

    public ValorMaestro GrupoAp { get; }
    public ValorMaestro GrupoPc { get; }
    public CentroAcopio Leticia { get; }

    public CatalogosEnMemoria()
    {
        GrupoAp = Grupo("AP", "Apistogramas");
        GrupoPc = Grupo("PC", "Criadero");
        var amazonas = new ValorMaestro(Guid.CreateVersion7(), "AMAZONAS", "Amazonas", true, Ahora);
        Cuencas.Add(amazonas);
        Leticia = new CentroAcopio { Nombre = "Leticia", CuencaId = amazonas.Id };
        Centros[Leticia.Id] = Leticia;

        var standard = Lista("STANDARD", "Standard", "USD", redondeo: 0.01m);
        Lista("MINORISTA_EXTERIOR", "Minoristas del exterior", "USD", redondeo: 0.01m, baseId: standard.Id, factores: (1.2m, 1.2m, 1.2m));
        var costo = Lista("COSTO", "Costo de compra", "COP", redondeo: 50m, esCosto: true);
        var mayorista = Lista("MAYORISTA_NACIONAL", "Mayorista nacional", "COP", redondeo: 50m, baseId: costo.Id,
            factores: (2m, 1.3m, 2m), impuestos: true);
        Lista("DETAL_NACIONAL", "Al detal nacional", "COP", redondeo: 50m, baseId: mayorista.Id, factores: (2m, 1.5m, 2m));
    }

    public ValorMaestro Grupo(string codigo, string nombre)
    {
        var grupo = new ValorMaestro(Guid.CreateVersion7(), codigo, nombre, true, Ahora);
        Grupos.Add(grupo);
        return grupo;
    }

    public ListaPrecios Lista(
        string codigo, string nombre, string moneda, decimal redondeo, bool esCosto = false, Guid? baseId = null,
        (decimal S, decimal C, decimal E)? factores = null, bool impuestos = false)
    {
        var lista = new ListaPrecios
        {
            Codigo = codigo, Nombre = nombre, Moneda = moneda, EsCosto = esCosto, Redondeo = redondeo, ListaBaseId = baseId,
            FactorSalvaje = factores?.S, FactorCriadero = factores?.C, FactorEspecial = factores?.E, AplicaImpuestos = impuestos,
            Orden = Listas.Count,
        };
        Listas[lista.Id] = lista;
        return lista;
    }

    public ListaPrecios ListaPorCodigo(string codigo) => Listas.Values.Single(l => l.Codigo == codigo);

    public ListaPreciosFila Fila(ListaPrecios l) =>
        new(l.Id, l.Codigo, l.Nombre, l.Moneda, l.EsCosto, l.ClienteId, null, l.Orden, l.Activo, l.ListaBaseId,
            l.ListaBaseId is { } b ? Listas[b].Nombre : null, l.FactorSalvaje, l.FactorCriadero, l.FactorEspecial,
            l.AplicaImpuestos, l.Redondeo, Precios.Keys.Count(k => k.Lista == l.Id));

    private ServicioAuditoria Auditar() => new(this, this, this);

    public ProductoService ProductoService() => new(this, this, this, this, this, Auditar());

    public PrecioService PrecioService() => new(this, this, this, this, Auditar());

    public GuardarProductoRequest Solicitud(
        string? codigo = null, string tipo = TiposProducto.Criadero, IReadOnlyDictionary<Guid, decimal?>? precios = null) =>
        new(codigo, GrupoPc.Id, tipo, "Andinoacara Rivulatus", "Green Terror", "Cíclido terror verde", 4m, 5m, null, 40, 50,
            "1 - 2 Bags", true, null, [Leticia.Id], precios);

    // --- IReloj / IUsuarioActual / auditoría ---
    public DateTime AhoraUtc => Ahora;
    public DateOnly Hoy => DateOnly.FromDateTime(Ahora);
    public Guid? UsuarioId => null;
    public string? Nombre => "Pruebas";

    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct = default)
    {
        Auditoria.Add(registro);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RegistroAuditoriaLectura>> ListarPorEntidadAsync(string entidad, Guid entidadId, int limite, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaLectura>>(
            [.. Auditoria.Where(r => r.Entidad == entidad && r.EntidadId == entidadId)
                .Select(r => new RegistroAuditoriaLectura(r.Id, r.Entidad, r.EntidadId, r.Accion.ToString(), r.CambiosJson, null, null, r.FechaUtc))]);

    // --- Maestros (grupos y cuencas) ---
    private List<ValorMaestro> Maestro(string tabla) => tabla == "grupos_producto" ? Grupos : Cuencas;

    Task<IReadOnlyList<ValorMaestro>> IMaestroRepository.ListarAsync(string tabla, bool soloActivos, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ValorMaestro>>(Maestro(tabla));

    Task<ValorMaestro?> IMaestroRepository.ObtenerAsync(string tabla, Guid id, CancellationToken ct) =>
        Task.FromResult(Maestro(tabla).FirstOrDefault(v => v.Id == id));

    Task<bool> IMaestroRepository.ExisteCodigoAsync(string tabla, string codigo, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);

    Task<bool> IMaestroRepository.ExisteNombreAsync(string tabla, string nombre, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);

    Task IMaestroRepository.CrearAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct) =>
        Task.CompletedTask;

    Task IMaestroRepository.ActualizarAsync(string tabla, Guid id, string codigo, string nombre, bool activo, DateTime fecha, CancellationToken ct) =>
        Task.CompletedTask;

    Task IMaestroRepository.EliminarAsync(string tabla, Guid id, DateTime fecha, CancellationToken ct) => Task.CompletedTask;

    // --- Centros ---
    private CentroAcopioFila Fila(CentroAcopio c) =>
        new(c.Id, c.Nombre, c.CuencaId, Cuencas.First(x => x.Id == c.CuencaId).Nombre, c.Activo,
            CentrosDe.Values.Count(l => l.Contains(c.Id)), 0, Ahora);

    Task<IReadOnlyList<CentroAcopioFila>> ICentroAcopioRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CentroAcopioFila>>([.. Centros.Values.Select(Fila)]);

    Task<CentroAcopioFila?> ICentroAcopioRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Centros.TryGetValue(id, out var c) ? Fila(c) : null);

    public Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Centros.Values.Any(c => c.Id != excluirId && string.Equals(c.Nombre, nombre, StringComparison.OrdinalIgnoreCase)));

    public Task GuardarAsync(CentroAcopio centro, bool esNuevo, DateTime fecha, CancellationToken ct = default)
    {
        Centros[centro.Id] = centro;
        return Task.CompletedTask;
    }

    Task ICentroAcopioRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct)
    {
        Centros.Remove(id);
        return Task.CompletedTask;
    }

    // --- Productos ---
    private ProductoFila Fila(Producto p)
    {
        var g = Grupos.First(x => x.Id == p.GrupoId);
        return new(p.Id, p.Codigo, p.GrupoId, g.Codigo, g.Nombre, p.Tipo, p.NombreCientifico, p.NombreComun, p.NombreEspanol,
            p.TallaMinCm, p.TallaMaxCm, p.TallaEtiqueta, p.BolsaMin, p.BolsaMax, p.Disponibilidad, p.Activo, p.Notas, p.CreadoEn);
    }

    Task<IReadOnlyList<ProductoFila>> IProductoRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ProductoFila>>([.. Productos.Values.Where(p => p.EliminadoEn is null).Select(Fila)]);

    Task<ProductoFila?> IProductoRepository.ObtenerAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Productos.TryGetValue(id, out var p) && p.EliminadoEn is null ? Fila(p) : null);

    public Task<IReadOnlyList<ProductoCentroFila>> CentrosAsync(Guid? productoId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProductoCentroFila>>(
            [.. CentrosDe.Where(x => productoId is null || x.Key == productoId)
                .SelectMany(x => x.Value.Select((c, i) => new ProductoCentroFila(x.Key, c, Centros[c].Nombre, i + 1)))]);

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Productos.Values.Any(p => p.EliminadoEn is null && p.Id != excluirId && p.Codigo == codigo));

    public Task<string> SiguienteCodigoAsync(string prefijo, CancellationToken ct = default)
    {
        var sufijos = Productos.Values.Select(p => p.Codigo)
            .Where(c => c.StartsWith(prefijo, StringComparison.Ordinal) && c[prefijo.Length..].All(char.IsDigit))
            .Select(c => c[prefijo.Length..]).ToList();
        var maximo = sufijos.Count == 0 ? 0 : sufijos.Max(int.Parse);
        var ancho = sufijos.Count == 0 ? 3 : sufijos.Max(s => s.Length);
        return Task.FromResult(prefijo + ((maximo / 5 + 1) * 5).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(ancho, '0'));
    }

    public Task GuardarAsync(
        Producto producto, bool esNuevo, IReadOnlyList<Guid> centros, IReadOnlyDictionary<Guid, decimal?> precios, DateTime fecha,
        CancellationToken ct = default)
    {
        producto.CreadoEn = esNuevo ? fecha : Productos[producto.Id].CreadoEn;
        Productos[producto.Id] = producto;
        CentrosDe[producto.Id] = [.. centros];
        foreach (var (lista, precio) in precios)
        {
            if (precio is > 0)
            {
                Precios[(producto.Id, lista)] = precio.Value;
            }
            else
            {
                Precios.Remove((producto.Id, lista));
            }
        }

        return Task.CompletedTask;
    }

    Task IProductoRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct)
    {
        Productos[id].EliminadoEn = fecha;
        return Task.CompletedTask;
    }

    // --- Precios ---
    public Task<IReadOnlyList<ListaPreciosFila>> ListarListasAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ListaPreciosFila>>([.. Listas.Values.Select(Fila)]);

    public Task<ListaPreciosFila?> ObtenerListaAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Listas.TryGetValue(id, out var l) ? Fila(l) : null);

    public Task<bool> ExisteListaAsync(string campo, string valor, Guid? excluirId, CancellationToken ct = default) =>
        Task.FromResult(Listas.Values.Any(l => l.Id != excluirId
                                               && string.Equals(campo == "codigo" ? l.Codigo : l.Nombre, valor, StringComparison.OrdinalIgnoreCase)));

    public Task GuardarListaAsync(ListaPrecios lista, bool esNueva, DateTime fecha, CancellationToken ct = default)
    {
        Listas[lista.Id] = lista;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PrecioFila>> PreciosAsync(Guid? productoId, Guid? listaId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<PrecioFila>>(
            [.. Precios.Where(x => (productoId is null || x.Key.Producto == productoId) && (listaId is null || x.Key.Lista == listaId))
                .Select(x => new PrecioFila(x.Key.Producto, x.Key.Lista, x.Value))]);

    public Task GuardarLoteAsync(Guid listaId, IReadOnlyDictionary<Guid, decimal> precios, DateTime fecha, CancellationToken ct = default)
    {
        foreach (var (producto, precio) in precios)
        {
            Precios[(producto, listaId)] = precio;
        }

        return Task.CompletedTask;
    }

    public Task<ParametrosPreciosInfo> ObtenerParametrosAsync(CancellationToken ct = default) => Task.FromResult(Parametros);

    public Task ActualizarParametrosAsync(GuardarParametrosPreciosRequest p, DateTime fecha, CancellationToken ct = default)
    {
        Parametros = new ParametrosPreciosInfo(p.Iva, p.Pasarela, p.TrmReferencia, fecha);
        return Task.CompletedTask;
    }

    // --- Clientes (solo para validar el cliente de una lista) ---
    Task<IReadOnlyList<ClienteFila>> IClienteRepository.ListarAsync(string? busqueda, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ClienteFila>>([]);

    Task<ClienteFila?> IClienteRepository.ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult<ClienteFila?>(null);

    Task<bool> IClienteRepository.ExisteCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);

    Task<bool> IClienteRepository.ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);

    Task<bool> IClienteRepository.TieneMovimientosAsync(Guid id, CancellationToken ct) => Task.FromResult(false);

    Task IClienteRepository.CrearAsync(Domain.Crm.Cliente cliente, CancellationToken ct) => Task.CompletedTask;

    Task IClienteRepository.ActualizarAsync(Domain.Crm.Cliente cliente, CancellationToken ct) => Task.CompletedTask;

    Task IClienteRepository.EliminarAsync(Guid id, DateTime fecha, CancellationToken ct) => Task.CompletedTask;
}
