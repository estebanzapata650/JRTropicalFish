using System.Text.Json;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Sanidad;
using JRTropicalFish.Domain.Sanidad;
using JRTropicalFish.Infrastructure.Persistence;
using Npgsql;

namespace JRTropicalFish.Infrastructure.Repositories;

/// <summary>
/// Base de sanidad: además de los SQLSTATE propios (JR001–JR004), devuelve como falla los <c>RAISE EXCEPTION</c>
/// sin código (P0001) de los procedimientos de sanidad, que son validaciones de negocio.
/// </summary>
public abstract class RepositorioSanidad(PostgresConnectionFactory conexiones) : RepositorioInventario(conexiones)
{
    protected async Task<FallaBaseDatos?> EjecutarValidandoAsync(string sql, object parametros, CancellationToken ct)
    {
        try
        {
            await EjecutarAsync(sql, parametros, ct);
            return null;
        }
        catch (PostgresException ex) when (ex.SqlState.StartsWith("JR", StringComparison.Ordinal) || ex.SqlState == "P0001")
        {
            return new FallaBaseDatos(ex.SqlState, ex.MessageText);
        }
    }
}

public class SanidadMaestrosRepository(PostgresConnectionFactory conexiones) : RepositorioSanidad(conexiones), ISanidadMaestrosRepository
{
    public Task<IReadOnlyList<MedicamentoFila>> MedicamentosAsync(CancellationToken ct = default) =>
        ListarAsync<MedicamentoFila>("SELECT * FROM fn_medicamentos_listar()", null, ct);

    public Task GuardarMedicamentoAsync(Medicamento m, bool esNuevo, IReadOnlyList<Guid> incompatibles, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_medicamento_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @Nombre::text, @PrincipioActivo::text,
                @Presentacion::text, @Unidad::text, @TipoDosis::text, @DosisCantidad::numeric, @DosisMaxima::numeric, @DosisBase::numeric,
                @ExposicionMinutos::integer, @RequiereAyuno::boolean, @DiasRetiro::integer, @Indicaciones::text, @Contraindicaciones::text,
                @Fuente::text, @Activo::boolean, @Incompatibles::uuid[], @Fecha::timestamptz)
            """,
            new
            {
                m.Id, EsNuevo = esNuevo, m.Codigo, m.Nombre, m.PrincipioActivo, m.Presentacion, m.Unidad, m.TipoDosis, m.DosisCantidad,
                m.DosisMaxima, m.DosisBase, m.ExposicionMinutos, m.RequiereAyuno, m.DiasRetiro, m.Indicaciones, m.Contraindicaciones,
                m.Fuente, m.Activo, Incompatibles = incompatibles.ToArray(), Fecha = fecha,
            },
            ct);

    public Task<IReadOnlyList<AlimentoFila>> AlimentosAsync(CancellationToken ct = default) =>
        ListarAsync<AlimentoFila>("SELECT * FROM fn_alimentos_listar()", null, ct);

    public Task GuardarAlimentoAsync(Alimento a, bool esNuevo, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            "CALL sp_alimento_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @Nombre::text, @Tipo::text, @Unidad::text, @Activo::boolean, @Fecha::timestamptz)",
            new { a.Id, EsNuevo = esNuevo, a.Codigo, a.Nombre, a.Tipo, a.Unidad, a.Activo, Fecha = fecha }, ct);

    public Task<IReadOnlyList<DietaFila>> DietasAsync(CancellationToken ct = default) =>
        ListarAsync<DietaFila>("SELECT * FROM fn_dietas_listar()", null, ct);

    public Task GuardarDietaAsync(Dieta d, bool esNueva, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_dieta_guardar(@Id::uuid, @EsNueva::boolean, @GrupoId::uuid, @ProductoId::uuid, @AlimentoId::uuid, @VecesDia::integer,
                @CantidadPor100::numeric, @Nota::text, @Fecha::timestamptz)
            """,
            new { d.Id, EsNueva = esNueva, d.GrupoId, d.ProductoId, d.AlimentoId, d.VecesDia, d.CantidadPor100, d.Nota, Fecha = fecha }, ct);

    public Task EliminarDietaAsync(Guid id, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_dieta_eliminar(@Id::uuid)", new { Id = id }, ct);

    public Task<IReadOnlyList<FichaResumenFila>> FichasAsync(CancellationToken ct = default) =>
        ListarAsync<FichaResumenFila>("SELECT * FROM fn_fichas_listar()", null, ct);

    public Task<FichaTecnicaInfo?> FichaAsync(Guid productoId, CancellationToken ct = default) =>
        UnoAsync<FichaTecnicaInfo>("SELECT * FROM fn_ficha_tecnica(@Id::uuid)", new { Id = productoId }, ct);

    public Task GuardarFichaAsync(FichaTecnicaInfo f, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_ficha_tecnica_guardar(@ProductoId::uuid, @Procedencia::text, @Caracteristicas::text, @Dimorfismo::text,
                @PhMin::numeric, @PhMax::numeric, @GhMin::numeric, @GhMax::numeric, @TemperaturaMin::numeric, @TemperaturaMax::numeric,
                @Etologia::text, @Empaque::text, @SensibilidadQuimica::text, @AyunoLlegadaHoras::integer, @Fecha::timestamptz)
            """,
            new
            {
                f.ProductoId, f.Procedencia, f.Caracteristicas, f.Dimorfismo, f.PhMin, f.PhMax, f.GhMin, f.GhMax, f.TemperaturaMin,
                f.TemperaturaMax, f.Etologia, f.Empaque, f.SensibilidadQuimica, f.AyunoLlegadaHoras, Fecha = fecha,
            },
            ct);

    public Task<IReadOnlyList<ProtocoloFila>> ProtocolosAsync(CancellationToken ct = default) =>
        ListarAsync<ProtocoloFila>("SELECT * FROM fn_protocolos_listar()", null, ct);

    public Task<IReadOnlyList<ProtocoloPasoFila>> PasosAsync(Guid protocoloId, CancellationToken ct = default) =>
        ListarAsync<ProtocoloPasoFila>("SELECT * FROM fn_protocolo_pasos(@Id::uuid)", new { Id = protocoloId }, ct);

    public Task GuardarProtocoloAsync(Protocolo p, bool esNuevo, IReadOnlyList<ProtocoloPaso> pasos, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_protocolo_guardar(@Id::uuid, @EsNuevo::boolean, @Codigo::text, @Nombre::text, @Descripcion::text, @ProductoId::uuid,
                @GrupoId::uuid, @CentroAcopioId::uuid, @CuencaId::uuid, @DiasCuarentena::integer, @AyunoInicialHoras::integer,
                @Pasos::jsonb, @Fecha::timestamptz)
            """,
            new
            {
                p.Id, EsNuevo = esNuevo, p.Codigo, p.Nombre, p.Descripcion, p.ProductoId, p.GrupoId, p.CentroAcopioId, p.CuencaId,
                p.DiasCuarentena, p.AyunoInicialHoras,
                Pasos = JsonSerializer.Serialize(pasos.Select(s => new
                {
                    id = s.Id, dia = s.Dia, orden = s.Orden, accion = s.Accion, medicamento = s.MedicamentoId, agua = s.PorcentajeAgua, nota = s.Nota,
                })),
                Fecha = fecha,
            },
            ct);

    public Task<FallaBaseDatos?> CambiarEstadoProtocoloAsync(
        Guid id, string esperado, string estado, Guid? usuarioId, DateTime fecha, CancellationToken ct = default) =>
        EjecutarValidandoAsync(
            "CALL sp_protocolo_cambiar_estado(@Id::uuid, @Esperado::text, @Estado::text, @UsuarioId::uuid, @Fecha::timestamptz)",
            new { Id = id, Esperado = esperado, Estado = estado, UsuarioId = usuarioId, Fecha = fecha }, ct);

    public async Task<ParametrosSanidadInfo> ParametrosAsync(CancellationToken ct = default) =>
        await UnoAsync<ParametrosSanidadInfo>("SELECT * FROM fn_parametros_sanidad()", null, ct)
        ?? throw new InvalidOperationException("Falta la fila de parametros_sanidad (migración FuncionesSanidad).");

    public Task ActualizarParametrosAsync(ParametrosSanidadInfo p, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_parametros_sanidad_actualizar(@HorasAyunoVuelo::integer, @Fecha::timestamptz)",
            new { p.HorasAyunoVuelo, Fecha = fecha }, ct);

    public Task AsignarResponsableAsync(Guid acuarioId, Guid? responsableId, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_acuario_responsable(@AcuarioId::uuid, @ResponsableId::uuid, @Fecha::timestamptz)",
            new { AcuarioId = acuarioId, ResponsableId = responsableId, Fecha = fecha }, ct);
}

public class SanidadRepository(PostgresConnectionFactory conexiones) : RepositorioSanidad(conexiones), ISanidadRepository
{
    public Task<IReadOnlyList<TratamientoFila>> TratamientosAsync(Guid? id, bool soloVigentes, DateOnly hoy, CancellationToken ct = default) =>
        ListarAsync<TratamientoFila>(
            "SELECT * FROM fn_tratamientos_listar(@Id::uuid, @SoloVigentes::boolean, @Hoy::date)",
            new { Id = id, SoloVigentes = soloVigentes, Hoy = hoy }, ct);

    public Task<IReadOnlyList<AplicacionFila>> AplicacionesAsync(Guid? tratamientoId, DateOnly desde, DateOnly hasta, CancellationToken ct = default) =>
        ListarAsync<AplicacionFila>(
            "SELECT * FROM fn_aplicaciones_listar(@Id::uuid, @Desde::date, @Hasta::date)",
            new { Id = tratamientoId, Desde = desde, Hasta = hasta }, ct);

    public Task<FallaBaseDatos?> CrearTratamientoAsync(
        Tratamiento t, IReadOnlyList<AplicacionNueva> aplicaciones, DateTime fecha, CancellationToken ct = default) =>
        EjecutarValidandoAsync(
            """
            CALL sp_tratamiento_crear(@Id::uuid, @Codigo::text, @AcuarioId::uuid, @ProtocoloId::uuid, @Motivo::text, @Diagnostico::text,
                @Litros::numeric, @FechaInicio::date, @FechaFin::date, @FechaLiberacion::date, @Nota::text, @CreadoPorId::uuid,
                @Aplicaciones::jsonb, @Fecha::timestamptz)
            """,
            new
            {
                t.Id, t.Codigo, t.AcuarioId, t.ProtocoloId, t.Motivo, t.Diagnostico, t.Litros, t.FechaInicio, t.FechaFin, t.FechaLiberacion,
                t.Nota, t.CreadoPorId,
                Aplicaciones = JsonSerializer.Serialize(aplicaciones.Select(a => new
                {
                    fecha = a.Fecha.ToString("yyyy-MM-dd"), dia = a.Dia, orden = a.Orden, accion = a.Accion, medicamento = a.MedicamentoId,
                    agua = a.PorcentajeAgua, cantidad = a.Cantidad, unidad = a.Unidad, nota = a.Nota,
                })),
                Fecha = fecha,
            },
            ct);

    public Task<FallaBaseDatos?> RegistrarAplicacionAsync(
        Guid id, string estado, decimal? cantidad, string? nota, Operador operador, DateTime fecha, CancellationToken ct = default) =>
        EjecutarValidandoAsync(
            """
            CALL sp_aplicacion_registrar(@Id::uuid, @Estado::text, @Cantidad::numeric, @Nota::text, @UsuarioId::uuid, @UsuarioNombre::text,
                @Fecha::timestamptz)
            """,
            new { Id = id, Estado = estado, Cantidad = cantidad, Nota = nota, operador.UsuarioId, UsuarioNombre = operador.Nombre, Fecha = fecha },
            ct);

    public Task<FallaBaseDatos?> CambiarEstadoTratamientoAsync(
        Guid id, string esperado, string estado, DateOnly? liberacion, DateTime fecha, CancellationToken ct = default) =>
        EjecutarValidandoAsync(
            "CALL sp_tratamiento_cambiar_estado(@Id::uuid, @Esperado::text, @Estado::text, @Liberacion::date, @Fecha::timestamptz)",
            new { Id = id, Esperado = esperado, Estado = estado, Liberacion = liberacion, Fecha = fecha }, ct);

    public Task GuardarReporteAsync(Guid id, string? clave, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync("CALL sp_tratamiento_reporte(@Id::uuid, @Clave::text, @Fecha::timestamptz)", new { Id = id, Clave = clave, Fecha = fecha }, ct);

    public Task RegistrarAlimentacionAsync(
        Guid acuarioId, Guid? alimentoId, decimal? cantidad, Operador operador, DateTime fecha, CancellationToken ct = default) =>
        EjecutarAsync(
            """
            CALL sp_alimentacion_registrar(@AcuarioId::uuid, @AlimentoId::uuid, @Cantidad::numeric, @UsuarioId::uuid, @UsuarioNombre::text,
                @Fecha::timestamptz)
            """,
            new { AcuarioId = acuarioId, AlimentoId = alimentoId, Cantidad = cantidad, operador.UsuarioId, UsuarioNombre = operador.Nombre, Fecha = fecha },
            ct);

    public Task<IReadOnlyList<AlimentacionFila>> AlimentacionDesdeAsync(DateTime desde, CancellationToken ct = default) =>
        ListarAsync<AlimentacionFila>("SELECT * FROM fn_alimentacion_desde(@Desde::timestamptz)", new { Desde = desde }, ct);

    public Task<IReadOnlyList<ReservaFila>> ReservasPedidoAsync(Guid pedidoId, CancellationToken ct = default) =>
        ListarAsync<ReservaFila>("SELECT * FROM fn_reservas_pedido(@Id::uuid)", new { Id = pedidoId }, ct);

    public Task<FallaBaseDatos?> FijarReservaAsync(
        Guid lineaId, Guid loteId, Guid acuarioId, int cantidad, Guid? usuarioId, DateTime fecha, CancellationToken ct = default) =>
        EjecutarValidandoAsync(
            "CALL sp_reserva_fijar(@LineaId::uuid, @LoteId::uuid, @AcuarioId::uuid, @Cantidad::integer, @UsuarioId::uuid, @Fecha::timestamptz)",
            new { LineaId = lineaId, LoteId = loteId, AcuarioId = acuarioId, Cantidad = cantidad, UsuarioId = usuarioId, Fecha = fecha }, ct);

    public Task<IReadOnlyList<SanidadAcuarioFila>> AcuariosAsync(CancellationToken ct = default) =>
        ListarAsync<SanidadAcuarioFila>("SELECT * FROM fn_sanidad_acuarios()", null, ct);

    public Task<IReadOnlyList<SanidadExistenciaFila>> ExistenciasAsync(CancellationToken ct = default) =>
        ListarAsync<SanidadExistenciaFila>("SELECT * FROM fn_sanidad_existencias()", null, ct);

    public Task<IReadOnlyList<TratamientoVigenteFila>> TratamientosVigentesAsync(DateOnly hoy, CancellationToken ct = default) =>
        ListarAsync<TratamientoVigenteFila>("SELECT * FROM fn_sanidad_tratamientos_vigentes(@Hoy::date)", new { Hoy = hoy }, ct);

    public Task<IReadOnlyList<SanidadReservaFila>> ReservasAsync(CancellationToken ct = default) =>
        ListarAsync<SanidadReservaFila>("SELECT * FROM fn_sanidad_reservas()", null, ct);
}
