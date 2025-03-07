using Blazored.Modal;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NimbleLoopWebApp.Client;
using NimbleLoopWebApp.Client.HttpClients;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddCascadingAuthenticationState( );

builder.Services.AddScoped<B2CAuthorizationMessageHandler>( );

builder.Services.AddHttpClient(Constants.BASE_CLIENT, client => client.BaseAddress = new Uri(builder.Configuration["DownstreamApi:BaseUrl"] ?? string.Empty))
	.AddHttpMessageHandler<B2CAuthorizationMessageHandler>( );

builder.Services.AddMsalAuthentication(options =>
{
	builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
	options.ProviderOptions.LoginMode = "redirect";
	options.ProviderOptions.Cache.CacheLocation = "localStorage";
	options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["DownstreamApi:Scope"]!);
});

builder.Services.AddBlazoredModal( );

await builder.Build( ).RunAsync( );
