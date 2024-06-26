using Microsoft.EntityFrameworkCore;

namespace SpookVooper.Database.Models;

public class District
{
    public virtual List<SvUser> Residents { get; set; }
    
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<District>(e =>
        {
            e.ToTable("districts");

            e.Property(x => x.Id)
                .HasColumnName("id");
            
            e.Property(x => x.Name)
                .HasColumnName("name");
            
            e.Property(x => x.Description)
                .HasColumnName("description");
            
            e.HasKey(x => x.Id);
        });
    }
}