using JRTropicalFish.Domain.Administracion;
using JRTropicalFish.Domain.Catalogos;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Domain.Comun;
using JRTropicalFish.Domain.Crm;
using JRTropicalFish.Domain.Exportaciones;
using JRTropicalFish.Domain.Inventario;
using JRTropicalFish.Domain.Maestros;
using JRTropicalFish.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace JRTropicalFish.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core usado <b>únicamente</b> para generar y aplicar migraciones (DDL). La lectura y
/// escritura en tiempo de ejecución va por Dapper contra funciones y procedimientos PL/pgSQL.
///
/// <para>
/// Convenciones: nombres en snake_case (<c>UseSnakeCaseNamingConvention</c>), llaves foráneas con
/// <c>OnDelete(Restrict)</c> salvo tablas puente, y una sección <c>Configurar{Modulo}</c> por módulo.
/// </para>
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();
    public DbSet<TipoIdentificacion> TiposIdentificacion => Set<TipoIdentificacion>();
    public DbSet<Cargo> Cargos => Set<Cargo>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<TipoContrato> TiposContrato => Set<TipoContrato>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<TrackingVuelo> TrackingVuelos => Set<TrackingVuelo>();
    public DbSet<Tarea> Tareas => Set<Tarea>();
    public DbSet<MetaComercial> MetasComerciales => Set<MetaComercial>();
    public DbSet<ParametrosComerciales> ParametrosComerciales => Set<ParametrosComerciales>();
    public DbSet<Consecutivo> Consecutivos => Set<Consecutivo>();
    public DbSet<GrupoProducto> GruposProducto => Set<GrupoProducto>();
    public DbSet<Cuenca> Cuencas => Set<Cuenca>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<CentroAcopio> CentrosAcopio => Set<CentroAcopio>();
    public DbSet<ListaPrecios> ListasPrecios => Set<ListaPrecios>();
    public DbSet<PrecioProducto> PreciosProducto => Set<PrecioProducto>();
    public DbSet<ParametrosPrecios> ParametrosPrecios => Set<ParametrosPrecios>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Acuario> Acuarios => Set<Acuario>();
    public DbSet<OfertaProveedor> OfertasProveedor => Set<OfertaProveedor>();
    public DbSet<PedidoProveedor> PedidosProveedor => Set<PedidoProveedor>();
    public DbSet<Recepcion> Recepciones => Set<Recepcion>();
    public DbSet<Lote> Lotes => Set<Lote>();
    public DbSet<Existencia> Existencias => Set<Existencia>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<PedidoCliente> PedidosCliente => Set<PedidoCliente>();
    public DbSet<Embarque> Embarques => Set<Embarque>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarSeguridad(modelBuilder);
        ConfigurarAuditoria(modelBuilder);
        ConfigurarMaestros(modelBuilder);
        ConfigurarAdministracion(modelBuilder);
        ConfigurarCrm(modelBuilder);
        ConfigurarCatalogos(modelBuilder);
        ConfigurarInventario(modelBuilder);
        ConfigurarExportaciones(modelBuilder);
    }

    /// <summary>
    /// Exportaciones: configuración de documentos (empresa, cajas, bolsas, cargos, tarifas) y el embarque
    /// con sus líneas y su empaque real. Montos en USD <c>numeric(14,2)</c>; fracciones con 4 decimales.
    /// </summary>
    private static void ConfigurarExportaciones(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DatosEmpresa>(entity =>
        {
            entity.ToTable("datos_empresa", t => t.HasCheckConstraint("ck_datos_empresa_fila_unica", "id = 1"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.RazonSocial).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Nit).HasMaxLength(30);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.CiudadPais).HasMaxLength(150);
            entity.Property(e => e.Telefono).HasMaxLength(60);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.SitioWeb).HasMaxLength(200);
            entity.Property(e => e.Banco).HasMaxLength(100);
            entity.Property(e => e.Swift).HasMaxLength(20);
            entity.Property(e => e.CuentaBancaria).HasMaxLength(60);
            entity.Property(e => e.Beneficiario).HasMaxLength(200);
            entity.Property(e => e.DireccionBanco).HasMaxLength(250);
            entity.Property(e => e.FirmanteNombre).HasMaxLength(150);
            entity.Property(e => e.FirmanteCargo).HasMaxLength(100);
            entity.Property(e => e.FirmanteTelefono).HasMaxLength(60);
            entity.Property(e => e.LogoClave).HasMaxLength(300);
        });

        modelBuilder.Entity<TarifasFlete>(entity =>
        {
            entity.ToTable("tarifas_flete", t => t.HasCheckConstraint("ck_tarifas_flete_fila_unica", "id = 1"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            foreach (var propiedad in new[]
                     {
                         nameof(TarifasFlete.TarifaKgJr), nameof(TarifasFlete.TarifaKgCliente), nameof(TarifasFlete.FuelKg),
                         nameof(TarifasFlete.JpFeeKg), nameof(TarifasFlete.VscKg), nameof(TarifasFlete.VscMinimo),
                         nameof(TarifasFlete.DbcPorcentaje), nameof(TarifasFlete.DbcMinimo), nameof(TarifasFlete.DfPorcentaje),
                         nameof(TarifasFlete.DfMinimo), nameof(TarifasFlete.DueAgentJr), nameof(TarifasFlete.DueAgentCliente),
                         nameof(TarifasFlete.DueCarrier),
                     })
            {
                entity.Property(propiedad).HasPrecision(12, 4);
            }
        });

        modelBuilder.Entity<TipoCaja>(entity =>
        {
            entity.ToTable("tipos_caja", t =>
            {
                t.HasCheckConstraint("ck_tipos_caja_valores", "costo_cop >= 0 AND precio_usd >= 0 AND kg_por_caja >= 0");
                t.HasCheckConstraint("ck_tipos_caja_pisos", "pisos BETWEEN 1 AND 5");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(80).IsRequired();
            entity.Property(e => e.DescripcionFactura).HasMaxLength(150).IsRequired();
            entity.Property(e => e.CostoCop).HasPrecision(14, 2);
            entity.Property(e => e.PrecioUsd).HasPrecision(12, 2);
            entity.Property(e => e.KgPorCaja).HasPrecision(8, 2);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<TipoBolsa>(entity =>
        {
            entity.ToTable("tipos_bolsa", t => t.HasCheckConstraint("ck_tipos_bolsa_valores", "porcion > 0 AND porcion <= 1 AND costo_cop >= 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(40).IsRequired();
            entity.Property(e => e.Tamano).HasMaxLength(20);
            entity.Property(e => e.Porcion).HasPrecision(8, 6);
            entity.Property(e => e.CostoCop).HasPrecision(12, 2);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<CargoExportacion>(entity =>
        {
            entity.ToTable("cargos_exportacion", t =>
            {
                t.HasCheckConstraint("ck_cargos_exportacion_codigo", EnLista("codigo", CodigosCargo.Todos));
                t.HasCheckConstraint("ck_cargos_exportacion_precio", "precio_usd >= 0");
            });
            entity.HasKey(e => e.Codigo);
            entity.Property(e => e.Codigo).HasMaxLength(20);
            entity.Property(e => e.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(e => e.PrecioUsd).HasPrecision(12, 2);
        });

        modelBuilder.Entity<ClienteExportacion>(entity =>
        {
            entity.ToTable("clientes_exportacion", t =>
            {
                t.HasCheckConstraint("ck_clientes_exportacion_formato", EnLista("formato_factura", FormatosFactura.Todos));
                t.HasCheckConstraint("ck_clientes_exportacion_descuento", "descuento_peces BETWEEN 0 AND 100");
            });
            entity.HasKey(e => e.ClienteId);
            entity.Property(e => e.Direccion).HasMaxLength(300);
            entity.Property(e => e.Eori).HasMaxLength(40);
            entity.Property(e => e.FormatoFactura).HasMaxLength(10).IsRequired();
            entity.Property(e => e.DescuentoPeces).HasPrecision(5, 2);
            entity.HasOne<Cliente>().WithOne().HasForeignKey<ClienteExportacion>(e => e.ClienteId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ListaPrecios>().WithMany().HasForeignKey(e => e.ListaPreciosId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Embarque>(entity =>
        {
            entity.ToTable("embarques", t =>
            {
                t.HasCheckConstraint("ck_embarques_estado", EnLista("estado", EstadosEmbarque.Todos));
                t.HasCheckConstraint("ck_embarques_formato", EnLista("formato_factura", FormatosFactura.Todos));
                t.HasCheckConstraint(
                    "ck_embarques_valores",
                    "descuento_peces BETWEEN 0 AND 100 AND anticipo_recibido >= 0 AND (awb_valor IS NULL OR awb_valor >= 0)");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DescuentoPeces).HasPrecision(5, 2);
            entity.Property(e => e.Aerolinea).HasMaxLength(150);
            entity.Property(e => e.Ruta).HasMaxLength(150);
            entity.Property(e => e.Awb).HasMaxLength(50);
            entity.Property(e => e.Destino).HasMaxLength(200);
            entity.Property(e => e.PreinvoiceNumero).HasMaxLength(20);
            entity.Property(e => e.FormatoFactura).HasMaxLength(10).IsRequired();
            entity.Property(e => e.AwbValor).HasPrecision(14, 2);
            entity.Property(e => e.AnticipoRecibido).HasPrecision(14, 2);
            entity.Property(e => e.Tarifas).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Cargos).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CajasEstimadas).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CajasReales).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Notas).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.VentaId).IsUnique();
            entity.HasIndex(e => e.FacturaNumero).IsUnique().HasFilter("factura_numero IS NOT NULL");
            entity.HasIndex(e => new { e.Estado, e.ComercialId });
            entity.HasOne<Venta>().WithMany().HasForeignKey(e => e.VentaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PedidoCliente>().WithMany().HasForeignKey(e => e.PedidoClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ListaPrecios>().WithMany().HasForeignKey(e => e.ListaPreciosId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmbarqueLinea>(entity =>
        {
            entity.ToTable("embarque_lineas", t =>
                t.HasCheckConstraint("ck_embarque_lineas_valores", "cantidad > 0 AND precio >= 0 AND fraccion_caja >= 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Precio).HasPrecision(12, 4);
            entity.Property(e => e.FraccionCaja).HasPrecision(8, 4);
            entity.Property(e => e.Empaque).HasMaxLength(60);
            entity.Property(e => e.Nota).HasMaxLength(300);
            entity.HasIndex(e => new { e.EmbarqueId, e.Orden });
            entity.HasOne<Embarque>().WithMany().HasForeignKey(e => e.EmbarqueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmbarqueEmpaque>(entity =>
        {
            entity.ToTable("embarque_empaque", t =>
            {
                t.HasCheckConstraint("ck_embarque_empaque_valores", "numero_caja > 0 AND bolsas > 0 AND peces_por_bolsa > 0 AND precio >= 0");
                t.HasCheckConstraint(
                    "ck_embarque_empaque_doa",
                    "doa >= 0 AND doa_adicional >= 0 AND doa_final >= 0 AND doa + doa_adicional + doa_final <= bolsas * peces_por_bolsa");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreFactura).HasMaxLength(200);
            entity.Property(e => e.Precio).HasPrecision(12, 4);
            entity.Property(e => e.Nota).HasMaxLength(300);
            entity.HasIndex(e => new { e.EmbarqueId, e.NumeroCaja, e.Orden });
            entity.HasOne<Embarque>().WithMany().HasForeignKey(e => e.EmbarqueId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TipoBolsa>().WithMany().HasForeignKey(e => e.TipoBolsaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DocumentoEmbarque>(entity =>
        {
            entity.ToTable("embarque_documentos", t => t.HasCheckConstraint("ck_embarque_documentos_tipo", EnLista("tipo", TiposDocumentoEmbarque.Todos)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Numero).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ArchivoClave).HasMaxLength(300).IsRequired();
            entity.Property(e => e.GeneradoPor).HasMaxLength(200);
            entity.HasIndex(e => new { e.EmbarqueId, e.Tipo });
            entity.HasOne<Embarque>().WithMany().HasForeignKey(e => e.EmbarqueId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EnvioDocumento>(entity =>
        {
            entity.ToTable("embarque_envios", t => t.HasCheckConstraint("ck_embarque_envios_tipo", EnLista("tipo_documento", TiposDocumentoEmbarque.Todos)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TipoDocumento).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Para).HasMaxLength(320).IsRequired();
            entity.Property(e => e.Asunto).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Proveedor).HasMaxLength(40).IsRequired();
            entity.Property(e => e.MensajeId).HasMaxLength(200);
            entity.Property(e => e.Error).HasMaxLength(1000);
            entity.Property(e => e.EnviadoPor).HasMaxLength(200);
            entity.HasIndex(e => new { e.EmbarqueId, e.CreadoEn });
            entity.HasOne<Embarque>().WithMany().HasForeignKey(e => e.EmbarqueId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Inventario, compras y pedidos de cliente. El stock vive en <c>existencias</c> (saldo por lote ×
    /// acuario × condición) y solo cambia por <c>sp_inventario_mover</c>, que escribe el kardex
    /// (<c>movimientos_inventario</c>, inmutable). Cantidades en piezas enteras.
    /// </summary>
    private static void ConfigurarInventario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Acuario>(entity =>
        {
            entity.ToTable("acuarios", t => t.HasCheckConstraint("ck_acuarios_capacidad", "capacidad_litros IS NULL OR capacidad_litros > 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Sector).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CapacidadLitros).HasPrecision(10, 2);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        modelBuilder.Entity<OfertaProveedor>(entity =>
        {
            entity.ToTable("ofertas_proveedor", t => t.HasCheckConstraint("ck_ofertas_estado", EnLista("estado", EstadosOferta.Todos)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne<Proveedor>().WithMany().HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OfertaProveedorLinea>(entity =>
        {
            entity.ToTable("ofertas_proveedor_lineas", t => t.HasCheckConstraint("ck_ofertas_lineas_cantidad", "cantidad > 0"));
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OfertaId, e.ProductoId }).IsUnique();
            entity.HasOne<OfertaProveedor>().WithMany().HasForeignKey(e => e.OfertaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoProveedor>(entity =>
        {
            entity.ToTable("pedidos_proveedor", t => t.HasCheckConstraint("ck_pedidos_proveedor_estado", EnLista("estado", EstadosPedidoProveedor.Todos)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Estado);
            entity.HasOne<Proveedor>().WithMany().HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OfertaProveedor>().WithMany().HasForeignKey(e => e.OfertaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoProveedorLinea>(entity =>
        {
            entity.ToTable("pedidos_proveedor_lineas", t =>
            {
                t.HasCheckConstraint("ck_pedidos_proveedor_lineas_cantidad", "cantidad > 0");
                t.HasCheckConstraint("ck_pedidos_proveedor_lineas_costo", "costo_unitario IS NULL OR costo_unitario >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostoUnitario).HasPrecision(14, 2);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasIndex(e => new { e.PedidoId, e.ProductoId }).IsUnique();
            entity.HasOne<PedidoProveedor>().WithMany().HasForeignKey(e => e.PedidoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Recepcion>(entity =>
        {
            entity.ToTable("recepciones");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasOne<PedidoProveedor>().WithMany().HasForeignKey(e => e.PedidoProveedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.ToTable("lotes", t =>
            {
                t.HasCheckConstraint("ck_lotes_origen", "origen IN ('recepcion', 'saldo_inicial')");
                t.HasCheckConstraint("ck_lotes_cantidades", "recibidos >= 0 AND muertos_llegada >= 0 AND muertos_llegada <= recibidos");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CostoUnitario).HasPrecision(14, 2);
            entity.Property(e => e.Origen).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.ProductoId);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Proveedor>().WithMany().HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CentroAcopio>().WithMany().HasForeignKey(e => e.CentroAcopioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecepcionLinea>(entity =>
        {
            entity.ToTable("recepciones_lineas", t =>
                t.HasCheckConstraint("ck_recepciones_lineas_cantidades",
                    "recibida > 0 AND enferma >= 0 AND muerta >= 0 AND enferma + muerta <= recibida AND costo_unitario >= 0"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostoUnitario).HasPrecision(14, 2);
            entity.Property(e => e.CostoTotal).HasPrecision(18, 2).HasComputedColumnSql("recibida * costo_unitario", stored: true);
            entity.HasIndex(e => e.LoteId).IsUnique();
            entity.HasOne<Recepcion>().WithMany().HasForeignKey(e => e.RecepcionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PedidoProveedorLinea>().WithMany().HasForeignKey(e => e.PedidoLineaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Lote>().WithMany().HasForeignKey(e => e.LoteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Existencia>(entity =>
        {
            entity.ToTable("existencias", t =>
            {
                t.HasCheckConstraint("ck_existencias_condicion", EnLista("condicion", Condiciones.Todos));
                // La garantía de fondo: el stock nunca queda negativo, pase lo que pase arriba.
                t.HasCheckConstraint("ck_existencias_no_negativa", "cantidad >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Condicion).HasMaxLength(10).IsRequired();
            // Acuario nulo = por ubicar: NULLS NOT DISTINCT para que haya una sola fila "por ubicar" por lote y condición.
            entity.HasIndex(e => new { e.LoteId, e.AcuarioId, e.Condicion }).IsUnique().AreNullsDistinct(false);
            entity.HasIndex(e => e.AcuarioId);
            entity.HasOne<Lote>().WithMany().HasForeignKey(e => e.LoteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Acuario>().WithMany().HasForeignKey(e => e.AcuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoCliente>(entity =>
        {
            entity.ToTable("pedidos_cliente", t => t.HasCheckConstraint("ck_pedidos_cliente_estado", EnLista("estado", EstadosPedidoCliente.Todos)));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Estado).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Observaciones).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.VentaId).IsUnique().HasFilter("venta_id IS NOT NULL");
            entity.HasIndex(e => new { e.Estado, e.ComercialId });
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Venta>().WithMany().HasForeignKey(e => e.VentaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoClienteLinea>(entity =>
        {
            entity.ToTable("pedidos_cliente_lineas", t =>
            {
                t.HasCheckConstraint("ck_pedidos_cliente_lineas_cantidad", "cantidad > 0");
                t.HasCheckConstraint("ck_pedidos_cliente_lineas_precio", "precio_unitario IS NULL OR precio_unitario >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PrecioUnitario).HasPrecision(14, 2);
            entity.Property(e => e.Observacion).HasMaxLength(500);
            entity.HasIndex(e => new { e.PedidoId, e.ProductoId }).IsUnique();
            entity.HasOne<PedidoCliente>().WithMany().HasForeignKey(e => e.PedidoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.ToTable("movimientos_inventario", t =>
            {
                t.HasCheckConstraint("ck_movimientos_tipo", EnLista("tipo", TiposMovimiento.Todos));
                t.HasCheckConstraint("ck_movimientos_cantidad", "cantidad > 0");
                t.HasCheckConstraint("ck_movimientos_condiciones",
                    $"({EnLista("condicion_origen", Condiciones.Todos, admiteNulo: true)}) AND ({EnLista("condicion_destino", Condiciones.Todos, admiteNulo: true)})");
                t.HasCheckConstraint("ck_movimientos_lado", "condicion_origen IS NOT NULL OR condicion_destino IS NOT NULL");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.CondicionOrigen).HasMaxLength(10);
            entity.Property(e => e.CondicionDestino).HasMaxLength(10);
            entity.Property(e => e.Nota).HasMaxLength(500);
            entity.Property(e => e.UsuarioNombre).HasMaxLength(200);
            entity.HasIndex(e => new { e.LoteId, e.Fecha });
            entity.HasIndex(e => new { e.Tipo, e.Fecha });
            entity.HasIndex(e => e.PedidoClienteLineaId);
            entity.HasOne<Lote>().WithMany().HasForeignKey(e => e.LoteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Acuario>().WithMany().HasForeignKey(e => e.AcuarioOrigenId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Acuario>().WithMany().HasForeignKey(e => e.AcuarioDestinoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PedidoClienteLinea>().WithMany().HasForeignKey(e => e.PedidoClienteLineaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<RecepcionLinea>().WithMany().HasForeignKey(e => e.RecepcionLineaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary><c>CHECK (columna IN (...))</c> a partir de las constantes de <c>Domain/Crm/EnumsCrm.cs</c>.</summary>
    private static string EnLista(string columna, IReadOnlyList<string> valores, bool admiteNulo = false) =>
        $"{(admiteNulo ? $"{columna} IS NULL OR " : "")}{columna} IN ({string.Join(", ", valores.Select(v => $"'{v}'"))})";

    /// <summary>
    /// CRM comercial. Los enums son texto con CHECK; los montos <c>numeric(14,2)</c>; las columnas
    /// derivadas (<c>total_cop</c>, <c>porcentaje_doa</c>) son generadas por Postgres para que nadie
    /// pueda guardarlas desalineadas.
    /// </summary>
    private static void ConfigurarCrm(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Consecutivo>(entity =>
        {
            entity.ToTable("consecutivos");
            entity.HasKey(e => e.Prefijo);
            entity.Property(e => e.Prefijo).HasMaxLength(10);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes", t =>
            {
                t.HasCheckConstraint("ck_clientes_tipo", EnLista("tipo_cliente", TiposCliente.Todos));
                t.HasCheckConstraint("ck_clientes_licencia", EnLista("licencia_importacion", LicenciasImportacion.Todos));
                t.HasCheckConstraint("ck_clientes_estado", EnLista("estado", EstadosCliente.Todos));
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NombreEmpresa).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ContactoPrincipal).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.TelefonoWhatsapp).HasMaxLength(40);
            entity.Property(e => e.Pais).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.AeropuertoIata).HasMaxLength(3);
            entity.Property(e => e.TipoCliente).HasMaxLength(30).IsRequired();
            entity.Property(e => e.LicenciaImportacion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Notas).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("eliminado_en IS NULL");
            entity.HasIndex(e => e.NombreEmpresa).IsUnique().HasFilter("eliminado_en IS NULL");
        });

        modelBuilder.Entity<Oportunidad>(entity =>
        {
            entity.ToTable("oportunidades", t =>
            {
                t.HasCheckConstraint("ck_oportunidades_tipo_venta", EnLista("tipo_venta", TiposVenta.Todos));
                t.HasCheckConstraint("ck_oportunidades_etapa", EnLista("etapa", EtapasPipeline.Todos));
                t.HasCheckConstraint("ck_oportunidades_moneda", EnLista("moneda", Monedas.Todos));
                t.HasCheckConstraint("ck_oportunidades_monto", "monto_estimado >= 0 AND cajas_estimadas >= 0");
                // Motivo obligatorio si y solo si está perdida; fecha de pago obligatoria si está ganada.
                t.HasCheckConstraint(
                    "ck_oportunidades_motivo_perdida",
                    "(etapa = 'perdida') = (motivo_perdida IS NOT NULL AND btrim(motivo_perdida) <> '')");
                t.HasCheckConstraint(
                    "ck_oportunidades_pago_ganada", "etapa <> 'ganada' OR fecha_pago_confirmado IS NOT NULL");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.TipoVenta).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Etapa).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Moneda).HasMaxLength(3).IsRequired();
            entity.Property(e => e.MontoEstimado).HasPrecision(14, 2);
            entity.Property(e => e.MotivoPerdida).HasMaxLength(500);
            entity.Property(e => e.NotasSeguimiento).HasMaxLength(4000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => new { e.Etapa, e.ComercialId });
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.ToTable("ventas", t =>
            {
                t.HasCheckConstraint("ck_ventas_origen", EnLista("origen_canal", OrigenesCanal.Todos));
                t.HasCheckConstraint("ck_ventas_tipo_venta", EnLista("tipo_venta", TiposVenta.Todos));
                t.HasCheckConstraint("ck_ventas_moneda", EnLista("moneda", Monedas.Todos));
                t.HasCheckConstraint("ck_ventas_estado_pago", EnLista("estado_pago", EstadosPago.Todos));
                t.HasCheckConstraint("ck_ventas_estado_preparacion", EnLista("estado_preparacion", EstadosPreparacion.Todos));
                t.HasCheckConstraint("ck_ventas_montos", "total >= 0 AND total_cajas >= 0");
                // USD exige TRM; COP no lleva TRM (total_cop = total).
                t.HasCheckConstraint("ck_ventas_trm", "(moneda = 'USD') = (trm IS NOT NULL AND trm > 0)");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.OrigenCanal).HasMaxLength(30).IsRequired();
            entity.Property(e => e.TipoVenta).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Moneda).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Total).HasPrecision(14, 2);
            entity.Property(e => e.Trm).HasPrecision(12, 2);
            entity.Property(e => e.TotalCop).HasPrecision(18, 2)
                .HasComputedColumnSql("round(total * coalesce(trm, 1), 2)", stored: true);
            entity.Property(e => e.TransportadoraAerolinea).HasMaxLength(150);
            entity.Property(e => e.NoGuiaAwb).HasMaxLength(50);
            entity.Property(e => e.EstadoPago).HasMaxLength(30).IsRequired();
            entity.Property(e => e.EstadoPreparacion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.CiudadEntrega).HasMaxLength(100);
            entity.Property(e => e.DireccionEntrega).HasMaxLength(250);
            entity.Property(e => e.Notas).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.OportunidadId).IsUnique().HasFilter("oportunidad_id IS NOT NULL");
            entity.HasIndex(e => e.FechaPedido);
            entity.HasOne<Oportunidad>().WithMany().HasForeignKey(e => e.OportunidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrackingVuelo>(entity =>
        {
            entity.ToTable("tracking_vuelos", t =>
            {
                t.HasCheckConstraint("ck_tracking_fase", EnLista("fase", FasesTracking.Todos));
                t.HasCheckConstraint(
                    "ck_tracking_montos",
                    "cajas_reales >= 0 AND monto_fob_usd >= 0 AND flete_aereo_usd >= 0 AND doa_reportado_usd >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NFacturaInvoice).HasMaxLength(50);
            entity.Property(e => e.NoAwb).HasMaxLength(50);
            entity.Property(e => e.Aerolinea).HasMaxLength(150);
            entity.Property(e => e.Fase).HasMaxLength(30).IsRequired();
            entity.Property(e => e.MontoFobUsd).HasPrecision(14, 2);
            entity.Property(e => e.FleteAereoUsd).HasPrecision(14, 2);
            entity.Property(e => e.DoaReportadoUsd).HasPrecision(14, 2);
            entity.Property(e => e.PorcentajeDoa).HasPrecision(7, 2)
                .HasComputedColumnSql(
                    "CASE WHEN monto_fob_usd > 0 THEN round(doa_reportado_usd / monto_fob_usd * 100, 2) END", stored: true);
            entity.Property(e => e.NotaCreditoNo).HasMaxLength(50);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.VentaId).IsUnique();
            entity.HasOne<Venta>().WithMany().HasForeignKey(e => e.VentaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Oportunidad>().WithMany().HasForeignKey(e => e.OportunidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.ToTable("tareas", t =>
            {
                t.HasCheckConstraint("ck_tareas_tipo_accion", EnLista("tipo_accion", TiposAccion.Todos));
                t.HasCheckConstraint("ck_tareas_estado", EnLista("estado", EstadosTarea.Todos));
                t.HasCheckConstraint("ck_tareas_prioridad", EnLista("prioridad", Prioridades.Todos));
                t.HasCheckConstraint("ck_tareas_fechas", "fecha_vencimiento >= fecha_asignacion");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Accion).HasMaxLength(300).IsRequired();
            entity.Property(e => e.TipoAccion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Estado).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Prioridad).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Notas).HasMaxLength(4000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => new { e.ComercialId, e.Estado });
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Oportunidad>().WithMany().HasForeignKey(e => e.OportunidadId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MetaComercial>(entity =>
        {
            entity.ToTable("metas_comerciales", t =>
            {
                t.HasCheckConstraint("ck_metas_mes", "extract(day FROM mes) = 1");
                t.HasCheckConstraint("ck_metas_valor", "meta_cop >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MetaCop).HasPrecision(16, 2);
            entity.HasIndex(e => new { e.Mes, e.ComercialId }).IsUnique();
            entity.HasOne<Usuario>().WithMany().HasForeignKey(e => e.ComercialId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParametrosComerciales>(entity =>
        {
            entity.ToTable("parametros_comerciales", t => t.HasCheckConstraint("ck_parametros_fila_unica", "id = 1"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.MetaEmpresaCop).HasPrecision(16, 2);
            entity.Property(e => e.MetaComercialCop).HasPrecision(16, 2);
            entity.Property(e => e.PorcentajeComision).HasPrecision(5, 2);
            entity.Property(e => e.UmbralDoa).HasPrecision(5, 2);
        });
    }

    /// <summary>
    /// Una tabla por maestro, todas con la misma forma (<see cref="CatalogoSimple"/>). Al agregar un
    /// maestro: entidad, DbSet, una línea aquí, migración y entrada en <c>RegistroMaestros</c>.
    /// </summary>
    private static void ConfigurarMaestros(ModelBuilder modelBuilder)
    {
        ConfigurarCatalogo<TipoIdentificacion>(modelBuilder, "tipos_identificacion");
        ConfigurarCatalogo<Cargo>(modelBuilder, "cargos");
        ConfigurarCatalogo<Area>(modelBuilder, "areas");
        ConfigurarCatalogo<TipoContrato>(modelBuilder, "tipos_contrato");
        ConfigurarCatalogo<GrupoProducto>(modelBuilder, "grupos_producto");
        ConfigurarCatalogo<Cuenca>(modelBuilder, "cuencas");
    }

    /// <summary>
    /// Código (guardado en mayúsculas) y nombre únicos entre los no eliminados: el índice parcial
    /// permite volver a crear un valor que se borró lógicamente.
    /// </summary>
    private static void ConfigurarCatalogo<T>(ModelBuilder modelBuilder, string tabla) where T : CatalogoSimple
    {
        modelBuilder.Entity<T>(entity =>
        {
            entity.ToTable(tabla);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("eliminado_en IS NULL");
            entity.HasIndex(e => e.Nombre).IsUnique().HasFilter("eliminado_en IS NULL");
        });
    }

    private static void ConfigurarAdministracion(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empleado>(entity =>
        {
            entity.ToTable("empleados");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NumeroIdentificacion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Nombres).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Apellidos).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Genero).HasMaxLength(20);
            entity.Property(e => e.Telefono).HasMaxLength(30);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Direccion).HasMaxLength(250);
            entity.Property(e => e.Ciudad).HasMaxLength(100);
            entity.Property(e => e.Salario).HasPrecision(14, 2);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FotoClave).HasMaxLength(300);

            entity.HasIndex(e => new { e.TipoIdentificacionId, e.NumeroIdentificacion })
                .IsUnique().HasFilter("eliminado_en IS NULL");

            entity.HasOne<TipoIdentificacion>().WithMany().HasForeignKey(e => e.TipoIdentificacionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Cargo>().WithMany().HasForeignKey(e => e.CargoId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Area>().WithMany().HasForeignKey(e => e.AreaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TipoContrato>().WithMany().HasForeignKey(e => e.TipoContratoId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Catálogo de productos, centros de acopio, listas de precios y proveedores. Los precios son una
    /// matriz producto × lista (sin fila = sin precio); las tablas puente borran en cascada.
    /// </summary>
    private static void ConfigurarCatalogos(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CentroAcopio>(entity =>
        {
            entity.ToTable("centros_acopio");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasIndex(e => e.Nombre).IsUnique().HasFilter("eliminado_en IS NULL");
            entity.HasOne<Cuenca>().WithMany().HasForeignKey(e => e.CuencaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("productos", t =>
            {
                t.HasCheckConstraint("ck_productos_tipo", EnLista("tipo", TiposProducto.Todos));
                t.HasCheckConstraint("ck_productos_talla", "talla_min_cm IS NULL OR talla_max_cm IS NULL OR talla_min_cm <= talla_max_cm");
                t.HasCheckConstraint("ck_productos_bolsa", "bolsa_min IS NULL OR bolsa_max IS NULL OR bolsa_min <= bolsa_max");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Tipo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.NombreCientifico).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NombreComun).HasMaxLength(300);
            entity.Property(e => e.NombreEspanol).HasMaxLength(300);
            entity.Property(e => e.TallaMinCm).HasPrecision(6, 2);
            entity.Property(e => e.TallaMaxCm).HasPrecision(6, 2);
            entity.Property(e => e.TallaEtiqueta).HasMaxLength(30);
            entity.Property(e => e.Disponibilidad).HasMaxLength(50);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Notas).HasMaxLength(2000);
            entity.HasIndex(e => e.Codigo).IsUnique().HasFilter("eliminado_en IS NULL");
            entity.HasIndex(e => e.GrupoId);
            entity.HasOne<GrupoProducto>().WithMany().HasForeignKey(e => e.GrupoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductoCentroAcopio>(entity =>
        {
            entity.ToTable("producto_centros_acopio", t => t.HasCheckConstraint("ck_producto_centros_orden", "orden BETWEEN 1 AND 3"));
            entity.HasKey(e => new { e.ProductoId, e.CentroAcopioId });
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CentroAcopio>().WithMany().HasForeignKey(e => e.CentroAcopioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ListaPrecios>(entity =>
        {
            entity.ToTable("listas_precios", t =>
            {
                t.HasCheckConstraint("ck_listas_moneda", EnLista("moneda", Monedas.Todos));
                t.HasCheckConstraint("ck_listas_redondeo", "redondeo > 0");
                t.HasCheckConstraint("ck_listas_base", "lista_base_id IS NULL OR lista_base_id <> id");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Moneda).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.FactorSalvaje).HasPrecision(8, 4);
            entity.Property(e => e.FactorCriadero).HasPrecision(8, 4);
            entity.Property(e => e.FactorEspecial).HasPrecision(8, 4);
            entity.Property(e => e.Redondeo).HasPrecision(12, 2);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.HasOne<Cliente>().WithMany().HasForeignKey(e => e.ClienteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ListaPrecios>().WithMany().HasForeignKey(e => e.ListaBaseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PrecioProducto>(entity =>
        {
            entity.ToTable("precios_producto", t => t.HasCheckConstraint("ck_precios_positivo", "precio > 0"));
            entity.HasKey(e => new { e.ProductoId, e.ListaPreciosId });
            entity.Property(e => e.Precio).HasPrecision(14, 2);
            entity.HasOne<Producto>().WithMany().HasForeignKey(e => e.ProductoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ListaPrecios>().WithMany().HasForeignKey(e => e.ListaPreciosId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ParametrosPrecios>(entity =>
        {
            entity.ToTable("parametros_precios", t => t.HasCheckConstraint("ck_parametros_precios_fila_unica", "id = 1"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Iva).HasPrecision(5, 2);
            entity.Property(e => e.Pasarela).HasPrecision(5, 2);
            entity.Property(e => e.TrmReferencia).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("proveedores");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NumeroIdentificacion).HasMaxLength(30).IsRequired();
            entity.Property(e => e.Telefono).HasMaxLength(40);
            entity.Property(e => e.Contacto).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(320);
            entity.Property(e => e.Notas).HasMaxLength(2000);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasIndex(e => new { e.TipoIdentificacionId, e.NumeroIdentificacion }).IsUnique().HasFilter("eliminado_en IS NULL");
            entity.HasOne<TipoIdentificacion>().WithMany().HasForeignKey(e => e.TipoIdentificacionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProveedorCentroAcopio>(entity =>
        {
            entity.ToTable("proveedor_centros_acopio");
            entity.HasKey(e => new { e.ProveedorId, e.CentroAcopioId });
            entity.HasOne<Proveedor>().WithMany().HasForeignKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CentroAcopio>().WithMany().HasForeignKey(e => e.CentroAcopioId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarSeguridad(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.ToTable("perfiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Nombre).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
        });

        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.ToTable("permisos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Descripcion).HasMaxLength(250);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.IntentosFallidos).HasDefaultValue(0);
            // Restrict, no Cascade (el default de EF Core para FK requeridas): eliminar un perfil
            // jamás debe arrastrar a sus usuarios.
            entity.HasOne<Perfil>().WithMany().HasForeignKey(e => e.PerfilId).OnDelete(DeleteBehavior.Restrict);
        });

        // Tabla puente perfil <-> permiso, sin entidad de dominio propia.
        modelBuilder.Entity("PerfilPermiso", builder =>
        {
            builder.ToTable("perfil_permisos");
            builder.Property<Guid>("PerfilId");
            builder.Property<Guid>("PermisoId");
            builder.HasKey("PerfilId", "PermisoId");
            builder.HasOne(typeof(Perfil)).WithMany().HasForeignKey("PerfilId").OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(typeof(Permiso)).WithMany().HasForeignKey("PermisoId").OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurarAuditoria(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroAuditoria>(entity =>
        {
            entity.ToTable("auditoria");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Entidad).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Accion).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.CambiosJson).HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.UsuarioNombre).HasMaxLength(200);
            entity.HasIndex(e => new { e.Entidad, e.EntidadId });
        });
    }
}
