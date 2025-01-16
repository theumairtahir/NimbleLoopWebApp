using NimbleLoop.Domain.Entities;

namespace NimbleLoopWebApp.Client.Extensions;

public static class DomainExtensions
{
	public static string GetArticleTitle(this Article article) => string.IsNullOrEmpty(article.MetaTitle) ? article.MetaTitle! : article.Title;

	public static string GetArticleImageAlt(this Article article) => string.IsNullOrEmpty(article.ImageAltText) ? article.ImageAltText! : article.Title;
}
