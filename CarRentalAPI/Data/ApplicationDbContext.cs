using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Car> Cars { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Optional: Set relationships, constraints etc
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
        });
        modelBuilder.Entity<Car>(entity =>
        {
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.StartDate).HasColumnType("date");
            entity.Property(b => b.EndDate).HasColumnType("date");
            entity.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");
        });
    }

    public override int SaveChanges()
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
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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
        return await base.SaveChangesAsync(cancellationToken);
    }
}
