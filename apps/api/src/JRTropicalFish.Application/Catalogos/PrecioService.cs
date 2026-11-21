using System.Text.RegularExpressions;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Domain.Catalogos;
using JRTropicalFish.Domain.Crm;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Catalogos;

/// <summary>
/// Listas de precios, su regla de cálculo y los parámetros (IVA, pasarela, TRM de referencia).
/// Las listas de costo solo existen para quien tiene <c>catalogos:ver-costos</c>.
/// </summary>
public partial class PrecioService(
    IPrecioRepository precios,
    IProductoRepository productos,
    IClienteRepository clientes,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadLista = "ListaPrecios";
    public const string EntidadParametros = "ParametrosPrecios";

    /// <summary>Id fijo para auditar los parámetros (la tabla tiene una sola fila).</summary>
    public static readonly Guid IdParametros = new("00000000-0000-0000-0000-000000000002");

    public async Task<IReadOnlyList<ListaPreciosFila>> ListasAsync(bool verCostos, CancellationToken ct = default) =>
        [.. (await precios.ListarListasAsync(ct)).Where(l => verCostos || !l.EsCosto)];

    public async Task<Resultado<Guid>> CrearListaAsync(GuardarListaPreciosRequest r, bool verCostos, CancellationToken ct = default)
    {
        var codigo = Limpiar(r.Codigo)?.ToUpperInvariant();
        if (codigo is null || !CodigoRegex().IsMatch(codigo))
        {
            return Error.Validacion("El código de la lista va en mayúsculas, sin espacios (ej. CLIENTE_JAPON).");
        }

        if (await precios.ExisteListaAsync("codigo", codigo, null, ct))
        {
            return Error.Conflicto($"Ya existe una lista con el código {codigo}.");
        }

        if (r.EsCosto && !verCostos)
        {
            return SinPermisoCostos();
        }

        var lista = new ListaPrecios { Codigo = codigo, Nombre = "", Moneda = r.Moneda ?? "", EsCosto = r.EsCosto };
        var datos = await AplicarAsync(lista, r, null, verCostos, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        await precios.GuardarListaAsync(lista, esNueva: true, reloj.AhoraUtc, ct);
        await auditoria.CreacionAsync(EntidadLista, lista.Id, datos.Valor, ct: ct);
        return lista.Id;
    }

    /// <summary>Código, moneda y «es costo» no cambian: los precios guardados dependen de ellos.</summary>
    public async Task<Resultado<Guid>> ActualizarListaAsync(Guid id, GuardarListaPreciosRequest r, bool verCostos, CancellationToken ct = default)
    {
        if (await Visible(id, verCostos, ct) is not { } existente)
        {
            return NoExiste();
        }

        var lista = new ListaPrecios
        {
            Id = id, Codigo = existente.Codigo, Nombre = "", Moneda = existente.Moneda, EsCosto = existente.EsCosto,
        };
        var datos = await AplicarAsync(lista, r, existente, verCostos, ct);
        if (!datos.Exitoso)
        {
            return datos.Error!;
        }

        await precios.GuardarListaAsync(lista, esNueva: false, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadLista, id, ComparadorAuditoria.Comparar(Auditable(existente), datos.Valor), ct);
        return id;
    }

    /// <summary>
    /// Recalcula todos los precios de una lista derivada desde su lista base con la regla. Reemplaza los
    /// precios existentes de los productos que tienen base; los que no tienen base quedan como estaban.
    /// </summary>
    public async Task<Resultado<ResultadoRecalculo>> RecalcularAsync(Guid id, bool verCostos, CancellationToken ct = default)
    {
        if (await Visible(id, verCostos, ct) is not { } lista)
        {
            return NoExiste();
        }

        if (lista.ListaBaseId is not { } baseId)
        {
            return Error.Validacion("Esta lista no tiene regla: sus precios se editan a mano en cada producto.");
        }

        var parametros = await precios.ObtenerParametrosAsync(ct);
        var bases = (await precios.PreciosAsync(null, baseId, ct)).ToDictionary(p => p.ProductoId, p => p.Precio);
        var actuales = (await precios.PreciosAsync(null, id, ct)).ToDictionary(p => p.ProductoId, p => p.Precio);
        var calculados = new Dictionary<Guid, decimal>();
        var sinBase = 0;
        foreach (var producto in await productos.ListarAsync(ct))
        {
            var valor = ReglasPrecio.Calcular(bases.GetValueOrDefault(producto.Id), producto.Tipo, lista, parametros);
            if (valor is > 0)
            {
                calculados[producto.Id] = valor.Value;
            }
            else
            {
                sinBase++;
            }
        }

        var cambiados = calculados.Count(c => !actuales.TryGetValue(c.Key, out var antes) || antes != c.Value);
        if (cambiados > 0)
        {
            await precios.GuardarLoteAsync(id, calculados, reloj.AhoraUtc, ct);
        }

        await auditoria.CambiosAsync(
            EntidadLista, id,
            [new CambioAuditado("Recalculo", null, $"{cambiados} precios actualizados desde «{lista.ListaBaseNombre}»")],
            ct);
        return new ResultadoRecalculo(calculados.Count, cambiados, sinBase);
    }

    public async Task<Resultado<IReadOnlyList<RegistroAuditoriaLectura>>> HistorialListaAsync(
        Guid id, bool verCostos, CancellationToken ct = default) =>
        await Visible(id, verCostos, ct) is null
            ? NoExiste()
            : Resultado.Ok(await auditoria.HistorialAsync(EntidadLista, id, ct));

    public Task<ParametrosPreciosInfo> ParametrosAsync(CancellationToken ct = default) => precios.ObtenerParametrosAsync(ct);

    public async Task<Resultado> ActualizarParametrosAsync(GuardarParametrosPreciosRequest r, CancellationToken ct = default)
    {
        var error = Primero(
            r.Iva is < 0 or > 100 ? Error.Validacion("El IVA va de 0 a 100 %.") : null,
            r.Pasarela is < 0 or > 100 ? Error.Validacion("La comisión de pasarela va de 0 a 100 %.") : null,
            OportunidadTrm(r.TrmReferencia));
        if (error is not null)
        {
            return error;
        }

        var anterior = await precios.ObtenerParametrosAsync(ct);
        await precios.ActualizarParametrosAsync(r, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(
            EntidadParametros, IdParametros,
            ComparadorAuditoria.Comparar(
                new ParametrosPreciosAuditable(anterior.Iva, anterior.Pasarela, anterior.TrmReferencia),
                new ParametrosPreciosAuditable(r.Iva, r.Pasarela, r.TrmReferencia)),
            ct);
        return Resultado.Ok();
    }

    private static Error? OportunidadTrm(decimal trm) => Crm.OportunidadService.ValidarTrm(trm);

    /// <summary>Valida los campos editables y los copia a <paramref name="lista"/>. Devuelve la proyección auditable.</summary>
    private async Task<Resultado<ListaPreciosAuditable>> AplicarAsync(
        ListaPrecios lista, GuardarListaPreciosRequest r, ListaPreciosFila? existente, bool verCostos, CancellationToken ct)
    {
        var nombre = Limpiar(r.Nombre);
        var error = Primero(
            Requerido(nombre, "Nombre", 100, 2),
            EnLista(lista.Moneda, Monedas.Todos, "Moneda"),
            r.Redondeo is <= 0 or > 100_000 ? Error.Validacion("El redondeo es el múltiplo al que se redondea: 50, 100, 0.01…") : null,
            r.Orden is < 0 or > 10_000 ? Error.Validacion("El orden no es válido.") : null);
        if (error is not null)
        {
            return error;
        }

        if (await precios.ExisteListaAsync("nombre", nombre!, existente?.Id, ct))
        {
            return Error.Conflicto($"Ya existe una lista llamada «{nombre}».");
        }

        string? clienteNombre = null;
        if (r.ClienteId is { } clienteId)
        {
            if (await clientes.ObtenerAsync(clienteId, ct) is not { } cliente)
            {
                return Error.Validacion("El cliente de la lista no existe.");
            }

            clienteNombre = cliente.NombreEmpresa;
        }

        string? baseNombre = null;
        if (r.ListaBaseId is { } baseId)
        {
            var todas = (await precios.ListarListasAsync(ct)).ToDictionary(l => l.Id);
            if (!todas.TryGetValue(baseId, out var listaBase) || (listaBase.EsCosto && !verCostos))
            {
                return todas.ContainsKey(baseId) ? SinPermisoCostos() : Error.Validacion("La lista base no existe.");
            }

            if (listaBase.Moneda != lista.Moneda)
            {
                return Error.Validacion("La lista base debe estar en la misma moneda.");
            }

            // Sin ciclos: la cadena de bases no puede volver a esta lista.
            for (Guid? actual = baseId; actual is { } paso; actual = todas.GetValueOrDefault(paso)?.ListaBaseId)
            {
                if (paso == lista.Id)
                {
                    return Error.Validacion("La regla formaría un ciclo: una lista no puede depender de sí misma.");
                }
            }

            var factores = new[] { r.FactorSalvaje, r.FactorCriadero, r.FactorEspecial };
            if (factores.All(f => f is null) || factores.Any(f => f is <= 0 or > 100))
            {
                return Error.Validacion("Con lista base, indica al menos un factor (mayor que 0 y hasta 100).");
            }

            baseNombre = listaBase.Nombre;
        }

        lista.Nombre = nombre!;
        lista.ClienteId = r.ClienteId;
        lista.Orden = r.Orden;
        lista.Activo = r.Activo;
        lista.ListaBaseId = r.ListaBaseId;
        lista.FactorSalvaje = r.ListaBaseId is null ? null : r.FactorSalvaje;
        lista.FactorCriadero = r.ListaBaseId is null ? null : r.FactorCriadero;
        lista.FactorEspecial = r.ListaBaseId is null ? null : r.FactorEspecial;
        lista.AplicaImpuestos = r.ListaBaseId is not null && r.AplicaImpuestos;
        lista.Redondeo = r.Redondeo;
        return new ListaPreciosAuditable(
            lista.Codigo, lista.Nombre, lista.Moneda, lista.EsCosto, clienteNombre, lista.Orden, lista.Activo, baseNombre,
            lista.FactorSalvaje, lista.FactorCriadero, lista.FactorEspecial, lista.AplicaImpuestos, lista.Redondeo);
    }

    private async Task<ListaPreciosFila?> Visible(Guid id, bool verCostos, CancellationToken ct) =>
        await precios.ObtenerListaAsync(id, ct) is { } l && (verCostos || !l.EsCosto) ? l : null;

    private static ListaPreciosAuditable Auditable(ListaPreciosFila l) =>
        new(l.Codigo, l.Nombre, l.Moneda, l.EsCosto, l.ClienteNombre, l.Orden, l.Activo, l.ListaBaseNombre, l.FactorSalvaje,
            l.FactorCriadero, l.FactorEspecial, l.AplicaImpuestos, l.Redondeo);

    private static Error NoExiste() => Error.NoEncontrado("La lista de precios no existe.");

    private static Error SinPermisoCostos() => Error.Prohibido("No tienes permiso para ver ni editar el costo de compra.");

    [GeneratedRegex("^[A-Z][A-Z0-9_]{1,29}$")]
    private static partial Regex CodigoRegex();
}
