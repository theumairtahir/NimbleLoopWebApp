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
		value = value.Substring(0, value.Length <= 45 ? value.Length : 45).Trim( );
		value = Regex.Replace(value, @"\s", "-");
		return value;
	}

	public static string ToTitleCase(this string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;
		return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant( ));
	}
}
