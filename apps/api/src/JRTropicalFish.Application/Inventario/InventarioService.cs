using System.Globalization;
using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Inventario;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Inventario;

/// <summary>
/// Inventario: acuarios, existencias, operaciones de stock (ubicar, trasladar, enfermo, aliviado,
/// mortalidad, ajuste), lotes con su trazabilidad, saldo inicial migrado y el dashboard.
/// El stock solo cambia por <c>sp_inventario_mover</c>; aquí se valida qué operación es y quién la hace.
/// </summary>
public partial class InventarioService(
    IInventarioRepository inventario,
    IProductoRepository productos,
    IConsecutivoRepository consecutivos,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadAcuario = "Acuario";
    public const string PrefijoLote = "LOT";

    /// <summary>Días a partir de los cuales un lote con stock se considera antiguo (indicador del Excel).</summary>
    public const int DiasLoteAntiguo = 20;

    private Operador Operador => new(usuario.UsuarioId, usuario.Nombre);

    // --- Acuarios ---

    public Task<IReadOnlyList<AcuarioFila>> AcuariosAsync(CancellationToken ct = default) => inventario.AcuariosAsync(ct);

    public async Task<Resultado<Guid>> CrearAcuarioAsync(GuardarAcuarioRequest r, CancellationToken ct = default)
    {
        var codigo = Limpiar(r.Codigo)?.ToUpperInvariant();
        if (codigo is null || !CodigoAcuarioRegex().IsMatch(codigo))
        {
            return Error.Validacion("El código del acuario es letras y números, ej. A101.");
        }

        if (ValidarAcuario(r) is { } error)
        {
            return error;
        }

        if (await inventario.ExisteAcuarioAsync(codigo, null, ct))
        {
            return Error.Conflicto($"Ya existe el acuario {codigo}.");
        }

        var acuario = NuevoAcuario(codigo, r);
        await inventario.GuardarAcuarioAsync(acuario, esNuevo: true, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(EntidadAcuario, acuario.Id, Auditable(acuario), ct: ct);
        return acuario.Id;
    }

    /// <summary>El código no cambia (va impreso en el acuario y en el kardex).</summary>
    public async Task<Resultado<Guid>> ActualizarAcuarioAsync(Guid id, GuardarAcuarioRequest r, CancellationToken ct = default)
    {
        if (await inventario.AcuarioAsync(id, ct) is not { } existente)
        {
            return Error.NoEncontrado("El acuario no existe.");
        }

        if (ValidarAcuario(r) is { } error)
        {
            return error;
        }

        if (!r.Activo && existente.Activo && existente.Sanos + existente.Enfermos > 0)
        {
            return Error.Conflicto($"El acuario {existente.Codigo} tiene peces: trasládalos antes de desactivarlo.");
        }

        var acuario = NuevoAcuario(existente.Codigo, r);
        acuario.Id = id;
        await inventario.GuardarAcuarioAsync(acuario, esNuevo: false, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadAcuario, id,
            ComparadorAuditoria.Comparar(
                new AcuarioAuditable(existente.Codigo, existente.Sector, existente.Nivel, existente.Posicion, existente.CapacidadLitros, existente.Activo),
                Auditable(acuario)),
            ct);
        return id;
    }

    // --- Existencias, lotes, kardex ---

    public Task<IReadOnlyList<ExistenciaFila>> ExistenciasAsync(
        Guid? productoId = null, Guid? loteId = null, Guid? acuarioId = null, CancellationToken ct = default) =>
        inventario.ExistenciasAsync(reloj.Hoy, productoId, loteId, acuarioId, ct);

    public Task<IReadOnlyList<LoteFila>> LotesAsync(Guid? productoId, bool soloConStock, CancellationToken ct = default) =>
        inventario.LotesAsync(reloj.Hoy, productoId, soloConStock, ct);

    public Task<IReadOnlyList<MovimientoFila>> MovimientosAsync(int limite = 200, CancellationToken ct = default) =>
        inventario.MovimientosAsync(null, Math.Clamp(limite, 1, 1000), ct);

    /// <summary>Aplica operaciones de stock (todas o ninguna). Los ajustes exigen <c>inventario:ajustar</c>.</summary>
    public async Task<Resultado> OperarAsync(OperacionesStockRequest r, bool puedeAjustar, CancellationToken ct = default)
    {
        var operaciones = r.Operaciones ?? [];
        if (operaciones.Count is 0 or > 200)
        {
            return Error.Validacion("Indica entre 1 y 200 operaciones.");
        }

        var limpias = new List<OperacionStock>(operaciones.Count);
        foreach (var o in operaciones)
        {
            if (ReglasStock.Validar(o, puedeAjustar) is { } error)
            {
                return error;
            }

            var nota = Limpiar(o.Nota);
            if (Opcional(nota, "Nota", 500) is { } errorNota)
            {
                return errorNota;
            }

            limpias.Add(o with { Nota = nota });
        }

        var falla = await inventario.MoverAsync(limpias, Operador, reloj.AhoraUtc, ct);
        return falla is null ? Resultado.Ok() : FallasInventario.AError(falla);
    }

    /// <summary>
    /// Carga el stock inicial de un lote migrado de la app anterior (conserva su código LOT-xxxx). Las
    /// cantidades en cero se ignoran. Idempotente: un código existente da 409.
    /// </summary>
    public async Task<Resultado<Guid>> SaldoInicialAsync(SaldoInicialRequest r, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(r.ProductoId, ct) is null)
        {
            return Error.Validacion("El producto no existe.");
        }

        var existencias = (r.Existencias ?? []).Where(e => e.Cantidad > 0).ToList();
        if (existencias.Any(e => e.Condicion is null || !Condiciones.Todos.Contains(e.Condicion)))
        {
            return Error.Validacion("La condición debe ser sano o enfermo.");
        }

        if (r.Recibidos < 0 || r.Muertos < 0 || r.Muertos > Math.Max(r.Recibidos, existencias.Sum(e => e.Cantidad) + r.Muertos))
        {
            return Error.Validacion("Las cantidades del lote no son válidas.");
        }

        var codigoPedido = Limpiar(r.Codigo)?.ToUpperInvariant();
        string codigo;
        if (codigoPedido is null)
        {
            codigo = await consecutivos.SiguienteAsync(PrefijoLote, ct);
        }
        else
        {
            var coincidencia = CodigoLoteRegex().Match(codigoPedido);
            if (!coincidencia.Success)
            {
                return Error.Validacion("El código del lote debe tener la forma LOT-0001.");
            }

            if (await inventario.ExisteLoteAsync(codigoPedido, ct))
            {
                return Error.Conflicto($"El lote {codigoPedido} ya existe.");
            }

            codigo = codigoPedido;
            await consecutivos.AjustarAsync(PrefijoLote, int.Parse(coincidencia.Groups[1].Value, CultureInfo.InvariantCulture), ct);
        }

        var acuarios = (await inventario.AcuariosAsync(ct)).ToDictionary(a => a.Codigo, StringComparer.OrdinalIgnoreCase);
        var destino = new List<(Guid? AcuarioId, string Condicion, int Cantidad)>();
        foreach (var e in existencias)
        {
            Guid? acuarioId = null;
            if (Limpiar(e.AcuarioCodigo) is { } codigoAcuario)
            {
                if (!acuarios.TryGetValue(codigoAcuario, out var acuario))
                {
                    return Error.Validacion($"El acuario {codigoAcuario} no existe.");
                }

                acuarioId = acuario.Id;
            }

            destino.Add((acuarioId, e.Condicion!, e.Cantidad));
        }

        var stock = destino.Sum(d => d.Cantidad);
        var lote = new Lote
        {
            Codigo = codigo,
            ProductoId = r.ProductoId,
            ProveedorId = r.ProveedorId,
            CentroAcopioId = r.CentroAcopioId,
            FechaIngreso = r.FechaIngreso ?? reloj.Hoy,
            CostoUnitario = r.CostoUnitario,
            Recibidos = Math.Max(r.Recibidos, stock + r.Muertos),
            MuertosLlegada = r.Muertos,
            Origen = "saldo_inicial",
        };
        var falla = await inventario.CrearSaldoInicialAsync(lote, destino, Operador, reloj.AhoraUtc, ct);
        return falla is null ? lote.Id : FallasInventario.AError(falla);
    }

    // --- Resúmenes ---

    /// <summary>Necesidades de compra (paso 6): productos con demanda de clientes y lo que falta comprar.</summary>
    public async Task<IReadOnlyList<InventarioProductoFila>> NecesidadesAsync(CancellationToken ct = default) =>
        [.. (await inventario.PorProductoAsync(ct)).Where(p => p.PendienteClientes > 0).OrderByDescending(p => p.Faltante)];

    private static Error? ValidarAcuario(GuardarAcuarioRequest r) => Primero(
        Requerido(Limpiar(r.Sector), "Sector", 10),
        r.Nivel is < 0 or > 100 ? Error.Validacion("El nivel no es válido.") : null,
        r.Posicion is < 0 or > 1000 ? Error.Validacion("La posición no es válida.") : null,
        r.CapacidadLitros is <= 0 or > 100_000 ? Error.Validacion("La capacidad en litros no es válida.") : null);

    private static Acuario NuevoAcuario(string codigo, GuardarAcuarioRequest r) => new()
    {
        Codigo = codigo,
        Sector = Limpiar(r.Sector)!.ToUpperInvariant(),
        Nivel = r.Nivel,
        Posicion = r.Posicion,
        CapacidadLitros = r.CapacidadLitros,
        Activo = r.Activo,
    };

    private static AcuarioAuditable Auditable(Acuario a) => new(a.Codigo, a.Sector, a.Nivel, a.Posicion, a.CapacidadLitros, a.Activo);

    [GeneratedRegex("^[A-Z0-9][A-Z0-9-]{0,19}$")]
    private static partial Regex CodigoAcuarioRegex();

    [GeneratedRegex(@"^LOT-(\d{1,6})$")]
    private static partial Regex CodigoLoteRegex();
}
