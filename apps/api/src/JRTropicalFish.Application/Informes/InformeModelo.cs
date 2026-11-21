namespace JRTropicalFish.Application.Informes;

/// <summary>
/// Descripción de un informe, independiente de la librería que lo dibuje.
///
/// Proformas, packing lists, facturas comerciales y reportes de inventario salen en PDF. Para que eso no signifique
/// escribir un PDF distinto cada vez, la pantalla solo arma este modelo y una única implementación lo
/// convierte en documento: agregar una exportación nueva es escribir su constructor de modelo y un
/// endpoint, sin volver a tocar código de PDF.
/// </summary>
public record InformeModelo(
    string Titulo,
    string? Subtitulo,
    string EmpresaNombre,
    string GeneradoPor,
    DateTime GeneradoEnUtc,
    IReadOnlyList<SeccionInforme> Secciones)
{
    /// <summary>Logo (PNG/JPG) a la izquierda del encabezado. Nulo = solo texto.</summary>
    public byte[]? Logo { get; init; }

    /// <summary>Líneas bajo el nombre de la empresa (NIT, dirección, teléfono…) en los documentos comerciales.</summary>
    public IReadOnlyList<string>? EmpresaDatos { get; init; }

    /// <summary>Hoja horizontal (tablas anchas).</summary>
    public bool Horizontal { get; init; }

    /// <summary>
    /// Texto del pie en lugar de "Generado por…" (los documentos que se envían al cliente no muestran quién
    /// los generó). La numeración de páginas se mantiene.
    /// </summary>
    public string? Pie { get; init; }
}

public abstract record SeccionInforme(string Titulo);

/// <summary>Pares etiqueta/valor, como los datos básicos de una ficha.</summary>
public record SeccionCampos(string Titulo, IReadOnlyList<CampoInforme> Campos) : SeccionInforme(Titulo);

/// <summary>Tabla. <paramref name="PesosColumna"/> reparte el ancho; si es nulo, todas iguales.</summary>
public record SeccionTabla(
    string Titulo,
    IReadOnlyList<string> Encabezados,
    IReadOnlyList<IReadOnlyList<string>> Filas,
    IReadOnlyList<int>? PesosColumna = null) : SeccionInforme(Titulo)
{
    /// <summary>Alineación por columna (las cifras van a la derecha). Nulo = todo a la izquierda.</summary>
    public IReadOnlyList<Alineacion>? Alineaciones { get; init; }

    /// <summary>Filas finales en negrita (subtotal, descuento, total), con las mismas columnas.</summary>
    public IReadOnlyList<IReadOnlyList<string>>? Totales { get; init; }

    /// <summary>Letra más pequeña para tablas con muchas filas (empaque, factura por caja).</summary>
    public bool Compacta { get; init; }
}

public enum Alineacion
{
    Izquierda,
    Derecha,
    Centro,
}

/// <summary>
/// Bloques de datos lado a lado: en los documentos comerciales, el cliente a la izquierda y el número,
/// la fecha, la aerolínea y la guía a la derecha.
/// </summary>
public record SeccionBloques(string Titulo, IReadOnlyList<BloqueInforme> Bloques) : SeccionInforme(Titulo);

public record BloqueInforme(string? Titulo, IReadOnlyList<CampoInforme> Campos);

/// <summary>Texto libre largo (observaciones, instrucciones).</summary>
public record SeccionTexto(string Titulo, string Contenido) : SeccionInforme(Titulo);

/// <summary>
/// Casillas de firma: una línea para firmar y un rótulo debajo.
///
/// Existe porque varios documentos se firman en papel: el acta de recepción de una importación
/// (quien entrega y quien recibe, con el conteo de DOA) o el packing list que acompaña el despacho
/// al aeropuerto. Sin un hueco donde firmar, el PDF no sirve para lo que se imprime.
///
/// Si la firma ya se hizo por vía electrónica, se pasa en <see cref="CasillaFirma.Firmado"/> y en
/// vez de la línea en blanco se imprime quién firmó y cuándo.
/// </summary>
public record SeccionFirmas(string Titulo, IReadOnlyList<CasillaFirma> Casillas)
    : SeccionInforme(Titulo);

/// <param name="Rotulo">Lo que va debajo de la línea: "Recibido por (bodega)".</param>
/// <param name="Nombre">Quién debe firmar, si se conoce. Se imprime sobre el rótulo.</param>
/// <param name="Firmado">Constancia de la firma electrónica, si ya existe.</param>
public record CasillaFirma(string Rotulo, string? Nombre = null, string? Firmado = null);

public record CampoInforme(string Etiqueta, string? Valor);

/// <summary>Genera el PDF de un informe. La implementación es el único punto que conoce QuestPDF.</summary>
public interface IGeneradorInformePdf
{
    byte[] Generar(InformeModelo informe);
}

public record InformePdf(byte[] Contenido, string NombreArchivo);
