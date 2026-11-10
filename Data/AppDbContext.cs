using BuildWorkflowAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildWorkflowAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<ApiConnection> Connections => Set<ApiConnection>();
    public DbSet<ApiDefinition> ApiDefinitions => Set<ApiDefinition>();
    public DbSet<ApiParameter> ApiParameters => Set<ApiParameter>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ApiCallLog> ApiCallLogs => Set<ApiCallLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ApiDefinition>().HasOne(x => x.Connection).WithMany(c => c.ApiDefinitions).HasForeignKey(x => x.ConnectionId);
        b.Entity<ApiParameter>().HasOne(x => x.ApiDefinition).WithMany(d => d.Parameters).HasForeignKey(x => x.ApiDefinitionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ApiKey>().HasOne(x => x.ApiDefinition).WithMany(d => d.ApiKeys).HasForeignKey(x => x.ApiDefinitionId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ApiDefinition>().HasIndex(x => x.Route).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        b.Entity<ApiCallLog>().HasIndex(x => x.Timestamp);
        b.Entity<ApiCallLog>().HasIndex(x => x.Route);
        b.Entity<ApiCallLog>().HasOne(x => x.ApiDefinition).WithMany().HasForeignKey(x => x.ApiDefinitionId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<ApiCallLog>().HasOne(x => x.ApiKey).WithMany().HasForeignKey(x => x.ApiKeyId).OnDelete(DeleteBehavior.SetNull);
    }
}