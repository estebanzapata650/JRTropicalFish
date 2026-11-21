namespace JRTropicalFish.Application.Maestros;

public record MaestroInfo(string Clave, string Nombre, string NombreSingular, string Descripcion);

/// <remarks>⚠️ Dapper lo construye por posición: el orden debe coincidir con <c>fn_maestro_listar</c>.</remarks>
public record ValorMaestro(Guid Id, string Codigo, string Nombre, bool Activo, DateTime CreadoEn);

public record GuardarValorMaestroRequest(string Codigo, string Nombre, bool Activo);

public record CambiarEstadoValorMaestroRequest(bool Activo);

/// <summary>Lo que se audita de un valor de maestro.</summary>
public record ValorMaestroAuditable(string Codigo, string Nombre, bool Activo);
