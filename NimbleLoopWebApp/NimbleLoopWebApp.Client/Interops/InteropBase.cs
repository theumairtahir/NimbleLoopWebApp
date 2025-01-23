using Microsoft.JSInterop;

namespace NimbleLoopWebApp.Client.Interops;

public class JsInteropBase(IJSRuntime jsRuntime, string jsModule)
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;
	private IJSObjectReference? _module;

	protected async ValueTask<IJSObjectReference> GetModuleAsync( ) => _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>(
			"import", $"./js/modules/{jsModule}.js");
}
