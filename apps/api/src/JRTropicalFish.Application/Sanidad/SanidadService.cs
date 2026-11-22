using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Archivos;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Comun;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Domain.Inventario;
using JRTropicalFish.Domain.Sanidad;
using static JRTropicalFish.Application.Crm.ValidacionCrm;

namespace JRTropicalFish.Application.Sanidad;

/// <summary>
/// Operación diaria de sanidad: tratamientos por acuario con sus dosis, registro de alimentación, reservas de
/// despacho, el tablero de bodega y "Mi día" del operario. Las reglas puras están en <see cref="ReglasSanidad"/>.
/// </summary>
public class SanidadService(
    ISanidadRepository repositorio,
    ISanidadMaestrosRepository maestros,
    IInventarioRepository inventario,
    IPedidoClienteRepository pedidos,
    ITablerosInventarioRepository tableros,
    IAlmacenArchivos almacen,
    IConsecutivoRepository consecutivos,
    IUsuarioActual usuario,
    IReloj reloj,
    ServicioAuditoria auditoria)
{
    public const string EntidadTratamiento = "Tratamiento";
    public const string PrefijoTratamiento = "TRT";
    public const long TamanoMaximoReporteBytes = 10 * 1024 * 1024;
    private const string CarpetaReportes = "tratamiento";

    private Operador Operador => new(usuario.UsuarioId, usuario.Nombre);

    // --- Tratamientos ---

    public Task<IReadOnlyList<TratamientoFila>> TratamientosAsync(bool soloVigentes, CancellationToken ct = default) =>
        repositorio.TratamientosAsync(null, soloVigentes, reloj.Hoy, ct);

    public async Task<Resultado<TratamientoDetalle>> TratamientoAsync(Guid id, CancellationToken ct = default) =>
        (await repositorio.TratamientosAsync(id, false, reloj.Hoy, ct)).FirstOrDefault() is { } tratamiento
            ? new TratamientoDetalle(tratamiento, await repositorio.AplicacionesAsync(id, reloj.Hoy, reloj.Hoy, ct))
            : Error.NoEncontrado("El tratamiento no existe.");

    /// <summary>
    /// Inicia un tratamiento en un acuario. Desde un protocolo, solo si está aprobado; sin protocolo (prescripción
    /// del veterinario con sus pasos) solo quien gestiona sanidad. Un acuario tiene un solo tratamiento activo.
    /// </summary>
    public async Task<Resultado<TratamientoCreado>> CrearTratamientoAsync(CrearTratamientoRequest r, bool puedeGestionar, CancellationToken ct = default)
    {
        if (await inventario.AcuarioAsync(r.AcuarioId, ct) is not { Activo: true } acuario)
        {
            return Error.Validacion("Elige un acuario activo.");
        }

        var hoy = reloj.Hoy;
        var inicio = r.FechaInicio ?? hoy;
        if (inicio < hoy.AddDays(-7) || inicio > hoy.AddDays(30))
        {
            return Error.Validacion("El inicio va de 7 días atrás a 30 días adelante.");
        }

        var vigentes = await repositorio.TratamientosAsync(null, true, hoy, ct);
        if (vigentes.FirstOrDefault(t => t.AcuarioId == acuario.Id && t.Estado == EstadosTratamiento.Activo) is { } activo)
        {
            return Error.Conflicto($"{acuario.Codigo} ya tiene el tratamiento {activo.Codigo} activo.");
        }

        var medicamentos = (await maestros.MedicamentosAsync(ct)).ToDictionary(m => m.Id);
        IReadOnlyList<PasoRequest> pasos;
        int? dias;
        string motivo;
        if (r.ProtocoloId is { } protocoloId)
        {
            if ((await maestros.ProtocolosAsync(ct)).FirstOrDefault(p => p.Id == protocoloId) is not { } protocolo)
            {
                return Error.Validacion("El protocolo no existe.");
            }

            if (protocolo.Estado != EstadosProtocolo.Aprobado)
            {
                return Error.Validacion("El protocolo no está aprobado por el veterinario o gerencia.");
            }

            pasos = [.. (await maestros.PasosAsync(protocoloId, ct))
                .Select(p => new PasoRequest(p.Dia, p.Accion, p.MedicamentoId, p.PorcentajeAgua, p.Nota))];
            dias = protocolo.DiasCuarentena;
            motivo = r.Motivo ?? MotivosTratamiento.ProfilaxisLlegada;
        }
        else
        {
            if (!puedeGestionar)
            {
                return Error.Prohibido("Un tratamiento sin protocolo aprobado lo registra quien gestiona sanidad.");
            }

            pasos = r.Pasos ?? [];
            dias = r.Dias;
            motivo = r.Motivo ?? MotivosTratamiento.Prescripcion;
        }

        var error = Primero(
            MotivosTratamiento.Todos.Contains(motivo) ? null : Error.Validacion("El motivo no es válido."),
            pasos.Count == 0 ? Error.Validacion("El tratamiento necesita al menos un paso.") : null,
            ReglasSanidad.ValidarPasos(pasos, medicamentos) is { } mensaje ? Error.Validacion(mensaje) : null,
            dias is { } d && d is < 1 or > 120 ? Error.Validacion("Los días van de 1 a 120.") : null,
            Opcional(LimpiarTextoLargo(r.Diagnostico), "Diagnóstico", 1000),
            Opcional(LimpiarTextoLargo(r.Nota), "Nota", 1000));
        if (error is not null)
        {
            return error;
        }

        var usados = pasos.Where(p => p.MedicamentoId is not null).Select(p => medicamentos[p.MedicamentoId!.Value]).DistinctBy(m => m.Id).ToList();
        var fin = ReglasSanidad.FechaFin(inicio, dias, pasos);
        var aplicaciones = ReglasSanidad.Aplicaciones(pasos, medicamentos, inicio, acuario.CapacidadLitros);
        var tratamiento = new Tratamiento
        {
            Codigo = await consecutivos.SiguienteAsync(PrefijoTratamiento, ct),
            AcuarioId = acuario.Id,
            ProtocoloId = r.ProtocoloId,
            Motivo = motivo,
            Diagnostico = LimpiarTextoLargo(r.Diagnostico),
            Litros = acuario.CapacidadLitros,
            FechaInicio = inicio,
            FechaFin = fin,
            FechaLiberacion = ReglasSanidad.FechaLiberacion(fin, usados),
            Estado = EstadosTratamiento.Activo,
            Nota = LimpiarTextoLargo(r.Nota),
            CreadoPorId = usuario.UsuarioId,
        };
        if (await repositorio.CrearTratamientoAsync(tratamiento, aplicaciones, reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        var avisos = new List<string>();
        if (acuario.CapacidadLitros is null && usados.Any(m => m.TipoDosis == TiposDosis.PorVolumen))
        {
            avisos.Add($"{acuario.Codigo} no tiene litraje: las dosis quedaron sin calcular. Regístralo en Acuarios.");
        }

        avisos.AddRange(ReglasSanidad.Incompatibles(usados).Select(p => $"{p.A.Nombre} y {p.B.Nombre} están marcados como incompatibles."));
        await auditoria.CreacionAsync(
            EntidadTratamiento, tratamiento.Id,
            new TratamientoAuditable(tratamiento.Codigo, acuario.Codigo, motivo, tratamiento.Diagnostico, tratamiento.FechaInicio,
                tratamiento.FechaFin, tratamiento.FechaLiberacion, aplicaciones.Count),
            ct: ct);
        return new TratamientoCreado(tratamiento.Id, tratamiento.Codigo, avisos);
    }

    /// <summary>Marca una dosis o paso como aplicado (con la cantidad real) u omitido (con nota obligatoria).</summary>
    public async Task<Resultado> RegistrarAplicacionAsync(Guid id, bool aplicada, RegistrarAplicacionRequest r, CancellationToken ct = default)
    {
        var nota = Limpiar(r.Nota);
        var error = Primero(
            !aplicada && nota is null ? Error.Validacion("Cuenta por qué no se aplicó.") : null,
            Opcional(nota, "Nota", 500),
            r.Cantidad is < 0 or > 1_000_000 ? Error.Validacion("La cantidad no es válida.") : null);
        if (error is not null)
        {
            return error;
        }

        var falla = await repositorio.RegistrarAplicacionAsync(
            id, aplicada ? EstadosAplicacion.Aplicada : EstadosAplicacion.Omitida, aplicada ? r.Cantidad : null, nota, Operador, reloj.AhoraUtc, ct);
        return falla is null ? Resultado.Ok() : FallasInventario.AError(falla);
    }

    /// <summary>
    /// Terminar (todo aplicado u omitido) o cancelar (solo si nada se aplicó todavía; si no, se termina omitiendo
    /// lo pendiente para que cuente el tiempo de retiro).
    /// </summary>
    public async Task<Resultado> CambiarEstadoTratamientoAsync(Guid id, CambiarEstadoRequest r, CancellationToken ct = default)
    {
        var leido = await TratamientoAsync(id, ct);
        if (!leido.Exitoso)
        {
            return leido.Error!;
        }

        var detalle = leido.Valor;
        var t = detalle.Tratamiento;
        if (t.Estado != EstadosTratamiento.Activo || r.Estado is not (EstadosTratamiento.Terminado or EstadosTratamiento.Cancelado))
        {
            return Error.Validacion($"El tratamiento no puede pasar de «{t.Estado}» a «{r.Estado}».");
        }

        if (r.Estado == EstadosTratamiento.Terminado && detalle.Aplicaciones.Any(a => a.Estado == EstadosAplicacion.Pendiente))
        {
            return Error.Validacion("Quedan aplicaciones pendientes: márcalas como aplicadas u omitidas antes de terminar.");
        }

        if (r.Estado == EstadosTratamiento.Cancelado && detalle.Aplicaciones.Any(a => a.Estado == EstadosAplicacion.Aplicada))
        {
            return Error.Validacion("Ya se aplicaron medicamentos: termina el tratamiento (omitiendo lo pendiente) para que cuente el retiro.");
        }

        if (await repositorio.CambiarEstadoTratamientoAsync(id, r.EstadoActual ?? t.Estado, r.Estado, null, reloj.AhoraUtc, ct) is { } falla)
        {
            return FallasInventario.AError(falla);
        }

        await auditoria.CambiosAsync(EntidadTratamiento, id, [new CambioAuditado("Estado", t.Estado, r.Estado)], ct);
        return Resultado.Ok();
    }

    /// <summary>Adjunta el reporte del veterinario (PDF, por sus bytes).</summary>
    public async Task<Resultado> SubirReporteAsync(Guid id, Stream contenido, CancellationToken ct = default)
    {
        if ((await repositorio.TratamientosAsync(id, false, reloj.Hoy, ct)).FirstOrDefault() is not { } tratamiento)
        {
            return Error.NoEncontrado("El tratamiento no existe.");
        }

        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer, ct);
        if (buffer.Length == 0 || buffer.Length > TamanoMaximoReporteBytes)
        {
            return Error.Validacion("El reporte debe pesar entre 1 byte y 10 MB.");
        }

        var tipo = DetectorTipoArchivo.Detectar(buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, DetectorTipoArchivo.BytesNecesarios)));
        if (tipo != "application/pdf")
        {
            return Error.Validacion("El reporte del veterinario debe ser un PDF.");
        }

        buffer.Position = 0;
        var clave = ClaveArchivo.ParaDocumento(CarpetaReportes, id, Guid.CreateVersion7(), tipo);
        await almacen.SubirAsync(clave, buffer, tipo, ct);
        await repositorio.GuardarReporteAsync(id, clave, reloj.AhoraUtc, ct);
        if (tratamiento.ReporteClave is { } anterior)
        {
            await almacen.EliminarAsync(anterior, ct);
        }

        await auditoria.CambiosAsync(EntidadTratamiento, id, [new CambioAuditado("Reporte del veterinario", tratamiento.TieneReporte ? "Anterior" : null, "Adjuntado")], ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<ArchivoDescargado>> DescargarReporteAsync(Guid id, CancellationToken ct = default) =>
        (await repositorio.TratamientosAsync(id, false, reloj.Hoy, ct)).FirstOrDefault() is { ReporteClave: { } clave }
        && await almacen.DescargarAsync(clave, ct) is { } archivo
            ? archivo
            : Error.NoEncontrado("El tratamiento no tiene reporte.");

    // --- Alimentación ---

    public async Task<Resultado> RegistrarAlimentacionAsync(RegistrarAlimentacionRequest r, CancellationToken ct = default)
    {
        if (await inventario.AcuarioAsync(r.AcuarioId, ct) is not { Activo: true })
        {
            return Error.Validacion("El acuario no existe.");
        }

        if (r.Cantidad is < 0 or > 100_000)
        {
            return Error.Validacion("La cantidad no es válida.");
        }

        if (r.AlimentoId is { } alimento && (await maestros.AlimentosAsync(ct)).All(a => a.Id != alimento))
        {
            return Error.Validacion("El alimento no existe.");
        }

        await repositorio.RegistrarAlimentacionAsync(r.AcuarioId, r.AlimentoId, r.Cantidad, Operador, reloj.AhoraUtc, ct);
        return Resultado.Ok();
    }

    // --- Reservas ---

    public async Task<Resultado<IReadOnlyList<ReservaFila>>> ReservasPedidoAsync(Guid pedidoId, CancellationToken ct = default) =>
        await pedidos.ObtenerAsync(pedidoId, ct) is null
            ? Error.NoEncontrado("El pedido no existe.")
            : Resultado<IReadOnlyList<ReservaFila>>.Ok(await repositorio.ReservasPedidoAsync(pedidoId, ct));

    /// <summary>Fija cuántos peces de un lote y acuario van para una línea (0 = quitar).</summary>
    public async Task<Resultado> FijarReservaAsync(FijarReservaRequest r, CancellationToken ct = default)
    {
        if (r.Cantidad is < 0 or > 1_000_000)
        {
            return Error.Validacion("La cantidad no es válida.");
        }

        var falla = await repositorio.FijarReservaAsync(r.LineaId, r.LoteId, r.AcuarioId, r.Cantidad, usuario.UsuarioId, reloj.AhoraUtc, ct);
        return falla is null ? Resultado.Ok() : FallasInventario.AError(falla);
    }

    /// <summary>Acuarios activos con su litraje y responsable (pantalla de configuración).</summary>
    public Task<IReadOnlyList<SanidadAcuarioFila>> AcuariosAsync(CancellationToken ct = default) => repositorio.AcuariosAsync(ct);

    // --- Tablero y Mi día ---

    public async Task<TableroSanidad> TableroAsync(CancellationToken ct = default)
    {
        var datos = await LeerAsync(ct);
        var tarjetas = new List<TarjetaSanidad>();

        foreach (var pedido in datos.Compras.GroupBy(c => c.PedidoId))
        {
            var primera = pedido.First();
            tarjetas.Add(new TarjetaSanidad(
                $"compra:{pedido.Key}", EstadosAcuario.PorLlegar, null, primera.PedidoCodigo, null, null, null,
                pedido.Sum(c => c.Pendiente),
                [.. pedido.Select(c => new EspecieAcuario(null, null, c.ProductoId, c.ProductoCodigo, c.NombreCientifico, Condiciones.Sano, c.Pendiente))],
                ["Preparar tanque"], null, null, null, primera.FechaEstimadaLlegada, primera.ProveedorNombre));
        }

        foreach (var lote in datos.Existencias.Where(e => e.AcuarioId is null).GroupBy(e => e.LoteId))
        {
            var primera = lote.First();
            tarjetas.Add(new TarjetaSanidad(
                $"lote:{lote.Key}", EstadosAcuario.Aclimatacion, null, $"Por ubicar · {primera.LoteCodigo}", null, null, null,
                lote.Sum(e => e.Cantidad), [.. lote.Select(Especie)], ["Ubicar", "No alimentar"], null, null, primera.Llegada, null, null));
        }

        foreach (var estado in Estados(datos))
        {
            tarjetas.Add(new TarjetaSanidad(
                $"acuario:{estado.Acuario.Id}", estado.Columna, estado.Acuario.Id, estado.Acuario.Codigo, estado.Acuario.Sector,
                estado.Acuario.Litros, estado.Acuario.ResponsableNombre, estado.Peces, estado.Especies, Etiquetas(estado),
                estado.Tratamiento, estado.Vuelo, estado.Especies.Count == 0 ? null : estado.UltimaLlegada, null, null));
        }

        return new TableroSanidad(datos.HorasAyuno, tarjetas);
    }

    public async Task<MiDia> MiDiaAsync(CancellationToken ct = default)
    {
        var datos = await LeerAsync(ct);
        var hoy = reloj.Hoy;
        var inicioDia = InicioDia(hoy);
        var protocolos = await maestros.ProtocolosAsync(ct);
        var estados = Estados(datos);
        var porAcuario = estados.ToDictionary(e => e.Acuario.Id);
        var codigos = datos.Acuarios.ToDictionary(a => a.Id, a => a.Codigo);

        var llegadas = datos.Existencias
            .GroupBy(e => e.LoteId)
            .Where(g => g.Any(e => e.AcuarioId is null) || g.First().Llegada >= inicioDia)
            .Select(g =>
            {
                var e = g.First();
                var acuario = g.Select(x => x.AcuarioId).FirstOrDefault(a => a is not null);
                var sugerido = ReglasSanidad.Sugerir(protocolos, e.ProductoId, e.GrupoId, e.CentroAcopioId, e.CuencaId);
                return new LlegadaHoy(
                    g.Key, e.LoteCodigo, e.ProductoCodigo, e.ProductoNombre, g.Sum(x => x.Cantidad), acuario,
                    acuario is { } a ? codigos.GetValueOrDefault(a) : null, e.Llegada, g.Any(x => x.AcuarioId is null), sugerido?.Id, sugerido?.Nombre,
                    acuario is { } conTanque && porAcuario.TryGetValue(conTanque, out var est) && est.Tratamiento is not null);
            })
            .OrderBy(l => l.PorUbicar ? 0 : 1).ThenBy(l => l.Llegada)
            .ToList();

        var medicacion = (await repositorio.AplicacionesAsync(null, hoy.AddDays(-14), hoy, ct))
            .Where(a => a.Fecha == hoy || a.Estado == EstadosAplicacion.Pendiente)
            .OrderBy(a => a.Estado == EstadosAplicacion.Pendiente ? 0 : 1).ThenBy(a => a.Fecha).ThenBy(a => a.AcuarioCodigo).ThenBy(a => a.Orden)
            .ToList();

        var alimentacionHoy = (await repositorio.AlimentacionDesdeAsync(inicioDia, ct)).ToDictionary(a => a.AcuarioId);
        var ronda = estados
            .Where(e => e.Peces > 0)
            .Select(e => Ronda(e, datos.Dietas, alimentacionHoy.GetValueOrDefault(e.Acuario.Id)))
            .OrderBy(r => r.Alimentar ? 1 : 0).ThenBy(r => r.AcuarioCodigo, StringComparer.Ordinal)
            .ToList();

        var salidas = datos.Reservas
            .Select(r => (Reserva: r, Vuelo: ReglasSanidad.SalidaVuelo(r.SalidaVuelo, r.FechaDespacho)))
            .Where(x => x.Vuelo is { } v && DiaBogota(v.Salida) <= hoy.AddDays(1))
            .GroupBy(x => x.Reserva.PedidoId)
            .Select(g =>
            {
                var r = g.First().Reserva;
                var lotes = datos.Existencias.GroupBy(e => e.LoteId).ToDictionary(x => x.Key, x => x.First().ProductoCodigo);
                return new SalidaHoy(
                    r.PedidoId, r.PedidoCodigo, r.ClienteNombre, r.EmbarqueId, r.EmbarqueCodigo, g.First().Vuelo!.Value.Salida, r.Destino,
                    [.. g.Select(x => new SalidaAcuario(
                        x.Reserva.AcuarioId, codigos.GetValueOrDefault(x.Reserva.AcuarioId) ?? "?", x.Reserva.LoteId,
                        lotes.GetValueOrDefault(x.Reserva.LoteId) ?? "", x.Reserva.Cantidad))
                        .OrderBy(s => s.AcuarioCodigo, StringComparer.Ordinal)]);
            })
            .OrderBy(s => s.Salida)
            .ToList();

        var avisos = new List<string>();
        if (!protocolos.Any(p => p.Estado == EstadosProtocolo.Aprobado))
        {
            avisos.Add("No hay protocolos aprobados: la profilaxis de llegada no se sugiere hasta que el veterinario apruebe uno.");
        }

        var sinLitraje = estados.Where(e => e.Tratamiento is not null && e.Acuario.Litros is null).Select(e => e.Acuario.Codigo).ToList();
        if (sinLitraje.Count > 0)
        {
            avisos.Add($"Sin litraje (dosis sin calcular): {string.Join(", ", sinLitraje)}.");
        }

        var sinHora = datos.Reservas.Where(r => r.SalidaVuelo is null && r.FechaDespacho is not null && r.EmbarqueCodigo is not null)
            .Select(r => r.EmbarqueCodigo!).Distinct().ToList();
        if (sinHora.Count > 0)
        {
            avisos.Add($"Embarques sin hora de vuelo (el ayuno cuenta desde las 00:00 del despacho): {string.Join(", ", sinHora)}.");
        }

        return new MiDia(hoy, datos.HorasAyuno, llegadas, medicacion, ronda, salidas, avisos);
    }

    // --- Cálculo del estado de cada acuario ---

    private async Task<Datos> LeerAsync(CancellationToken ct)
    {
        var hoy = reloj.Hoy;
        return new Datos(
            reloj.AhoraUtc,
            hoy,
            (await maestros.ParametrosAsync(ct)).HorasAyunoVuelo,
            await repositorio.AcuariosAsync(ct),
            await repositorio.ExistenciasAsync(ct),
            await repositorio.TratamientosVigentesAsync(hoy, ct),
            await repositorio.ReservasAsync(ct),
            await tableros.ComprasPendientesAsync(ct),
            await maestros.DietasAsync(ct));
    }

    private static List<EstadoCalculado> Estados(Datos d)
    {
        var existencias = d.Existencias.Where(e => e.AcuarioId is not null).ToLookup(e => e.AcuarioId!.Value);
        var tratamientos = d.Tratamientos.ToLookup(t => t.AcuarioId);
        var reservas = d.Reservas.ToLookup(r => r.AcuarioId);
        var resultado = new List<EstadoCalculado>();

        foreach (var acuario in d.Acuarios)
        {
            var especies = existencias[acuario.Id].ToList();
            var vigente = tratamientos[acuario.Id]
                .OrderBy(t => t.Estado == EstadosTratamiento.Activo ? 0 : 1).ThenByDescending(t => t.FechaLiberacion).FirstOrDefault();
            var reservadas = reservas[acuario.Id].ToList();
            if (especies.Count == 0 && vigente is null && reservadas.Count == 0)
            {
                continue;
            }

            var tratamiento = vigente is null
                ? null
                : new TratamientoAcuario(
                    vigente.Id, vigente.Codigo, vigente.Nombre, vigente.Estado, ReglasSanidad.DiaActual(vigente.FechaInicio, vigente.FechaFin, d.Hoy),
                    ReglasSanidad.DiasTotal(vigente.FechaInicio, vigente.FechaFin), vigente.FechaLiberacion, vigente.PendientesHoy,
                    vigente.RequiereAyuno, vigente.AlimentoMedicado);

            var vuelo = reservadas
                .Select(r => (Reserva: r, Salida: ReglasSanidad.SalidaVuelo(r.SalidaVuelo, r.FechaDespacho)))
                .Where(x => x.Salida is not null)
                .OrderBy(x => x.Salida!.Value.Salida)
                .Select(x => new VueloAcuario(
                    x.Reserva.PedidoId, x.Reserva.PedidoCodigo, x.Reserva.ClienteNombre, x.Reserva.EmbarqueId, x.Reserva.EmbarqueCodigo,
                    x.Salida!.Value.Salida, x.Salida.Value.SinHora, Math.Round((x.Salida.Value.Salida - d.Ahora).TotalHours, 1),
                    reservadas.Where(r => r.PedidoId == x.Reserva.PedidoId).Sum(r => r.Cantidad)))
                .FirstOrDefault();

            var ayunoVuelo = vuelo is not null && ReglasSanidad.EnAyunoVuelo(vuelo.Salida, d.Ahora, d.HorasAyuno);
            var ayunoLlegada = especies.Any(e => ReglasSanidad.EnAyunoLlegada(e.Llegada, e.AyunoLlegadaHoras, d.Ahora));
            var enfermos = especies.Any(e => e.Condicion == Condiciones.Enfermo);
            resultado.Add(new EstadoCalculado(
                acuario,
                ReglasSanidad.Columna(ayunoVuelo, tratamiento is not null, enfermos, ayunoLlegada),
                [.. especies.Select(Especie)],
                especies.Sum(e => e.Cantidad),
                tratamiento,
                vuelo,
                ayunoVuelo,
                ayunoLlegada,
                enfermos,
                ReglasSanidad.RazonesAyuno(ayunoVuelo, tratamiento, ayunoLlegada),
                especies.Count == 0 ? DateTime.MinValue : especies.Max(e => e.Llegada),
                especies));
        }

        return resultado;
    }

    private static IReadOnlyList<string> Etiquetas(EstadoCalculado e)
    {
        var etiquetas = new List<string>();
        if (e.Motivos.Count > 0)
        {
            etiquetas.Add("No alimentar");
        }

        if (e.Tratamiento is { Estado: EstadosTratamiento.Activo, PendientesHoy: > 0 })
        {
            etiquetas.Add("Medicar hoy");
        }

        if (e.Tratamiento is { Estado: EstadosTratamiento.Terminado } t)
        {
            etiquetas.Add($"Retiro hasta {t.FechaLiberacion:dd/MM}");
        }

        if (e.Tratamiento is { AlimentoMedicado: true, Estado: EstadosTratamiento.Activo })
        {
            etiquetas.Add("Alimento medicado");
        }

        if (e.HayEnfermos)
        {
            etiquetas.Add("Enfermos");
        }

        if (e.Vuelo is { } v)
        {
            etiquetas.Add(v.HorasRestantes <= 0 ? "Vuelo hoy" : $"Vuelo en {Math.Ceiling(v.HorasRestantes):0} h");
            if (v.SinHora)
            {
                etiquetas.Add("Sin hora de vuelo");
            }
        }

        if (e.Tratamiento is not null && e.Acuario.Litros is null)
        {
            etiquetas.Add("Sin litraje");
        }

        return etiquetas;
    }

    private static RondaAlimentacion Ronda(EstadoCalculado e, IReadOnlyList<DietaFila> dietas, AlimentacionFila? hoy)
    {
        var raciones = new List<RacionAlimento>();
        var sinDieta = false;
        foreach (var producto in e.Filas.GroupBy(f => (f.ProductoId, f.GrupoId, f.ProductoCodigo)))
        {
            var peces = producto.Sum(f => f.Cantidad);
            var aplican = ReglasSanidad.DietasDe(producto.Key.ProductoId, producto.Key.GrupoId, dietas);
            if (aplican.Count == 0)
            {
                sinDieta = true;
            }

            raciones.AddRange(aplican.Select(d => new RacionAlimento(
                d.AlimentoId, d.AlimentoNombre, d.Unidad, d.VecesDia, ReglasSanidad.CantidadToma(d.CantidadPor100, peces, d.Unidad),
                producto.Key.ProductoCodigo)));
        }

        string? detalle = null;
        if (e.Motivos.Contains(MotivosAyuno.Vuelo) && e.Vuelo is { } v)
        {
            detalle = $"Vuelo {(v.EmbarqueCodigo ?? v.PedidoCodigo)} en {Math.Max(0, Math.Ceiling(v.HorasRestantes)):0} h";
        }
        else if (e.Motivos.Contains(MotivosAyuno.Tratamiento) && e.Tratamiento is { } t)
        {
            detalle = $"{t.Codigo} · Día {t.Dia}/{t.DiasTotal}";
        }
        else if (e.Motivos.Contains(MotivosAyuno.Llegada))
        {
            detalle = "Llegada reciente";
        }

        return new RondaAlimentacion(
            e.Acuario.Id, e.Acuario.Codigo, e.Peces, e.Motivos.Count == 0, e.Motivos, detalle,
            e.Tratamiento is { AlimentoMedicado: true, Estado: EstadosTratamiento.Activo }, raciones, sinDieta, hoy?.Tomas ?? 0, hoy?.Ultima);
    }

    private static EspecieAcuario Especie(SanidadExistenciaFila e) =>
        new(e.LoteId, e.LoteCodigo, e.ProductoId, e.ProductoCodigo, e.ProductoNombre, e.Condicion, e.Cantidad);

    private static DateTime InicioDia(DateOnly dia) => dia.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + ReglasSanidad.DesfaseBogota;

    private static DateOnly DiaBogota(DateTime utc) => DateOnly.FromDateTime(utc - ReglasSanidad.DesfaseBogota);

    private sealed record Datos(
        DateTime Ahora,
        DateOnly Hoy,
        int HorasAyuno,
        IReadOnlyList<SanidadAcuarioFila> Acuarios,
        IReadOnlyList<SanidadExistenciaFila> Existencias,
        IReadOnlyList<TratamientoVigenteFila> Tratamientos,
        IReadOnlyList<SanidadReservaFila> Reservas,
        IReadOnlyList<CompraPendienteFila> Compras,
        IReadOnlyList<DietaFila> Dietas);

    private sealed record EstadoCalculado(
        SanidadAcuarioFila Acuario,
        string Columna,
        IReadOnlyList<EspecieAcuario> Especies,
        int Peces,
        TratamientoAcuario? Tratamiento,
        VueloAcuario? Vuelo,
        bool AyunoVuelo,
        bool AyunoLlegada,
        bool HayEnfermos,
        IReadOnlyList<string> Motivos,
        DateTime UltimaLlegada,
        IReadOnlyList<SanidadExistenciaFila> Filas);

    private sealed record TratamientoAuditable(
        string Codigo, string Acuario, string Motivo, string? Diagnostico, DateOnly Inicio, DateOnly Fin, DateOnly Liberacion, int Aplicaciones);
}
