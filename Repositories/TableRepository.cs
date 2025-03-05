using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Caching;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppSettings _appSettings;
        private readonly ICacheManager _cacheManager;

        public TableRepository(
            IDatabaseConnectionFactory connectionFactory,
            IAppSettings appSettings,
            ICacheManager cacheManager)
        {
            _connectionFactory = connectionFactory;
            _appSettings = appSettings;
            _cacheManager = cacheManager;
        }

        private async Task<bool> IsOfflineModeAsync()
        {
            // Check if user has explicitly enabled offline mode
            if (_appSettings.UseOfflineMode)
                return true;

            // Try to connect to remote database to see if it's available
            try
            {
                using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
                await connection.OpenAsync();
                await connection.CloseAsync();
                return false; // Connection successful, we're online
            }
            catch
            {
                return true; // Connection failed, we're offline
            }
        }

        public async Task<IEnumerable<ServicingPoint>> GetAllAsync()
        {
            if (await IsOfflineModeAsync())
            {
                return await GetAllFromCacheAsync();
            }
            else
            {
                try
                {
                    return await GetAllFromRemoteAsync();
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetAllFromCacheAsync();
                }
            }
        }

        private async Task<IEnumerable<ServicingPoint>> GetAllFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedTables = await connection.Table<CachedServicingPoint>().ToListAsync();

            return cachedTables.Select(Convert);
        }

        private async Task<IEnumerable<ServicingPoint>> GetAllFromRemoteAsync()
        {
            var tables = new List<ServicingPoint>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Post_ID, Description, Post_Number, Active, Active_Order_ID, Reserve, Yper_Main_ID
                FROM Posts_Main", connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tables.Add(new ServicingPoint
                {
                    PostID = reader.GetInt32("Post_ID"),
                    Description = reader.GetString("Description"),
                    Active = reader.GetBoolean("Active"),
                    ActiveOrderID = reader.IsDBNull(reader.GetOrdinal("Active_Order_ID")) ? 0 : reader.GetInt32("Active_Order_ID"),
                    PostNumber = reader.GetInt32("Post_Number"),
                    Reserved = reader.GetBoolean("Reserve"),
                    // Set full description property
                    FullDescription = $"{reader.GetString("Description")} {reader.GetInt32("Post_Number")}"
                });
            }

            return tables;
        }

        public async Task<ServicingPoint> GetByIdAsync(int id)
        {
            if (await IsOfflineModeAsync())
            {
                return await GetByIdFromCacheAsync(id);
            }
            else
            {
                try
                {
                    return await GetByIdFromRemoteAsync(id);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetByIdFromCacheAsync(id);
                }
            }
        }

        private async Task<ServicingPoint> GetByIdFromCacheAsync(int id)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedTable = await connection.Table<CachedServicingPoint>()
                .Where(t => t.PostID == id)
                .FirstOrDefaultAsync();

            return cachedTable != null ? Convert(cachedTable) : null;
        }

        private async Task<ServicingPoint> GetByIdFromRemoteAsync(int id)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Post_ID, Description, Post_Number, Active, Active_Order_ID, Reserve, Yper_Main_ID
                FROM Posts_Main 
                WHERE Post_ID = @postId", connection);

            command.Parameters.AddWithValue("@postId", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new ServicingPoint
                {
                    PostID = reader.GetInt32("Post_ID"),
                    Description = reader.GetString("Description"),
                    Active = reader.GetBoolean("Active"),
                    ActiveOrderID = reader.IsDBNull(reader.GetOrdinal("Active_Order_ID")) ? 0 : reader.GetInt32("Active_Order_ID"),
                    PostNumber = reader.GetInt32("Post_Number"),
                    Reserved = reader.GetBoolean("Reserve"),
                    // Set full description property
                    FullDescription = $"{reader.GetString("Description")} {reader.GetInt32("Post_Number")}"
                };
            }

            return null;
        }

        public async Task<IEnumerable<ServicingPoint>> GetByAreaAsync(int areaId)
        {
            if (await IsOfflineModeAsync())
            {
                return await GetByAreaFromCacheAsync(areaId);
            }
            else
            {
                try
                {
                    return await GetByAreaFromRemoteAsync(areaId);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetByAreaFromCacheAsync(areaId);
                }
            }
        }

        private async Task<IEnumerable<ServicingPoint>> GetByAreaFromCacheAsync(int areaId)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedTables = await connection.Table<CachedServicingPoint>()
                .Where(t => t.YperMainID == areaId)
                .ToListAsync();

            return cachedTables.Select(Convert);
        }

        private async Task<IEnumerable<ServicingPoint>> GetByAreaFromRemoteAsync(int areaId)
        {
            var tables = new List<ServicingPoint>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Post_ID, Description, Post_Number, Active, Active_Order_ID, Reserve, Yper_Main_ID
                FROM Posts_Main 
                WHERE Yper_Main_ID = @areaId", connection);

            command.Parameters.AddWithValue("@areaId", areaId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tables.Add(new ServicingPoint
                {
                    PostID = reader.GetInt32("Post_ID"),
                    Description = reader.GetString("Description"),
                    Active = reader.GetBoolean("Active"),
                    ActiveOrderID = reader.IsDBNull(reader.GetOrdinal("Active_Order_ID")) ? 0 : reader.GetInt32("Active_Order_ID"),
                    PostNumber = reader.GetInt32("Post_Number"),
                    Reserved = reader.GetBoolean("Reserve"),
                    // Set full description property
                    FullDescription = $"{reader.GetString("Description")} {reader.GetInt32("Post_Number")}"
                });
            }

            return tables;
        }

        public async Task<bool> SetTableStatusAsync(int tableId, bool isActive, int? orderId = null)
        {
            // Always try to update local cache first
            bool cacheUpdated = await SetTableStatusInCacheAsync(tableId, isActive, orderId);

            // If we're in offline mode, just return cache result
            if (await IsOfflineModeAsync())
            {
                return cacheUpdated;
            }

            // If online, update remote database
            try
            {
                return await SetTableStatusInRemoteAsync(tableId, isActive, orderId);
            }
            catch
            {
                // If remote update fails, we already updated cache, so return cache result
                return cacheUpdated;
            }
        }

        private async Task<bool> SetTableStatusInCacheAsync(int tableId, bool isActive, int? orderId = null)
        {
            try
            {
                using var connection = await _connectionFactory.CreateLocalConnectionAsync();

                var table = await connection.Table<CachedServicingPoint>()
                    .Where(t => t.PostID == tableId)
                    .FirstOrDefaultAsync();

                if (table == null)
                    return false;

                table.Active = isActive;
                table.ActiveOrderID = orderId ?? 0;

                await connection.UpdateAsync(table);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating table status in cache: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SetTableStatusInRemoteAsync(int tableId, bool isActive, int? orderId = null)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"UPDATE Posts_Main 
                SET Active = @active, Active_Order_ID = @orderId 
                WHERE Post_ID = @tableId", connection);

            command.Parameters.AddWithValue("@active", isActive);
            command.Parameters.AddWithValue("@orderId", orderId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@tableId", tableId);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> AddAsync(ServicingPoint entity)
        {
            // This operation is not supported on mobile
            throw new NotSupportedException("Adding tables is not supported in the mobile app");
        }

        public async Task<bool> UpdateAsync(ServicingPoint entity)
        {
            // This is mainly used by admin, not supported in mobile except for table status
            if (await IsOfflineModeAsync())
            {
                return await UpdateInCacheAsync(entity);
            }
            else
            {
                try
                {
                    bool remoteUpdated = await UpdateInRemoteAsync(entity);
                    if (remoteUpdated)
                    {
                        // Also update cache to keep it in sync
                        await UpdateInCacheAsync(entity);
                    }
                    return remoteUpdated;
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await UpdateInCacheAsync(entity);
                }
            }
        }

        private async Task<bool> UpdateInCacheAsync(ServicingPoint entity)
        {
            try
            {
                using var connection = await _connectionFactory.CreateLocalConnectionAsync();

                var cachedTable = await connection.Table<CachedServicingPoint>()
                    .Where(t => t.PostID == entity.PostID)
                    .FirstOrDefaultAsync();

                if (cachedTable == null)
                {
                    // Insert new if not exists
                    cachedTable = new CachedServicingPoint
                    {
                        PostID = entity.PostID,
                        Description = entity.Description,
                        Active = entity.Active,
                        ActiveOrderID = entity.ActiveOrderID,
                        PostNumber = entity.PostNumber,
                        Reserved = entity.Reserved,
                        YperMainID = 1 // Default area if unknown
                    };

                    await connection.InsertAsync(cachedTable);
                }
                else
                {
                    // Update existing
                    cachedTable.Description = entity.Description;
                    cachedTable.Active = entity.Active;
                    cachedTable.ActiveOrderID = entity.ActiveOrderID;
                    cachedTable.PostNumber = entity.PostNumber;
                    cachedTable.Reserved = entity.Reserved;

                    await connection.UpdateAsync(cachedTable);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating table in cache: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> UpdateInRemoteAsync(ServicingPoint entity)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"UPDATE Posts_Main 
                SET Description = @description, 
                    Active = @active, 
                    Active_Order_ID = @activeOrderId,
                    Post_Number = @postNumber,
                    Reserve = @reserved
                WHERE Post_ID = @postId", connection);

            command.Parameters.AddWithValue("@description", entity.Description);
            command.Parameters.AddWithValue("@active", entity.Active);
            command.Parameters.AddWithValue("@activeOrderId", entity.ActiveOrderID);
            command.Parameters.AddWithValue("@postNumber", entity.PostNumber);
            command.Parameters.AddWithValue("@reserved", entity.Reserved);
            command.Parameters.AddWithValue("@postId", entity.PostID);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // This operation is not supported on mobile
            throw new NotSupportedException("Deleting tables is not supported in the mobile app");
        }

        // Helper method to convert from cached entity to domain entity
        private ServicingPoint Convert(CachedServicingPoint cachedTable)
        {
            return new ServicingPoint
            {
                PostID = cachedTable.PostID,
                Description = cachedTable.Description,
                Active = cachedTable.Active,
                ActiveOrderID = cachedTable.ActiveOrderID,
                PostNumber = cachedTable.PostNumber,
                Reserved = cachedTable.Reserved,
                FullDescription = $"{cachedTable.Description} {cachedTable.PostNumber}"
            };
        }
    }
}
