using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JRTropicalFish.Api.Auth;
using JRTropicalFish.Api.Startup;
using JRTropicalFish.Infrastructure;
using JRTropicalFish.Infrastructure.Auth;
using JRTropicalFish.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

// Licencia Community de QuestPDF: gratuita para empresas por debajo de USD 1M de ingreso bruto
// anual. Debe declararse antes de generar el primer documento.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey debe tener al menos 32 bytes (HMAC-SHA256).");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sin esto el handler renombra "sub" y otros claims a los URIs largos de WS-Fed.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Seguro por defecto: todo endpoint exige sesión salvo los marcados [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
// Genera al vuelo las políticas "permiso:modulo:accion" que usa [RequierePermiso].
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<PostgresConnectionFactory>().ConnectionString, name: "postgres");

var app = builder.Build();

// `dotnet JRTropicalFish.Api.dll --migrar`: aplica migraciones y sale. Es el paso explícito de
// despliegue en producción (npm run prod:migrar).
if (args.Contains("--migrar"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MigradorBaseDatos>().AplicarAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous(); // UI interactiva en /scalar/v1

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<MigradorBaseDatos>().AplicarAsync();

    var seed = app.Configuration.GetSection(SeedAdminOptions.SectionName).Get<SeedAdminOptions>();
    if (seed is not null)
    {
        await AdminSeeder.SeedAsync(scope.ServiceProvider, seed);
    }
}

// Toda excepción no controlada sale como JSON { error }, la misma forma que devuelven los
// controladores en los 4xx, para que el web muestre el mensaje en vez de un "Error 500" mudo. En
// Development va el mensaje real; en producción uno genérico (el detalle queda en el log).
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var excepcion = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var mensaje = app.Environment.IsDevelopment() && excepcion is not null
        ? excepcion.Message
        : "Ocurrió un error inesperado. Intenta de nuevo o contacta al administrador.";

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = mensaje });
}));

// En producción la API no se expone: solo la alcanza el web por la red interna de Docker, y el TLS
// lo termina Caddy. Por eso no hay UseHttpsRedirection.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
{
    ResponseWriter = async (context, reporte) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = reporte.Status.ToString(),
            checks = reporte.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        }));
    },
}).AllowAnonymous();

app.Run();
