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
	public required Editor Editor { get; set; }
}
