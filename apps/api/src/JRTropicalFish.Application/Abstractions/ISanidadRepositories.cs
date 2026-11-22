using JRTropicalFish.Application.Sanidad;
using JRTropicalFish.Domain.Sanidad;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>Maestros de sanidad: medicamentos, alimentos, dietas, fichas técnicas, protocolos y parámetros.</summary>
public interface ISanidadMaestrosRepository
{
    Task<IReadOnlyList<MedicamentoFila>> MedicamentosAsync(CancellationToken ct = default);

    Task GuardarMedicamentoAsync(Medicamento m, bool esNuevo, IReadOnlyList<Guid> incompatibles, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<AlimentoFila>> AlimentosAsync(CancellationToken ct = default);

    Task GuardarAlimentoAsync(Alimento a, bool esNuevo, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<DietaFila>> DietasAsync(CancellationToken ct = default);

    Task GuardarDietaAsync(Dieta d, bool esNueva, DateTime fecha, CancellationToken ct = default);

    Task EliminarDietaAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<FichaResumenFila>> FichasAsync(CancellationToken ct = default);

    Task<FichaTecnicaInfo?> FichaAsync(Guid productoId, CancellationToken ct = default);

    Task GuardarFichaAsync(FichaTecnicaInfo f, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<ProtocoloFila>> ProtocolosAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProtocoloPasoFila>> PasosAsync(Guid protocoloId, CancellationToken ct = default);

    Task GuardarProtocoloAsync(Protocolo p, bool esNuevo, IReadOnlyList<ProtocoloPaso> pasos, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> CambiarEstadoProtocoloAsync(Guid id, string esperado, string estado, Guid? usuarioId, DateTime fecha, CancellationToken ct = default);

    Task<ParametrosSanidadInfo> ParametrosAsync(CancellationToken ct = default);

    Task ActualizarParametrosAsync(ParametrosSanidadInfo p, DateTime fecha, CancellationToken ct = default);

    Task AsignarResponsableAsync(Guid acuarioId, Guid? responsableId, DateTime fecha, CancellationToken ct = default);
}

/// <summary>Tratamientos, aplicaciones, alimentación, reservas y las lecturas del tablero.</summary>
public interface ISanidadRepository
{
    Task<IReadOnlyList<TratamientoFila>> TratamientosAsync(Guid? id, bool soloVigentes, DateOnly hoy, CancellationToken ct = default);

    Task<IReadOnlyList<AplicacionFila>> AplicacionesAsync(Guid? tratamientoId, DateOnly desde, DateOnly hasta, CancellationToken ct = default);

    Task<FallaBaseDatos?> CrearTratamientoAsync(Tratamiento t, IReadOnlyList<AplicacionNueva> aplicaciones, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> RegistrarAplicacionAsync(
        Guid id, string estado, decimal? cantidad, string? nota, Operador operador, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> CambiarEstadoTratamientoAsync(
        Guid id, string esperado, string estado, DateOnly? liberacion, DateTime fecha, CancellationToken ct = default);

    Task GuardarReporteAsync(Guid id, string? clave, DateTime fecha, CancellationToken ct = default);

    Task RegistrarAlimentacionAsync(Guid acuarioId, Guid? alimentoId, decimal? cantidad, Operador operador, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<AlimentacionFila>> AlimentacionDesdeAsync(DateTime desde, CancellationToken ct = default);

    Task<IReadOnlyList<ReservaFila>> ReservasPedidoAsync(Guid pedidoId, CancellationToken ct = default);

    Task<FallaBaseDatos?> FijarReservaAsync(Guid lineaId, Guid loteId, Guid acuarioId, int cantidad, Guid? usuarioId, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<SanidadAcuarioFila>> AcuariosAsync(CancellationToken ct = default);

    Task<IReadOnlyList<SanidadExistenciaFila>> ExistenciasAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TratamientoVigenteFila>> TratamientosVigentesAsync(DateOnly hoy, CancellationToken ct = default);

    Task<IReadOnlyList<SanidadReservaFila>> ReservasAsync(CancellationToken ct = default);
}
