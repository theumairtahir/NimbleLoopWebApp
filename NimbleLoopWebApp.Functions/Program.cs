using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder( )
	.ConfigureFunctionsWebApplication( )
	.ConfigureServices((context, services) =>
	{
		services.AddApplicationInsightsTelemetryWorkerService( );
		services.ConfigureFunctionsApplicationInsights( );
		string connectionString = context.Configuration.GetValue<string>("BlobServiceConnection")!;
		services.AddSingleton(new BlobServiceClient(connectionString));
	})
	.Build( );

await host.RunAsync( );
