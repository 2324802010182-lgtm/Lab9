using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ASC.Web.Models;

namespace ASC.Web.Data
{
    public class NavigationCacheOperations : INavigationCacheOperations
    {
        private readonly IDistributedCache _cache;
        private readonly string NavigationCacheName = "NavigationCache";

        public NavigationCacheOperations(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task CreateNavigationCacheAsync()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Navigation.json");

            await _cache.SetStringAsync(
                NavigationCacheName,
                await File.ReadAllTextAsync(path));
        }

        public async Task<NavigationRoot> GetNavigationCacheAsync()
        {
            var json = await _cache.GetStringAsync(NavigationCacheName);

            if (string.IsNullOrEmpty(json))
            {
                await CreateNavigationCacheAsync();
                json = await _cache.GetStringAsync(NavigationCacheName);
            }

            return JsonConvert.DeserializeObject<NavigationRoot>(json);
        }
    }
}