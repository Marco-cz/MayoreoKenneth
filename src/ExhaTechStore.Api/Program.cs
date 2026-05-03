using ExhaTechStore.Api.Features.Auth;
using ExhaTechStore.Api.Features.Catalog;
using ExhaTechStore.Api.Features.Platform;
using ExhaTechStore.Api.Features.Checkout;
using ExhaTechStore.Api.Features.Soporte;
using ExhaTechStore.Api.Tenancy;
using ExhaTechStore.Api.Workers;
using ExhaTechStore.Infrastructure.CatalogSync;
using ExhaTechStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Yurguen: Registramos OpenAPI para documentar endpoints.
builder.Services.AddOpenApi();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.AddScoped<TenantSlugResolutionFilter>();
builder.Services.AddControllers();
// Yurguen: JSON más liviano por la red (tiendas ágiles).
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddResponseCaching();
builder.Services.AddSingleton<InMemoryCatalogStore>();
builder.Services.AddSingleton<InMemoryCheckoutStore>();
builder.Services.AddSingleton<InMemoryAuthStore>();
builder.Services.AddSingleton<InMemoryPlatformAuthStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<InMemorySupportTicketsStore>();
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Yurguen_ClaveTemporal_MuyLarga_Para_MVP_2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ExhaTechStore.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ExhaTechStore.Web";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// Yurguen: Orígenes del front (local + GitHub Pages). En prod: Cors__AllowedOrigins__0 = https://<user>.github.io
var corsOrigenes = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is { Length: > 0 } arr
    ? arr
    :
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173",
        "http://localhost:5500",
        "https://localhost:7253"
    ];
builder.Services.AddCors(options =>
{
    options.AddPolicy("TiendaWeb", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().WithOrigins(corsOrigenes);
    });
});

// Yurguen: Registramos DbContext con PostgreSQL usando appsettings.
builder.Services.AddDbContext<ExhaTechStoreDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=exhatechstoredb;Username=postgres;Password=postgres;";
    options.UseNpgsql(connectionString);
});

// Yurguen: Misma implementación simulada sirve catálogo masivo y consulta de stock por producto.
builder.Services.AddScoped<SimulatedExternalCatalogSource>();
builder.Services.AddScoped<IExternalCatalogSource>(sp => sp.GetRequiredService<SimulatedExternalCatalogSource>());
builder.Services.AddScoped<IExternalLiveStockSource>(sp => sp.GetRequiredService<SimulatedExternalCatalogSource>());
builder.Services.AddScoped<CatalogSynchronizer>();
builder.Services.AddHostedService<CatalogSyncHostedService>();
builder.Services.AddHostedService<DataRetentionHostedService>();

var app = builder.Build();

// Yurguen: Exponemos spec OpenAPI en ambiente de desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Yurguen: Detrás del TLS del PaaS conviene no forzar redirect HTTPS interno (rompe health checks).
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("TiendaWeb");
app.UseResponseCompression();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
