using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetActiveOrdersAsync();
        Task<IEnumerable<Order>> GetByClerkIdAsync(int clerkId);
        Task<IEnumerable<Order>> GetByTableIdAsync(int tableId);
        Task<IEnumerable<OrderItem>> GetOrderItemsAsync(int orderId);
        Task<bool> AddOrderItemAsync(OrderItem item);
        Task<bool> UpdateOrderItemAsync(OrderItem item);
        Task<bool> DeleteOrderItemAsync(int itemId);
        Task<bool> ApplyDiscountAsync(int orderId, decimal discountPercentage);
        Task<bool> CloseOrderAsync(int orderId, PaymentDetails payment);
    }
}
