using OlymPOS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OlymPOS.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly ITableRepository _tableRepository;
        private readonly IPrintService _printService;

        public OrderService(
            IOrderRepository orderRepository,
            IProductRepository productRepository,
            ITableRepository tableRepository,
            IPrintService printService)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _tableRepository = tableRepository;
            _printService = printService;
        }

        public async Task<int> CreateOrderAsync(int tableId)
        {
            // Get the next available order ID
            // In a production app, this would be handled by the database
            // But for offline mode, we need to generate IDs that won't conflict
            var orders = await _orderRepository.GetAllAsync();
            int nextOrderId = orders.Any() ? orders.Max(o => o.OrderID) + 1 : 1;

            // Get the table
            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null)
                throw new ArgumentException($"Table with ID {tableId} not found");

            // Create new order
            var order = new Order
            {
                OrderID = nextOrderId,
                TimeDate = DateTime.Now,
                ClerkID = UserSettings.ClerkID,
                OrderType = "Table Order",
                Description = $"Table {table.PostNumber}",
                OrderTotal = 0,
                Receipt = false,
                History = false,
                Served = false,
                PostID = tableId,
                PostNumber = table.PostNumber,
                HasDiscount = false,
                Closed = false
            };

            // Save the order
            bool success = await _orderRepository.AddAsync(order);
            if (!success)
                throw new ApplicationException("Failed to create order");

            // Update table status
            await _tableRepository.SetTableStatusAsync(tableId, true, nextOrderId);

            // Update global settings
            ProgSettings.Actordrid = nextOrderId;
            ProgSettings.Actpostid = tableId;

            return nextOrderId;
        }

        public async Task<bool> AddProductToOrderAsync(int orderId, int productId, int quantity)
        {
            // Get the order
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Order with ID {orderId} not found");

            // Get the product
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            // Generate a new order item ID
            var orderItems = await _orderRepository.GetOrderItemsAsync(orderId);
            int nextOrderItemId = orderItems.Any() ? orderItems.Max(i => i.OrderIDSub) + 1 : 1;

            // Create new order item
            var orderItem = new OrderItem
            {
                OrderIDSub = nextOrderItemId,
                OrderID = orderId,
                ProductID = productId,
                Description = product.Description,
                Quantity = quantity,
                PostID = order.PostID ?? 0,
                Price = product.Price,
                Printer = false,
                Receipt = false,
                OrderTime = DateTime.Now,
                StaffID = UserSettings.ClerkID.ToString(),
                ServingRow = product.Row_Print,
                HasExtra = false
            };

            // Save the order item
            bool success = await _orderRepository.AddOrderItemAsync(orderItem);
            if (!success)
                throw new ApplicationException("Failed to add product to order");

            // Recalculate order total
            order.OrderTotal = (orderItems.Sum(i => i.Price * i.Quantity) + (product.Price * quantity));
            await _orderRepository.UpdateAsync(order);

            return true;
        }

        public async Task<bool> UpdateProductQuantityAsync(int orderId, int productId, int quantity)
        {
            // Get the order items
            var orderItems = await _orderRepository.GetOrderItemsAsync(orderId);
            var existingItem = orderItems.FirstOrDefault(i => i.ProductID == productId && !i.Cancelled);

            if (existingItem == null)
            {
                // If the item doesn't exist, add it
                return await AddProductToOrderAsync(orderId, productId, quantity);
            }
            else
            {
                // If quantity is 0, mark as cancelled
                if (quantity == 0)
                {
                    await _orderRepository.DeleteOrderItemAsync(existingItem.OrderIDSub);
                    return true;
                }

                // Update quantity
                existingItem.Quantity = quantity;
                bool success = await _orderRepository.UpdateOrderItemAsync(existingItem);

                // Update order total
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order != null)
                {
                    order.OrderTotal = orderItems.Where(i => !i.Cancelled).Sum(i => i.Price * i.Quantity);
                    await _orderRepository.UpdateAsync(order);
                }

                return success;
            }
        }

        public async Task<bool> ApplyDiscountAsync(int orderId, decimal discountPercentage)
        {
            return await _orderRepository.ApplyDiscountAsync(orderId, discountPercentage);
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, PaymentDetails payment)
        {
            // Close the order
            bool success = await _orderRepository.CloseOrderAsync(orderId, payment);
            if (!success)
                return false;

            // Release the table
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order?.PostID != null)
            {
                await _tableRepository.SetTableStatusAsync(order.PostID.Value, false);
            }

            // Print receipt if requested
            if (payment.PrintReceipt)
            {
                await _printService.PrintPaymentAsync(orderId, payment);
            }

            return true;
        }

        public async Task<bool> SendOrderToPrinterAsync(int orderId, bool withReceipt)
        {
            // Mark items as printed
            var orderItems = await _orderRepository.GetOrderItemsAsync(orderId);
            bool anyChanges = false;

            foreach (var item in orderItems.Where(i => !i.Printer))
            {
                item.Printer = true;
                item.Receipt = withReceipt;
                await _orderRepository.UpdateOrderItemAsync(item);
                anyChanges = true;
            }

            // If no changes, return false
            if (!anyChanges)
                return false;

            // Print the order
            await _printService.PrintOrderAsync(orderId, withReceipt);

            return true;
        }
    }
}
