using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Services.Interfaces;
using OlymPOS.Caching;

namespace OlymPOS.Services
{
    public class DataService : IDataService
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ICacheManager _cacheManager;
        private readonly IAppSettings _appSettings;

        private static DataService _instance;
        public static DataService Instance => _instance ?? (_instance = new DataService());

        public ObservableCollection<ProductGroup> ProductCategories { get; private set; }
        public ObservableCollection<Product> AllProducts { get; private set; }
        public ObservableCollection<Course> Courses { get; private set; }

        // Constructor for dependency injection
        public DataService(
            IDatabaseConnectionFactory connectionFactory = null,
            ICacheManager cacheManager = null,
            IAppSettings appSettings = null)
        {
            _connectionFactory = connectionFactory;
            _cacheManager = cacheManager;
            _appSettings = appSettings;

            ProductCategories = new ObservableCollection<ProductGroup>();
            AllProducts = new ObservableCollection<Product>();
            Courses = new ObservableCollection<Course>();

            _instance = this;
        }

        private async Task<bool> IsOfflineModeAsync()
        {
            // If dependencies aren't injected, assume we're using direct database access
            if (_appSettings == null)
                return false;

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

        public async Task LoadAllDataAsync()
        {
            try
            {
                // If we have the caching infrastructure, try to use it
                if (_connectionFactory != null && _cacheManager != null)
                {
                    bool useOffline = await IsOfflineModeAsync();

                    // If offline, load from cache
                    if (useOffline)
                    {
                        await LoadAllFromCacheAsync();
                    }
                    else
                    {
                        // Try to load from remote, fall back to cache if it fails
                        try
                        {
                            await LoadAllFromRemoteAsync();
                        }
                        catch
                        {
                            await LoadAllFromCacheAsync();
                        }
                    }
                }
                else
                {
                    // Fall back to the original implementation
                    await LoadDirectFromDatabaseAsync();
                }

                // Organize the data after loading
                OrganizeProductGroups();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
                throw;
            }
        }

        private async Task LoadAllFromCacheAsync()
        {
            await _cacheManager.InitializeAsync();

            using var connection = await _connectionFactory.CreateLocalConnectionAsync();

            // Load product categories
            var cachedGroups = await connection.Table<CachedProductGroup>().ToListAsync();
            ProductCategories.Clear();
            foreach (var cachedGroup in cachedGroups)
            {
                ProductCategories.Add(new ProductGroup
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
                    Subcategories = new ObservableCollection<ProductGroup>()
                });
            }

            // Load products
            var cachedProducts = await connection.Table<CachedProduct>().ToListAsync();
            AllProducts.Clear();
            foreach (var cachedProduct in cachedProducts)
            {
                AllProducts.Add(new Product
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
                });
            }

            // Load courses
            var cachedCourses = await connection.Table<CachedCourse>().ToListAsync();
            Courses.Clear();
            foreach (var cachedCourse in cachedCourses)
            {
                Courses.Add(new Course
                {
                    CourseId = cachedCourse.CourseId,
                    Description = cachedCourse.Description
                });
            }
        }

        private async Task LoadAllFromRemoteAsync()
        {
            using var connection = await _connectionFactory.CreateRemoteConnectionAsync();
            await connection.OpenAsync();

            // Load product categories
            await LoadProductCategoriesFromDb(connection);

            // Load products
            await LoadProductsFromDb(connection);

            // Load courses
            await LoadCoursesFromDb(connection);

            // Update the cache with the loaded data
            await UpdateCacheAsync();
        }

        private async Task LoadDirectFromDatabaseAsync()
        {
            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            await connection.OpenAsync();

            // Load product categories
            await LoadProductCategoriesFromDb(connection);

            // Load products
            await LoadProductsFromDb(connection);

            // Load courses
            await LoadCoursesFromDb(connection);
        }

        private async Task LoadProductCategoriesFromDb(MySqlConnection connection)
        {
            ProductCategories.Clear();
            var query = "SELECT * FROM ProductGroups";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ProductCategories.Add(new ProductGroup
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
                    Subcategories = new ObservableCollection<ProductGroup>()
                });
            }
        }

        private async Task LoadProductsFromDb(MySqlConnection connection)
        {
            AllProducts.Clear();
            var query = "SELECT * FROM Products";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                AllProducts.Add(new Product
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
        }

        private async Task LoadCoursesFromDb(MySqlConnection connection)
        {
            Courses.Clear();
            var query = "SELECT * FROM Grouping_Basis";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Courses.Add(new Course
                {
                    CourseId = reader.GetInt32("Serving_Row"),
                    Description = reader.GetString("Description")
                });
            }
        }

        private async Task UpdateCacheAsync()
        {
            if (_cacheManager == null || _connectionFactory == null)
                return;

            await _cacheManager.InitializeAsync();

            using var connection = await _connectionFactory.CreateLocalConnectionAsync();

            // Clear existing data
            await connection.DeleteAllAsync<CachedProductGroup>();
            await connection.DeleteAllAsync<CachedProduct>();
            await connection.DeleteAllAsync<CachedCourse>();

            // Insert product groups
            foreach (var group in ProductCategories)
            {
                await connection.InsertAsync(new CachedProductGroup
                {
                    ProductGroupID = group.ProductGroupID,
                    Description = group.Description,
                    Description2 = group.Description2,
                    View = group.View,
                    ViewOrder = group.ViewOrder,
                    Extra_ID = group.Extra_ID,
                    Icon_ID = group.Icon_ID,
                    ISSub = group.ISSub,
                    Sub_From_GroupID = group.Sub_From_GroupID,
                    Has_Sub = group.Has_Sub
                });
            }

            // Insert products
            foreach (var product in AllProducts)
            {
                await connection.InsertAsync(new CachedProduct
                {
                    ProductID = product.ProductID,
                    Description = product.Description,
                    Description2 = product.Description2,
                    Price = product.Price,
                    ProductGroupID = product.ProductGroupID,
                    Printer = product.Printer,
                    VAT = product.VAT,
                    Build = product.Build,
                    Extra_ID = product.Extra_ID,
                    Row_Print = product.Row_Print,
                    Auto_Extra = product.Auto_Extra,
                    Has_Options = product.Has_Options,
                    Extra_ID_Key = product.Extra_ID_Key,
                    Menu_Number = product.Menu_Number,
                    Include_Group = product.Include_Group,
                    Favorite = product.Favorite,
                    Drink_Or_Food = product.Drink_Or_Food,
                    CPrinter = product.CPrinter,
                    Sale_Lock = product.Sale_Lock
                });
            }

            // Insert courses
            foreach (var course in Courses)
            {
                await connection.InsertAsync(new CachedCourse
                {
                    CourseId = course.CourseId,
                    Description = course.Description
                });
            }

            // Update last sync time
            await _cacheManager.SetLastSyncTimeAsync(DateTime.Now);
        }

        private void OrganizeProductGroups()
        {
            var rootCategories = ProductCategories.Where(pc => !pc.ISSub).ToList();

            foreach (var rootCategory in rootCategories)
            {
                var subCategories = ProductCategories
                                    .Where(pc => pc.ISSub && pc.Sub_From_GroupID == rootCategory.ProductGroupID)
                                    .ToList();

                rootCategory.Subcategories = new ObservableCollection<ProductGroup>(subCategories);
            }

            // Replace the original list with only the root categories, which now include their subcategories
            ProductCategories = new ObservableCollection<ProductGroup>(rootCategories);
        }

        public async Task<ObservableCollection<ProductGroup>> LoadProductGroups()
        {
            await LoadAllDataAsync();
            return ProductCategories;
        }

        public async Task<ObservableCollection<Product>> LoadProducts()
        {
            await LoadAllDataAsync();
            return AllProducts;
        }
    }
}
