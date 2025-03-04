using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Models;
using OlymPOS.Services;

namespace OlymPOS.Services
{
    public class OrderDataService
    {
        private readonly string _connectionString = GlobalConString.ConnStr;

        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            var orderItems = new List<OrderItem>();
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            var query = "SELECT * FROM Order_Rep WHERE Order_ID = @orderId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderId", orderId);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orderItems.Add(new OrderItem
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    ProductID = reader.GetInt32("Product_ID"),
                    Description = reader.GetString("prDescription"),
                    Quantity = reader.GetInt32("Quantity"),
                    Price = reader.GetDecimal("Price")
                });
            }
            return orderItems;
        }

        public async Task<List<Extra>> GetOrderExtrasForOrderItemAsync(long orderItemId)
        {
            var orderExtras = new List<Extra>();

            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            await connection.OpenAsync();
            var query = "SELECT * FROM Qry_Order_Extras_Sub_Sum WHERE Order_ID_Sub = @orderItemId";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@orderItemId", orderItemId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orderExtras.Add(new Extra
                {
                    OrderIDSub = reader.GetInt32("Order_ID_Sub"),
                    Description = reader.GetString("Extra_Description"),
                    quantity = reader.GetInt32("sumOfQuantity")
                });
            }
            return orderExtras;
        }
        public async Task<List<Order>> GetOrdersAsync()
        {
            var orders = new List<Order>();
            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            await connection.OpenAsync();
            var query = "SELECT * FROM Qry_Orders";

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    OrderID = reader.GetInt32("Order_ID"),
                    TimeDate = reader.GetDateTime("Time_Date"),
                    OrderTotal = reader.GetDecimal("Order_Total")
                });
            }
            return orders;
        }
        public async Task dbexecute(string commandText)
        {
            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            try
            {
                Console.WriteLine("Start writing to database");
                await connection.OpenAsync();
                var query = "INSERT INTO CSOrders (CSOrder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@value1", commandText);
                command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
                command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

                var result = await command.ExecuteNonQueryAsync();
                if (result >= 1)
                {
                    Console.WriteLine("Command successfully executed.");
                }
                else
                {
                    Console.WriteLine("Failed to insert data into the database.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
        }
        public async Task ApplyPercentageDiscountAsync(double percentage, int activeOrderId)
        {
            try
            {
                using var connection = new MySqlConnection(GlobalConString.ConnStr);
                await connection.OpenAsync();

                var query = "UPDATE Orders SET DiscountPercentage = @percentage, HasDiscount = True WHERE Order_ID = @orderId";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@percentage", percentage);
                command.Parameters.AddWithValue("@orderId", activeOrderId);

                int affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows > 0)
                {
                    Console.WriteLine($"Discount applied: {percentage}% to Order ID {activeOrderId}");
                }
                else
                {
                    Console.WriteLine("No rows affected. Check Order ID.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying discount: {ex.Message}");
            }
        }

    }

}
