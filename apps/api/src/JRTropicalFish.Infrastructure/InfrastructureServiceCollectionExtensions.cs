using Amazon.Runtime;
using Amazon.S3;
using JRTropicalFish.Application.Abstractions;
using JRTropicalFish.Application.Administracion;
using JRTropicalFish.Application.Maestros;
using JRTropicalFish.Application.Auditoria;
using JRTropicalFish.Application.Auth;
using JRTropicalFish.Application.Catalogos;
using JRTropicalFish.Application.Crm;
using JRTropicalFish.Application.Exportaciones;
using JRTropicalFish.Application.Sanidad;
using JRTropicalFish.Application.Inventario;
using JRTropicalFish.Application.Informes;
using JRTropicalFish.Application.Seguridad;
using JRTropicalFish.Infrastructure.Almacenamiento;
using JRTropicalFish.Infrastructure.Auth;
using JRTropicalFish.Infrastructure.Comun;
using JRTropicalFish.Infrastructure.Correo;
using JRTropicalFish.Infrastructure.Informes;
using JRTropicalFish.Infrastructure.Persistence;
using JRTropicalFish.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JRTropicalFish.Infrastructure;

/// <summary>
/// Punto de entrada único de la inyección de dependencias. Cada módulo nuevo agrega aquí sus
/// repositorios (interfaz → implementación Dapper) y sus servicios de Application (clase concreta,
/// scoped). Sin MediatR ni AutoMapper: los servicios se inyectan directo en los controladores.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Las funciones PL/pgSQL devuelven columnas en snake_case (ej. password_hash); esto las
        // mapea a las propiedades PascalCase de C#.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        Dapper.SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());

        services.Configure<PostgresOptions>(configuration.GetSection(PostgresOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AlmacenamientoOptions>(configuration.GetSection(AlmacenamientoOptions.SectionName));
        services.Configure<CorreoOptions>(configuration.GetSection(CorreoOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IReloj, RelojSistema>();

        // Persistencia: Dapper en tiempo de ejecución, EF Core solo para migraciones.
        services.AddSingleton<PostgresConnectionFactory>();
        services.AddDbContext<AppDbContext>((proveedor, opciones) => opciones
            .UseNpgsql(proveedor.GetRequiredService<PostgresConnectionFactory>().ConnectionString)
            .UseSnakeCaseNamingConvention());
        services.AddScoped<MigradorBaseDatos>();

        // Seguridad
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IUsuarioActual, UsuarioActual>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        services.AddScoped<IPermisoRepository, PermisoRepository>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<PerfilService>();

        // Transversales
        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
        services.AddScoped<ServicioAuditoria>();
        services.AddSingleton<IGeneradorInformePdf, GeneradorInformePdfQuestPdf>();
        services.AddSingleton<IGeneradorInformeAunapExcel, GeneradorInformeAunapExcel>();
        RegistrarAlmacenArchivos(services, configuration);
        RegistrarCorreo(services, configuration);

        // Módulos de negocio: registrar aquí repositorios y servicios de cada módulo.

        // Administración: maestros y empleados
        services.AddScoped<IMaestroRepository, MaestroRepository>();
        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<MaestroService>();
        services.AddScoped<EmpleadoService>();

        // CRM comercial
        services.AddScoped<IConsecutivoRepository, ConsecutivoRepository>();
        services.AddScoped<IComercialRepository, ComercialRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IOportunidadRepository, OportunidadRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<ITrackingRepository, TrackingRepository>();
        services.AddScoped<ITareaRepository, TareaRepository>();
        services.AddScoped<IMetricasRepository, MetricasRepository>();
        services.AddScoped<ClienteService>();
        services.AddScoped<OportunidadService>();
        services.AddScoped<VentaService>();
        services.AddScoped<TrackingService>();
        services.AddScoped<TareaService>();
        services.AddScoped<MetricasService>();

        // Catálogos: productos, precios, centros de acopio y proveedores
        services.AddScoped<ICentroAcopioRepository, CentroAcopioRepository>();
        services.AddScoped<IProductoRepository, ProductoRepository>();
        services.AddScoped<IPrecioRepository, PrecioRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<CentroAcopioService>();
        services.AddScoped<ProductoService>();
        services.AddScoped<PrecioService>();
        services.AddScoped<ProveedorService>();

        // Inventario, compras y pedidos de cliente
        services.AddScoped<IInventarioRepository, InventarioRepository>();
        services.AddScoped<ITablerosInventarioRepository, TablerosInventarioRepository>();
        services.AddScoped<IComprasRepository, ComprasRepository>();
        services.AddScoped<IPedidoClienteRepository, PedidoClienteRepository>();
        services.AddScoped<InventarioService>();
        services.AddScoped<TablerosInventarioService>();
        services.AddScoped<IConfiguracionExportacionRepository, ConfiguracionExportacionRepository>();
        services.AddScoped<IEmbarqueRepository, EmbarqueRepository>();
        services.AddScoped<IAunapRepository, AunapRepository>();
        services.AddScoped<AunapService>();
        services.AddScoped<ConfiguracionExportacionService>();
        services.AddScoped<EmbarqueService>();
        services.AddScoped<ComprasService>();
        services.AddScoped<PedidoClienteService>();

        // Sanidad: medicación, cuarentena, alimentación y reservas de despacho
        services.AddScoped<ISanidadMaestrosRepository, SanidadMaestrosRepository>();
        services.AddScoped<ISanidadRepository, SanidadRepository>();
        services.AddScoped<SanidadMaestrosService>();
        services.AddScoped<SanidadService>();

        return services;
    }

    /// <summary>
    /// Correo con proveedor elegido por configuración (<c>Correo:Proveedor</c>).
    ///
    /// <para>
    /// ⚠️ El proveedor <b>por defecto escribe a disco y no envía nada</b>. Si
    /// <c>Correo:RedirigirTodoA</c> tiene valor, el decorador <see cref="CorreoModoPruebas"/> envuelve
    /// al proveedor y nadie real recibe correo.
    /// </para>
    /// </summary>
    private static void RegistrarCorreo(IServiceCollection services, IConfiguration configuration)
    {
        var opciones = configuration.GetSection(CorreoOptions.SectionName).Get<CorreoOptions>() ?? new CorreoOptions();

        services.AddHttpClient(nameof(CorreoSendGrid), c => c.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos));
        services.AddScoped<CorreoDisco>();
        services.AddScoped<CorreoSendGrid>();

        services.AddScoped<IServicioCorreo>(proveedor =>
        {
            IServicioCorreo interno = opciones.Proveedor.Equals("SendGrid", StringComparison.OrdinalIgnoreCase)
                ? proveedor.GetRequiredService<CorreoSendGrid>()
                : proveedor.GetRequiredService<CorreoDisco>();

            if (string.IsNullOrWhiteSpace(opciones.RedirigirTodoA))
            {
                return interno;
            }

            var entorno = proveedor.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().EnvironmentName;
            return new CorreoModoPruebas(interno, opciones.RedirigirTodoA, entorno, opciones.MantenerPlantillaEnPruebas);
        });
    }

    /// <summary>
    /// Cliente S3. Con <c>ForcePathStyle</c> habla igual con Garage en desarrollo que con S3 o R2 en
    /// producción. Si se dejan vacías las credenciales, el SDK usa las del entorno.
    /// </summary>
    private static void RegistrarAlmacenArchivos(IServiceCollection services, IConfiguration configuration)
    {
        var opciones = configuration.GetSection(AlmacenamientoOptions.SectionName).Get<AlmacenamientoOptions>()
            ?? new AlmacenamientoOptions();

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ForcePathStyle = true,
                AuthenticationRegion = opciones.Region,
            };

            if (!string.IsNullOrWhiteSpace(opciones.Endpoint))
            {
                config.ServiceURL = opciones.Endpoint;
            }
            else
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(opciones.Region);
            }

            return string.IsNullOrWhiteSpace(opciones.AccessKey)
                ? new AmazonS3Client(config)
                : new AmazonS3Client(new BasicAWSCredentials(opciones.AccessKey, opciones.SecretKey), config);
        });

        services.AddScoped<IAlmacenArchivos, AlmacenArchivosS3>();
    }
}
