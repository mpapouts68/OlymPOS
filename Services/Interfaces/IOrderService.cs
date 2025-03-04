using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IOrderService
    {
        Task<int> CreateOrderAsync(int tableId);
        Task<bool> AddProductToOrderAsync(int orderId, int productId, int quantity);
        Task<bool> UpdateProductQuantityAsync(int orderId, int productId, int quantity);
        Task<bool> ApplyDiscountAsync(int orderId, decimal discountPercentage);
        Task<bool> ProcessPaymentAsync(int orderId, PaymentDetails payment);
        Task<bool> SendOrderToPrinterAsync(int orderId, bool withReceipt);
    }
}