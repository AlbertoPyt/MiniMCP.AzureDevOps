namespace MCP.AzureDevOps.Infrastructure.Persistence;

public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountEntity>(b =>
        {
            b.ToTable("Accounts");

            b.HasKey(e => e.Id);

            b.Property(e => e.Id)
                .HasMaxLength(100)
                .IsRequired();

            b.Property(e => e.EncryptedPat)
                .IsRequired();

            b.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            b.Property(e => e.TargetUrl)
                .HasMaxLength(500);

            b.Property(e => e.CreatedAt)
                .IsRequired();

            b.Property(e => e.UpdatedAt)
                .IsRequired();
        });
    }
}
