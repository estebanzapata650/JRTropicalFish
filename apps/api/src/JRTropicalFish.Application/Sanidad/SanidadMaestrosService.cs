using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Sanidad;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Sanidad;

/// <summary>
/// Maestros de sanidad: medicamentos (dosis de referencia y su fuente), alimentos, dietas por grupo o producto,
/// fichas técnicas por especie, protocolos (borrador → aprobado) y parámetros. Todo cambio queda auditado.
/// </summary>
public class SanidadMaestrosService(
    ISanidadMaestrosRepository repositorio,
    IProductoRepository productos,
    ICentroAcopioRepository centros,
    IMaestroRepository maestros,
    IUsuarioRepository usuarios,
    IInventarioRepository inventario,
    IConsecutivoRepository consecutivos,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadMedicamento = "Medicamento";
    public const string EntidadAlimento = "Alimento";
    public const string EntidadDieta = "Dieta";
    public const string EntidadFicha = "FichaTecnica";
    public const string EntidadProtocolo = "Protocolo";
    public const string EntidadParametros = "ParametrosSanidad";
    public const string PrefijoProtocolo = "PRT";

    /// <summary>Id fijo para auditar los parámetros (fila única).</summary>
    public static readonly Guid IdParametros = new("00000000-0000-0000-0000-000000000004");

    // --- Medicamentos ---

    public Task<IReadOnlyList<MedicamentoFila>> MedicamentosAsync(CancellationToken ct = default) => repositorio.MedicamentosAsync(ct);

    public async Task<Resultado<Guid>> GuardarMedicamentoAsync(Guid? id, GuardarMedicamentoRequest r, CancellationToken ct = default)
    {
        var existentes = await repositorio.MedicamentosAsync(ct);
        var anterior = id is { } buscado ? existentes.FirstOrDefault(m => m.Id == buscado) : null;
        if (id is not null && anterior is null)
        {
            return Error.NoEncontrado("El medicamento no existe.");
        }

        var codigo = Limpiar(r.Codigo)?.ToUpperInvariant();
        var nombre = Limpiar(r.Nombre);
        var error = Primero(
            Requerido(codigo, "Código", 20),
            Requerido(nombre, "Nombre", 120),
            Opcional(Limpiar(r.PrincipioActivo), "Principio activo", 120),
            Opcional(Limpiar(r.Presentacion), "Presentación", 120),
            r.Unidad is null || !UnidadesSanidad.Medicamento.Contains(r.Unidad) ? Error.Validacion("Elige la unidad de aplicación.") : null,
            r.TipoDosis is null || !TiposDosis.Todos.Contains(r.TipoDosis) ? Error.Validacion("Elige cómo se calcula la dosis.") : null,
            r.DosisCantidad is <= 0 or > 1_000_000 ? Error.Validacion("La dosis debe ser mayor que 0.") : null,
            r.DosisMaxima is { } max && max < r.DosisCantidad ? Error.Validacion("La dosis máxima no puede ser menor que la dosis.") : null,
            r.DosisBase is <= 0 or > 100_000 ? Error.Validacion("La base de la dosis (litros o gramos) debe ser mayor que 0.") : null,
            r.ExposicionMinutos is { } minutos && minutos is < 1 or > 10_080 ? Error.Validacion("La exposición va de 1 minuto a 7 días.") : null,
            r.DiasRetiro is < 0 or > 365 ? Error.Validacion("El tiempo de retiro va de 0 a 365 días.") : null,
            Opcional(LimpiarTextoLargo(r.Indicaciones), "Indicaciones", 1000),
            Opcional(LimpiarTextoLargo(r.Contraindicaciones), "Contraindicaciones", 1000),
            Opcional(Limpiar(r.Fuente), "Fuente", 300));
        if (error is not null)
        {
            return error;
        }

        if (existentes.Any(m => m.Id != id && string.Equals(m.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflicto($"Ya existe un medicamento con el código {codigo}.");
        }

        var incompatibles = (r.Incompatibles ?? []).Where(i => i != id).Distinct().ToList();
        if (incompatibles.Any(i => existentes.All(m => m.Id != i)))
        {
            return Error.Validacion("Un medicamento incompatible no existe.");
        }

        var medicamento = new Medicamento
        {
            Id = id ?? Guid.CreateVersion7(),
            Codigo = codigo!,
            Nombre = nombre!,
            PrincipioActivo = Limpiar(r.PrincipioActivo),
            Presentacion = Limpiar(r.Presentacion),
            Unidad = r.Unidad!,
            TipoDosis = r.TipoDosis!,
            DosisCantidad = r.DosisCantidad,
            DosisMaxima = r.DosisMaxima,
            DosisBase = r.DosisBase,
            ExposicionMinutos = r.ExposicionMinutos,
            RequiereAyuno = r.RequiereAyuno,
            DiasRetiro = r.DiasRetiro,
            Indicaciones = LimpiarTextoLargo(r.Indicaciones),
            Contraindicaciones = LimpiarTextoLargo(r.Contraindicaciones),
            Fuente = Limpiar(r.Fuente),
            Activo = r.Activo,
        };
        await repositorio.GuardarMedicamentoAsync(medicamento, anterior is null, incompatibles, reloj.AhoraUtc, ct);

        var nombres = existentes.ToDictionary(m => m.Id, m => m.Nombre);
        var nuevo = MedicamentoAuditable(medicamento, incompatibles.Select(i => nombres[i]));
        if (anterior is null)
        {
            await auditoria.CreacionAsync(EntidadMedicamento, medicamento.Id, nuevo, ct: ct);
        }
        else
        {
            var previo = MedicamentoAuditable(
                new Medicamento
                {
                    Codigo = anterior.Codigo, Nombre = anterior.Nombre, PrincipioActivo = anterior.PrincipioActivo,
                    Presentacion = anterior.Presentacion, Unidad = anterior.Unidad, TipoDosis = anterior.TipoDosis,
                    DosisCantidad = anterior.DosisCantidad, DosisMaxima = anterior.DosisMaxima, DosisBase = anterior.DosisBase,
                    ExposicionMinutos = anterior.ExposicionMinutos, RequiereAyuno = anterior.RequiereAyuno, DiasRetiro = anterior.DiasRetiro,
                    Indicaciones = anterior.Indicaciones, Contraindicaciones = anterior.Contraindicaciones, Fuente = anterior.Fuente,
                    Activo = anterior.Activo,
                },
                anterior.Incompatibles.Where(nombres.ContainsKey).Select(i => nombres[i]));
            await auditoria.CambiosAsync(EntidadMedicamento, medicamento.Id, ComparadorAuditoria.Comparar(previo, nuevo), ct);
        }

        return medicamento.Id;
    }

    // --- Alimentos ---

    public Task<IReadOnlyList<AlimentoFila>> AlimentosAsync(CancellationToken ct = default) => repositorio.AlimentosAsync(ct);

    public async Task<Resultado<Guid>> GuardarAlimentoAsync(Guid? id, GuardarAlimentoRequest r, CancellationToken ct = default)
    {
        var existentes = await repositorio.AlimentosAsync(ct);
        var anterior = id is { } buscado ? existentes.FirstOrDefault(a => a.Id == buscado) : null;
        if (id is not null && anterior is null)
        {
            return Error.NoEncontrado("El alimento no existe.");
        }

        var codigo = Limpiar(r.Codigo)?.ToUpperInvariant();
        var nombre = Limpiar(r.Nombre);
        var error = Primero(
            Requerido(codigo, "Código", 20),
            Requerido(nombre, "Nombre", 120),
            r.Tipo is null || !TiposAlimento.Todos.Contains(r.Tipo) ? Error.Validacion("Elige el tipo de alimento.") : null,
            r.Unidad is null || !UnidadesSanidad.Alimento.Contains(r.Unidad) ? Error.Validacion("Elige la unidad del alimento.") : null);
        if (error is not null)
        {
            return error;
        }

        if (existentes.Any(a => a.Id != id && string.Equals(a.Codigo, codigo, StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflicto($"Ya existe un alimento con el código {codigo}.");
        }

        var alimento = new Alimento { Id = id ?? Guid.CreateVersion7(), Codigo = codigo!, Nombre = nombre!, Tipo = r.Tipo!, Unidad = r.Unidad!, Activo = r.Activo };
        await repositorio.GuardarAlimentoAsync(alimento, anterior is null, reloj.AhoraUtc, ct);
        var nuevo = new AlimentoFila(alimento.Id, alimento.Codigo, alimento.Nombre, alimento.Tipo, alimento.Unidad, alimento.Activo);
        if (anterior is null)
        {
            await auditoria.CreacionAsync(EntidadAlimento, alimento.Id, nuevo, ct: ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadAlimento, alimento.Id, ComparadorAuditoria.Comparar(anterior, nuevo), ct);
        }

        return alimento.Id;
    }

    // --- Dietas ---

    public Task<IReadOnlyList<DietaFila>> DietasAsync(CancellationToken ct = default) => repositorio.DietasAsync(ct);

    public async Task<Resultado<Guid>> GuardarDietaAsync(Guid? id, GuardarDietaRequest r, CancellationToken ct = default)
    {
        var existentes = await repositorio.DietasAsync(ct);
        var anterior = id is { } buscado ? existentes.FirstOrDefault(d => d.Id == buscado) : null;
        if (id is not null && anterior is null)
        {
            return Error.NoEncontrado("La dieta no existe.");
        }

        var error = Primero(
            (r.GrupoId is null) == (r.ProductoId is null) ? Error.Validacion("La dieta es de un grupo o de un producto (uno de los dos).") : null,
            r.VecesDia is < 1 or > 12 ? Error.Validacion("Las veces al día van de 1 a 12.") : null,
            r.CantidadPor100 is <= 0 or > 100_000 ? Error.Validacion("La cantidad por cada 100 peces debe ser mayor que 0.") : null,
            Opcional(Limpiar(r.Nota), "Nota", 300));
        if (error is not null)
        {
            return error;
        }

        if ((await repositorio.AlimentosAsync(ct)).FirstOrDefault(a => a.Id == r.AlimentoId) is not { Activo: true })
        {
            return Error.Validacion("Elige un alimento activo.");
        }

        if (r.GrupoId is { } grupo && await maestros.ObtenerAsync("grupos_producto", grupo, ct) is null)
        {
            return Error.Validacion("El grupo de producto no existe.");
        }

        if (r.ProductoId is { } producto && await productos.ObtenerAsync(producto, ct) is null)
        {
            return Error.Validacion("El producto no existe.");
        }

        if (existentes.Any(d => d.Id != id && d.AlimentoId == r.AlimentoId && d.GrupoId == r.GrupoId && d.ProductoId == r.ProductoId))
        {
            return Error.Conflicto("Ese alimento ya está en la dieta; edítalo en vez de repetirlo.");
        }

        var dieta = new Dieta
        {
            Id = id ?? Guid.CreateVersion7(),
            GrupoId = r.GrupoId,
            ProductoId = r.ProductoId,
            AlimentoId = r.AlimentoId,
            VecesDia = r.VecesDia,
            CantidadPor100 = r.CantidadPor100,
            Nota = Limpiar(r.Nota),
        };
        await repositorio.GuardarDietaAsync(dieta, anterior is null, reloj.AhoraUtc, ct);
        var despues = (await repositorio.DietasAsync(ct)).First(d => d.Id == dieta.Id);
        if (anterior is null)
        {
            await auditoria.CreacionAsync(EntidadDieta, dieta.Id, DietaAuditable(despues), ct: ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadDieta, dieta.Id, ComparadorAuditoria.Comparar(DietaAuditable(anterior), DietaAuditable(despues)), ct);
        }

        return dieta.Id;
    }

    public async Task<Resultado> EliminarDietaAsync(Guid id, CancellationToken ct = default)
    {
        if ((await repositorio.DietasAsync(ct)).FirstOrDefault(d => d.Id == id) is not { } dieta)
        {
            return Error.NoEncontrado("La dieta no existe.");
        }

        await repositorio.EliminarDietaAsync(id, ct);
        await auditoria.EliminacionAsync(EntidadDieta, id, DietaAuditable(dieta), ct);
        return Resultado.Ok();
    }

    // --- Fichas técnicas ---

    public Task<IReadOnlyList<FichaResumenFila>> FichasAsync(CancellationToken ct = default) => repositorio.FichasAsync(ct);

    public async Task<Resultado<FichaTecnicaInfo>> FichaAsync(Guid productoId, CancellationToken ct = default) =>
        await productos.ObtenerAsync(productoId, ct) is null
            ? Error.NoEncontrado("El producto no existe.")
            : await repositorio.FichaAsync(productoId, ct)
              ?? new FichaTecnicaInfo(productoId, null, null, null, null, null, null, null, null, null, null, null, null, 24);

    public async Task<Resultado> GuardarFichaAsync(Guid productoId, GuardarFichaRequest r, CancellationToken ct = default)
    {
        if (await productos.ObtenerAsync(productoId, ct) is null)
        {
            return Error.NoEncontrado("El producto no existe.");
        }

        var ficha = new FichaTecnicaInfo(
            productoId, LimpiarTextoLargo(r.Procedencia), LimpiarTextoLargo(r.Caracteristicas), LimpiarTextoLargo(r.Dimorfismo), r.PhMin, r.PhMax,
            r.GhMin, r.GhMax, r.TemperaturaMin, r.TemperaturaMax, LimpiarTextoLargo(r.Etologia), LimpiarTextoLargo(r.Empaque),
            LimpiarTextoLargo(r.SensibilidadQuimica), r.AyunoLlegadaHoras);
        var error = Primero(
            Opcional(ficha.Procedencia, "Procedencia", 500),
            Opcional(ficha.Caracteristicas, "Características", 2000),
            Opcional(ficha.Dimorfismo, "Dimorfismo sexual", 1000),
            Opcional(ficha.Etologia, "Etología", 2000),
            Opcional(ficha.Empaque, "Empaque", 500),
            Opcional(ficha.SensibilidadQuimica, "Sensibilidad química", 1000),
            Rango(r.PhMin, r.PhMax, 0, 14, "El pH"),
            Rango(r.GhMin, r.GhMax, 0, 100, "La dureza (GH)"),
            Rango(r.TemperaturaMin, r.TemperaturaMax, 0, 40, "La temperatura"),
            r.AyunoLlegadaHoras is < 0 or > 168 ? Error.Validacion("El ayuno de llegada va de 0 a 168 horas.") : null);
        if (error is not null)
        {
            return error;
        }

        var anterior = await repositorio.FichaAsync(productoId, ct);
        await repositorio.GuardarFichaAsync(ficha, reloj.AhoraUtc, ct);
        if (anterior is null)
        {
            await auditoria.CreacionAsync(EntidadFicha, productoId, ficha, ct: ct);
        }
        else
        {
            await auditoria.CambiosAsync(EntidadFicha, productoId, ComparadorAuditoria.Comparar(anterior, ficha), ct);
        }

        return Resultado.Ok();
    }

    // --- Protocolos ---

    public Task<IReadOnlyList<ProtocoloFila>> ProtocolosAsync(CancellationToken ct = default) => repositorio.ProtocolosAsync(ct);

    public async Task<Resultado<ProtocoloDetalle>> ProtocoloAsync(Guid id, CancellationToken ct = default) =>
        (await repositorio.ProtocolosAsync(ct)).FirstOrDefault(p => p.Id == id) is { } protocolo
            ? new ProtocoloDetalle(protocolo, await repositorio.PasosAsync(id, ct))
            : Error.NoEncontrado("El protocolo no existe.");

    /// <summary>Crea o edita. Editar uno aprobado lo devuelve a borrador (hay que volver a aprobarlo).</summary>
    public async Task<Resultado<Guid>> GuardarProtocoloAsync(Guid? id, GuardarProtocoloRequest r, CancellationToken ct = default)
    {
        var anterior = id is { } buscado ? (await repositorio.ProtocolosAsync(ct)).FirstOrDefault(p => p.Id == buscado) : null;
        if (id is not null && anterior is null)
        {
            return Error.NoEncontrado("El protocolo no existe.");
        }

        if (anterior is { Estado: EstadosProtocolo.Inactivo })
        {
            return Error.Conflicto("El protocolo está inactivo: reactívalo para editarlo.");
        }

        var nombre = Limpiar(r.Nombre);
        var pasos = r.Pasos ?? [];
        var medicamentos = (await repositorio.MedicamentosAsync(ct)).ToDictionary(m => m.Id);
        var error = Primero(
            Requerido(nombre, "Nombre", 150),
            Opcional(LimpiarTextoLargo(r.Descripcion), "Descripción", 2000),
            r.DiasCuarentena is { } dias && dias is < 1 or > 120 ? Error.Validacion("Los días de cuarentena van de 1 a 120.") : null,
            r.AyunoInicialHoras is < 0 or > 168 ? Error.Validacion("El ayuno inicial va de 0 a 168 horas.") : null,
            ReglasSanidad.ValidarPasos(pasos, medicamentos) is { } mensaje ? Error.Validacion(mensaje) : null,
            r.DiasCuarentena is { } total && pasos.Any(p => p.Dia > total)
                ? Error.Validacion("Hay pasos después del último día de cuarentena.")
                : null);
        if (error is not null)
        {
            return error;
        }

        if (await ValidarCriteriosAsync(r, ct) is { } errorCriterio)
        {
            return errorCriterio;
        }

        var protocolo = new Protocolo
        {
            Id = id ?? Guid.CreateVersion7(),
            Codigo = anterior?.Codigo ?? await consecutivos.SiguienteAsync(PrefijoProtocolo, ct),
            Nombre = nombre!,
            Descripcion = LimpiarTextoLargo(r.Descripcion),
            Estado = EstadosProtocolo.Borrador,
            ProductoId = r.ProductoId,
            GrupoId = r.GrupoId,
            CentroAcopioId = r.CentroAcopioId,
            CuencaId = r.CuencaId,
            DiasCuarentena = r.DiasCuarentena,
            AyunoInicialHoras = r.AyunoInicialHoras,
        };
        var filas = pasos
            .OrderBy(p => p.Dia)
            .Select((p, i) => new ProtocoloPaso
            {
                ProtocoloId = protocolo.Id,
                Dia = p.Dia,
                Orden = i + 1,
                Accion = p.Accion!,
                MedicamentoId = p.Accion == AccionesSanidad.Medicar ? p.MedicamentoId : null,
                PorcentajeAgua = p.Accion == AccionesSanidad.CambioAgua ? p.PorcentajeAgua : null,
                Nota = Limpiar(p.Nota),
            })
            .ToList();

        var pasosAntes = anterior is null ? [] : await repositorio.PasosAsync(anterior.Id, ct);
        await repositorio.GuardarProtocoloAsync(protocolo, anterior is null, filas, reloj.AhoraUtc, ct);
        var despues = (await repositorio.ProtocolosAsync(ct)).First(p => p.Id == protocolo.Id);
        var pasosDespues = await repositorio.PasosAsync(protocolo.Id, ct);
        if (anterior is null)
        {
            await auditoria.CreacionAsync(EntidadProtocolo, protocolo.Id, ProtocoloAuditable(despues, pasosDespues), ct: ct);
        }
        else
        {
            await auditoria.CambiosAsync(
                EntidadProtocolo, protocolo.Id,
                ComparadorAuditoria.Comparar(ProtocoloAuditable(anterior, pasosAntes), ProtocoloAuditable(despues, pasosDespues)), ct);
        }

        return protocolo.Id;
    }

    /// <summary>
    /// Aprobar (veterinario o gerencia; el controlador exige el permiso), inactivar o reactivar (a borrador).
    /// Para aprobar: días de cuarentena y al menos un paso.
    /// </summary>
    public async Task<Resultado> CambiarEstadoProtocoloAsync(Guid id, CambiarEstadoRequest r, CancellationToken ct = default)
    {
        if ((await repositorio.ProtocolosAsync(ct)).FirstOrDefault(p => p.Id == id) is not { } protocolo)
        {
            return Error.NoEncontrado("El protocolo no existe.");
        }

        var permitido = (protocolo.Estado, r.Estado) switch
        {
            (EstadosProtocolo.Borrador, EstadosProtocolo.Aprobado) => true,
            (EstadosProtocolo.Borrador or EstadosProtocolo.Aprobado, EstadosProtocolo.Inactivo) => true,
            (EstadosProtocolo.Inactivo, EstadosProtocolo.Borrador) => true,
            _ => false,
        };
        if (!permitido)
        {
            return Error.Validacion($"El protocolo no puede pasar de «{protocolo.Estado}» a «{r.Estado}».");
        }

        if (r.Estado == EstadosProtocolo.Aprobado)
        {
            if (protocolo.DiasCuarentena is null)
            {
                return Error.Validacion("Fija los días de cuarentena antes de aprobar.");
            }

            if (protocolo.Pasos == 0)
            {
                return Error.Validacion("El protocolo no tiene pasos.");
            }
        }

        if (await repositorio.CambiarEstadoProtocoloAsync(id, r.EstadoActual ?? protocolo.Estado, r.Estado!, usuario.UsuarioId, reloj.AhoraUtc, ct)
            is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        await auditoria.CambiosAsync(EntidadProtocolo, id, [new CambioAuditado("Estado", protocolo.Estado, r.Estado)], ct);
        return Resultado.Ok();
    }

    // --- Parámetros y responsables ---

    public Task<ParametrosSanidadInfo> ParametrosAsync(CancellationToken ct = default) => repositorio.ParametrosAsync(ct);

    public async Task<Resultado> ActualizarParametrosAsync(ParametrosSanidadInfo r, CancellationToken ct = default)
    {
        if (r.HorasAyunoVuelo is < ReglasSanidad.HorasAyunoVueloMinimo or > ReglasSanidad.HorasAyunoVueloMaximo)
        {
            return Error.Validacion($"El ayuno antes del vuelo va de {ReglasSanidad.HorasAyunoVueloMinimo} a {ReglasSanidad.HorasAyunoVueloMaximo} horas.");
        }

        var anterior = await repositorio.ParametrosAsync(ct);
        await repositorio.ActualizarParametrosAsync(r, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(EntidadParametros, IdParametros, ComparadorAuditoria.Comparar(anterior, r), ct);
        return Resultado.Ok();
    }

    public async Task<Resultado> AsignarResponsableAsync(Guid acuarioId, Guid? responsableId, CancellationToken ct = default)
    {
        if (await inventario.AcuarioAsync(acuarioId, ct) is null)
        {
            return Error.NoEncontrado("El acuario no existe.");
        }

        string? nombre = null;
        if (responsableId is { } id)
        {
            if (await usuarios.ObtenerPorIdAsync(id, ct) is not { Activo: true } persona)
            {
                return Error.Validacion("Elige un usuario activo.");
            }

            nombre = persona.Nombre;
        }

        await repositorio.AsignarResponsableAsync(acuarioId, responsableId, reloj.AhoraUtc, ct);
        await auditoria.CambiosAsync(InventarioService.EntidadAcuario, acuarioId, [new CambioAuditado("Responsable", null, nombre ?? "Sin responsable")], ct);
        return Resultado.Ok();
    }

    private async Task<Error?> ValidarCriteriosAsync(GuardarProtocoloRequest r, CancellationToken ct)
    {
        if (r.ProductoId is { } producto && await productos.ObtenerAsync(producto, ct) is null)
        {
            return Error.Validacion("El producto no existe.");
        }

        if (r.GrupoId is { } grupo && await maestros.ObtenerAsync("grupos_producto", grupo, ct) is null)
        {
            return Error.Validacion("El grupo de producto no existe.");
        }

        if (r.CentroAcopioId is { } centro && await centros.ObtenerAsync(centro, ct) is null)
        {
            return Error.Validacion("El centro de acopio no existe.");
        }

        return r.CuencaId is { } cuenca && await maestros.ObtenerAsync("cuencas", cuenca, ct) is null
            ? Error.Validacion("La cuenca no existe.")
            : null;
    }

    private static Error? Rango(decimal? minimo, decimal? maximo, decimal desde, decimal hasta, string nombre) =>
        minimo is { } a && (a < desde || a > hasta) || maximo is { } b && (b < desde || b > hasta)
            ? Error.Validacion($"{nombre} va de {desde} a {hasta}.")
            : minimo is { } x && maximo is { } y && x > y
                ? Error.Validacion($"{nombre}: el mínimo no puede ser mayor que el máximo.")
                : null;

    private static MedicamentoAuditableRec MedicamentoAuditable(Medicamento m, IEnumerable<string> incompatibles) =>
        new(m.Codigo, m.Nombre, m.PrincipioActivo, m.Presentacion, m.Unidad, m.TipoDosis, m.DosisCantidad, m.DosisMaxima, m.DosisBase,
            m.ExposicionMinutos, m.RequiereAyuno, m.DiasRetiro, m.Indicaciones, m.Contraindicaciones, m.Fuente, m.Activo,
            string.Join(", ", incompatibles.Order()));

    private static DietaAuditableRec DietaAuditable(DietaFila d) =>
        new(d.GrupoNombre ?? d.ProductoCodigo ?? "", d.AlimentoNombre, d.VecesDia, d.CantidadPor100, d.Nota);

    private static ProtocoloAuditableRec ProtocoloAuditable(ProtocoloFila p, IReadOnlyList<ProtocoloPasoFila> pasos) =>
        new(p.Nombre, p.Descripcion, p.Estado, p.ProductoCodigo, p.GrupoNombre, p.CentroNombre, p.CuencaNombre, p.DiasCuarentena,
            p.AyunoInicialHoras,
            string.Join(" | ", pasos.Select(s => $"D{s.Dia} {s.Accion}{(s.MedicamentoNombre is null ? "" : " " + s.MedicamentoNombre)}"
                                                 + $"{(s.PorcentajeAgua is null ? "" : $" {s.PorcentajeAgua}%")}")));

    private sealed record MedicamentoAuditableRec(
        string Codigo, string Nombre, string? PrincipioActivo, string? Presentacion, string Unidad, string TipoDosis, decimal DosisCantidad,
        decimal? DosisMaxima, decimal DosisBase, int? ExposicionMinutos, bool RequiereAyuno, int DiasRetiro, string? Indicaciones,
        string? Contraindicaciones, string? Fuente, bool Activo, string Incompatibles);

    private sealed record DietaAuditableRec(string Para, string Alimento, int VecesDia, decimal CantidadPor100, string? Nota);

    private sealed record ProtocoloAuditableRec(
        string Nombre, string? Descripcion, string Estado, string? Producto, string? Grupo, string? CentroAcopio, string? Cuenca,
        int? DiasCuarentena, int AyunoInicialHoras, string Pasos);
}
