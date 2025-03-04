using System;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface ICacheManager
    {
        Task InitializeAsync();
        Task<bool> IsCacheInitializedAsync();
        Task ClearCacheAsync();
        Task<DateTime> GetLastSyncTimeAsync();
        Task SetLastSyncTimeAsync(DateTime syncTime);
        Task<T> GetCachedItemAsync<T>(string key) where T : class;
        Task SetCachedItemAsync<T>(string key, T item) where T : class;
    }
}
