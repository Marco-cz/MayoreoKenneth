using MayoreoKenneth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Yurguen: Registramos OpenAPI para documentar endpoints.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Yurguen: Registramos DbContext con PostgreSQL usando appsettings.
builder.Services.AddDbContext<MayoreoKennethDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=localhost;Port=5432;Database=mayoreokennethdb;Username=postgres;Password=postgres;";
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Yurguen: Exponemos spec OpenAPI en ambiente de desarrollo.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
