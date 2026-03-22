using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BibleReader.Api.Data;

/// <summary>Usado por <c>dotnet ef</c> (design-time).</summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var conn = Environment.GetEnvironmentVariable("BIBLIA_EF_CONNECTION")
            ?? "Server=localhost;Database=BibleReader;Trusted_Connection=True;TrustServerCertificate=True";
        optionsBuilder.UseSqlServer(conn);
        return new AppDbContext(optionsBuilder.Options);
    }
}
