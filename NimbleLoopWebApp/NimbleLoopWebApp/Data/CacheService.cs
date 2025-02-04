using Microsoft.Extensions.Caching.Memory;

namespace NimbleLoopWebApp.Data;

public interface ICacheService
{
	Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> retrieveDataFunc, TimeSpan? expiration = null);
}

internal class CacheService(IMemoryCache cache, IHostEnvironment environment) : ICacheService
{
	private readonly IMemoryCache _cache = cache;
	private readonly IHostEnvironment _environment = environment;

	public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> retrieveDataFunc, TimeSpan? expiration = null)
	{
		if (_environment.IsDevelopment( ))
			return await retrieveDataFunc( );

		if (_cache.TryGetValue(cacheKey, out T? cachedData) && cachedData is not null)
			return cachedData;
		cachedData = await retrieveDataFunc( );
		var cacheEntryOptions = new MemoryCacheEntryOptions
		{
			AbsoluteExpiration = DateTime.UtcNow.Add(expiration ?? TimeSpan.FromMinutes(60)),
			SlidingExpiration = TimeSpan.FromMinutes(10)
		};
		_cache.Set(cacheKey, cachedData, cacheEntryOptions);
		return cachedData;
	}
}
