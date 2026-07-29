using InfotecsTest.Domain;
using Microsoft.EntityFrameworkCore;

namespace InfotecsTest.DBInfrastructure;

public class InfotecsTestDBContext: DbContext
{
    public InfotecsTestDBContext(DbContextOptions<InfotecsTestDBContext> options) : base(options)
    {
    }
    public DbSet<ValueData> Values { get; set; } = null!;
    public DbSet<Result> Results { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ValueData>()
            .HasIndex(v => new { v.Name, v.Date });
    }
}
