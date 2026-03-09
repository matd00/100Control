using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Context;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PaintballManagerDbContext>
{
    public PaintballManagerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaintballManagerDbContext>();
        optionsBuilder.UseSqlite("Data Source=paintball_manager.db");

        return new PaintballManagerDbContext(optionsBuilder.Options);
    }
}
