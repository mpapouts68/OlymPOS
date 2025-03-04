using System;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.Services
{
    public class PrintService : IPrintService
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IOrderRepository _orderRepository;

        public PrintService(
            IDatabaseConnectionFactory connectionFactory,
            IOrderRepository orderRepository)
        {
            _connectionFactory = connectionFactory;
            _orderRepository = orderRepository;
        }

        public async Task<bool> PrintOrderAsync(int orderId, bool isReceipt)
        {
            try
            {
                // Get the order to print
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                    return false;

                // Connect to the database
                using var conn = await _connectionFactory.CreateRemoteConnectionAsync();
                await conn.OpenAsync();

                // Insert a print command to the CSOrders table
                // This table is monitored by the kitchen/bar printer service
                string command = isReceipt ? "PrintWithReceipt" : "PrintNoReceipt";

                using var cmd = new MySqlCommand(
                    @"INSERT INTO CSOrders (CSOrder, Staff_ID, Order_ID) 
                    VALUES (@command, @staffId, @orderId)", conn);

                cmd.Parameters.AddWithValue("@command", command);
                cmd.Parameters.AddWithValue("@staffId", UserSettings.ClerkID);
                cmd.Parameters.AddWithValue("@orderId", orderId);

                int result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Print error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PrintPaymentAsync(int orderId, PaymentDetails payment)
        {
            try
            {
                // Connect to the database
                using var conn = await _connectionFactory.CreateRemoteConnectionAsync();
                await conn.OpenAsync();

                // Insert a print command to the CSOrders table
                string command = payment.PrintReceipt ? "PrintReceipt" : "CloseOrder";

                using var cmd = new MySqlCommand(
                    @"INSERT INTO CSOrders (CSOrder, Staff_ID, Order_ID, Cash_Amount, Card_Amount, Voucher_Amount) 
                    VALUES (@command, @staffId, @orderId, @cashAmount, @cardAmount, @voucherAmount)", conn);

                cmd.Parameters.AddWithValue("@command", command);
                cmd.Parameters.AddWithValue("@staffId", UserSettings.ClerkID);
                cmd.Parameters.AddWithValue("@orderId", orderId);
                cmd.Parameters.AddWithValue("@cashAmount", payment.CashAmount);
                cmd.Parameters.AddWithValue("@cardAmount", payment.CardAmount);
                cmd.Parameters.AddWithValue("@voucherAmount", payment.VoucherAmount);

                int result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Payment print error: {ex.Message}");
                return false;
            }
        }
    }
}