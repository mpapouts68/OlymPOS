using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using CommunityToolkit.Maui.Core;
using OlymPOS;

namespace OlymPOS
{
    public class OrderDataService
    {
        private readonly string _connectionString;
       

        public OrderDataService()
        {
            _connectionString = GlobalConString.ConnStr; // Ensure GlobalConString.ConnStr is correctly set up
           
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            Console.WriteLine($"Start loading Order");
            var orders = new List<Order>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = $"SELECT * FROM Qry_Orders WHERE Order_ID = {ProgSettings.Actordrid}"; // Update as needed

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orders.Add(new Order
                        {
                            OrderID = reader.IsDBNull(reader.GetOrdinal("Order_ID")) ? 0 : Convert.ToInt32(reader["Order_ID"]),
                            TimeDate = reader.IsDBNull(reader.GetOrdinal("Time_Date")) ? DateTime.MinValue : Convert.ToDateTime(reader["Time_Date"]),
                            ClerkID = reader.IsDBNull(reader.GetOrdinal("Clerk_ID")) ? 0 : Convert.ToInt32(reader["Clerk_ID"]),
                            OrderTotal = reader.IsDBNull(reader.GetOrdinal("Order_Total")) ? 0 : Convert.ToDecimal(reader["Order_Total"]),
                            PostID = reader.IsDBNull(reader.GetOrdinal("Post_ID")) ? 0 : Convert.ToInt32(reader["Post_ID"]),
                            PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? 0 : Convert.ToInt32(reader["Post_Number"]),
                            HasDiscount = reader.IsDBNull(reader.GetOrdinal("HasDiscount")) ? false : Convert.ToBoolean(reader["HasDiscount"]),
                            DiscountPercentage = reader.IsDBNull(reader.GetOrdinal("DiscountPercentage")) ? null : (int?)Convert.ToInt32(reader["DiscountPercentage"]),
                            Closed = reader.IsDBNull(reader.GetOrdinal("Closed")) ? false : Convert.ToBoolean(reader["Closed"]),
                            OrderDiscountAmount = reader.IsDBNull(reader.GetOrdinal("Order_Discount_Amount")) ? null : (double?)Convert.ToDouble(reader["Order_Discount_Amount"]),
                            OrderTotalAfterD = reader.IsDBNull(reader.GetOrdinal("Order_Total_AfterD")) ? null : (double?)Convert.ToDouble(reader["Order_Total_AfterD"]),
                            SplitPayment = reader.IsDBNull(reader.GetOrdinal("Split_Payment")) ? null : (decimal?)Convert.ToDecimal(reader["Split_Payment"]),
                            VATHigh = reader.IsDBNull(reader.GetOrdinal("VAT_High")) ? null : (decimal?)Convert.ToDecimal(reader["VAT_High"]),
                            VATLOw = reader.IsDBNull(reader.GetOrdinal("VAT_Low")) ? null : (decimal?)Convert.ToDecimal(reader["VAT_Low"]),
                            EmployeeID = reader.IsDBNull(reader.GetOrdinal("Employee_ID")) ? 0 : Convert.ToInt32(reader["Employee_ID"]),
                            NumberOfPersons = reader.IsDBNull(reader.GetOrdinal("NumberOfPersons")) ? 0 : Convert.ToInt32(reader["NumberOfPersons"]),
                            CashAmount = reader.IsDBNull(reader.GetOrdinal("Cash_Amount")) ? null : (decimal?)Convert.ToDecimal(reader["Cash_Amount"]),
                            CardAmount = reader.IsDBNull(reader.GetOrdinal("Card_Amount")) ? null : (decimal?)Convert.ToDecimal(reader["Card_Amount"]),
                            VoucherAmount = reader.IsDBNull(reader.GetOrdinal("Voucher_Amount")) ? null : (decimal?)Convert.ToDecimal(reader["Voucher_Amount"]),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString("Description"),

                        });
                    }
                }
            }
            return orders;

        }

        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            Console.WriteLine($"Start loading OrderItems");
            var orderItems = new List<OrderItem>();
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = $"SELECT * FROM Order_Rep WHERE Order_ID  = {ProgSettings.Actordrid}";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        orderItems.Add(new OrderItem
                        {
                            OrderIDSub = reader.GetInt32OrDefault("Order_ID_Sub"),
                            Description = reader.GetStringOrDefault("prDescription"),
                            OrderID = reader.GetDoubleOrDefault("Order_ID"),
                            ProductID = reader.GetInt32OrDefault("Product_ID"),
                            //  Unit = reader.GetStringOrDefault("Unit"),
                            Quantity = reader.GetInt32OrDefault("Quantity"),
                            Price = reader.GetDecimalOrDefault("Price"),
                            // DescriptionEx_UK = reader.GetStringOrDefault("DescriptionEx_UK"),
                            Printer = reader.GetBooleanOrDefault("Printer"),
                            //  Receipt = reader.GetBooleanOrDefault("Receipt"),
                            OrderTime = reader.GetDateTimeOrDefault("OrderTime"),
                            StaffID = reader.GetStringOrDefault("Staff_ID"),
                            // Free = reader.GetBooleanOrDefault("Free"),
                            // Pricefree = reader.GetDecimalOrDefault("Price_free"),
                            //Cancelled = reader.GetBooleanOrDefault("Cancelled"),
                            //OrderBy = reader.GetStringOrDefault("Order_By"),
                            // ServingRow = reader.GetInt32OrDefault("Serving_Row"),
                            //HasExtra = reader.GetBooleanOrDefault("Has_Extra"),


                            // PartECR = reader.GetBooleanOrDefault("PartECR"),

                            // Add the rest of the properties, using the extension methods for handling DBNull values
                        });
                    }
                }
                Console.WriteLine($"Finished loading Order");
            }
            return orderItems;
        }
        public async Task<List<Extra>> GetOrderExtrasForOrderItemAsync(long orderItemId)
        {
            Console.WriteLine($"Start loading extrass");
            var orderExtras = new List<Extra>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = $"SELECT * FROM Qry_Order_Extras_Sub_Sum WHERE Order_ID_Sub = @orderItemId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@orderItemId", orderItemId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            orderExtras.Add(new Extra
                            {
                                OrderIDSub = reader.GetInt32OrDefault("Order_ID_Sub"),
                                Description = reader.GetStringOrDefault("Extra_Description"),
                                quantity = reader.GetInt32OrDefault("sumOfQuantity"),

                            });
                        }
                    }
                }
            }

            return orderExtras;
        }

        public async Task ApplyPercentageDiscountAsync(double percentage, int activeOrderId)
        {
            try
            {
                using (var connection = new MySqlConnection(GlobalConString.ConnStr))
                {
                    await connection.OpenAsync();

                    var query = "UPDATE Orders SET DiscountPercentage = @percentage, HasDiscount = True  WHERE order_id = @orderId";
                    Console.WriteLine(query);
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@percentage", percentage);
                        command.Parameters.AddWithValue("@orderId", activeOrderId);

                        int affectedRows = await command.ExecuteNonQueryAsync();
                        // Log or handle the affectedRows as needed
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

        }
        public async Task dbexecute(string msg1)
        {
            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            try
            {
                Console.WriteLine("Start writing to database");
                await connection.OpenAsync();
                var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@value1", msg1);
                command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
                command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

                var result = await command.ExecuteNonQueryAsync();
                if (result >= 1)
                {
                    // Display short snackbar alert for success
                    //await Application.Current.MainPage.Snackbar("The Command is Accepted.\nWait for Print notification");
                }
                else
                {
                    // Display error Alert
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
                }

            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine(ex.Message);

            }
        }

    }

    public static class DataReaderExtensions
    {
        public static int GetInt32OrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? default : reader.GetInt32(ordinal);
        }

        public static double GetDoubleOrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? default : reader.GetDouble(ordinal);
        }

        public static string GetStringOrDefault(this MySqlDataReader reader, string columnName, string defaultValue = "")
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
        }

        public static bool GetBooleanOrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        public static decimal GetDecimalOrDefault(this MySqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? default : reader.GetDecimal(ordinal);
        }

        public static DateTime GetDateTimeOrDefault(this MySqlDataReader reader, string columnName, DateTime defaultValue = default)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDateTime(ordinal);
        }

        



    }

}

