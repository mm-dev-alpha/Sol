using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Sol.Models;

namespace Sol.Services;

public class SearchService : ISearchService
{
    private readonly IActiveDirectoryService _adService;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public SearchService(IActiveDirectoryService adService, IMemoryCache cache)
    {
        _adService = adService;
        _cache = cache;
    }

    public async Task<IEnumerable<AdUser>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<AdUser>();
        }

        string cacheKey = $"SearchUsers_{query.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<AdUser>? cachedResult))
        {
            return cachedResult ?? Array.Empty<AdUser>();
        }

        // Simulating cancellation support by checking token before AD call.
        // ActiveDirectoryService doesn't accept CancellationToken right now, 
        // but we can check before we invoke.
        cancellationToken.ThrowIfCancellationRequested();

        var results = await _adService.SearchUsersAsync(query);

        cancellationToken.ThrowIfCancellationRequested();

        _cache.Set(cacheKey, results, CacheDuration);

        return results;
    }
}
