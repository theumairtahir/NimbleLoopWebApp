using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

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
				string imgBlobName = string.Join('/', path, savedName);
				var blobClient = containerClient.GetBlobClient(imgBlobName);

				using (var stream = file.OpenReadStream( ))
				{
					await blobClient.UploadAsync(stream, overwrite: true);
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
