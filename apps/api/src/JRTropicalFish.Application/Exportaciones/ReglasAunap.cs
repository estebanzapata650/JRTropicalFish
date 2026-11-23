namespace JRTropicalFish.Application.Exportaciones;

/// <summary>
/// Reglas AUNAP, sin base de datos. Todo documento para autoridades sale de lo real: las especies, cantidades y
/// valores de las líneas (solicitud) o del empaque (aduana y aeropuerto), agrupados por su especie AUNAP. Nunca hay
/// factores, sustituciones de especie ni cantidades distintas a las reales.
/// </summary>
public static class ReglasAunap
{
    /// <summary>
    /// Resumen por especie AUNAP: solicitado (líneas del pedido), autorizado (permiso) y empacado (empaque real).
    /// Los productos sin código AUNAP se listan aparte: no se pueden declarar.
    /// </summary>
    public static ResumenAunap Resumen(
        IReadOnlyList<(Guid ProductoId, string ProductoCodigo, string NombreCientifico, int Cantidad)> lineas,
        IReadOnlyList<(Guid ProductoId, int Cantidad)> empaque,
        IReadOnlyDictionary<Guid, ProductoAunapFila> mapa,
        PermisoAunapFila? permiso,
        IReadOnlyDictionary<Guid, int> autorizados)
    {
        var sinCodigo = new Dictionary<Guid, ProductoSinAunap>();
        var porEspecie = new Dictionary<Guid, (ProductoAunapFila Especie, HashSet<string> Productos, int Solicitado, int Empacado)>();

        void Sumar(Guid productoId, string codigo, string nombre, int solicitado, int empacado)
        {
            if (!mapa.TryGetValue(productoId, out var p) || p.EspecieAunapId is not { } especie)
            {
                var previo = sinCodigo.GetValueOrDefault(productoId);
                sinCodigo[productoId] = new ProductoSinAunap(productoId, codigo, nombre, (previo?.Cantidad ?? 0) + Math.Max(solicitado, empacado));
                return;
            }

            var (fila, productos, previoSolicitado, previoEmpacado) = porEspecie.TryGetValue(especie, out var a) ? a : (p, new HashSet<string>(), 0, 0);
            productos.Add(codigo);
            porEspecie[especie] = (fila, productos, previoSolicitado + solicitado, previoEmpacado + empacado);
        }

        foreach (var l in lineas)
        {
            Sumar(l.ProductoId, l.ProductoCodigo, l.NombreCientifico, l.Cantidad, 0);
        }

        foreach (var e in empaque)
        {
            var p = mapa.GetValueOrDefault(e.ProductoId);
            Sumar(e.ProductoId, p?.ProductoCodigo ?? "?", p?.NombreCientifico ?? "?", 0, e.Cantidad);
        }

        var hayEmpaque = empaque.Count > 0;
        var filas = porEspecie
            .Select(x =>
            {
                var (p, productos, solicitado, empacado) = x.Value;
                int? autorizado = permiso is null ? null : autorizados.TryGetValue(x.Key, out var n) ? n : null;
                return new FilaAunap(
                    x.Key, p.AunapCodigo!, p.AunapNombreCientifico!, p.AunapNombreComun, p.AunapFamilia, [.. productos.Order()],
                    solicitado, autorizado, empacado, Estado(permiso is not null, autorizado, hayEmpaque ? empacado : solicitado));
            })
            .OrderBy(f => f.NombreCientifico, StringComparer.Ordinal)
            .ToList();

        var avisos = new List<string>();
        if (sinCodigo.Count > 0)
        {
            avisos.Add($"Sin código AUNAP (no se pueden declarar ni exportar): {string.Join(", ", sinCodigo.Values.Select(s => $"{s.ProductoCodigo} {s.NombreCientifico}"))}.");
        }

        var exceden = filas.Where(f => f.Estado is EstadosAunap.Excede or EstadosAunap.NoAutorizada).ToList();
        if (exceden.Count > 0)
        {
            avisos.Add($"Supera lo autorizado en el permiso: {string.Join(", ", exceden.Select(f => f.NombreCientifico))}. Pide la ampliación antes de despachar.");
        }

        return new ResumenAunap(permiso, filas, [.. sinCodigo.Values.OrderBy(s => s.ProductoCodigo, StringComparer.Ordinal)], hayEmpaque, avisos);
    }

    public static string Estado(bool hayPermiso, int? autorizado, int cantidad) =>
        !hayPermiso ? EstadosAunap.SinPermiso
        : autorizado is null or 0 ? (cantidad > 0 ? EstadosAunap.NoAutorizada : EstadosAunap.Ok)
        : cantidad > autorizado ? EstadosAunap.Excede
        : EstadosAunap.Ok;

    /// <summary>
    /// Agrupa filas reales (producto, cantidad, valor) por especie AUNAP. Si algún producto no tiene código, devuelve
    /// el error: el documento no se puede generar (declararlo con otra especie no es una opción).
    /// </summary>
    public static (IReadOnlyList<LineaDeclarada> Lineas, string? Error) Agrupar(
        IEnumerable<(Guid ProductoId, string ProductoCodigo, string NombreCientifico, int Cantidad, decimal Total)> filas,
        IReadOnlyDictionary<Guid, ProductoAunapFila> mapa)
    {
        var lista = filas.ToList();
        var sin = lista.Where(f => !mapa.TryGetValue(f.ProductoId, out var p) || p.EspecieAunapId is null)
            .Select(f => $"{f.ProductoCodigo} {f.NombreCientifico}").Distinct().ToList();
        if (sin.Count > 0)
        {
            return ([], $"Estas especies no tienen código AUNAP y no se pueden declarar: {string.Join(", ", sin)}. "
                        + "Enlázalas en Exportaciones → AUNAP (solo si es la misma especie) o retíralas del embarque.");
        }

        var lineas = lista
            .GroupBy(f => mapa[f.ProductoId].EspecieAunapId!.Value)
            .Select(g =>
            {
                var p = mapa[g.First().ProductoId];
                return new LineaDeclarada(p.AunapCodigo!, p.AunapFamilia ?? "", p.AunapNombreCientifico!, p.AunapNombreComun ?? "",
                    g.Sum(x => x.Cantidad), g.Sum(x => x.Total));
            })
            .OrderBy(l => l.Familia, StringComparer.Ordinal).ThenBy(l => l.NombreCientifico, StringComparer.Ordinal)
            .ToList();
        return (lineas, null);
    }

    public static string? ValidarImportacion(IReadOnlyList<EspecieAunapImportar> especies)
    {
        if (especies.Count is 0 or > 5000)
        {
            return "El listado debe traer entre 1 y 5.000 especies.";
        }

        if (especies.Any(e => string.IsNullOrWhiteSpace(e.Codigo) || e.Codigo.Trim().Length > 20 || string.IsNullOrWhiteSpace(e.NombreCientifico)
                              || e.NombreCientifico.Trim().Length > 200))
        {
            return "Cada especie necesita código (hasta 20 caracteres) y nombre científico (hasta 200).";
        }

        var repetido = especies.GroupBy(e => e.Codigo!.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        return repetido is null ? null : $"El código AUNAP {repetido.Key} está repetido en el listado.";
    }
}
