using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Order>> GetOrdersAsync(int clerkId)
        {
            var orders = new List<Order>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var query = "SELECT * FROM Orders WHERE Clerk_ID = @ClerkId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@ClerkId", clerkId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    ClerkID = reader.GetInt32("Clerk_ID"),
                    OrderTotal = reader.GetDecimal("Order_Total"),
                    HasDiscount = reader.GetBoolean("HasDiscount"),
                    DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? null : reader.GetInt32("DiscountPercentage"),
                    OrderTotalAfterD = reader.GetDouble("Order_Total_AfterD")
                });
            }
            return orders;
        }

        // ✅ Fix: Implement `GetOrderItemsAsync`
        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            var orderItems = new List<OrderItem>();

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var query = "SELECT * FROM Orders_Actual WHERE Order_ID = @OrderId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orderItems.Add(new OrderItem
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    Description = reader.GetString("DescriptionEx"),
                    ProductID = reader.GetInt32("Product_ID"),
                    Quantity = reader.GetInt32("Quantity"),
                    Price = reader.GetDecimal("Price"),
                    OrderTime = reader.GetDateTime("OrderTime"),
                    StaffID = reader.GetString("Staff_ID")
                });
            }
            return orderItems;
        }

        // ✅ Fix: Implement `ApplyDiscountAsync`
        public async Task ApplyDiscountAsync(double percentage, int orderId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "UPDATE Orders SET DiscountPercentage = @percentage, HasDiscount = True WHERE Order_ID = @OrderId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@percentage", percentage);
            command.Parameters.AddWithValue("@OrderId", orderId);

            await command.ExecuteNonQueryAsync();
        }

        // ✅ Fix: Implement `ExecuteCommandAsync`
        public async Task ExecuteCommandAsync(string commandText)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new MySqlCommand(commandText, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
