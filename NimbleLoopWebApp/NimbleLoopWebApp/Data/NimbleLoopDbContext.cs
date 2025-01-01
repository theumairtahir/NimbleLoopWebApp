using Microsoft.EntityFrameworkCore;
using NimbleLoopWebApp.Domain;

namespace NimbleLoopWebApp.Data;

public class NimbleLoopDbContext(DbContextOptions<NimbleLoopDbContext> options) : DbContext(options)
{
	public DbSet<Article> Articles { get; set; } = null!;
	public DbSet<Editor> Editors { get; set; } = null!;
}
