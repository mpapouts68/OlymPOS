using System;

namespace OlymPOS.Services.Interfaces
{
    public interface IAppSettings
    {
        string RemoteConnectionString { get; }
        string LocalConnectionString { get; }
        bool UseOfflineMode { get; set; }
        bool AutoSyncEnabled { get; set; }
        TimeSpan SyncInterval { get; set; }
    }
}
