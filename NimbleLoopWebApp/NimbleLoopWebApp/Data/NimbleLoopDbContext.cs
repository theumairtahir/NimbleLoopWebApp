using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using NimbleLoopWebApp.Domain;

namespace NimbleLoopWebApp.Data;

public class NimbleLoopDbContext(DbContextOptions<NimbleLoopDbContext> options) : DbContext(options)
{
	public DbSet<Article> Articles { get; set; } = null!;
	public DbSet<Editor> Editors { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<Article>( ).ToCollection("articles");
		modelBuilder.Entity<Editor>( ).ToCollection("editors");
	}
}
