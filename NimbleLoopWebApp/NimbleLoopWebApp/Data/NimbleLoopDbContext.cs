using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MongoDB.Bson;
using MongoDB.Driver;
using NimbleLoop.Domain.Entities;
using System.Security.Claims;

namespace NimbleLoopWebApp.Data;

public class NimbleLoopDbContext(DbContextOptions<NimbleLoopDbContext> options, IMongoDatabase mongoDatabase) : DbContext(options)
{
	private const string SYSTEM_USER = "System";
	private readonly IMongoDatabase _mongoDatabase = mongoDatabase;

	public DbSet<Article> Articles { get; set; } = null!;
	public DbSet<Editor> Editors { get; set; } = null!;
	public DbSet<Prospect> Prospects { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		var articleCollection = modelBuilder.Entity<Article>( ).ToCollection( );
		articleCollection.HasKey(x => x.Id);
		articleCollection.Ignore(x => x.LastModified);
		articleCollection.Property(x => x.Id).HasConversion<ObjectId>( ).HasValueGenerator<BsonIdValueGenerator>( ).ValueGeneratedOnAdd( );
		articleCollection.Property(x => x.EditorId).HasConversion<ObjectId>( );
		articleCollection.HasOne(x => x.Editor).WithMany( ).HasForeignKey(x => x.EditorId).HasPrincipalKey(x => x.Id);
		articleCollection.HasIndex(x => x.Key).IsUnique( );
		var editorsCollection = modelBuilder.Entity<Editor>( ).ToCollection( );
		editorsCollection.HasKey(x => x.Id);
		editorsCollection.Ignore(x => x.LastModified);
		editorsCollection.Property(x => x.Id).HasConversion<ObjectId>( ).HasValueGenerator<BsonIdValueGenerator>( ).ValueGeneratedOnAdd( );
		var prospectsCollection = modelBuilder.Entity<Prospect>( ).ToCollection( );
		prospectsCollection.HasKey(x => x.Id);
		prospectsCollection.Ignore(x => x.LastModified);
		prospectsCollection.Property(x => x.Id).HasConversion<ObjectId>( ).HasValueGenerator<BsonIdValueGenerator>( ).ValueGeneratedOnAdd( );
		prospectsCollection.HasIndex(x => x.Email).IsUnique( );
	}

	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		foreach (var entry in ChangeTracker.Entries( ))
		{
			if (entry.State is not EntityState.Modified && entry.State is not EntityState.Added)
				continue;

			if (entry.Entity is not BaseEntity entity)
				continue;

			var exists = await EntityExistsInDatabaseAsync(entity, entry.Entity.GetType( ), cancellationToken);
			if (exists)
			{
				entry.State = EntityState.Modified;
				entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
				entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
				entity.ModifiedAt = DateTime.UtcNow;
				entity.ModifiedBy = string.IsNullOrEmpty(entity.ModifiedBy) ? SYSTEM_USER : entity.ModifiedBy;
			}
			else
			{
				entry.State = EntityState.Added;
				if (string.IsNullOrEmpty(entity.Id))
					entity.Id = ObjectId.GenerateNewId( ).ToString( );
				entity.CreatedAt = DateTime.UtcNow;
				entity.CreatedBy = string.IsNullOrEmpty(entity.CreatedBy) ? SYSTEM_USER : entity.CreatedBy;
			}
		}
		return await base.SaveChangesAsync(cancellationToken);
	}

	public async Task<int> SaveChangesAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
	{
		foreach (var entry in ChangeTracker.Entries( ))
		{
			if (entry.Entity is BaseEntity entity)
			{
				switch (entry.State)
				{
					case EntityState.Added:
						entity.CreatedBy = user is { Identity.IsAuthenticated: true } ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value! : SYSTEM_USER;
						continue;
					case EntityState.Modified:
						entity.ModifiedBy = user is { Identity.IsAuthenticated: true } ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value! : SYSTEM_USER;
						continue;
					default:
						continue;
				}
			}
		}
		return await SaveChangesAsync(cancellationToken);
	}

	private async Task<bool> EntityExistsInDatabaseAsync(BaseEntity entity, Type entityType, CancellationToken cancellationToken)
	{
		var collectionName = entityType.GetCollectionName( );
		var collection = _mongoDatabase.GetCollection<BaseEntity>(collectionName);
		if (string.IsNullOrEmpty(entity.Id))
			return false;
		var filter = Builders<BaseEntity>.Filter.Eq("_id", ObjectId.Parse(entity.Id));
		var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
		return count > 0;
	}
}
