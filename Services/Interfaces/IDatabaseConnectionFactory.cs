using System.Threading.Tasks;
using MySqlConnector;
using SQLite;

namespace OlymPOS.Services.Interfaces
{
    public interface IDatabaseConnectionFactory
    {
        Task<MySqlConnection> CreateRemoteConnectionAsync();
        Task<SQLiteAsyncConnection> CreateLocalConnectionAsync();
    }
}
