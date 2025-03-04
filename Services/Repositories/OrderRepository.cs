using OlymPOS.Services.Interfaces;
using OlymPOS.Services.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using SQLite;

namespace OlymPOS.Services.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppSettings _appSettings;
        private readonly ICacheManager _cacheManager;
        private readonly ISyncService _syncService;

        public OrderRepository(
            IDatabaseConnectionFactory connectionFactory,
            IAppSettings appSettings,
            ICacheManager cacheManager,
            ISyncService syncService)
        {
            _connectionFactory = connectionFactory;
            _appSettings = appSettings;
            _cacheManager = cacheManager;
            _syncService = syncService;
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

        public async Task<IEnumerable<Order>> GetAllAsync()
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

        private async Task<IEnumerable<Order>> GetAllFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedOrders = await connection.Table<CachedOrder>().ToListAsync();

            var orders = new List<Order>();
            foreach (var cachedOrder in cachedOrders)
            {
                var order = ConvertOrder(cachedOrder);

                // Get order items for this order
                var orderItems = await connection.Table<CachedOrderItem>()
                    .Where(oi => oi.OrderID == cachedOrder.OrderID)
                    .ToListAsync();

                foreach (var cachedItem in orderItems)
                {
                    var orderItem = ConvertOrderItem(cachedItem);
                    order.OrderItems.Add(orderItem);

                    // Get extras for this order item
                    var orderExtras = await connection.Table<CachedOrderExtra>()
                        .Where(oe => oe.OrderIDSub == cachedItem.OrderIDSub)
                        .ToListAsync();

                    foreach (var cachedExtra in orderExtras)
                    {
                        var extra = new Extra
                        {
                            ExtraId = cachedExtra.ExtraId,
                            Description = cachedExtra.Prefix ?? "",
                            quantity = cachedExtra.Quantity
                        };

                        orderItem.OrderExtras.Add(extra);
                    }
                }

                orders.Add(order);
            }

            return orders;
        }

        private async Task<IEnumerable<Order>> GetAllFromRemoteAsync()
        {
            var orders = new List<Order>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            // Get all orders
            using (var command = new MySqlCommand(
                @"SELECT Order_ID, Time_Date, Clerk_ID, Order_Type, Order_Total, Receipt, 
                History, Served, Post_ID, Post_Number, Name_ID, Customer_ID, HasDiscount, 
                DiscountPercentage, Closed, Closed_Date, Order_Discount_Amount, Order_Total_AfterD 
                FROM Orders", connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var order = new Order
                    {
                        OrderID = reader.GetInt32("Order_ID"),
                        TimeDate = reader.GetDateTime("Time_Date"),
                        ClerkID = reader.GetInt32("Clerk_ID"),
                        OrderType = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? null : reader.GetString("Order_Type"),
                        Description = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? "" : reader.GetString("Order_Type"),
                        OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? (decimal?)null : reader.GetDecimal("Order_Total"),
                        Receipt = reader.GetBoolean("Receipt"),
                        History = reader.GetBoolean("History"),
                        Served = reader.GetBoolean("Served"),
                        PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? (int?)null : reader.GetInt32("Post_ID"),
                        PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? (int?)null : reader.GetInt32("Post_Number"),
                        NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? (int?)null : reader.GetInt32("Name_ID"),
                        CustomerID = reader.IsDBNull(reader.GetOrdinal("Customer_ID")) ? (int?)null : reader.GetInt32("Customer_ID"),
                        HasDiscount = reader.GetBoolean("HasDiscount"),
                        DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? (int?)null : reader.GetInt32("DiscountPercentage"),
                        Closed = reader.GetBoolean("Closed"),
                        ClosedDate = reader.IsDBNull(reader.GetOrdinal("Closed_Date")) ? (DateTime?)null : reader.GetDateTime("Closed_Date"),
                        OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? (double?)null : reader.GetDouble("Order_Discount_Amount"),
                        OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? (double?)null : reader.GetDouble("Order_Total_AfterD")
                    };

                    orders.Add(order);
                }
            }

            // Get order items for each order
            foreach (var order in orders)
            {
                await LoadOrderItemsForOrderFromRemote(connection, order);
            }

            return orders;
        }

        public async Task<Order> GetByIdAsync(int id)
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

        private async Task<Order> GetByIdFromCacheAsync(int id)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();

            var cachedOrder = await connection.Table<CachedOrder>()
                .Where(o => o.OrderID == id)
                .FirstOrDefaultAsync();

            if (cachedOrder == null)
                return null;

            var order = ConvertOrder(cachedOrder);

            // Get order items for this order
            var orderItems = await connection.Table<CachedOrderItem>()
                .Where(oi => oi.OrderID == id)
                .ToListAsync();

            foreach (var cachedItem in orderItems)
            {
                var orderItem = ConvertOrderItem(cachedItem);
                order.OrderItems.Add(orderItem);

                // Get extras for this order item
                var orderExtras = await connection.Table<CachedOrderExtra>()
                    .Where(oe => oe.OrderIDSub == cachedItem.OrderIDSub)
                    .ToListAsync();

                foreach (var cachedExtra in orderExtras)
                {
                    var extra = new Extra
                    {
                        ExtraId = cachedExtra.ExtraId,
                        Description = cachedExtra.Prefix ?? "",
                        quantity = cachedExtra.Quantity
                    };

                    orderItem.OrderExtras.Add(extra);
                }
            }

            return order;
        }

        private async Task<Order> GetByIdFromRemoteAsync(int id)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Order_ID, Time_Date, Clerk_ID, Order_Type, Order_Total, Receipt, 
                History, Served, Post_ID, Post_Number, Name_ID, Customer_ID, HasDiscount, 
                DiscountPercentage, Closed, Closed_Date, Order_Discount_Amount, Order_Total_AfterD 
                FROM Orders WHERE Order_ID = @OrderID", connection);

            command.Parameters.AddWithValue("@OrderID", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    ClerkID = reader.GetInt32("Clerk_ID"),
                    OrderType = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? null : reader.GetString("Order_Type"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? "" : reader.GetString("Order_Type"),
                    OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? (decimal?)null : reader.GetDecimal("Order_Total"),
                    Receipt = reader.GetBoolean("Receipt"),
                    History = reader.GetBoolean("History"),
                    Served = reader.GetBoolean("Served"),
                    PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? (int?)null : reader.GetInt32("Post_ID"),
                    PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? (int?)null : reader.GetInt32("Post_Number"),
                    NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? (int?)null : reader.GetInt32("Name_ID"),
                    CustomerID = reader.IsDBNull(reader.GetOrdinal("Customer_ID")) ? (int?)null : reader.GetInt32("Customer_ID"),
                    HasDiscount = reader.GetBoolean("HasDiscount"),
                    DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? (int?)null : reader.GetInt32("DiscountPercentage"),
                    Closed = reader.GetBoolean("Closed"),
                    ClosedDate = reader.IsDBNull(reader.GetOrdinal("Closed_Date")) ? (DateTime?)null : reader.GetDateTime("Closed_Date"),
                    OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? (double?)null : reader.GetDouble("Order_Discount_Amount"),
                    OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? (double?)null : reader.GetDouble("Order_Total_AfterD")
                };

                await LoadOrderItemsForOrderFromRemote(connection, order);

                return order;
            }

            return null;
        }

        private async Task LoadOrderItemsForOrderFromRemote(MySqlConnection connection, Order order)
        {
            using var command = new MySqlCommand(
                @"SELECT Order_ID_Sub, Order_ID, Product_ID, Unit, Quantity, Post_ID, 
                Name_ID, Price, DescriptionEx, DescriptionEx_UK, Printer, Receipt, 
                OrderTime, Staff_ID, Personel_Closed, Free, Price_free, Cancelled, 
                Order_By, Serving_Row, Has_Extra, Served
                FROM Orders_Actual WHERE Order_ID = @OrderID", connection);

            command.Parameters.AddWithValue("@OrderID", order.OrderID);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var orderItem = new OrderItem
                {
                    OrderIDSub = reader.GetInt32("Order_ID_Sub"),
                    OrderID = reader.GetDouble("Order_ID"),
                    ProductID = reader.GetInt32("Product_ID"),
                    Unit = reader.IsDBNull(reader.GetOrdinal("Unit")) ? null : reader.GetString("Unit"),
                    Quantity = reader.GetInt32("Quantity"),
                    PostID = reader.GetInt32("Post_ID"),
                    NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? 0 : reader.GetInt32("Name_ID"),
                    Price = reader.GetDecimal("Price"),
                    Description = reader.IsDBNull(reader.GetOrdinal("DescriptionEx")) ? "" : reader.GetString("DescriptionEx"),
                    DescriptionEx_UK = reader.IsDBNull(reader.GetOrdinal("DescriptionEx_UK")) ? null : reader.GetString("DescriptionEx_UK"),
                    Printer = reader.GetBoolean("Printer"),
                    Receipt = reader.GetBoolean("Receipt"),
                    OrderTime = reader.GetDateTime("OrderTime"),
                    StaffID = reader.IsDBNull(reader.GetOrdinal("Staff_ID")) ? null : reader.GetString("Staff_ID"),
                    PersonelClosed = reader.GetBoolean("Personel_Closed"),
                    Free = reader.GetBoolean("Free"),
                    Pricefree = reader.IsDBNull(reader.GetOrdinal("Price_free")) ? 0 : reader.GetDecimal("Price_free"),
                    Cancelled = reader.GetBoolean("Cancelled"),
                    OrderBy = reader.IsDBNull(reader.GetOrdinal("Order_By")) ? null : reader.GetString("Order_By"),
                    ServingRow = reader.GetInt32("Serving_Row"),
                    HasExtra = reader.GetBoolean("Has_Extra"),
                    Served = reader.GetBoolean("Served")
                };

                order.OrderItems.Add(orderItem);

                // If the item has extras, load them
                if (orderItem.HasExtra)
                {
                    await LoadExtrasForOrderItemFromRemote(connection, orderItem);
                }
            }
        }

        private async Task LoadExtrasForOrderItemFromRemote(MySqlConnection connection, OrderItem orderItem)
        {
            using var command = new MySqlCommand(
                @"SELECT Extra_S_ID, Order_ID_Sub, Quantity, Prefix, Extra_ID 
                FROM Order_Extras_Sub WHERE Order_ID_Sub = @OrderIDSub", connection);

            command.Parameters.AddWithValue("@OrderIDSub", orderItem.OrderIDSub);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var extra = new Extra
                {
                    ExtraId = reader.GetInt32("Extra_ID"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Prefix")) ? "" : reader.GetString("Prefix"),
                    quantity = reader.GetInt32("Quantity"),
                    OrderIDSub = reader.GetInt32("Order_ID_Sub")
                };

                orderItem.OrderExtras.Add(extra);
            }
        }

        public async Task<IEnumerable<Order>> GetActiveOrdersAsync()
        {
            if (await IsOfflineModeAsync())
            {
                return await GetActiveOrdersFromCacheAsync();
            }
            else
            {
                try
                {
                    return await GetActiveOrdersFromRemoteAsync();
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetActiveOrdersFromCacheAsync();
                }
            }
        }

        private async Task<IEnumerable<Order>> GetActiveOrdersFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedOrders = await connection.Table<CachedOrder>()
                .Where(o => !o.Closed && !o.History)
                .ToListAsync();

            var orders = new List<Order>();
            foreach (var cachedOrder in cachedOrders)
            {
                var order = await GetByIdFromCacheAsync(cachedOrder.OrderID);
                if (order != null)
                {
                    orders.Add(order);
                }
            }

            return orders;
        }

        private async Task<IEnumerable<Order>> GetActiveOrdersFromRemoteAsync()
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            var orders = new List<Order>();

            using var command = new MySqlCommand(
                @"SELECT Order_ID, Time_Date, Clerk_ID, Order_Type, Order_Total, Receipt, 
                History, Served, Post_ID, Post_Number, Name_ID, Customer_ID, HasDiscount, 
                DiscountPercentage, Closed, Closed_Date, Order_Discount_Amount, Order_Total_AfterD 
                FROM Orders WHERE Closed = 0 AND History = 0", connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    ClerkID = reader.GetInt32("Clerk_ID"),
                    OrderType = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? null : reader.GetString("Order_Type"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? "" : reader.GetString("Order_Type"),
                    OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? (decimal?)null : reader.GetDecimal("Order_Total"),
                    Receipt = reader.GetBoolean("Receipt"),
                    History = reader.GetBoolean("History"),
                    Served = reader.GetBoolean("Served"),
                    PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? (int?)null : reader.GetInt32("Post_ID"),
                    PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? (int?)null : reader.GetInt32("Post_Number"),
                    NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? (int?)null : reader.GetInt32("Name_ID"),
                    CustomerID = reader.IsDBNull(reader.GetOrdinal("Customer_ID")) ? (int?)null : reader.GetInt32("Customer_ID"),
                    HasDiscount = reader.GetBoolean("HasDiscount"),
                    DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? (int?)null : reader.GetInt32("DiscountPercentage"),
                    Closed = reader.GetBoolean("Closed"),
                    ClosedDate = reader.IsDBNull(reader.GetOrdinal("Closed_Date")) ? (DateTime?)null : reader.GetDateTime("Closed_Date"),
                    OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? (double?)null : reader.GetDouble("Order_Discount_Amount"),
                    OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? (double?)null : reader.GetDouble("Order_Total_AfterD")
                };

                await LoadOrderItemsForOrderFromRemote(connection, order);

                orders.Add(order);
            }

            return orders;
        }

        public async Task<IEnumerable<Order>> GetByClerkIdAsync(int clerkId)
        {
            if (await IsOfflineModeAsync())
            {
                return await GetByClerkIdFromCacheAsync(clerkId);
            }
            else
            {
                try
                {
                    return await GetByClerkIdFromRemoteAsync(clerkId);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetByClerkIdFromCacheAsync(clerkId);
                }
            }
        }

        private async Task<IEnumerable<Order>> GetByClerkIdFromCacheAsync(int clerkId)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedOrders = await connection.Table<CachedOrder>()
                .Where(o => o.ClerkID == clerkId)
                .ToListAsync();

            var orders = new List<Order>();
            foreach (var cachedOrder in cachedOrders)
            {
                var order = await GetByIdFromCacheAsync(cachedOrder.OrderID);
                if (order != null)
                {
                    orders.Add(order);
                }
            }

            return orders;
        }

        private async Task<IEnumerable<Order>> GetByClerkIdFromRemoteAsync(int clerkId)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            var orders = new List<Order>();

            using var command = new MySqlCommand(
                @"SELECT Order_ID, Time_Date, Clerk_ID, Order_Type, Order_Total, Receipt, 
                History, Served, Post_ID, Post_Number, Name_ID, Customer_ID, HasDiscount, 
                DiscountPercentage, Closed, Closed_Date, Order_Discount_Amount, Order_Total_AfterD 
                FROM Orders WHERE Clerk_ID = @ClerkID", connection);

            command.Parameters.AddWithValue("@ClerkID", clerkId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    ClerkID = reader.GetInt32("Clerk_ID"),
                    OrderType = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? null : reader.GetString("Order_Type"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? "" : reader.GetString("Order_Type"),
                    OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? (decimal?)null : reader.GetDecimal("Order_Total"),
                    Receipt = reader.GetBoolean("Receipt"),
                    History = reader.GetBoolean("History"),
                    Served = reader.GetBoolean("Served"),
                    PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? (int?)null : reader.GetInt32("Post_ID"),
                    PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? (int?)null : reader.GetInt32("Post_Number"),
                    NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? (int?)null : reader.GetInt32("Name_ID"),
                    CustomerID = reader.IsDBNull(reader.GetOrdinal("Customer_ID")) ? (int?)null : reader.GetInt32("Customer_ID"),
                    HasDiscount = reader.GetBoolean("HasDiscount"),
                    DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? (int?)null : reader.GetInt32("DiscountPercentage"),
                    Closed = reader.GetBoolean("Closed"),
                    ClosedDate = reader.IsDBNull(reader.GetOrdinal("Closed_Date")) ? (DateTime?)null : reader.GetDateTime("Closed_Date"),
                    OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? (double?)null : reader.GetDouble("Order_Discount_Amount"),
                    OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? (double?)null : reader.GetDouble("Order_Total_AfterD")
                };

                await LoadOrderItemsForOrderFromRemote(connection, order);

                orders.Add(order);
            }

            return orders;
        }

        public async Task<IEnumerable<Order>> GetByTableIdAsync(int tableId)
        {
            if (await IsOfflineModeAsync())
            {
                return await GetByTableIdFromCacheAsync(tableId);
            }
            else
            {
                try
                {
                    return await GetByTableIdFromRemoteAsync(tableId);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetByTableIdFromCacheAsync(tableId);
                }
            }
        }

        private async Task<IEnumerable<Order>> GetByTableIdFromCacheAsync(int tableId)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedOrders = await connection.Table<CachedOrder>()
                .Where(o => o.PostID == tableId)
                .ToListAsync();

            var orders = new List<Order>();
            foreach (var cachedOrder in cachedOrders)
            {
                var order = await GetByIdFromCacheAsync(cachedOrder.OrderID);
                if (order != null)
                {
                    orders.Add(order);
                }
            }

            return orders;
        }

        private async Task<IEnumerable<Order>> GetByTableIdFromRemoteAsync(int tableId)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            var orders = new List<Order>();

            using var command = new MySqlCommand(
                @"SELECT Order_ID, Time_Date, Clerk_ID, Order_Type, Order_Total, Receipt, 
                History, Served, Post_ID, Post_Number, Name_ID, Customer_ID, HasDiscount, 
                DiscountPercentage, Closed, Closed_Date, Order_Discount_Amount, Order_Total_AfterD 
                FROM Orders WHERE Post_ID = @PostID", connection);

            command.Parameters.AddWithValue("@PostID", tableId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    ClerkID = reader.GetInt32("Clerk_ID"),
                    OrderType = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? null : reader.GetString("Order_Type"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Order_Type")) ? "" : reader.GetString("Order_Type"),
                    OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? (decimal?)null : reader.GetDecimal("Order_Total"),
                    Receipt = reader.GetBoolean("Receipt"),
                    History = reader.GetBoolean("History"),
                    Served = reader.GetBoolean("Served"),
                    PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? (int?)null : reader.GetInt32("Post_ID"),
                    PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? (int?)null : reader.GetInt32("Post_Number"),
                    NameID = reader.IsDBNull(reader.GetOrdinal("Name_ID")) ? (int?)null : reader.GetInt32("Name_ID"),
                    CustomerID = reader.IsDBNull(reader.GetOrdinal("Customer_ID")) ? (int?)null : reader.GetInt32("Customer_ID"),
                    HasDiscount = reader.GetBoolean("HasDiscount"),
                    DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? (int?)null : reader.GetInt32("DiscountPercentage"),
                    Closed = reader.GetBoolean("Closed"),
                    ClosedDate = reader.IsDBNull(reader.GetOrdinal("Closed_Date")) ? (DateTime?)null : reader.GetDateTime("Closed_Date"),
                    OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? (double?)null : reader.GetDouble("Order_Discount_Amount"),
                    OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? (double?)null : reader.GetDouble("Order_Total_AfterD")
                };

                await LoadOrderItemsForOrderFromRemote(connection, order);

                orders.Add(order);
            }

            return orders;
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            var order = await GetByIdAsync(orderId);
            return order?.OrderItems ?? new List<OrderItem>();
        }

        public async Task<bool> AddAsync(Order entity)
        {
            // Always save to local cache first
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                // Check if order already exists
                var existingOrder = await localDb.Table<CachedOrder>()
                    .Where(o => o.OrderID == entity.OrderID)
                    .FirstOrDefaultAsync();

                var cachedOrder = new CachedOrder
                {
                    OrderID = entity.OrderID,
                    TimeDate = entity.TimeDate,
                    ClerkID = entity.ClerkID,
                    OrderType = entity.OrderType,
                    Description = entity.Description,
                    OrderTotal = entity.OrderTotal,
                    Receipt = entity.Receipt,
                    History = entity.History,
                    Served = entity.Served,
                    PostID = entity.PostID,
                    PostNumber = entity.PostNumber,
                    NameID = entity.NameID,
                    CustomerID = entity.CustomerID,
                    HasDiscount = entity.HasDiscount,
                    DiscountPercentage = entity.DiscountPercentage,
                    Closed = entity.Closed,
                    ClosedDate = entity.ClosedDate,
                    OrderDiscountAmount = entity.OrderDiscountAmount,
                    OrderTotalAfterD = entity.OrderTotalAfterD,
                    IsSynced = false
                };

                if (existingOrder != null)
                {
                    // Update existing order
                    cachedOrder.IsSynced = existingOrder.IsSynced;
                    await localDb.UpdateAsync(cachedOrder);
                }
                else
                {
                    // Insert new order
                    await localDb.InsertAsync(cachedOrder);
                }

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing order to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding order to local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddOrderItemAsync(OrderItem item)
        {
            // Always save to local cache first
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                // Check if order item already exists
                var existingItem = await localDb.Table<CachedOrderItem>()
                    .Where(i => i.OrderIDSub == item.OrderIDSub)
                    .FirstOrDefaultAsync();

                var cachedItem = new CachedOrderItem
                {
                    OrderIDSub = item.OrderIDSub,
                    OrderID = item.OrderID,
                    ProductID = item.ProductID,
                    Unit = item.Unit,
                    Quantity = item.Quantity,
                    PostID = item.PostID,
                    NameID = item.NameID,
                    Price = item.Price,
                    Description = item.Description,
                    DescriptionEx_UK = item.DescriptionEx_UK,
                    Printer = item.Printer,
                    Receipt = item.Receipt,
                    OrderTime = item.OrderTime,
                    StaffID = item.StaffID,
                    IsSynced = false
                };

                if (existingItem != null)
                {
                    // Update existing item
                    cachedItem.IsSynced = existingItem.IsSynced;
                    await localDb.UpdateAsync(cachedItem);
                }
                else
                {
                    // Insert new item
                    await localDb.InsertAsync(cachedItem);
                }

                // Add extras if any
                if (item.OrderExtras != null && item.OrderExtras.Count > 0)
                {
                    foreach (var extra in item.OrderExtras)
                    {
                        var cachedExtra = new CachedOrderExtra
                        {
                            OrderIDSub = item.OrderIDSub,
                            ExtraId = extra.ExtraId,
                            Quantity = extra.quantity,
                            Prefix = extra.Description,
                            IsSynced = false
                        };

                        await localDb.InsertOrReplaceAsync(cachedExtra);
                    }
                }

                // Update order totals
                var orderId = (int)item.OrderID;
                var order = await GetByIdFromCacheAsync(orderId);
                if (order != null)
                {
                    // Recalculate order total
                    decimal total = 0;
                    foreach (var orderItem in order.OrderItems)
                    {
                        total += orderItem.Price * orderItem.Quantity;
                    }

                    order.OrderTotal = total;
                    await UpdateAsync(order);
                }

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing order item to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding order item to local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateOrderItemAsync(OrderItem item)
        {
            // Same implementation as adding, since we use InsertOrReplace
            return await AddOrderItemAsync(item);
        }

        public async Task<bool> DeleteOrderItemAsync(int itemId)
        {
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                // Find the item
                var cachedItem = await localDb.Table<CachedOrderItem>()
                    .Where(i => i.OrderIDSub == itemId)
                    .FirstOrDefaultAsync();

                if (cachedItem == null)
                    return false;

                // Delete extras for this item
                await localDb.ExecuteAsync("DELETE FROM CachedOrderExtras WHERE OrderIDSub = ?", itemId);

                // Mark item as cancelled instead of deleting it
                cachedItem.Cancelled = true;
                cachedItem.IsSynced = false;
                await localDb.UpdateAsync(cachedItem);

                // Update order totals
                var orderId = (int)cachedItem.OrderID;
                var order = await GetByIdFromCacheAsync(orderId);
                if (order != null)
                {
                    // Recalculate order total
                    decimal total = 0;
                    foreach (var orderItem in order.OrderItems.Where(i => !i.Cancelled))
                    {
                        total += orderItem.Price * orderItem.Quantity;
                    }

                    order.OrderTotal = total;
                    await UpdateAsync(order);
                }

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing order item deletion to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting order item from local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ApplyDiscountAsync(int orderId, decimal discountPercentage)
        {
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                // Find the order
                var cachedOrder = await localDb.Table<CachedOrder>()
                    .Where(o => o.OrderID == orderId)
                    .FirstOrDefaultAsync();

                if (cachedOrder == null)
                    return false;

                // Update discount
                cachedOrder.HasDiscount = true;
                cachedOrder.DiscountPercentage = (int)discountPercentage;

                // Calculate discount amount
                if (cachedOrder.OrderTotal.HasValue)
                {
                    decimal orderTotal = cachedOrder.OrderTotal.Value;
                    double discountAmount = (double)(orderTotal * (decimal)discountPercentage / 100);
                    double totalAfterDiscount = (double)orderTotal - discountAmount;

                    cachedOrder.OrderDiscountAmount = discountAmount;
                    cachedOrder.OrderTotalAfterD = totalAfterDiscount;
                }

                cachedOrder.IsSynced = false;
                await localDb.UpdateAsync(cachedOrder);

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing discount to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying discount in local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CloseOrderAsync(int orderId, PaymentDetails payment)
        {
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                // Find the order
                var cachedOrder = await localDb.Table<CachedOrder>()
                    .Where(o => o.OrderID == orderId)
                    .FirstOrDefaultAsync();

                if (cachedOrder == null)
                    return false;

                // Update order status
                cachedOrder.Closed = true;
                cachedOrder.ClosedDate = DateTime.Now;
                cachedOrder.CashAmount = payment.CashAmount;
                cachedOrder.CardAmount = payment.CardAmount;
                cachedOrder.VoucherAmount = payment.VoucherAmount;
                cachedOrder.IsSynced = false;

                await localDb.UpdateAsync(cachedOrder);

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing closed order to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing order in local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateAsync(Order entity)
        {
            // Similar to Add, but we'll make sure to update
            using var localDb = await _connectionFactory.CreateLocalConnectionAsync();

            try
            {
                var cachedOrder = await localDb.Table<CachedOrder>()
                    .Where(o => o.OrderID == entity.OrderID)
                    .FirstOrDefaultAsync();

                if (cachedOrder == null)
                {
                    // Order doesn't exist, use Add instead
                    return await AddAsync(entity);
                }

                // Update the order
                cachedOrder.TimeDate = entity.TimeDate;
                cachedOrder.ClerkID = entity.ClerkID;
                cachedOrder.OrderType = entity.OrderType;
                cachedOrder.Description = entity.Description;
                cachedOrder.OrderTotal = entity.OrderTotal;
                cachedOrder.Receipt = entity.Receipt;
                cachedOrder.History = entity.History;
                cachedOrder.Served = entity.Served;
                cachedOrder.PostID = entity.PostID;
                cachedOrder.PostNumber = entity.PostNumber;
                cachedOrder.NameID = entity.NameID;
                cachedOrder.CustomerID = entity.CustomerID;
                cachedOrder.HasDiscount = entity.HasDiscount;
                cachedOrder.DiscountPercentage = entity.DiscountPercentage;
                cachedOrder.Closed = entity.Closed;
                cachedOrder.ClosedDate = entity.ClosedDate;
                cachedOrder.OrderDiscountAmount = entity.OrderDiscountAmount;
                cachedOrder.OrderTotalAfterD = entity.OrderTotalAfterD;
                cachedOrder.IsSynced = false;

                await localDb.UpdateAsync(cachedOrder);

                // Try to sync to remote if online
                if (!await IsOfflineModeAsync())
                {
                    try
                    {
                        await _syncService.SyncOrdersAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error syncing updated order to remote: {ex.Message}");
                        // We'll still consider this a success since it's in the local cache
                        return true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating order in local cache: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // We don't actually delete orders, we just mark them as closed
            var order = await GetByIdAsync(id);
            if (order != null)
            {
                order.Closed = true;
                order.ClosedDate = DateTime.Now;
                return await UpdateAsync(order);
            }

            return false;
        }

        // Helper methods to convert between cached and domain models
        private Order ConvertOrder(CachedOrder cachedOrder)
        {
            return new Order
            {
                OrderID = cachedOrder.OrderID,
                TimeDate = cachedOrder.TimeDate,
                ClerkID = cachedOrder.ClerkID,
                OrderType = cachedOrder.OrderType,
                Description = cachedOrder.Description,
                OrderTotal = cachedOrder.OrderTotal,
                Receipt = cachedOrder.Receipt,
                History = cachedOrder.History,
                Served = cachedOrder.Served,
                PostID = cachedOrder.PostID,
                PostNumber = cachedOrder.PostNumber,
                NameID = cachedOrder.NameID,
                CustomerID = cachedOrder.CustomerID,
                HasDiscount = cachedOrder.HasDiscount,
                DiscountPercentage = cachedOrder.DiscountPercentage,
                Closed = cachedOrder.Closed,
                ClosedDate = cachedOrder.ClosedDate,
                OrderDiscountAmount = cachedOrder.OrderDiscountAmount,
                OrderTotalAfterD = cachedOrder.OrderTotalAfterD,
                SplitPayment = cachedOrder.SplitPayment,
                CatIDOpen = cachedOrder.CatIDOpen,
                CatIDClose = cachedOrder.CatIDClose,
                VATHigh = cachedOrder.VATHigh,
                VATLOw = cachedOrder.VATLOw,
                EmployeeID = cachedOrder.EmployeeID,
                IDPool = cachedOrder.IDPool,
                NumberOfPersons = cachedOrder.NumberOfPersons,
                CashAmount = cachedOrder.CashAmount,
                CardAmount = cachedOrder.CardAmount,
                VoucherAmount = cachedOrder.VoucherAmount
            };
        }

        private OrderItem ConvertOrderItem(CachedOrderItem cachedItem)
        {
            return new OrderItem
            {
                OrderIDSub = cachedItem.OrderIDSub,
                OrderID = cachedItem.OrderID,
                Description = cachedItem.Description,
                ProductID = cachedItem.ProductID,
                Unit = cachedItem.Unit,
                Quantity = cachedItem.Quantity,
                PostID = cachedItem.PostID,
                NameID = cachedItem.NameID,
                Price = cachedItem.Price,
                DescriptionEx_UK = cachedItem.DescriptionEx_UK,
                Printer = cachedItem.Printer,
                Receipt = cachedItem.Receipt,
                OrderTime = cachedItem.OrderTime,
                StaffID = cachedItem.StaffID,
                PersonelClosed = cachedItem.PersonelClosed,
                Free = cachedItem.Free,
                Pricefree = cachedItem.Pricefree,
                Cancelled = cachedItem.Cancelled,
                OrderBy = cachedItem.OrderBy,
                ServingRow = cachedItem.ServingRow,
                HasExtra = cachedItem.HasExtra,
                Served = cachedItem.Served,
                PartECR = cachedItem.PartECR
            };
        }
    }
}