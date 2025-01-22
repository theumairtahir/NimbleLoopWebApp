using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.RegularExpressions;

namespace NimbleLoopWebApp.Functions;

public class SaveImage(ILogger<SaveImage> logger, BlobServiceClient blobServiceClient)
{
	private const string DEFAULT_SIZE = "1920x1080";
	private readonly ILogger<SaveImage> _logger = logger;
	private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

	[Function(nameof(SaveImage))]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "SaveImage/{imageName}")] HttpRequest req, string imageName)
	{
		_logger.LogInformation("Processing single image upload request.");

		if (!req.HasFormContentType)
			return new BadRequestObjectResult("Invalid form data.");

		var allowedContentTypes = new[ ] { "image/jpeg", "image/png", "image/webp" };
		var form = await req.ReadFormAsync( );

		if (form.Files is { Count: <= 0 })
			return new BadRequestObjectResult("No image provided.");

		var file = form.Files["image"];
		if (file == null || file.Length == 0)
			return new BadRequestObjectResult("No image provided.");

		if (!allowedContentTypes.Contains(file.ContentType))
		{
			_logger.LogInformation("Invalid image format: {FileName}", file.FileName);
			return new BadRequestObjectResult("Invalid image format.");
		}

		var path = req.Query["path"].FirstOrDefault( );

		try
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient("images");
			await containerClient.CreateIfNotExistsAsync( );

			var extension = Path.GetExtension(file.FileName);
			var savedName = GenerateSlug(imageName) + extension;

			// Check if blob exists
			string imgBlobName = string.Join('/', path, DEFAULT_SIZE, savedName);
			var blobClient = containerClient.GetBlobClient(imgBlobName);
			if (await blobClient.ExistsAsync( ))
				return new ConflictObjectResult("An image with the same name already exists. Please try again with a different name.");

			using var stream = file.OpenReadStream( );
			var sizes = new Dictionary<string, (int x, int y)>
			{
				{ DEFAULT_SIZE, (1920, 1080) },
				{ "1280x720", (1280, 720) },
				{ "1200x630", (1200, 630) },
				{ "150x150", (150, 150) },
				{ "100x100", (100, 100) },
			};

			var image = await Image.LoadAsync(stream);
			foreach (var size in sizes)
			{
				var resizedImage = image.Clone(ctx => ctx.Resize(new ResizeOptions
				{
					Mode = ResizeMode.Max,
					Size = new Size(size.Value.x, size.Value.y)
				}));

				using var ms = new MemoryStream( );
				await resizedImage.SaveAsync(ms, extension switch
				{
					".png" => new PngEncoder( ),
					".webp" => new WebpEncoder( ),
					_ => new JpegEncoder( )
				});
				ms.Position = 0;

				string resizedBlobName = string.Join('/', path, size.Key, savedName);
				var resizedBlobClient = containerClient.GetBlobClient(resizedBlobName);
				await resizedBlobClient.UploadAsync(ms, overwrite: true);
			}

			var savedInformation = new { path, savedName };
			return new OkObjectResult(savedInformation);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
			return new StatusCodeResult(StatusCodes.Status500InternalServerError);
		}

		static string GenerateSlug(string value)
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
	}
}


