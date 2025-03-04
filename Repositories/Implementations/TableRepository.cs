using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.Repositories.Implementations
{
    public class TableRepository : ITableRepository
    {
        private readonly string _connectionString;
        private List<ServicingPoint> _cachedTables;

        public TableRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<ServicingPoint>> GetTablesAsync()
        {
            if (_cachedTables != null) return _cachedTables;

            _cachedTables = new List<ServicingPoint>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var query = "SELECT * FROM Posts_Main";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                _cachedTables.Add(new ServicingPoint
                {
                    PostID = reader.GetInt32("Post_ID"),
                    Description = reader.GetString("Description"),
                    PostNumber = reader.GetInt32("Post_Number"),
                    Active = reader.GetBoolean("Active"),
                    Reserved = reader.GetBoolean("Reserve"),
                    ActiveOrderID = reader.IsDBNull(reader.GetOrdinal("Active_Order_ID")) ? 0 : reader.GetInt32("Active_Order_ID")
                });
            }
            return _cachedTables;
        }

        public async Task UpdateTableStatusAsync(int tableId, bool occupied, bool reserved, int? activeOrderId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // ✅ Check if the status has already been set to avoid redundant updates
            var checkQuery = "SELECT Active, Reserve FROM Posts_Main WHERE Post_ID = @tableId";
            using var checkCommand = new MySqlCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@tableId", tableId);

            using var reader = await checkCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                bool currentActive = reader.GetBoolean("Active");
                bool currentReserved = reader.GetBoolean("Reserve");

                // ✅ Only update if something has changed
                if (currentActive != occupied || currentReserved != reserved)
                {
                    reader.Close();

                    var updateQuery = "UPDATE Posts_Main SET Active = @occupied, Reserve = @reserved, Active_Order_ID = @activeOrderId WHERE Post_ID = @tableId";
                    using var updateCommand = new MySqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@occupied", occupied);
                    updateCommand.Parameters.AddWithValue("@reserved", reserved);
                    updateCommand.Parameters.AddWithValue("@activeOrderId", activeOrderId ?? (object)DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@tableId", tableId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
