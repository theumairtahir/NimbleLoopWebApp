using Humanizer;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace NimbleLoopWebApp.Data;

public static class DbContextExtensions
{
	public static EntityTypeBuilder<T> ToCollection<T>(this EntityTypeBuilder<T> builder) where T : class => builder.ToCollection(typeof(T).GetCollectionName( ));
	public static string GetCollectionName(this Type entityType) => entityType.Name.ToLower( ).Pluralize( );
}
