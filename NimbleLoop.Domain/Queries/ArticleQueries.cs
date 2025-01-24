using NimbleLoop.Domain.Entities;

namespace NimbleLoop.Domain.Queries;
public static class ArticleQueries
{
	public static IQueryable<Article> GetArticles(this IQueryable<Article> articles, int? top = null)
	{
		var queryableArticles = articles.OrderByDescending(x => x.CreatedAt).AsQueryable( );
		if (top != null)
			queryableArticles = queryableArticles.Take(top.Value);
		return queryableArticles;
	}
	public static IQueryable<Article> GetFeaturedArticles(this IQueryable<Article> articles, int? top = null)
	{
		var featuredArticles = articles.GetArticles( ).Where(x => x.IsFeatured);
		if (top != null)
			featuredArticles = featuredArticles.Take(top.Value);
		return featuredArticles;
	}
}
