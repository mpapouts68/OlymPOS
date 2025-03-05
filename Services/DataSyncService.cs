using OlymPOS.Services.Interfaces;
using MySqlConnector;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using OlymPOS.Caching;

namespace OlymPOS.Services
{
    public class DataSyncService : ISyncService
    {
        private readonly ICacheManager _cacheManager;
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppSettings _appSettings;
        private bool _syncNeeded = false;

        public event EventHandler<SyncEventArgs> SyncProgress;
        public event EventHandler<SyncEventArgs> SyncCompleted;

        public DataSyncService(
            ICacheManager cacheManager,
            IDatabaseConnectionFactory connectionFactory,
            IAppSettings appSettings)
        {
            _cacheManager = cacheManager;
            _connectionFactory = connectionFactory;
            _appSettings = appSettings;
        }

        public async Task<bool> IsSyncNeededAsync()
        {
            if (_syncNeeded)
                return true;

            try
            {
                // Check if we have connectivity to remote database
                using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
                await connection.OpenAsync();

                // Check for server-side flag indicating data changes
                using var command = new MySqlCommand("SELECT Change_POS FROM Menu_Changes LIMIT 1", connection);
                var result = await command.ExecuteScalarAsync();

                if (result != null && (bool)result)
                {
                    _syncNeeded = true;
                    return true;
                }

                // Check if we have any unsynced orders
                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();
                var unsyncedOrders = await localDb.Table<CachedOrder>()
                    .Where(o => !o.IsSynced)
                    .CountAsync();

                return unsyncedOrders > 0;
            }
            catch
            {
                // If we can't connect to remote, no sync needed right now
                return false;
            }
        }

        public async Task<bool> SyncAllDataAsync()
        {
            try
            {
                // Report progress
                OnSyncProgress("Starting full data sync...", 0);

                // Initialize cache if needed
                if (!await _cacheManager.IsCacheInitializedAsync())
                {
                    await _cacheManager.InitializeAsync();
                }

                // Sync critical tables for offline operation
                bool productsSuccess = await SyncProductsAsync();
                OnSyncProgress("Products synchronized", 20);

                bool groupsSuccess = await SyncProductGroupsAsync();
                OnSyncProgress("Product groups synchronized", 40);

                bool tablesSuccess = await SyncTablesAsync();
                OnSyncProgress("Tables synchronized", 60);

                bool extrasSuccess = await SyncExtrasAsync();
                OnSyncProgress("Extras synchronized", 80);

                bool ordersSuccess = await SyncOrdersAsync();
                OnSyncProgress("Orders synchronized", 100);

                // Reset server change flag if sync was successful
                bool allSuccess = productsSuccess && groupsSuccess && tablesSuccess && extrasSuccess && ordersSuccess;
                if (allSuccess)
                {
                    await ResetServerChangeFlag();
                    _syncNeeded = false;

                    // Update last sync time
                    await _cacheManager.SetLastSyncTimeAsync(DateTime.Now);
                }

                OnSyncCompleted($"Data sync {(allSuccess ? "completed successfully" : "completed with errors")}", allSuccess);

                return allSuccess;
            }
            catch (Exception ex)
            {
                OnSyncCompleted($"Sync failed: {ex.Message}", false, ex);
                return false;
            }
        }

        private async Task ResetServerChangeFlag()
        {
            try
            {
                using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
                await connection.OpenAsync();

                using var command = new MySqlCommand("UPDATE Menu_Changes SET Change_POS = 0", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting server change flag: {ex.Message}");
                // Don't throw - this is not critical
            }
        }

        public async Task<bool> SyncProductsAsync()
        {
            try
            {
                // Connect to remote database
                using var remoteConnection = await _connectionFactory.CreateRemoteConnectionAsync();
                await remoteConnection.OpenAsync();

                // Get all products from remote
                var products = new List<CachedProduct>();
                using (var command = new MySqlCommand(
                    @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                    Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                    Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                    CPrinter, Sale_Lock FROM Products", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        products.Add(new CachedProduct
                        {
                            ProductID = reader.GetInt32("Product_ID"),
                            Description = reader.GetString("Description"),
                            Description2 = reader.IsDBNull(reader.GetOrdinal("Description2")) ? null : reader.GetString("Description2"),
                            Price = reader.GetDecimal("Price"),
                            ProductGroupID = reader.GetInt32("ProductGroup_ID"),
                            Printer = reader.IsDBNull(reader.GetOrdinal("Printer")) ? null : reader.GetString("Printer"),
                            VAT = reader.IsDBNull(reader.GetOrdinal("VAT")) ? (int?)null : reader.GetInt32("VAT"),
                            Build = reader.GetBoolean("Build"),
                            Extra_ID = reader.IsDBNull(reader.GetOrdinal("Extra_ID")) ? (int?)null : reader.GetInt32("Extra_ID"),
                            Row_Print = reader.GetInt32("Row_Print"),
                            Auto_Extra = reader.GetBoolean("Auto_Extra"),
                            Has_Options = reader.GetBoolean("Has_Options"),
                            Extra_ID_Key = reader.IsDBNull(reader.GetOrdinal("Extra_ID_Key")) ? (int?)null : reader.GetInt32("Extra_ID_Key"),
                            Menu_Number = reader.IsDBNull(reader.GetOrdinal("Menu_Number")) ? (int?)null : reader.GetInt32("Menu_Number"),
                            Include_Group = reader.GetBoolean("Include_Group"),
                            Favorite = reader.GetBoolean("Favorite"),
                            Drink_Or_Food = reader.IsDBNull(reader.GetOrdinal("Drink_Or_Food")) ? null : reader.GetString("Drink_Or_Food"),
                            CPrinter = reader.IsDBNull(reader.GetOrdinal("CPrinter")) ? null : reader.GetString("CPrinter"),
                            Sale_Lock = reader.GetBoolean("Sale_Lock")
                        });
                    }
                }

                // Open local connection and update cache
                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

                // Clear existing products
                await localDb.DeleteAllAsync<CachedProduct>();

                // Insert all products
                foreach (var batch in products.Chunk(100)) // Process in batches for performance
                {
                    await localDb.InsertAllAsync(batch);
                }

                return true;
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Error syncing products: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncProductGroupsAsync()
        {
            try
            {
                // Connect to remote database
                using var remoteConnection = await _connectionFactory.CreateRemoteConnectionAsync();
                await remoteConnection.OpenAsync();

                // Get all product groups from remote
                var groups = new List<CachedProductGroup>();
                using (var command = new MySqlCommand(
                    @"SELECT ProductGroup_ID, Description, Description2, View, ViewOrder, 
                    Extra_ID, Icon_ID, ISSub, Sub_From_GroupID, Has_Sub 
                    FROM ProductGroups", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        groups.Add(new CachedProductGroup
                        {
                            ProductGroupID = reader.GetInt32("ProductGroup_ID"),
                            Description = reader.GetString("Description"),
                            Description2 = reader.IsDBNull(reader.GetOrdinal("Description2")) ? null : reader.GetString("Description2"),
                            View = reader.IsDBNull(reader.GetOrdinal("View")) ? (int?)null : reader.GetInt32("View"),
                            ViewOrder = reader.IsDBNull(reader.GetOrdinal("ViewOrder")) ? (int?)null : reader.GetInt32("ViewOrder"),
                            Extra_ID = reader.IsDBNull(reader.GetOrdinal("Extra_ID")) ? (int?)null : reader.GetInt32("Extra_ID"),
                            Icon_ID = reader.IsDBNull(reader.GetOrdinal("Icon_ID")) ? (int?)null : reader.GetInt32("Icon_ID"),
                            ISSub = reader.GetBoolean("ISSub"),
                            Sub_From_GroupID = reader.IsDBNull(reader.GetOrdinal("Sub_From_GroupID")) ? (int?)null : reader.GetInt32("Sub_From_GroupID"),
                            Has_Sub = reader.GetBoolean("Has_Sub")
                        });
                    }
                }

                // Open local connection and update cache
                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

                // Clear existing groups
                await localDb.DeleteAllAsync<CachedProductGroup>();

                // Insert all groups
                foreach (var batch in groups.Chunk(100))
                {
                    await localDb.InsertAllAsync(batch);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing product groups: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncTablesAsync()
        {
            try
            {
                // Connect to remote database
                using var remoteConnection = await _connectionFactory.CreateRemoteConnectionAsync();
                await remoteConnection.OpenAsync();

                // Get all servicing points from remote
                var tables = new List<CachedServicingPoint>();
                using (var command = new MySqlCommand(
                    @"SELECT Post_ID, Description, Post_Number, Active, Active_Order_ID, Reserve, Yper_Main_ID
                    FROM Posts_Main", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tables.Add(new CachedServicingPoint
                        {
                            PostID = reader.GetInt32("Post_ID"),
                            Description = reader.GetString("Description"),
                            Active = reader.GetBoolean("Active"),
                            ActiveOrderID = reader.IsDBNull(reader.GetOrdinal("Active_Order_ID")) ? 0 : reader.GetInt32("Active_Order_ID"),
                            PostNumber = reader.GetInt32("Post_Number"),
                            Reserved = reader.GetBoolean("Reserve"),
                            YperMainID = reader.GetInt32("Yper_Main_ID")
                        });
                    }
                }

                // Get all areas
                var areas = new List<CachedArea>();
                using (var command = new MySqlCommand(
                    "SELECT Yper_Main_ID, Yper_Description, Yper_Icon FROM Yper_Posts", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        areas.Add(new CachedArea
                        {
                            YperMainID = reader.GetInt32("Yper_Main_ID"),
                            Description = reader.GetString("Yper_Description"),
                            IconID = reader.IsDBNull(reader.GetOrdinal("Yper_Icon")) ? 0 : reader.GetInt32("Yper_Icon")
                        });
                    }
                }

                // Open local connection and update cache
                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

                // Clear existing tables and areas
                await localDb.DeleteAllAsync<CachedServicingPoint>();
                await localDb.DeleteAllAsync<CachedArea>();

                // Insert all tables and areas
                foreach (var batch in tables.Chunk(100))
                {
                    await localDb.InsertAllAsync(batch);
                }

                foreach (var batch in areas.Chunk(20))
                {
                    await localDb.InsertAllAsync(batch);
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing tables: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncExtrasAsync()
        {
            try
            {
                // Connect to remote database
                using var remoteConnection = await _connectionFactory.CreateRemoteConnectionAsync();
                await remoteConnection.OpenAsync();

                // Get all extras from remote
                var extras = new List<CachedExtra>();
                using (var command = new MySqlCommand(
                    "SELECT ExtraID, Description, Price FROM Extras", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        extras.Add(new CachedExtra
                        {
                            ExtraId = reader.GetInt32("ExtraID"),
                            Description = reader.GetString("Description"),
                            Price = reader.GetDecimal("Price")
                        });
                    }
                }

                // Open local connection and update cache
                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

                // Clear existing extras
                await localDb.DeleteAllAsync<CachedExtra>();

                // Insert all extras
                foreach (var batch in extras.Chunk(100))
                {
                    await localDb.InsertAllAsync(batch);
                }

                // Get courses (Grouping_Basis)
                var courses = new List<CachedCourse>();
                using (var command = new MySqlCommand(
                    "SELECT Description, Serving_Row FROM Grouping_Basis", remoteConnection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        courses.Add(new CachedCourse
                        {
                            CourseId = reader.GetInt32("Serving_Row"),
                            Description = reader.GetString("Description")
                        });
                    }
                }

                // Clear existing courses
                await localDb.DeleteAllAsync<CachedCourse>();

                // Insert all courses
                await localDb.InsertAllAsync(courses);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing extras: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncOrdersAsync()
        {
            try
            {
                // We only need to upload pending orders, not download existing ones
                // since those are managed by the server

                using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

                // Get all unsynced orders
                var unsyncedOrders = await localDb.Table<CachedOrder>()
                    .Where(o => !o.IsSynced)
                    .ToListAsync();

                if (unsyncedOrders.Count == 0)
                    return true; // Nothing to sync

                // For each unsynced order, get its order items
                foreach (var order in unsyncedOrders)
                {
                    var orderItems = await localDb.Table<CachedOrderItem>()
                        .Where(oi => oi.OrderID == order.OrderID)
                        .ToListAsync();

                    // Upload to server if connected
                    try
                    {
                        using var remoteConnection = await _connectionFactory.CreateRemoteConnectionAsync();
                        await remoteConnection.OpenAsync();

                        // Begin transaction
                        using var transaction = await remoteConnection.BeginTransactionAsync();

                        try
                        {
                            // Insert order
                            using (var orderCmd = new MySqlCommand(@"
                                INSERT INTO Orders 
                                (Order_ID, Time_Date, Clerk_ID, Order_Total, Receipt, History, Served, Post_ID, HasDiscount, DiscountPercentage) 
                                VALUES 
                                (@OrderID, @TimeDate, @ClerkID, @OrderTotal, @Receipt, @History, @Served, @PostID, @HasDiscount, @DiscountPercentage)
                                ON DUPLICATE KEY UPDATE
                                Order_Total = @OrderTotal,
                                HasDiscount = @HasDiscount,
                                DiscountPercentage = @DiscountPercentage", remoteConnection, transaction))
                            {
                                orderCmd.Parameters.AddWithValue("@OrderID", order.OrderID);
                                orderCmd.Parameters.AddWithValue("@TimeDate", order.TimeDate);
                                orderCmd.Parameters.AddWithValue("@ClerkID", order.ClerkID);
                                orderCmd.Parameters.AddWithValue("@OrderTotal", order.OrderTotal ?? 0);
                                orderCmd.Parameters.AddWithValue("@Receipt", order.Receipt);
                                orderCmd.Parameters.AddWithValue("@History", order.History);
                                orderCmd.Parameters.AddWithValue("@Served", order.Served);
                                orderCmd.Parameters.AddWithValue("@PostID", order.PostID ?? 0);
                                orderCmd.Parameters.AddWithValue("@HasDiscount", order.HasDiscount);
                                orderCmd.Parameters.AddWithValue("@DiscountPercentage", order.DiscountPercentage ?? 0);

                                await orderCmd.ExecuteNonQueryAsync();
                            }

                            // Insert order items
                            foreach (var item in orderItems)
                            {
                                using var itemCmd = new MySqlCommand(@"
                                    INSERT INTO Orders_Actual 
                                    (Order_ID_Sub, Order_ID, Product_ID, Quantity, Post_ID, Price, Printer, Receipt, OrderTime, Staff_ID, Serving_Row)
                                    VALUES
                                    (@OrderIDSub, @OrderID, @ProductID, @Quantity, @PostID, @Price, @Printer, @Receipt, @OrderTime, @StaffID, @ServingRow)
                                    ON DUPLICATE KEY UPDATE
                                    Quantity = @Quantity,
                                    Price = @Price", remoteConnection, transaction);

                                itemCmd.Parameters.AddWithValue("@OrderIDSub", item.OrderIDSub);
                                itemCmd.Parameters.AddWithValue("@OrderID", item.OrderID);
                                itemCmd.Parameters.AddWithValue("@ProductID", item.ProductID);
                                itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                itemCmd.Parameters.AddWithValue("@PostID", item.PostID);
                                itemCmd.Parameters.AddWithValue("@Price", item.Price);
                                itemCmd.Parameters.AddWithValue("@Printer", item.Printer);
                                itemCmd.Parameters.AddWithValue("@Receipt", item.Receipt);
                                itemCmd.Parameters.AddWithValue("@OrderTime", item.OrderTime);
                                itemCmd.Parameters.AddWithValue("@StaffID", item.StaffID);
                                itemCmd.Parameters.AddWithValue("@ServingRow", item.ServingRow);

                                await itemCmd.ExecuteNonQueryAsync();

                                // Get extras for this order item
                                var extras = await localDb.Table<CachedOrderExtra>()
                                    .Where(e => e.OrderIDSub == item.OrderIDSub)
                                    .ToListAsync();

                                foreach (var extra in extras)
                                {
                                    using var extraCmd = new MySqlCommand(@"
                                        INSERT INTO Order_Extras_Sub
                                        (Extra_S_ID, Order_ID_Sub, Quantity, Prefix, Extra_ID)
                                        VALUES
                                        (@ExtraID, @OrderIDSub, @Quantity, @Prefix, @ExtraID)
                                        ON DUPLICATE KEY UPDATE
                                        Quantity = @Quantity", remoteConnection, transaction);

                                    extraCmd.Parameters.AddWithValue("@ExtraID", extra.ExtraId);
                                    extraCmd.Parameters.AddWithValue("@OrderIDSub", extra.OrderIDSub);
                                    extraCmd.Parameters.AddWithValue("@Quantity", extra.Quantity);
                                    extraCmd.Parameters.AddWithValue("@Prefix", extra.Prefix ?? "");
                                    extraCmd.Parameters.AddWithValue("@ExtraID", extra.ExtraId);

                                    await extraCmd.ExecuteNonQueryAsync();
                                }
                            }

                            // If table status needs to be updated
                            if (order.PostID.HasValue)
                            {
                                using var tableCmd = new MySqlCommand(@"
                                    UPDATE Posts_Main
                                    SET Active = 1, Active_Order_ID = @OrderID
                                    WHERE Post_ID = @PostID", remoteConnection, transaction);

                                tableCmd.Parameters.AddWithValue("@OrderID", order.OrderID);
                                tableCmd.Parameters.AddWithValue("@PostID", order.PostID.Value);

                                await tableCmd.ExecuteNonQueryAsync();
                            }

                            // Commit transaction
                            await transaction.CommitAsync();

                            // Mark order as synced in local DB
                            order.IsSynced = true;
                            await localDb.UpdateAsync(order);

                            // Mark all order items as synced
                            foreach (var item in orderItems)
                            {
                                item.IsSynced = true;
                                await localDb.UpdateAsync(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            System.Diagnostics.Debug.WriteLine($"Error syncing order {order.OrderID}: {ex.Message}");
                            continue; // Try next order
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error connecting to remote server: {ex.Message}");
                        return false; // Can't sync orders if server is unavailable
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing orders: {ex.Message}");
                return false;
            }
        }

        private void OnSyncProgress(string message, int progress)
        {
            SyncProgress?.Invoke(this, new SyncEventArgs
            {
                Message = message,
                Progress = progress,
                Success = true
            });
        }

        private void OnSyncCompleted(string message, bool success, Exception error = null)
        {
            SyncCompleted?.Invoke(this, new SyncEventArgs
            {
                Message = message,
                Progress = 100,
                Success = success,
                Error = error
            });
        }
    }

    // Adding CachedArea class definition
    [Table("CachedAreas")]
    public class CachedArea
    {
        [PrimaryKey]
        public int YperMainID { get; set; }
        public string Description { get; set; }
        public int IconID { get; set; }
    }

    // Adding CachedCourse class definition
    [Table("CachedCourses")]
    public class CachedCourse
    {
        [PrimaryKey]
        public int CourseId { get; set; }
        public string Description { get; set; }
    }

    // Adding CachedOrderExtra class definition
    [Table("CachedOrderExtras")]
    public class CachedOrderExtra
    {
        [PrimaryKey, AutoIncrement]
        public int ExtraId { get; set; }
        public int OrderIDSub { get; set; }
        public int Quantity { get; set; }
        public string Prefix { get; set; }
        public int ExtraID { get; set; }
        public bool IsSynced { get; set; }
    }
}
