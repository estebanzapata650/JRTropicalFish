namespace JRTropicalFish.Application.Administracion;

/// <summary>Empleado tal como sale de la base, con los nombres de sus maestros.</summary>
/// <remarks>⚠️ Dapper lo construye por posición: el orden debe coincidir con <c>fn_empleados_listar</c>.</remarks>
public record EmpleadoFila(
    Guid Id,
    Guid TipoIdentificacionId,
    string TipoIdentificacionCodigo,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string? Genero,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Ciudad,
    Guid CargoId,
    string CargoNombre,
    Guid? AreaId,
    string? AreaNombre,
    Guid TipoContratoId,
    string TipoContratoNombre,
    DateOnly FechaIngreso,
    DateOnly? FechaRetiro,
    decimal Salario,
    bool Activo,
    string? FotoClave,
    DateTime CreadoEn);

/// <summary>Lo que recibe el web.</summary>
/// <param name="Salario"><c>null</c> si quien consulta no tiene <c>empleados:ver-salario</c>.</param>
/// <param name="FotoVersion">
/// Cambia cada vez que cambia la foto (sirve para romper la caché del navegador); <c>null</c> si no tiene.
/// No se expone la clave del almacén.
/// </param>
public record EmpleadoDetalle(
    Guid Id,
    Guid TipoIdentificacionId,
    string TipoIdentificacionCodigo,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string? Genero,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Ciudad,
    Guid CargoId,
    string CargoNombre,
    Guid? AreaId,
    string? AreaNombre,
    Guid TipoContratoId,
    string TipoContratoNombre,
    DateOnly FechaIngreso,
    DateOnly? FechaRetiro,
    decimal? Salario,
    bool Activo,
    string? FotoVersion,
    DateTime CreadoEn);

/// <param name="Salario">Solo lo envía quien tiene <c>empleados:ver-salario</c>.</param>
public record GuardarEmpleadoRequest(
    Guid TipoIdentificacionId,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string? Genero,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Ciudad,
    Guid CargoId,
    Guid? AreaId,
    Guid TipoContratoId,
    DateOnly FechaIngreso,
    DateOnly? FechaRetiro,
    decimal? Salario);

public record CambiarEstadoEmpleadoRequest(bool Activo);

/// <summary>
/// Lo que se audita de un empleado: nombres de los maestros (no ids) para que el historial se lea.
/// La foto se audita aparte ("Foto: actualizada").
/// </summary>
public record EmpleadoAuditable(
    string TipoIdentificacion,
    string NumeroIdentificacion,
    string Nombres,
    string Apellidos,
    string? Genero,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Email,
    string? Direccion,
    string? Ciudad,
    string Cargo,
    string? Area,
    string TipoContrato,
    DateOnly FechaIngreso,
    DateOnly? FechaRetiro,
    decimal Salario,
    bool Activo);
