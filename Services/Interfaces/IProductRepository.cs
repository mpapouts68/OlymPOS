using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<IEnumerable<Product>> GetByGroupIdAsync(int groupId);
        Task<IEnumerable<Product>> GetFavoritesAsync();
        Task<IEnumerable<Product>> SearchAsync(string query);
    }
}
