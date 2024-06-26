using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SpookVooper.Config;
using SpookVooper.Database.Models;

namespace SpookVooper.Database;

public class SvDb : DbContext
{
    public static readonly string ConnectionString = $"Host={DbConfig.Instance.Host};Database={DbConfig.Instance.Database};Username={DbConfig.Instance.Username};Password={DbConfig.Instance.Password};SslMode=Prefer;";
    
    public DbSet<District> Districts { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.ConfigureWarnings(w => w.Ignore(RelationalEventId.ForeignKeyPropertiesMappedToUnrelatedTables));
        options.UseNpgsql(ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        SvUser.OnModelCreating(modelBuilder);
        Official.OnModelCreating(modelBuilder);
        District.OnModelCreating(modelBuilder);
    }
}