namespace NimbleLoopWebApp.Domain;

public class Editor : BaseEntity
{
	public required string Name { get; set; }
	public required string Email { get; set; }
	public string? Bio { get; set; }
	public string? ImageUrl { get; set; }
	public string? ImageAltText { get; set; }
	public List<string> Tags { get; set; } = [ ];
}
