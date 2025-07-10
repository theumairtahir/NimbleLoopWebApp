using Blazored.Modal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NimbleLoopWebApp.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient(Constants.BASE_CLIENT, client => client.BaseAddress = new Uri(builder.Configuration["DownstreamApi:BaseUrl"] ?? string.Empty));

builder.Services.AddBlazoredModal( );

await builder.Build( ).RunAsync( );
