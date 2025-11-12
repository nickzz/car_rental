using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== USER CONFIGURATION =====
        modelBuilder.Entity<User>(entity =>
        {
            // DOB as date-only
            entity.Property(u => u.DOB)
                  .HasColumnType("date");

            // CreatedAt: default now()
            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("now()");

            // UpdatedAt: default now()
            entity.Property(u => u.UpdatedAt)
                  .HasDefaultValueSql("now()");

            // ✅ Indexes for performance
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email");

            entity.HasIndex(u => u.ICNumber)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_ICNumber");

            entity.HasIndex(u => u.Role)
                  .HasDatabaseName("IX_Users_Role");
        });

        // ===== CAR CONFIGURATION =====
        modelBuilder.Entity<Car>(entity =>
        {
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");

            // ✅ Indexes for performance
            entity.HasIndex(c => c.PlateNo)
                  .IsUnique()
                  .HasDatabaseName("IX_Cars_PlateNo");

            entity.HasIndex(c => new { c.Brand, c.Model })
                  .HasDatabaseName("IX_Cars_Brand_Model");
        });

        // ===== BOOKING CONFIGURATION =====
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.StartDate).HasColumnType("date");
            entity.Property(b => b.EndDate).HasColumnType("date");
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");

            // ✅ Indexes for performance - Critical for overlap checks!
            entity.HasIndex(b => new { b.CarId, b.StartDate, b.EndDate })
                  .HasDatabaseName("IX_Bookings_CarId_Dates");

            entity.HasIndex(b => b.UserId)
                  .HasDatabaseName("IX_Bookings_UserId");

            entity.HasIndex(b => b.Status)
                  .HasDatabaseName("IX_Bookings_Status");

            entity.HasIndex(b => b.CreatedAt)
                  .HasDatabaseName("IX_Bookings_CreatedAt");

            // Foreign key relationships
            entity.HasOne(b => b.Car)
                  .WithMany()
                  .HasForeignKey(b => b.CarId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion

            entity.HasOne(b => b.User)
                  .WithMany()
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== REFRESH TOKEN CONFIGURATION =====
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(rt => rt.CreatedAt).HasDefaultValueSql("now()");

            // ✅ Indexes for refresh token lookups
            entity.HasIndex(rt => rt.Token)
                  .IsUnique()
                  .HasDatabaseName("IX_RefreshTokens_Token");

            entity.HasIndex(rt => rt.UserId)
                  .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.HasIndex(rt => rt.ExpiresAt)
                  .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

            entity.HasOne(rt => rt.User)
                  .WithMany()
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is User || e.Entity is Car || e.Entity is Booking);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                ((dynamic)entry.Entity).CreatedAt = DateTime.UtcNow;
                ((dynamic)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                ((dynamic)entry.Entity).UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}