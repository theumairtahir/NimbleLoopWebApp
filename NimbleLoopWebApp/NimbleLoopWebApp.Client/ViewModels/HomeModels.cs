using System.ComponentModel.DataAnnotations;

namespace NimbleLoopWebApp.Client.ViewModels;

public class HomeContactViewModel
{
	[Required]
	public string Name { get; set; } = string.Empty;

	[Required, EmailAddress]
	public string Email { get; set; } = string.Empty;

	public string? CompanyName { get; set; }

	[Required]
	public string ServiceInterestedIn { get; set; } = string.Empty;

	public string? Budget { get; set; }

	public string? Message { get; set; }
}

public record HomeServiceViewModel(string Title, string Description);
