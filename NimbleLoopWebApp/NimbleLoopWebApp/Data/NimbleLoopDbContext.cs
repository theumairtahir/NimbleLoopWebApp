using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore.Extensions;
using NimbleLoop.Domain.Entities;
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
		var articleCollection = modelBuilder.Entity<Article>( ).ToCollection("articles");
		articleCollection.HasKey(x => x.Id);
		articleCollection.Ignore(x => x.LastModified);
		articleCollection.Property(x => x.Id).HasConversion<ObjectId>( );
		articleCollection.HasIndex(x => x.Key).IsUnique( );
		var editorsCollection = modelBuilder.Entity<Editor>( ).ToCollection("editors");
		editorsCollection.HasKey(x => x.Id);
		editorsCollection.Ignore(x => x.LastModified);
		editorsCollection.Property(x => x.Id).HasConversion<ObjectId>( );
		var prospectsCollection = modelBuilder.Entity<Prospect>( ).ToCollection("prospects");
		prospectsCollection.HasKey(x => x.Id);
		prospectsCollection.Ignore(x => x.LastModified);
		prospectsCollection.Property(x => x.Id).HasConversion<ObjectId>( );
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		base.ChangeTracker.Entries( ).Where(e => e.State == EntityState.Added).ToList( ).ForEach(e =>
		{
			if (e.Entity is BaseEntity entity)
			{
				entity.Id = ObjectId.GenerateNewId( ).ToString( );
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
