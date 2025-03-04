using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.Services
{
    public class BaseDataService : IDataService
    {
        private readonly string _connectionString;

        public ObservableCollection<ProductGroup> ProductCategories { get; private set; }
        public ObservableCollection<Product> AllProducts { get; private set; }
        public ObservableCollection<Product> FavoriteProducts { get; private set; }
        public ObservableCollection<Course> Courses { get; private set; } // Added

        public BaseDataService(string connectionString)
        {
            _connectionString = connectionString;
            ProductCategories = new ObservableCollection<ProductGroup>();
            AllProducts = new ObservableCollection<Product>();
            FavoriteProducts = new ObservableCollection<Product>();
            Courses = new ObservableCollection<Course>(); // Initialized
        }

        public async Task LoadAllDataAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Load ProductGroups with subcategories using Sub_From_GroupID
            var groups = await connection.QueryAsync<ProductGroup>("SELECT ProductGroupID, Description, Sub_From_GroupID FROM ProductGroups");
            var groupDict = groups.ToDictionary(g => g.ProductGroupID);
            ProductCategories.Clear();
            foreach (var group in groups)
            {
                if (group.Sub_From_GroupID.HasValue && groupDict.TryGetValue(group.Sub_From_GroupID.Value, out var parent))
                {
                    parent.Subcategories.Add(group);
                }
                else
                {
                    ProductCategories.Add(group);
                }
            }

            // Load Products and Favorites
            var products = await connection.QueryAsync<Product>("SELECT ProductID, Description, Description2, Price, ProductGroupID, Favorite FROM Products");
            AllProducts.Clear();
            FavoriteProducts.Clear();
            foreach (var product in products)
            {
                AllProducts.Add(product);
                if (product.Favorite)
                    FavoriteProducts.Add(product);
            }

            // Load Courses
            var courses = await connection.QueryAsync<Course>("SELECT * FROM Grouping_Basis");
            Courses.Clear();
            foreach (var course in courses)
            {
                Courses.Add(course);
            }
        }

        public async Task<List<ProductGroup>> GetProductGroupsAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            const string query = "SELECT ProductGroupID, Description, Sub_From_GroupID FROM ProductGroups";
            return (await connection.QueryAsync<ProductGroup>(query)).AsList();
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            const string query = "SELECT ProductID, Description, Description2, Price, ProductGroupID, Favorite FROM Products";
            return (await connection.QueryAsync<Product>(query)).AsList();
        }

        public async Task<List<Course>> GetCoursesAsync()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            const string query = "SELECT * FROM Grouping_Basis";
            return (await connection.QueryAsync<Course>(query)).AsList();
        }

        public async Task<T> GetAsync<T>(string query, object parameters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<T>(query, parameters);
        }

        public async Task<int> ExecuteAsync(string query, object parameters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteAsync(query, parameters);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, object parameters = null)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<T>(query, parameters);
        }
    }
}