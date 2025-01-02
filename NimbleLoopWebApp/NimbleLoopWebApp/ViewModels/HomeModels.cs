using System.ComponentModel.DataAnnotations;

namespace NimbleLoopWebApp.ViewModels;

public record HomeArticleViewModel(string Key, string Title, string Summary, string ImageUrl);

