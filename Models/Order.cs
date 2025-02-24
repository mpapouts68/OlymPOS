using OlymPOS;
using System.Collections.ObjectModel;

namespace OlymPOS
{
    public class Order
    {
        public int OrderID { get; set; }
        public DateTime TimeDate { get; set; }
        public int ClerkID { get; set; }
        public string OrderType { get; set; }
        public string Description { get; set; }
        public decimal? OrderTotal { get; set; }
        public bool Receipt { get; set; }
        public bool History { get; set; }
        public bool Served { get; set; }
        public int? PostID { get; set; }
        public int? PostNumber { get; set; }
        public int? NameID { get; set; }
        public int? CustomerID { get; set; }
        public bool HasDiscount { get; set; }
        public int? DiscountPercentage { get; set; }
        public bool Closed { get; set; }
        public DateTime? ClosedDate { get; set; }
        public double? OrderDiscountAmount { get; set; }
        public double? OrderTotalAfterD { get; set; }
        public decimal? SplitPayment { get; set; }
        public int? CatIDOpen { get; set; }
        public int? CatIDClose { get; set; }
        public decimal? VATHigh { get; set; }
        public decimal? VATLOw { get; set; }
        public int? EmployeeID { get; set; }
        public int? IDPool { get; set; }
        public int? NumberOfPersons { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? CardAmount { get; set; }
        public decimal? VoucherAmount { get; set; }
        public string DescriptionWithPostNumber => $"{Description} {PostNumber}";
        public ObservableCollection<OrderItem> OrderItems { get; set; } = new ObservableCollection<OrderItem>();
    
    public async Task LoadOrderItems(OrderDataService service)
    {
        var items = await service.GetOrderItemsAsync(OrderID);
        foreach (var item in items)
        {
            OrderItems.Add(item);
        }
    }
}
}
