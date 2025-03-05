using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Models;
using OlymPOS.Services.Interfaces;
using OlymPOS.Caching;

namespace OlymPOS.Repositories
{
    public class ProductGroupRepository : IProductGroupRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IAppSettings _appSettings;
        private readonly ICacheManager _cacheManager;

        public ProductGroupRepository(
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

        public async Task<IEnumerable<ProductGroup>> GetAllAsync()
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

        private async Task<IEnumerable<ProductGroup>> GetAllFromCacheAsync()
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedGroups = await connection.Table<CachedProductGroup>().ToListAsync();

            var groups = new List<ProductGroup>();
            foreach (var cachedGroup in cachedGroups)
            {
                groups.Add(new ProductGroup
                {
                    ProductGroupID = cachedGroup.ProductGroupID,
                    Description = cachedGroup.Description,
                    Description2 = cachedGroup.Description2,
                    View = cachedGroup.View,
                    ViewOrder = cachedGroup.ViewOrder,
                    Extra_ID = cachedGroup.Extra_ID,
                    Icon_ID = cachedGroup.Icon_ID,
                    ISSub = cachedGroup.ISSub,
                    Sub_From_GroupID = cachedGroup.Sub_From_GroupID,
                    Has_Sub = cachedGroup.Has_Sub,
                    Subcategories = new System.Collections.ObjectModel.ObservableCollection<ProductGroup>()
                });
            }

            // Build subcategory relationships
            foreach (var group in groups)
            {
                if (group.Sub_From_GroupID.HasValue)
                {
                    var parent = groups.FirstOrDefault(g => g.ProductGroupID == group.Sub_From_GroupID.Value);
                    if (parent != null)
                    {
                        parent.Subcategories.Add(group);
                    }
                }
            }

            // Return only root level categories
            return groups.Where(g => !g.Sub_From_GroupID.HasValue || !groups.Any(p => p.ProductGroupID == g.Sub_From_GroupID.Value));
        }

        private async Task<IEnumerable<ProductGroup>> GetAllFromRemoteAsync()
        {
            var groups = new List<ProductGroup>();

            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT ProductGroup_ID, Description, Description2, View, ViewOrder, 
                  Extra_ID, Icon_ID, ISSub, Sub_From_GroupID, Has_Sub 
                  FROM ProductGroups", connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                groups.Add(new ProductGroup
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
                    Has_Sub = reader.GetBoolean("Has_Sub"),
                    Subcategories = new System.Collections.ObjectModel.ObservableCollection<ProductGroup>()
                });
            }

            // Build subcategory relationships
            foreach (var group in groups)
            {
                if (group.Sub_From_GroupID.HasValue)
                {
                    var parent = groups.FirstOrDefault(g => g.ProductGroupID == group.Sub_From_GroupID.Value);
                    if (parent != null)
                    {
                        parent.Subcategories.Add(group);
                    }
                }
            }

            // Return only root level categories
            return groups.Where(g => !g.Sub_From_GroupID.HasValue || !groups.Any(p => p.ProductGroupID == g.Sub_From_GroupID.Value));
        }

        public async Task<ProductGroup> GetByIdAsync(int id)
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

        private async Task<ProductGroup> GetByIdFromCacheAsync(int id)
        {
            using var connection = await _connectionFactory.CreateLocalConnectionAsync();
            var cachedGroup = await connection.Table<CachedProductGroup>()
                .Where(g => g.ProductGroupID == id)
                .FirstOrDefaultAsync();

            if (cachedGroup == null)
                return null;

            return new ProductGroup
            {
                ProductGroupID = cachedGroup.ProductGroupID,
                Description = cachedGroup.Description,
                Description2 = cachedGroup.Description2,
                View = cachedGroup.View,
                ViewOrder = cachedGroup.ViewOrder,
                Extra_ID = cachedGroup.Extra_ID,
                Icon_ID = cachedGroup.Icon_ID,
                ISSub = cachedGroup.ISSub,
                Sub_From_GroupID = cachedGroup.Sub_From_GroupID,
                Has_Sub = cachedGroup.Has_Sub,
                Subcategories = new System.Collections.ObjectModel.ObservableCollection<ProductGroup>()
            };
        }

        private async Task<ProductGroup> GetByIdFromRemoteAsync(int id)
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            using var command = new MySqlCommand(
                @"SELECT ProductGroup_ID, Description, Description2, View, ViewOrder, 
                  Extra_ID, Icon_ID, ISSub, Sub_From_GroupID, Has_Sub 
                  FROM ProductGroups 
                  WHERE ProductGroup_ID = @ProductGroupID", connection);

            command.Parameters.AddWithValue("@ProductGroupID", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ProductGroup
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
                    Has_Sub = reader.GetBoolean("Has_Sub"),
                    Subcategories = new System.Collections.ObjectModel.ObservableCollection<ProductGroup>()
                };
            }

            return null;
        }

        public async Task<IEnumerable<ProductGroup>> GetRootGroupsAsync()
        {
            var allGroups = await GetAllAsync();
            return allGroups.Where(g => !g.ISSub);
        }

        public async Task<IEnumerable<ProductGroup>> GetSubgroupsAsync(int parentGroupId)
        {
            var allGroups = await GetAllAsync();
            return allGroups.Where(g => g.Sub_From_GroupID == parentGroupId);
        }

        public async Task<int> AddAsync(ProductGroup entity)
        {
            // This operation is not supported on mobile
            throw new NotSupportedException("Adding product groups is not supported in the mobile app");
        }

        public async Task<bool> UpdateAsync(ProductGroup entity)
        {
            // This operation is not supported on mobile
            throw new NotSupportedException("Updating product groups is not supported in the mobile app");
        }

        public async Task<bool> DeleteAsync(int id)
        {
            // This operation is not supported on mobile
            throw new NotSupportedException("Deleting product groups is not supported in the mobile app");
        }
    }
}
