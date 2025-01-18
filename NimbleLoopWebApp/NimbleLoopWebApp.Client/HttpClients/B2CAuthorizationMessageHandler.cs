using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Net.Http.Headers;

namespace NimbleLoopWebApp.Client.HttpClients;

public class B2CAuthorizationMessageHandler : DelegatingHandler
{
	private readonly IAccessTokenProvider _provider;
	private readonly NavigationManager _navigation;
	private readonly IConfiguration _configuration;
	private AccessToken? _lastToken;
	private AuthenticationHeaderValue? _cachedHeader;

	public B2CAuthorizationMessageHandler(
		IAccessTokenProvider provider,
		NavigationManager navigation,
		IConfiguration configuration)
	{
		_provider = provider;
		_navigation = navigation;
		_configuration = configuration;
		if (_provider is AuthenticationStateProvider authenticationStateProvider)
		{
			authenticationStateProvider.AuthenticationStateChanged += async authStateTask =>
			{
				var authState = await authStateTask;
				if (authState.User?.Identity?.IsAuthenticated == true)
				{
					_lastToken = null;
					_cachedHeader = null;
				}
			};
		}
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.Now;
		var apiBaseUrl = _configuration["DownstreamApi:BaseUrl"] ?? _navigation.BaseUri;
		var authorizedEndpoints = new[ ]
		{
			$"{apiBaseUrl}api/articles",
			$"{apiBaseUrl}api/validate-unique-key",
			$"{apiBaseUrl}api/editors"
            // Add other authenticated endpoints here
        };
		var scope = _configuration["DownstreamApi:Scope"] ?? string.Empty;
		var tokenOptions = new AccessTokenRequestOptions
		{
			Scopes = new[ ] { scope }
		};

		if (request.RequestUri is Uri requestUrl && authorizedEndpoints.Any(endpoint => requestUrl.ToString( ).StartsWith(endpoint, StringComparison.OrdinalIgnoreCase)))
		{
			if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
			{
				var tokenResult = await _provider.RequestAccessToken(tokenOptions);

				if (tokenResult.TryGetToken(out var token))
				{
					_lastToken = token;
					_cachedHeader = new AuthenticationHeaderValue("Bearer", _lastToken.Value);
				}
				else
				{
					throw new AccessTokenNotAvailableException(_navigation, tokenResult, new[ ] { scope });
				}
			}
			request.Headers.Authorization = _cachedHeader;
		}

		return await base.SendAsync(request, cancellationToken);
	}
}
