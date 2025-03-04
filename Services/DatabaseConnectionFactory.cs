using System;
using System.IO;
using System.Threading.Tasks;
using MySqlConnector;
using SQLite;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.Services
{
    public class DatabaseConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly IAppSettings _appSettings;

        public DatabaseConnectionFactory(IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public Task<MySqlConnection> CreateRemoteConnectionAsync()
        {
            var connection = new MySqlConnection(_appSettings.RemoteConnectionString);
            return Task.FromResult(connection);
        }

        public Task<SQLiteAsyncConnection> CreateLocalConnectionAsync()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, _appSettings.LocalConnectionString);
            var connection = new SQLiteAsyncConnection(dbPath);
            return Task.FromResult(connection);
        }
    }
}
