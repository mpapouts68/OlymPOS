using System.Collections.Generic;
using System.Threading.Tasks;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces; // ✅ Ensure correct namespace for IRepository and IDataService
using OlymPOS.Services;
namespace OlymPOS.Repositories.Implementations
{
    public class ProductRepository : IRepository<Product>
    {
        private readonly IDataService _dataService;

        public ProductRepository(IDataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            const string query = "SELECT * FROM Products WHERE Product_ID = @Id";
            return await _dataService.GetAsync<Product>(query, new { Id = id });
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            const string query = "SELECT * FROM Products";
            return await _dataService.GetAsync<IEnumerable<Product>>(query);
        }

        public async Task<int> AddAsync(Product entity)
        {
            const string query = @"INSERT INTO Products 
                (Description, Description2, Price, ProductGroup_ID) 
                VALUES (@Description, @Description2, @Price, @ProductGroupID);
                SELECT LAST_INSERT_ID();";

            return await _dataService.ExecuteAsync(query, entity);
        }

        public async Task<bool> UpdateAsync(Product entity)
        {
            const string query = @"UPDATE Products 
                SET Description = @Description, 
                    Description2 = @Description2,
                    Price = @Price,
                    ProductGroup_ID = @ProductGroupID
                WHERE Product_ID = @ProductID";

            return await _dataService.ExecuteAsync(query, entity) > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string query = "DELETE FROM Products WHERE Product_ID = @Id";
            return await _dataService.ExecuteAsync(query, new { Id = id }) > 0;
        }
    }
}
