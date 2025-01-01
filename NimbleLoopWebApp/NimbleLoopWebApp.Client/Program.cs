using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddCascadingAuthenticationState( );
builder.Services.AddScoped(sp =>
{
	var authorizationMessageHandler = sp.GetRequiredService<AuthorizationMessageHandler>( );
	authorizationMessageHandler.InnerHandler = new HttpClientHandler( );
	authorizationMessageHandler = authorizationMessageHandler.ConfigureHandler(
	  authorizedUrls: new[ ] { builder.Configuration["DownstreamApi:BaseUrl"] }!,
	  scopes: new[ ] { builder.Configuration["DownstreamApi:Scope"] }!);
	return new HttpClient(authorizationMessageHandler)
	{
		BaseAddress = new Uri(builder.Configuration["DownstreamApi:BaseUrl"] ?? string.Empty)
	};
});

builder.Services.AddMsalAuthentication(options =>
{
	builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
	options.ProviderOptions.LoginMode = "redirect";
	options.ProviderOptions.Cache.CacheLocation = "localStorage";
	options.ProviderOptions.DefaultAccessTokenScopes.Add(builder.Configuration["DownstreamApi:Scope"]!);
});

await builder.Build( ).RunAsync( );
