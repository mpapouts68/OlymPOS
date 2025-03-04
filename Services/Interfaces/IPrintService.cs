using System.Threading.Tasks;

namespace OlymPOS.Services.Interfaces
{
    public interface IPrintService
    {
        Task<bool> PrintOrderAsync(int orderId, bool isReceipt);
        Task<bool> PrintPaymentAsync(int orderId, PaymentDetails payment);
    }

    public class PaymentDetails
    {
        public decimal CashAmount { get; set; }
        public decimal CardAmount { get; set; }
        public decimal VoucherAmount { get; set; }
        public bool PrintReceipt { get; set; }
        public DateTime PaymentTime { get; set; } = DateTime.Now;
    }
}
