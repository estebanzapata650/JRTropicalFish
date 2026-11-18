using System.Data;
using Dapper;
using NpgsqlTypes;

namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>
/// Enseña a Dapper a mapear <see cref="DateOnly"/> contra las columnas <c>date</c> de Postgres.
///
/// Npgsql sí entiende <see cref="DateOnly"/>, pero Dapper no lo reconoce como tipo primitivo y
/// rechaza el parámetro con "cannot be used as a parameter value". Las fechas de expedición y
/// vencimiento de un documento son fechas sin hora: usar <c>DateTime</c> arrastraría una zona
/// horaria que no significa nada aquí.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value;
        if (parameter is Npgsql.NpgsqlParameter npgsql)
        {
            npgsql.NpgsqlDbType = NpgsqlDbType.Date;
        }
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly fecha => fecha,
        DateTime fechaHora => DateOnly.FromDateTime(fechaHora),
        string texto => DateOnly.Parse(texto),
        _ => throw new InvalidCastException($"No se puede convertir {value.GetType()} a DateOnly."),
    };
}
