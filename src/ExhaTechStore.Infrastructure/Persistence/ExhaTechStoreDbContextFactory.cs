using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExhaTechStore.Infrastructure.Persistence;

public class ExhaTechStoreDbContextFactory : IDesignTimeDbContextFactory<ExhaTechStoreDbContext>
{
    public ExhaTechStoreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExhaTechStoreDbContext>();

        // Yurguen: Connection string temporal para generar migraciones al inicio del proyecto.
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=exhatechstoredb;Username=postgres;Password=postgres;");

        return new ExhaTechStoreDbContext(optionsBuilder.Options);
    }
}
