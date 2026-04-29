using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MayoreoKenneth.Infrastructure.Persistence;

public class MayoreoKennethDbContextFactory : IDesignTimeDbContextFactory<MayoreoKennethDbContext>
{
    public MayoreoKennethDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MayoreoKennethDbContext>();

        // Yurguen: Connection string temporal para generar migraciones al inicio del proyecto.
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=mayoreokennethdb;Username=postgres;Password=postgres;");

        return new MayoreoKennethDbContext(optionsBuilder.Options);
    }
}
