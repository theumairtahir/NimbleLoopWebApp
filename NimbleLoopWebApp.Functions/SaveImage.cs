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

namespace NimbleLoopWebApp.Functions;

public class SaveImage(ILogger<SaveImage> logger, BlobServiceClient blobServiceClient)
{
	private readonly ILogger<SaveImage> _logger = logger;
	private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

	[Function(nameof(SaveImage))]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
	{
		_logger.LogInformation("Processing image upload request.");

		if (!req.HasFormContentType)
		{
			return new BadRequestObjectResult("Invalid form data.");
		}

		var allowedContentTypes = new[ ] { "image/jpeg", "image/png", "image/webp" };
		var form = await req.ReadFormAsync( );

		if (form.Files is { Count: <= 0 })
			return new BadRequestObjectResult("No images provided.");

		Dictionary<string, object> savedInformation = [ ];
		var path = req.Query["path"].FirstOrDefault( );

		foreach (var file in form.Files)
		{
			if (file == null || file.Length == 0)
				return new BadRequestObjectResult("No image provided.");

			if (!allowedContentTypes.Contains(file.ContentType))
			{
				_logger.LogInformation("Invalid image format: {FileName}", file.FileName);
				continue;
			}

			try
			{
				var containerClient = _blobServiceClient.GetBlobContainerClient("images");
				await containerClient.CreateIfNotExistsAsync( );
				var extension = Path.GetExtension(file.FileName);
				var savedName = Guid.NewGuid( ) + extension;

				using (var stream = file.OpenReadStream( ))
				{
					var sizes = new Dictionary<string, (int x, int y)>
					{
						{ "1920x1080", (1920, 1080) },
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

						string imgBlobName = string.Join('/', path, size.Key, savedName);
						var blobClient = containerClient.GetBlobClient(imgBlobName);
						await blobClient.UploadAsync(ms, overwrite: true);
					}
				}

				savedInformation.Add(file.FileName, new { path, savedName });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading image: {FileName}", file.FileName);
				continue;
			}
		}
		return new OkObjectResult(savedInformation);
	}
}
