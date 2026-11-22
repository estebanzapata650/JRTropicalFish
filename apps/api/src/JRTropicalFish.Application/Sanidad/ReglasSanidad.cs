using JRTropicalFish.Domain.Sanidad;

namespace JRTropicalFish.Application.Sanidad;

/// <summary>
/// Reglas de sanidad, sin base de datos: dosis, estado operativo del acuario (columna del tablero), motivos de
/// ayuno, ración de alimento y sugerencia de protocolo. Todo lo que ve el operario sale de aquí.
/// </summary>
public static class ReglasSanidad
{
    /// <summary>Colombia no tiene horario de verano: Bogotá es UTC−5 todo el año.</summary>
    public static readonly TimeSpan DesfaseBogota = TimeSpan.FromHours(5);

    public const int HorasAyunoVueloMinimo = 48;
    public const int HorasAyunoVueloMaximo = 120;

    // --- Dosis ---

    /// <summary>
    /// Cantidad a aplicar en el acuario. Por volumen: proporcional a los litros (0,6 mL/100 L en 80 L = 0,48 mL).
    /// Fija: la cantidad de referencia. Por alimento: no depende del acuario (nulo; se indica "por cada N g de alimento").
    /// Sin litraje no se calcula (nulo).
    /// </summary>
    public static decimal? Dosis(string tipoDosis, decimal cantidad, decimal baseDosis, decimal? litros, string unidad) =>
        tipoDosis switch
        {
            TiposDosis.PorVolumen when litros is > 0 && baseDosis > 0 => Redondear(cantidad * litros.Value / baseDosis, unidad),
            TiposDosis.Fija => Redondear(cantidad, unidad),
            _ => null,
        };

    /// <summary>Redondeo práctico por unidad: lo que un operario puede medir.</summary>
    public static decimal Redondear(decimal valor, string unidad) => unidad switch
    {
        UnidadesSanidad.Mililitro => valor < 1 ? Math.Round(valor, 2, MidpointRounding.AwayFromZero) : Math.Round(valor, 1, MidpointRounding.AwayFromZero),
        UnidadesSanidad.Gramo => Math.Round(valor, 2, MidpointRounding.AwayFromZero),
        UnidadesSanidad.Miligramo => valor < 10 ? Math.Round(valor, 1, MidpointRounding.AwayFromZero) : Math.Round(valor, 0, MidpointRounding.AwayFromZero),
        UnidadesSanidad.Gota => Math.Max(1, Math.Round(valor, 0, MidpointRounding.AwayFromZero)),
        UnidadesSanidad.Tableta => Math.Max(0.5m, Math.Round(valor * 2, 0, MidpointRounding.AwayFromZero) / 2),
        UnidadesSanidad.Cubo => Math.Max(1, Math.Round(valor, 0, MidpointRounding.AwayFromZero)),
        _ => Math.Round(valor, 2, MidpointRounding.AwayFromZero),
    };

    // --- Tratamientos ---

    /// <summary>
    /// Aplicaciones de un tratamiento a partir de sus pasos: fecha = inicio + (día − 1); dosis calculada con los
    /// litros del acuario. Los pasos "por alimento" llevan la indicación en la nota.
    /// </summary>
    public static IReadOnlyList<AplicacionNueva> Aplicaciones(
        IReadOnlyList<PasoRequest> pasos, IReadOnlyDictionary<Guid, MedicamentoFila> medicamentos, DateOnly inicio, decimal? litros)
    {
        var resultado = new List<AplicacionNueva>();
        foreach (var grupo in pasos.GroupBy(p => p.Dia).OrderBy(g => g.Key))
        {
            var orden = 1;
            foreach (var paso in grupo)
            {
                var medicamento = paso.MedicamentoId is { } id ? medicamentos.GetValueOrDefault(id) : null;
                var cantidad = medicamento is null
                    ? null
                    : Dosis(medicamento.TipoDosis, medicamento.DosisCantidad, medicamento.DosisBase, litros, medicamento.Unidad);
                var nota = medicamento is { TipoDosis: TiposDosis.PorAlimento }
                    ? Unir(paso.Nota, $"En el alimento: {Numero(medicamento.DosisCantidad)} {medicamento.Unidad} por cada {Numero(medicamento.DosisBase)} g.")
                    : paso.Nota;
                resultado.Add(new AplicacionNueva(
                    inicio.AddDays(paso.Dia - 1), paso.Dia, orden++, paso.Accion!, paso.MedicamentoId,
                    paso.Accion == AccionesSanidad.CambioAgua ? paso.PorcentajeAgua : null, cantidad, medicamento?.Unidad, nota));
            }
        }

        return resultado;
    }

    /// <summary>Último día del tratamiento: el mayor entre los días pedidos y el último paso.</summary>
    public static DateOnly FechaFin(DateOnly inicio, int? dias, IReadOnlyList<PasoRequest> pasos) =>
        inicio.AddDays(Math.Max(dias ?? 1, pasos.Count == 0 ? 1 : pasos.Max(p => p.Dia)) - 1);

    /// <summary>Desde cuándo se puede despachar: el día siguiente al fin más el mayor tiempo de retiro de lo aplicado.</summary>
    public static DateOnly FechaLiberacion(DateOnly fin, IEnumerable<MedicamentoFila> usados) =>
        fin.AddDays(1 + usados.Select(m => m.DiasRetiro).DefaultIfEmpty(0).Max());

    /// <summary>Día actual del tratamiento (1..total), para "Día 2/5".</summary>
    public static int DiaActual(DateOnly inicio, DateOnly fin, DateOnly hoy) =>
        Math.Clamp(hoy.DayNumber - inicio.DayNumber + 1, 1, fin.DayNumber - inicio.DayNumber + 1);

    public static int DiasTotal(DateOnly inicio, DateOnly fin) => fin.DayNumber - inicio.DayNumber + 1;

    // --- Vuelos y ayuno ---

    /// <summary>
    /// Hora de salida que cuenta para el ayuno: la del vuelo; sin hora, el inicio del día de despacho en Bogotá
    /// (lo más conservador: el ayuno empieza antes, nunca después).
    /// </summary>
    public static (DateTime Salida, bool SinHora)? SalidaVuelo(DateTime? salidaVuelo, DateOnly? fechaDespacho) =>
        salidaVuelo is { } s
            ? (DateTime.SpecifyKind(s, DateTimeKind.Utc), false)
            : fechaDespacho is { } f
                ? (f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) + DesfaseBogota, true)
                : null;

    /// <summary>Está en ayuno pre-vuelo: faltan las horas del parámetro o menos (también si el vuelo ya pasó).</summary>
    public static bool EnAyunoVuelo(DateTime salida, DateTime ahora, int horasAyuno) => (salida - ahora).TotalHours <= horasAyuno;

    /// <summary>Sigue en el ayuno de llegada: menos de las horas de la ficha desde que llegó.</summary>
    public static bool EnAyunoLlegada(DateTime llegada, int horas, DateTime ahora) => ahora < llegada.AddHours(horas);

    /// <summary>
    /// Columna del tablero, por prioridad: ayuno pre-vuelo &gt; cuarentena/medicación &gt; aclimatación &gt; estable.
    /// </summary>
    public static string Columna(bool ayunoVuelo, bool enTratamiento, bool hayEnfermos, bool enAclimatacion) =>
        ayunoVuelo ? EstadosAcuario.AyunoVuelo
        : enTratamiento || hayEnfermos ? EstadosAcuario.Cuarentena
        : enAclimatacion ? EstadosAcuario.Aclimatacion
        : EstadosAcuario.Estable;

    /// <summary>
    /// Motivos para no alimentar hoy (semáforo rojo), en orden de importancia. Un tratamiento con alimento medicado
    /// y sin baños que exijan ayuno no bloquea: se alimenta con el alimento medicado.
    /// </summary>
    public static IReadOnlyList<string> RazonesAyuno(bool ayunoVuelo, TratamientoAcuario? tratamiento, bool enAyunoLlegada)
    {
        var motivos = new List<string>();
        if (ayunoVuelo)
        {
            motivos.Add(MotivosAyuno.Vuelo);
        }

        if (tratamiento is { Estado: EstadosTratamiento.Activo, RequiereAyuno: true })
        {
            motivos.Add(MotivosAyuno.Tratamiento);
        }

        if (enAyunoLlegada)
        {
            motivos.Add(MotivosAyuno.Llegada);
        }

        return motivos;
    }

    // --- Alimentación ---

    /// <summary>
    /// Dietas que aplican a un producto: las propias si tiene alguna; si no, las de su grupo.
    /// </summary>
    public static IReadOnlyList<DietaFila> DietasDe(Guid productoId, Guid grupoId, IReadOnlyList<DietaFila> dietas)
    {
        var propias = dietas.Where(d => d.ProductoId == productoId).ToList();
        return propias.Count > 0 ? propias : [.. dietas.Where(d => d.GrupoId == grupoId)];
    }

    /// <summary>Cantidad por toma: ración por cada 100 peces × peces / 100, redondeada a la unidad del alimento.</summary>
    public static decimal CantidadToma(decimal cantidadPor100, int peces, string unidad) =>
        peces <= 0 ? 0 : Redondear(cantidadPor100 * peces / 100m, unidad);

    // --- Protocolo sugerido ---

    /// <summary>
    /// Protocolo aprobado que mejor encaja con el lote. Un criterio fijado que no coincide descarta el protocolo;
    /// entre los que quedan gana el más específico (producto &gt; grupo &gt; centro de acopio &gt; cuenca). Uno sin
    /// criterios aplica a todo. Un borrador nunca se sugiere.
    /// </summary>
    public static ProtocoloFila? Sugerir(
        IReadOnlyList<ProtocoloFila> protocolos, Guid productoId, Guid grupoId, Guid? centroAcopioId, Guid? cuencaId) =>
        protocolos
            .Where(p => p.Estado == EstadosProtocolo.Aprobado)
            .Where(p => (p.ProductoId is null || p.ProductoId == productoId)
                        && (p.GrupoId is null || p.GrupoId == grupoId)
                        && (p.CentroAcopioId is null || p.CentroAcopioId == centroAcopioId)
                        && (p.CuencaId is null || p.CuencaId == cuencaId))
            .OrderByDescending(p => (p.ProductoId is null ? 0 : 8) + (p.GrupoId is null ? 0 : 4) + (p.CentroAcopioId is null ? 0 : 2)
                                    + (p.CuencaId is null ? 0 : 1))
            .ThenBy(p => p.Codigo, StringComparer.Ordinal)
            .FirstOrDefault();

    // --- Validaciones ---

    public static string? ValidarPasos(IReadOnlyList<PasoRequest> pasos, IReadOnlyDictionary<Guid, MedicamentoFila> medicamentos)
    {
        if (pasos.Count > 200)
        {
            return "Un protocolo tiene como máximo 200 pasos.";
        }

        foreach (var p in pasos)
        {
            if (p.Dia is < 1 or > 120)
            {
                return "El día de cada paso va de 1 a 120.";
            }

            if (p.Accion is null || !AccionesSanidad.Todas.Contains(p.Accion))
            {
                return "Cada paso necesita una acción válida.";
            }

            if (p.Accion == AccionesSanidad.Medicar && (p.MedicamentoId is not { } id || !medicamentos.TryGetValue(id, out var m) || !m.Activo))
            {
                return $"El paso del día {p.Dia} necesita un medicamento activo.";
            }

            if (p.Accion == AccionesSanidad.CambioAgua && p.PorcentajeAgua is not (>= 1 and <= 100))
            {
                return $"El cambio de agua del día {p.Dia} necesita un porcentaje de 1 a 100.";
            }

            if (p.Nota is { Length: > 500 })
            {
                return "La nota de un paso tiene como máximo 500 caracteres.";
            }
        }

        return null;
    }

    /// <summary>Medicamentos incompatibles entre sí dentro de una misma lista (aviso, no bloqueo).</summary>
    public static IReadOnlyList<(MedicamentoFila A, MedicamentoFila B)> Incompatibles(IReadOnlyList<MedicamentoFila> usados)
    {
        var pares = new List<(MedicamentoFila, MedicamentoFila)>();
        for (var i = 0; i < usados.Count; i++)
        {
            for (var j = i + 1; j < usados.Count; j++)
            {
                if (usados[i].Incompatibles.Contains(usados[j].Id))
                {
                    pares.Add((usados[i], usados[j]));
                }
            }
        }

        return pares;
    }

    private static string Numero(decimal valor) => valor.ToString("0.####", System.Globalization.CultureInfo.GetCultureInfo("es-CO"));

    private static string Unir(string? a, string b) => string.IsNullOrWhiteSpace(a) ? b : $"{a} {b}";
}
