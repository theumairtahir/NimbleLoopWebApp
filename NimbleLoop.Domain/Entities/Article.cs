namespace NimbleLoop.Domain.Entities;

public class Article : BaseEntity
{
	public required string Title { get; set; }
	public required string ContentMarkdown { get; set; } = null!;
	public required string ImageUrl { get; set; }
	public required string Key { get; set; }
	public bool IsFeatured { get; set; }
	public string? ImageAltText { get; set; }
	public List<string> Tags { get; set; } = [ ];
	public string? MetaTitle { get; set; }
	public string? MetaDescription { get; set; }
	public string EditorId { get; set; } = null!;
	public required Editor Editor { get; set; }
	public List<ArticleComment> Comments { get; set; } = [ ];
}

public class ArticleComment
{
	public required string CommentedBy { get; set; }
	public required string Comment { get; set; }
	public required DateTime CommentedAt { get; set; }
	public string ProspectId { get; set; } = null!;
}
