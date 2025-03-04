using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using OlymPOS.Services.Interfaces;
using System.Text.Json;

namespace OlymPOS.Services.Caching
{
    public class SqliteCacheManager : ICacheManager
    {
        private SQLiteAsyncConnection _db;
        private readonly string _dbPath;
        private bool _isInitialized = false;

        public SqliteCacheManager()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "olympos_cache.db3");
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            _db = new SQLiteAsyncConnection(_dbPath);

            // Create tables for our entities
            await _db.CreateTableAsync<CachedProduct>();
            await _db.CreateTableAsync<CachedProductGroup>();
            await _db.CreateTableAsync<CachedOrder>();
            await _db.CreateTableAsync<CachedOrderItem>();
            await _db.CreateTableAsync<CachedServicingPoint>();
            await _db.CreateTableAsync<CachedExtra>();
            await _db.CreateTableAsync<CachedCourse>();
            await _db.CreateTableAsync<CachedArea>();
            await _db.CreateTableAsync<CachedOrderExtra>();
            await _db.CreateTableAsync<CachedMetadata>();

            _isInitialized = true;
        }

        public async Task<bool> IsCacheInitializedAsync()
        {
            if (_isInitialized)
                return true;

            bool exists = File.Exists(_dbPath);
            if (!exists)
                return false;

            await InitializeAsync();
            return true;
        }

        public async Task ClearCacheAsync()
        {
            if (!_isInitialized)
                await InitializeAsync();

            await _db.DeleteAllAsync<CachedProduct>();
            await _db.DeleteAllAsync<CachedProductGroup>();
            await _db.DeleteAllAsync<CachedOrder>();
            await _db.DeleteAllAsync<CachedOrderItem>();
            await _db.DeleteAllAsync<CachedServicingPoint>();
            await _db.DeleteAllAsync<CachedExtra>();
            await _db.DeleteAllAsync<CachedCourse>();
            await _db.DeleteAllAsync<CachedArea>();
            await _db.DeleteAllAsync<CachedOrderExtra>();
        }

        public async Task<DateTime> GetLastSyncTimeAsync()
        {
            if (!_isInitialized)
                await InitializeAsync();

            var metadata = await _db.Table<CachedMetadata>()
                .Where(m => m.Key == "LastSyncTime")
                .FirstOrDefaultAsync();

            if (metadata == null)
                return DateTime.MinValue;

            if (DateTime.TryParse(metadata.Value, out DateTime result))
                return result;

            return DateTime.MinValue;
        }

        public async Task SetLastSyncTimeAsync(DateTime syncTime)
        {
            if (!_isInitialized)
                await InitializeAsync();

            var metadata = new CachedMetadata
            {
                Key = "LastSyncTime",
                Value = syncTime.ToString("o")
            };

            await _db.InsertOrReplaceAsync(metadata);
        }

        public async Task<T> GetCachedItemAsync<T>(string key) where T : class
        {
            if (!_isInitialized)
                await InitializeAsync();

            var metadata = await _db.Table<CachedMetadata>()
                .Where(m => m.Key == key)
                .FirstOrDefaultAsync();

            if (metadata == null)
                return null;

            return JsonSerializer.Deserialize<T>(metadata.Value);
        }

        public async Task SetCachedItemAsync<T>(string key, T item) where T : class
        {
            if (!_isInitialized)
                await InitializeAsync();

            var metadata = new CachedMetadata
            {
                Key = key,
                Value = JsonSerializer.Serialize(item)
            };

            await _db.InsertOrReplaceAsync(metadata);
        }
    }

    // Cache entity models with SQLite attributes

    [Table("CachedMetadata")]
    public class CachedMetadata
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    [Table("CachedProducts")]
    public class CachedProduct
    {
        [PrimaryKey]
        public int ProductID { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public decimal Price { get; set; }
        public int ProductGroupID { get; set; }
        public string Printer { get; set; }
        public int? VAT { get; set; }
        public bool Build { get; set; }
        public int? Extra_ID { get; set; }
        public int Row_Print { get; set; }
        public bool Auto_Extra { get; set; }
        public bool Has_Options { get; set; }
        public int? Extra_ID_Key { get; set; }
        public int? Menu_Number { get; set; }
        public bool Include_Group { get; set; }
        public bool Favorite { get; set; }
        public string Drink_Or_Food { get; set; }
        public string CPrinter { get; set; }
        public bool Sale_Lock { get; set; }
    }

    [Table("CachedProductGroups")]
    public class CachedProductGroup
    {
        [PrimaryKey]
        public int ProductGroupID { get; set; }
        public string Description { get; set; }
        public string Description2 { get; set; }
        public int? View { get; set; }
        public int? ViewOrder { get; set; }
        public int? Extra_ID { get; set; }
        public int? Icon_ID { get; set; }
        public bool ISSub { get; set; }
        public int? Sub_From_GroupID { get; set; }
        public bool Has_Sub { get; set; }
    }

    [Table("CachedOrders")]
    public class CachedOrder
    {
        [PrimaryKey]
        public int OrderID { get; set; }
        public DateTime TimeDate { get; set; }
        public int ClerkID { get; set; }
        public string OrderType { get; set; }
        public string Description { get; set; }
        public decimal? OrderTotal { get; set; }
        public bool Receipt { get; set; }
        public bool History { get; set; }
        public bool Served { get; set; }
        public int? PostID { get; set; }
        public int? PostNumber { get; set; }
        public int? NameID { get; set; }
        public int? CustomerID { get; set; }
        public bool HasDiscount { get; set; }
        public int? DiscountPercentage { get; set; }
        public bool Closed { get; set; }
        public DateTime? ClosedDate { get; set; }
        public double? OrderDiscountAmount { get; set; }
        public double? OrderTotalAfterD { get; set; }
        public decimal? SplitPayment { get; set; }
        public int? CatIDOpen { get; set; }
        public int? CatIDClose { get; set; }
        public decimal? VATHigh { get; set; }
        public decimal? VATLOw { get; set; }
        public int? EmployeeID { get; set; }
        public int? IDPool { get; set; }
        public int? NumberOfPersons { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? CardAmount { get; set; }
        public decimal? VoucherAmount { get; set; }
        public bool IsSynced { get; set; }
    }

    [Table("CachedOrderItems")]
    public class CachedOrderItem
    {
        [PrimaryKey]
        public int OrderIDSub { get; set; }
        public double OrderID { get; set; }
        public string Description { get; set; }
        public int ProductID { get; set; }
        public string Unit { get; set; }
        public int Quantity { get; set; }
        public int PostID { get; set; }
        public int NameID { get; set; }
        public decimal Price { get; set; }
        public string DescriptionEx_UK { get; set; }
        public bool Printer { get; set; }
        public bool Receipt { get; set; }
        public DateTime OrderTime { get; set; }
        public string StaffID { get; set; }
        public bool PersonelClosed { get; set; }
        public bool Free { get; set; }
        public decimal Pricefree { get; set; }
        public bool Cancelled { get; set; }
        public string OrderBy { get; set; }
        public int ServingRow { get; set; }
        public bool HasExtra { get; set; }
        public bool Served { get; set; }
        public bool PartECR { get; set; }
        public bool IsSynced { get; set; }
    }

    [Table("CachedServicingPoints")]
    public class CachedServicingPoint
    {
        [PrimaryKey]
        public int PostID { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
        public int ActiveOrderID { get; set; }
        public int PostNumber { get; set; }
        public bool Reserved { get; set; }
        public int YperMainID { get; set; }
    }

    [Table("CachedExtras")]
    public class CachedExtra
    {
        [PrimaryKey]
        public int ExtraId { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
    }

    [Table("CachedCourses")]
    public class CachedCourse
    {
        [PrimaryKey]
        public int CourseId { get; set; }
        public string Description { get; set; }
    }

    [Table("CachedAreas")]
    public class CachedArea
    {
        [PrimaryKey]
        public int YperMainID { get; set; }
        public string Description { get; set; }
        public int IconID { get; set; }
    }

    [Table("CachedOrderExtras")]
    public class CachedOrderExtra
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int OrderIDSub { get; set; }
        public int ExtraId { get; set; }
        public int Quantity { get; set; }
        public string Prefix { get; set; }
        public bool IsSynced { get; set; }
    }
}