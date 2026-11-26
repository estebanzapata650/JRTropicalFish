using JRTropicalFish.Domain.Comun;

namespace JRTropicalFish.Domain.Maestros;

// Maestros: una tabla por maestro, todas con la forma de CatalogoSimple. La lista de los que
// existen (clave en la API, tabla y nombre en pantalla) está en Application/Maestros/RegistroMaestros.

/// <summary>Tipo de identificación de una persona (CC, CE, Pasaporte…). No confundir con los
/// tipos de documento de importación/exportación (factura, CITES…).</summary>
public class TipoIdentificacion : CatalogoSimple;

/// <summary>Cargo de un empleado.</summary>
public class Cargo : CatalogoSimple;

/// <summary>Área o dependencia de la empresa.</summary>
public class Area : CatalogoSimple;

/// <summary>Tipo de contrato laboral.</summary>
public class TipoContrato : CatalogoSimple;

/// <summary>Grupo de productos del catálogo. El código es el prefijo del código de producto (AP, CO, TE…).</summary>
public class GrupoProducto : CatalogoSimple;

/// <summary>Cuenca de origen de un centro de acopio (Orinoco, Amazonas, Magdalena…).</summary>
public class Cuenca : CatalogoSimple;

/// <summary>Aerolínea de los despachos (código IATA de 2 letras: QR, AV, LH…).</summary>
public class Aerolinea : CatalogoSimple;

/// <summary>Agencia de carga que cotiza y gestiona el flete aéreo.</summary>
public class AgenteCarga : CatalogoSimple;

/// <summary>Aeropuerto de destino u origen (código IATA de 3 letras: HKG, NRT…).</summary>
public class Aeropuerto : CatalogoSimple;

/// <summary>Causa de mortalidad de los peces (en bodega o DOA).</summary>
public class CausaMortalidad : CatalogoSimple;

/// <summary>Concepto de costo de una compra o importación (flete, nacionalización, certificados…).</summary>
public class ConceptoCosto : CatalogoSimple;

/// <summary>Tipo de documento de comercio exterior (proforma, VUCE, DEX, CITES, AWB…). No confundir con los tipos de
/// identificación de las personas.</summary>
public class TipoDocumento : CatalogoSimple;
