using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using NimbleLoopWebApp.Domain;
using System.Security.Claims;

namespace NimbleLoopWebApp.Data;

public class NimbleLoopDbContext(DbContextOptions<NimbleLoopDbContext> options) : DbContext(options)
{
	private const string SYSTEM_USER = "System";
	public DbSet<Article> Articles { get; set; } = null!;
	public DbSet<Editor> Editors { get; set; } = null!;
	public DbSet<Prospect> Prospects { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<Article>( ).ToCollection("articles");
		modelBuilder.Entity<Editor>( ).ToCollection("editors");
		modelBuilder.Entity<Prospect>( ).ToCollection("prospects");
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		base.ChangeTracker.Entries( ).Where(e => e.State == EntityState.Added).ToList( ).ForEach(e =>
		{
			if (e.Entity is BaseEntity entity)
			{
				entity.Id = ObjectId.GenerateNewId( );
				entity.CreatedAt = DateTime.UtcNow;
				entity.CreatedBy = string.IsNullOrEmpty(entity.CreatedBy) ? SYSTEM_USER : entity.CreatedBy;
			}
		});

		ChangeTracker.Entries( ).Where(e => e.State == EntityState.Modified).ToList( ).ForEach(e =>
		{
			if (e.Entity is BaseEntity entity)
			{
				entity.ModifiedAt = DateTime.UtcNow;
				entity.ModifiedBy = string.IsNullOrEmpty(entity.ModifiedBy) ? SYSTEM_USER : entity.ModifiedBy;
			}
		});

		return await base.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> SaveChangesAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		base.ChangeTracker.Entries( ).Where(e => e.State == EntityState.Added).ToList( ).ForEach(e =>
		{
			if (e.Entity is BaseEntity entity)
				entity.CreatedBy = user is { Identity.IsAuthenticated: true } ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value! : SYSTEM_USER;
		});
		ChangeTracker.Entries( ).Where(e => e.State == EntityState.Modified).ToList( ).ForEach(e =>
		{
			if (e.Entity is BaseEntity entity)
				entity.ModifiedBy = user is { Identity.IsAuthenticated: true } ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value! : SYSTEM_USER;
		});
		return await SaveChangesAsync(cancellationToken);
	}
}
