using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OlymPOS
{
    public enum ReceiptStatus
    {
        AllTrue, AllFalse, Partial
    }

    public class OrderModel
    {
        public int OrderId { get; set; }
        public DateTime TimeDate { get; set; }
        public int ClerkId { get; set; }
        public decimal OrderTotal { get; set; }
        public bool HasDiscount { get; set; }
        public double DiscountPercentage { get; set; }
        public decimal OrderTotalAfterDiscount { get; set; }
        public ReceiptStatus ReceiptStatus { get; set; }
        public string Description { get; set; }
        public int PostNumber { get; set; }
        public string DescriptionAndPostNumber => $"{Description} {PostNumber}";
        public bool IsHistorical { get; set; }
    }

    public class DatabaseService
    {
        private readonly string _connectionString = GlobalConString.ConnStr;

        public async Task<List<OrderModel>> GetOrdersByClerkId(int clerkId)
        {
            var orders = new Dictionary<int, OrderModel>();
            var orderReceiptStatuses = new Dictionary<int, List<bool>>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT o.Order_ID, o.Time_Date, o.Clerk_ID, o.Order_Total, o.HasDiscount, 
                           o.History, o.DiscountPercentage, o.Order_Total_AfterD, o.Description, o.Post_Number, oa.Receipt
                    FROM post_report o
                    JOIN Orders_Actual oa ON o.Order_ID = oa.Order_ID
                    WHERE o.Clerk_ID = @ClerkId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClerkId", clerkId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int orderId = reader.GetInt32("Order_ID");
                            OrderModel order;
                            if (!orders.TryGetValue(orderId, out order))
                            {
                                order = new OrderModel
                                {
                                    OrderId = orderId,
                                    TimeDate = reader.GetDateTime("Time_Date"),
                                    ClerkId = reader.GetInt32("Clerk_ID"),
                                    OrderTotal = reader.GetDecimal("Order_Total"),
                                    HasDiscount = reader.GetBoolean("HasDiscount"),
                                    DiscountPercentage = Convert.ToDouble(reader["DiscountPercentage"]),
                                    OrderTotalAfterDiscount = reader.GetDecimal("Order_Total_AfterD"),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                                    PostNumber = reader.IsDBNull(reader.GetOrdinal("Post_Number")) ? 0 : reader.GetInt32("Post_Number"),
                                    //ReceiptStatus = DetermineReceiptStatus(new List<bool> { reader.GetBoolean("Receipt") })
                                    IsHistorical = reader.GetBoolean("History")
                                };
                                orders[orderId] = order;
                                orderReceiptStatuses[orderId] = new List<bool>();
                            }
                            orderReceiptStatuses[orderId].Add(reader.GetBoolean("Receipt"));
                        }
                    }
                }

                // Set the ReceiptStatus for each order based on the aggregated Receipt values
                foreach (var orderId in orderReceiptStatuses.Keys)
                {
                    var receiptStatuses = orderReceiptStatuses[orderId];
                    var allTrue = receiptStatuses.All(x => x);
                    var allFalse = receiptStatuses.All(x => !x);
                    orders[orderId].ReceiptStatus = allTrue ? ReceiptStatus.AllTrue : allFalse ? ReceiptStatus.AllFalse : ReceiptStatus.Partial;
                }
            }

            return orders.Values.ToList();
        }
    }
}
