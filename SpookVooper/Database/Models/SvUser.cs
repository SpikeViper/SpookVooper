using Microsoft.EntityFrameworkCore;

namespace SpookVooper.Database.Models;

public class SvUser
{
    public virtual District District { get; set; }
    
    public long Id { get; set; }
    
    public long Xp { get; set; }
    
    public int DistrictId { get; set; }
    
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SvUser>(e =>
        {
            e.ToTable("users");

            e.Property(x => x.Id)
                .HasColumnName("id");
            
            e.Property(x => x.Xp)
                .HasColumnName("xp");
            
            e.Property(x => x.DistrictId)
                .HasColumnName("district_id");

            e.HasOne(x => x.District)
                .WithMany(x => x.Residents)
                .HasForeignKey(x => x.DistrictId);
            
            e.HasKey(x => x.Id);
        });
    }
}