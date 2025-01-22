using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace NimbleLoopWebApp.Functions;

public class ReadImageNames(ILogger<ReadImageNames> logger, BlobServiceClient blobServiceClient)
{
	private readonly ILogger<ReadImageNames> _logger = logger;
	private readonly BlobServiceClient _blobServiceClient = blobServiceClient;

	[Function(nameof(ReadImageNames))]
	public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
	{
		_logger.LogInformation("Reading image names from blob storage.");
		var path = req.Query["path"].FirstOrDefault( );

		try
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient("images");
			var imageNames = new List<string>( );

			await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: path))
				imageNames.Add(blobItem.Name);
			return new OkObjectResult(imageNames);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading image names due to {Message}", ex.Message);
			return new BadRequestObjectResult("Error reading image names.");
		}

	}
}
