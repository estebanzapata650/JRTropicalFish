using JRTropicalFish.Application.Exportaciones;
using JRTropicalFish.Domain.Exportaciones;

namespace JRTropicalFish.Application.Abstractions;

/// <summary>Configuración de los documentos de exportación (empresa, tarifas, cajas, bolsas, cargos, cliente).</summary>
public interface IConfiguracionExportacionRepository
{
    Task<DatosEmpresaInfo> DatosEmpresaAsync(CancellationToken ct = default);

    Task ActualizarDatosEmpresaAsync(GuardarDatosEmpresaRequest datos, DateTime fecha, CancellationToken ct = default);

    Task ActualizarLogoAsync(string? clave, DateTime fecha, CancellationToken ct = default);

    Task<TarifasFleteInfo> TarifasAsync(CancellationToken ct = default);

    Task ActualizarTarifasAsync(TarifasFleteInfo tarifas, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<TipoCajaFila>> TiposCajaAsync(CancellationToken ct = default);

    Task GuardarTipoCajaAsync(Guid id, GuardarTipoCaja caja, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<TipoBolsaFila>> TiposBolsaAsync(CancellationToken ct = default);

    Task GuardarTipoBolsaAsync(Guid id, GuardarTipoBolsa bolsa, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<CargoFila>> CargosAsync(CancellationToken ct = default);

    Task ActualizarCargoAsync(string codigo, string nombre, decimal precio, CancellationToken ct = default);

    Task<ClienteExportacionInfo?> ClienteAsync(Guid clienteId, CancellationToken ct = default);

    Task GuardarClienteAsync(ClienteExportacionInfo cliente, DateTime fecha, CancellationToken ct = default);
}

public interface IEmbarqueRepository
{
    Task<IReadOnlyList<EmbarqueFila>> ListarAsync(Guid? comercialId, CancellationToken ct = default);

    Task<EmbarqueFila?> ObtenerAsync(Guid id, CancellationToken ct = default);

    Task<Guid?> IdPorVentaAsync(Guid ventaId, CancellationToken ct = default);

    Task<IReadOnlyList<EmbarqueLineaFila>> LineasAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<EmbarqueEmpaqueFila>> EmpaqueAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentoEmbarqueFila>> DocumentosAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<EnvioDocumentoFila>> EnviosAsync(Guid id, CancellationToken ct = default);

    Task CrearAsync(Embarque embarque, DateTime fecha, CancellationToken ct = default);

    Task ActualizarAsync(Embarque embarque, DateTime fecha, CancellationToken ct = default);

    Task GuardarLineasAsync(Guid id, IReadOnlyList<EmbarqueLinea> lineas, DateTime fecha, CancellationToken ct = default);

    Task GuardarEmpaqueAsync(Guid id, IReadOnlyList<EmbarqueEmpaque> filas, DateTime fecha, CancellationToken ct = default);

    Task GuardarDoaAsync(Guid id, IReadOnlyList<DoaFilaRequest> filas, DateTime fecha, CancellationToken ct = default);

    Task<FallaBaseDatos?> CambiarEstadoAsync(Guid id, string actual, string nuevo, DateTime fecha, CancellationToken ct = default);

    Task RegistrarPreinvoiceAsync(Guid id, string numero, DateOnly fechaDocumento, DateTime fecha, CancellationToken ct = default);

    /// <summary>Asigna el número de factura (consecutivo FACT) y pasa a facturado. JR001 si cambió entretanto.</summary>
    Task<(int? Numero, FallaBaseDatos? Falla)> FacturarAsync(
        Guid id, string estadoActual, DateOnly fechaFactura, DateTime fecha, CancellationToken ct = default);

    Task RegistrarNotaCreditoAsync(Guid id, DateOnly fechaNota, DateTime fecha, CancellationToken ct = default);

    Task SincronizarCrmAsync(
        Guid id, decimal total, int cajas, decimal fob, decimal flete, decimal doa, string? notaCredito, DateTime fecha,
        CancellationToken ct = default);

    Task RegistrarDocumentoAsync(DocumentoEmbarque documento, DateTime fecha, CancellationToken ct = default);

    Task RegistrarEnvioAsync(EnvioDocumento envio, DateTime fecha, CancellationToken ct = default);
}

/// <summary>AUNAP: especies autorizadas, enlace producto → especie y permiso por embarque.</summary>
public interface IAunapRepository
{
    Task<IReadOnlyList<EspecieAunapFila>> EspeciesAsync(CancellationToken ct = default);

    Task<ImportacionAunap> ImportarAsync(IReadOnlyList<EspecieAunapImportar> especies, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<ProductoAunapFila>> ProductosAsync(CancellationToken ct = default);

    Task EnlazarProductoAsync(Guid productoId, Guid? especieAunapId, DateTime fecha, CancellationToken ct = default);

    Task<int> EnlazarAutomaticoAsync(DateTime fecha, CancellationToken ct = default);

    Task<PermisoAunapFila?> PermisoAsync(Guid embarqueId, CancellationToken ct = default);

    Task<IReadOnlyList<PermisoAunapLineaFila>> PermisoLineasAsync(Guid embarqueId, CancellationToken ct = default);

    Task GuardarPermisoAsync(Guid embarqueId, GuardarPermisoAunapRequest permiso, DateTime fecha, CancellationToken ct = default);

    /// <summary>Numera la proforma (consecutivo PRO) si aún no tiene número; devuelve el número.</summary>
    Task<string> AsignarProformaAsync(Guid embarqueId, DateOnly fecha, DateTime actualizado, CancellationToken ct = default);

    Task<IReadOnlyList<MunicipioAunapFila>> MunicipiosAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CentroAcopioAunapFila>> CentrosAsync(CancellationToken ct = default);

    Task GuardarCentroAsync(Guid centroId, int? municipioId, bool excluir, DateTime fecha, CancellationToken ct = default);

    Task<ParametrosAunapFila> ParametrosAsync(CancellationToken ct = default);

    Task GuardarParametrosAsync(GuardarParametrosAunapRequest p, DateTime fecha, CancellationToken ct = default);

    Task<IReadOnlyList<DespachoAunapFila>> DespachosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default);
}

/// <summary>Arma el .xlsx del informe mensual en el formato de la AUNAP.</summary>
public interface IGeneradorInformeAunapExcel
{
    byte[] Generar(InformeAunap informe);
}
