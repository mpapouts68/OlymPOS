using System;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface ISyncService
    {
        Task<bool> SyncAllDataAsync();
        Task<bool> SyncOrdersAsync();
        Task<bool> SyncProductsAsync();
        Task<bool> SyncTablesAsync();
        Task<bool> IsSyncNeededAsync();
        event EventHandler<SyncEventArgs> SyncProgress;
        event EventHandler<SyncEventArgs> SyncCompleted;
    }

    public class SyncEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int Progress { get; set; }
        public bool Success { get; set; }
        public Exception Error { get; set; }
    }
}