using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace NimbleLoopWebApp.Functions;

public class ReadImage(ILogger<ReadImage> logger, BlobServiceClient blobServiceClient)
{
	private readonly ILogger<ReadImage> _logger = logger;
	private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
	private static readonly string[ ] AVAILABLE_SIZES = ["1920x1080", "1280x720", "1200x630", "150x150", "100x100"];

	[Function(nameof(ReadImage))]
	public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadImage/{imageName}")] HttpRequestData req, string imageName)
	{
		_logger.LogInformation("Processing image read request.");

		if (string.IsNullOrEmpty(imageName))
		{
			var badRequestResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
			await badRequestResponse.WriteStringAsync("Image name is required.");
			return badRequestResponse;
		}

		var path = req.Query["path"];
		var size = req.Query["size"] ?? "1920x1080";

		try
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient("images");
			var qualifiedName = AVAILABLE_SIZES.Contains(size) ? $"{size}/{imageName}" : $"{GetNearestSize(size)}/{imageName}";

			return await FetchImageFromBlob(req, qualifiedName, path, containerClient);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error reading image.");
			var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
			await errorResponse.WriteStringAsync("Error reading image.");
			return errorResponse;
		}

		static string GetNearestSize(string size) => AVAILABLE_SIZES.OrderBy(s => Math.Abs(int.Parse(s.Split('x')[0]) - int.Parse(size.Split('x')[0]))).ThenBy(s => Math.Abs(int.Parse(s.Split('x')[1]) - int.Parse(size.Split('x')[1]))).First( );

		static async Task<HttpResponseData> FetchImageFromBlob(HttpRequestData req, string imageName, string? path, BlobContainerClient containerClient)
		{
			if (path is not null)
				imageName = $"{path}/{imageName}";

			var blobClient = containerClient.GetBlobClient(imageName);

			if (await blobClient.ExistsAsync( ))
			{
				var downloadInfo = await blobClient.DownloadAsync( );
				var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
				response.Headers.Add("Content-Type", downloadInfo.Value.ContentType);
				await downloadInfo.Value.Content.CopyToAsync(response.Body);
				return response;
			}
			else
			{
				var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
				await notFoundResponse.WriteStringAsync("Image not found.");
				return notFoundResponse;
			}
		}
	}
}

