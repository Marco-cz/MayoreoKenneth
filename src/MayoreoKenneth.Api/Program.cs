using MayoreoKenneth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MayoreoKenneth.Api.Features.Catalog;
using MayoreoKenneth.Api.Features.Checkout;
using MayoreoKenneth.Api.Features.Auth;
using MayoreoKenneth.Api.Features.Soporte;
using MayoreoKenneth.Infrastructure.CatalogSync;
using MayoreoKenneth.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Yurguen: Registramos OpenAPI para documentar endpoints.
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<InMemoryCatalogStore>();
builder.Services.AddSingleton<InMemoryCheckoutStore>();
builder.Services.AddSingleton<InMemoryAuthStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<InMemorySupportTicketsStore>();
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Yurguen_ClaveTemporal_MuyLarga_Para_MVP_2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MayoreoKenneth.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MayoreoKenneth.Web";
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalWeb", policy =>
    {
        // Yurguen: Habilitamos CORS local para que la web consuma el API.
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5500");
    });
});

// Yurguen: Registramos DbContext con PostgreSQL usando appsettings.
builder.Services.AddDbContext<MayoreoKennethDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=mayoreokennethdb;Username=postgres;Password=postgres;";
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

app.UseHttpsRedirection();
app.UseCors("LocalWeb");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
