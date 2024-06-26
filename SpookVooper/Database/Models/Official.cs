using Microsoft.EntityFrameworkCore;

namespace SpookVooper.Database.Models;

public enum OfficialType
{
    Senator,
    Judge,
    Minister,
    Emperor
}

public class Official
{
    public long UserId { get; set; }
    public int DistrictId { get; set; }
    public OfficialType Type { get; set; }
    public DateTime ElectedTime { get; set; }
    
    public static void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Official>(e =>
        {
            e.ToTable("officials");

            e.Property(x => x.UserId)
                .HasColumnName("user_id");
            
            e.Property(x => x.DistrictId)
                .HasColumnName("district_id");
            
            e.Property(x => x.Type)
                .HasColumnName("type");
            
            e.Property(x => x.ElectedTime)
                .HasColumnName("elected_time");
            
            e.HasKey(x => new { x.UserId, x.DistrictId });
        });
    }
}