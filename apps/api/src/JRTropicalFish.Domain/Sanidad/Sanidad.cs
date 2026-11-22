using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Sanidad;

// Sanidad: medicación, cuarentena y alimentación de los acuarios de bodega. Las dosis de referencia
// vienen de los reportes del veterinario (AQUAVET); los protocolos solo se usan cuando están aprobados.

/// <summary>Insumo veterinario con su dosis de referencia y la fuente de donde salió.</summary>
public class Medicamento : EntidadBase
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public string? PrincipioActivo { get; set; }

    /// <summary>"Levamisol 15 %", "Tableta 500 mg".</summary>
    public string? Presentacion { get; set; }

    /// <summary>Unidad en que se aplica (<see cref="UnidadesSanidad.Medicamento"/>).</summary>
    public required string Unidad { get; set; }

    /// <summary><see cref="TiposDosis"/>.</summary>
    public required string TipoDosis { get; set; }

    /// <summary>Cantidad de referencia (0,6 mL…). En un rango (2,5–5 mg/L) es el mínimo.</summary>
    public decimal DosisCantidad { get; set; }

    /// <summary>Máximo del rango, si la referencia es un rango.</summary>
    public decimal? DosisMaxima { get; set; }

    /// <summary>Por cada cuántos litros (o gramos de alimento) va la cantidad: 100 en "0,6 mL/100 L".</summary>
    public decimal DosisBase { get; set; }

    /// <summary>Tiempo de exposición del baño, en minutos (nulo = prolongado o no aplica).</summary>
    public int? ExposicionMinutos { get; set; }

    /// <summary>Los peces no se alimentan durante el tratamiento ni las 24 h previas.</summary>
    public bool RequiereAyuno { get; set; } = true;

    /// <summary>Días después de la última aplicación antes de poder despachar (lo fija el veterinario).</summary>
    public int DiasRetiro { get; set; }

    public string? Indicaciones { get; set; }

    public string? Contraindicaciones { get; set; }

    /// <summary>De dónde sale la dosis de referencia ("AQUAVET, Corydoras 15/01/2026").</summary>
    public string? Fuente { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>Par de medicamentos que no se deben combinar en el mismo acuario.</summary>
public class MedicamentoIncompatible
{
    public Guid MedicamentoId { get; set; }

    public Guid IncompatibleId { get; set; }
}

public class Alimento : EntidadBase
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    /// <summary><see cref="TiposAlimento"/>.</summary>
    public required string Tipo { get; set; }

    /// <summary><see cref="UnidadesSanidad.Alimento"/>.</summary>
    public required string Unidad { get; set; }

    public bool Activo { get; set; } = true;
}

/// <summary>
/// Ración de un alimento para un grupo de producto o para un producto. La del producto reemplaza a la del
/// grupo (si un producto tiene alguna dieta propia, no hereda ninguna del grupo).
/// </summary>
public class Dieta : EntidadBase
{
    public Guid? GrupoId { get; set; }

    public Guid? ProductoId { get; set; }

    public Guid AlimentoId { get; set; }

    public int VecesDia { get; set; }

    /// <summary>Cantidad por toma por cada 100 peces, en la unidad del alimento.</summary>
    public decimal CantidadPor100 { get; set; }

    public string? Nota { get; set; }
}

/// <summary>Ficha técnica de la especie (formato FT.CA.01). Una por producto.</summary>
public class FichaTecnica
{
    public Guid ProductoId { get; set; }

    public string? Procedencia { get; set; }

    public string? Caracteristicas { get; set; }

    public string? Dimorfismo { get; set; }

    public decimal? PhMin { get; set; }

    public decimal? PhMax { get; set; }

    public decimal? GhMin { get; set; }

    public decimal? GhMax { get; set; }

    public decimal? TemperaturaMin { get; set; }

    public decimal? TemperaturaMax { get; set; }

    public string? Etologia { get; set; }

    public string? Empaque { get; set; }

    public string? SensibilidadQuimica { get; set; }

    /// <summary>Horas sin alimentar después de llegar a bodega.</summary>
    public int AyunoLlegadaHoras { get; set; } = 24;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }
}

/// <summary>
/// Plantilla de cuarentena o profilaxis. Los criterios (producto, grupo, centro de acopio, cuenca) deciden
/// a qué lotes se sugiere; solo se sugiere aprobado.
/// </summary>
public class Protocolo : EntidadBase
{
    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public string? Descripcion { get; set; }

    /// <summary><see cref="EstadosProtocolo"/>.</summary>
    public required string Estado { get; set; }

    public Guid? ProductoId { get; set; }

    public Guid? GrupoId { get; set; }

    public Guid? CentroAcopioId { get; set; }

    public Guid? CuencaId { get; set; }

    /// <summary>Duración de la cuarentena; obligatoria para aprobar.</summary>
    public int? DiasCuarentena { get; set; }

    public int AyunoInicialHoras { get; set; } = 24;

    public Guid? AprobadoPorId { get; set; }

    public DateTime? AprobadoEn { get; set; }
}

public class ProtocoloPaso
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid ProtocoloId { get; set; }

    /// <summary>Día del protocolo, desde 1.</summary>
    public int Dia { get; set; }

    public int Orden { get; set; }

    /// <summary><see cref="AccionesSanidad"/>.</summary>
    public required string Accion { get; set; }

    public Guid? MedicamentoId { get; set; }

    /// <summary>Porcentaje del cambio de agua.</summary>
    public int? PorcentajeAgua { get; set; }

    public string? Nota { get; set; }
}

/// <summary>Tratamiento de un acuario: desde un protocolo o una prescripción del veterinario.</summary>
public class Tratamiento : EntidadBase
{
    public required string Codigo { get; set; }

    public Guid AcuarioId { get; set; }

    public Guid? ProtocoloId { get; set; }

    /// <summary><see cref="MotivosTratamiento"/>.</summary>
    public required string Motivo { get; set; }

    public string? Diagnostico { get; set; }

    /// <summary>Litros del acuario cuando se calcularon las dosis.</summary>
    public decimal? Litros { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    /// <summary>Desde cuándo se puede despachar: fin + días de retiro.</summary>
    public DateOnly FechaLiberacion { get; set; }

    /// <summary><see cref="EstadosTratamiento"/>.</summary>
    public required string Estado { get; set; }

    /// <summary>Clave S3 del reporte del veterinario (PDF).</summary>
    public string? ReporteClave { get; set; }

    public string? Nota { get; set; }

    public Guid? CreadoPorId { get; set; }
}

public class TratamientoAplicacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid TratamientoId { get; set; }

    public DateOnly Fecha { get; set; }

    public int Dia { get; set; }

    public int Orden { get; set; }

    public required string Accion { get; set; }

    public Guid? MedicamentoId { get; set; }

    public int? PorcentajeAgua { get; set; }

    public decimal? CantidadCalculada { get; set; }

    public decimal? CantidadAplicada { get; set; }

    public string? Unidad { get; set; }

    public required string Estado { get; set; }

    public string? Nota { get; set; }

    public Guid? UsuarioId { get; set; }

    public string? UsuarioNombre { get; set; }

    public DateTime? RegistradoEn { get; set; }
}

/// <summary>Toque "Alimentado" del operario.</summary>
public class RegistroAlimentacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid AcuarioId { get; set; }

    public Guid? AlimentoId { get; set; }

    public decimal? Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public Guid? UsuarioId { get; set; }

    public string? UsuarioNombre { get; set; }
}

/// <summary>
/// Peces apartados de un lote y acuario para una línea de pedido de cliente. No mueve stock: lo
/// disponible es lo sano menos lo reservado. El despacho la consume.
/// </summary>
public class ReservaDespacho
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid PedidoClienteLineaId { get; set; }

    public Guid LoteId { get; set; }

    public Guid AcuarioId { get; set; }

    public int Cantidad { get; set; }

    public Guid? CreadoPorId { get; set; }

    public DateTime CreadoEn { get; set; }
}

/// <summary>Parámetros del módulo. Una sola fila (<c>Id = 1</c>).</summary>
public class ParametrosSanidad
{
    public int Id { get; set; } = 1;

    /// <summary>Horas de ayuno antes de la salida del vuelo. Nunca menos de 48.</summary>
    public int HorasAyunoVuelo { get; set; } = 48;

    public DateTime? ActualizadoEn { get; set; }
}
