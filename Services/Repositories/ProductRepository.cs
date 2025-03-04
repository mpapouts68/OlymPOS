using OlymPOS.Services.Interfaces;
using OlymPOS.Services.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace OlymPOS.Services.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppSettings _appSettings;
        private readonly ICacheManager _cacheManager;

        public ProductRepository(
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

        public async Task<IEnumerable<Product>> GetAllAsync()
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

        private async Task<IEnumerable<Product>> GetAllFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedProducts = await connection.Table<CachedProduct>().ToListAsync();

            return cachedProducts.Select(Convert);
        }

        private async Task<IEnumerable<Product>> GetAllFromRemoteAsync()
        {
            var products = new List<Product>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                CPrinter, Sale_Lock FROM Products", connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
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
                    Sale_Lock = reader.GetBoolean("Sale_Lock"),
                    Quantity = 0
                });
            }

            return products;
        }

        public async Task<Product> GetByIdAsync(int id)
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

        private async Task<Product> GetByIdFromCacheAsync(int id)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedProduct = await connection.Table<CachedProduct>()
                .Where(p => p.ProductID == id)
                .FirstOrDefaultAsync();

            return cachedProduct != null ? Convert(cachedProduct) : null;
        }

        private async Task<Product> GetByIdFromRemoteAsync(int id)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                CPrinter, Sale_Lock FROM Products 
                WHERE Product_ID = @ProductID", connection);

            command.Parameters.AddWithValue("@ProductID", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Product
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
                    Sale_Lock = reader.GetBoolean("Sale_Lock"),
                    Quantity = 0
                };
            }

            return null;
        }

        public async Task<IEnumerable<Product>> GetByGroupIdAsync(int groupId)
        {
            if (await IsOfflineModeAsync())
            {
                return await GetByGroupIdFromCacheAsync(groupId);
            }
            else
            {
                try
                {
                    return await GetByGroupIdFromRemoteAsync(groupId);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetByGroupIdFromCacheAsync(groupId);
                }
            }
        }

        private async Task<IEnumerable<Product>> GetByGroupIdFromCacheAsync(int groupId)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedProducts = await connection.Table<CachedProduct>()
                .Where(p => p.ProductGroupID == groupId)
                .ToListAsync();

            return cachedProducts.Select(Convert);
        }

        private async Task<IEnumerable<Product>> GetByGroupIdFromRemoteAsync(int groupId)
        {
            var products = new List<Product>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                CPrinter, Sale_Lock FROM Products 
                WHERE ProductGroup_ID = @ProductGroupID", connection);

            command.Parameters.AddWithValue("@ProductGroupID", groupId);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
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
                    Sale_Lock = reader.GetBoolean("Sale_Lock"),
                    Quantity = 0
                });
            }

            return products;
        }

        public async Task<IEnumerable<Product>> GetFavoritesAsync()
        {
            if (await IsOfflineModeAsync())
            {
                return await GetFavoritesFromCacheAsync();
            }
            else
            {
                try
                {
                    return await GetFavoritesFromRemoteAsync();
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await GetFavoritesFromCacheAsync();
                }
            }
        }

        private async Task<IEnumerable<Product>> GetFavoritesFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedProducts = await connection.Table<CachedProduct>()
                .Where(p => p.Favorite)
                .ToListAsync();

            return cachedProducts.Select(Convert);
        }

        private async Task<IEnumerable<Product>> GetFavoritesFromRemoteAsync()
        {
            var products = new List<Product>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                CPrinter, Sale_Lock FROM Products 
                WHERE Favorite = 1", connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
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
                    Sale_Lock = reader.GetBoolean("Sale_Lock"),
                    Quantity = 0
                });
            }

            return products;
        }

        public async Task<IEnumerable<Product>> SearchAsync(string query)
        {
            if (await IsOfflineModeAsync())
            {
                return await SearchFromCacheAsync(query);
            }
            else
            {
                try
                {
                    return await SearchFromRemoteAsync(query);
                }
                catch
                {
                    // Fallback to cache if remote fails
                    return await SearchFromCacheAsync(query);
                }
            }
        }

        private async Task<IEnumerable<Product>> SearchFromCacheAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetFavoritesFromCacheAsync();

            query = query.ToLowerInvariant();

            using var connection = await _connectionFactory.CreateLocalConnectionAsync();

            // SQLite doesn't have great text search capabilities, so we'll just use LIKE
            var cachedProducts = await connection.QueryAsync<CachedProduct>(
                "SELECT * FROM CachedProducts WHERE LOWER(Description) LIKE ? OR LOWER(Description2) LIKE ?",
                $"%{query}%", $"%{query}%");

            return cachedProducts.Select(Convert);
        }

        private async Task<IEnumerable<Product>> SearchFromRemoteAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetFavoritesFromRemoteAsync();

            var products = new List<Product>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT Product_ID, Description, Description2, Price, ProductGroup_ID, 
                Printer, VAT, Build, Extra_ID, Row_Print, Auto_Extra, Has_Options, 
                Extra_ID_Key, Menu_Number, Include_Group, Favorite, Drink_Or_Food, 
                CPrinter, Sale_Lock FROM Products 
                WHERE Description LIKE @Query OR Description2 LIKE @Query", connection);

            command.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                products.Add(new Product
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
                    Sale_Lock = reader.GetBoolean("Sale_Lock"),
                    Quantity = 0
                });
            }

            return products;
        }

        public async Task<bool> AddAsync(Product entity)
        {
            // This is typically an admin operation that would only happen when online
            throw new NotImplementedException("Adding products is not supported in the mobile app");
        }

        public async Task<bool> UpdateAsync(Product entity)
        {
            // This is typically an admin operation that would only happen when online
            throw new NotImplementedException("Updating products is not supported in the mobile app");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // This is typically an admin operation that would only happen when online
            throw new NotImplementedException("Deleting products is not supported in the mobile app");
        }

        // Helper method to convert from cached entity to domain entity
        private Product Convert(CachedProduct cachedProduct)
        {
            return new Product
            {
                ProductID = cachedProduct.ProductID,
                Description = cachedProduct.Description,
                Description2 = cachedProduct.Description2,
                Price = cachedProduct.Price,
                ProductGroupID = cachedProduct.ProductGroupID,
                Printer = cachedProduct.Printer,
                VAT = cachedProduct.VAT,
                Build = cachedProduct.Build,
                Extra_ID = cachedProduct.Extra_ID,
                Row_Print = cachedProduct.Row_Print,
                Auto_Extra = cachedProduct.Auto_Extra,
                Has_Options = cachedProduct.Has_Options,
                Extra_ID_Key = cachedProduct.Extra_ID_Key,
                Menu_Number = cachedProduct.Menu_Number,
                Include_Group = cachedProduct.Include_Group,
                Favorite = cachedProduct.Favorite,
                Drink_Or_Food = cachedProduct.Drink_Or_Food,
                CPrinter = cachedProduct.CPrinter,
                Sale_Lock = cachedProduct.Sale_Lock,
                Quantity = 0
            };
        }
    }
