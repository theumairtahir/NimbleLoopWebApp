using System.ComponentModel.DataAnnotations;

namespace NimbleLoopWebApp.Client.ViewModels;

public class ArticleViewModel
{
	public string? Id { get; set; }

	[Required]
	public string Title { get; set; } = string.Empty;

	[Required]
	public string Content { get; set; } = string.Empty;

	[Required]
	[Url]
	public string ImageUrl { get; set; } = string.Empty;

	[Required]
	public string Key { get; set; } = string.Empty;

	public bool IsFeatured { get; set; }

	public string? ImageAltText { get; set; }

	public string? MetaTitle { get; set; }

	public string? MetaDescription { get; set; }
	public string? MetaKeywords { get; set; }

	// Editor Details
	[Required(ErrorMessage = "Editor Name is required.")]
	public string EditorName { get; set; } = string.Empty;

	[Required(ErrorMessage = "Editor Email is required.")]
	[EmailAddress(ErrorMessage = "Invalid Email Address.")]
	public string EditorEmail { get; set; } = string.Empty;
	public string? EditorBio { get; set; } = string.Empty;
	public string? EditorImageUrl { get; set; } = string.Empty;
	public string? EditorImageAltText { get; set; } = string.Empty;
	public string EditorId { get; set; } = string.Empty;
}
