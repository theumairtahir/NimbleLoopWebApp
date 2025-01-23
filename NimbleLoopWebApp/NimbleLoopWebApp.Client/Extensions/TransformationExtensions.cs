using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NimbleLoopWebApp.Client.Extensions;

public static class TransformationExtensions
{
	public static string ToSlug(this string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		value = value.ToLowerInvariant( ).Trim( );
		var bytes = Encoding.UTF8.GetBytes(value);
		value = Encoding.ASCII.GetString(bytes);
		value = Regex.Replace(value, @"[^a-z0-9\s-]", "");
		value = Regex.Replace(value, @"\s+", " ").Trim( );
		value = value.Substring(0, value.Length <= 70 ? value.Length : 70).Trim( );
		value = Regex.Replace(value, @"\s", "-");
		return value;
	}

	public static string ToTitleCase(this string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant( ));
	}

	public static string ImageWithSize(this string? imageUrl, ImageSizes? size = null)
	{
		if (string.IsNullOrEmpty(imageUrl))
			return string.Empty;
		if (size is null)
			return imageUrl;
		return imageUrl + $"&size={size.Value.ToString( ).TrimStart('S')}";
	}

	public enum ImageSizes
	{
		S100x100,
		S150x150,
		S1200x630,
		S1280x720,
		S1920x1080,
	}
}
