namespace JRTropicalFish.Application.Abstractions;

/// <summary>
/// La hora actual, inyectable para que las reglas que dependen del tiempo (vencimiento de un
/// certificado sanitario, días en cuarentena, expiración de una proforma) se puedan probar.
/// </summary>
public interface IReloj
{
    DateTime AhoraUtc { get; }

    DateOnly Hoy { get; }
}
