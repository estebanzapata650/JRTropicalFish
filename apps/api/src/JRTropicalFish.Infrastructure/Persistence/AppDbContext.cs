using JRTropicalFish.Domain.Administracion;
using JRTropicalFish.Domain.Auditoria;
using JRTropicalFish.Domain.Comun;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarSeguridad(modelBuilder);
        ConfigurarAuditoria(modelBuilder);
        ConfigurarMaestros(modelBuilder);
        ConfigurarAdministracion(modelBuilder);
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
