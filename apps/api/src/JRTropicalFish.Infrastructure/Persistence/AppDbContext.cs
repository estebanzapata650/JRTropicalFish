using JRTropicalFish.Domain.Auditoria;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarSeguridad(modelBuilder);
        ConfigurarAuditoria(modelBuilder);
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
