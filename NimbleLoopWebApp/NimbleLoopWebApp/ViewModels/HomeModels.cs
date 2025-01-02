using System.ComponentModel.DataAnnotations;

namespace NimbleLoopWebApp.ViewModels;

public record HomeArticleViewModel(string Id, string Title, string Summary, string ImageUrl);

