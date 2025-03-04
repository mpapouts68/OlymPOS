using System.Collections.Generic;
using System.Threading.Tasks;
using OlymPOS.Models;

namespace OlymPOS.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetOrdersAsync(int clerkId);
        Task<List<OrderItem>> GetOrderItemsAsync(int orderId);  // ✅ Ensure this exists
        Task ApplyDiscountAsync(double percentage, int orderId);
        Task ExecuteCommandAsync(string command);
    }
}



