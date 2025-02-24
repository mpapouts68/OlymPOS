using System.Collections.ObjectModel;
using OlymPOS;

namespace OlymPOS;

public class OrderItem
{
    public int OrderIDSub { get; set; }
    public double OrderID { get; set; }
    public string Description { get; set; }
    public int ProductID { get; set; }
    public string Unit { get; set; }
    public int Quantity { get; set; }
    public int PostID { get; set; }
    public int NameID { get; set; }
    public decimal Price { get; set; }
    public string DescriptionEx_UK { get; set; }
    public bool Printer { get; set; }
    public bool Receipt { get; set; }
    public DateTime OrderTime { get; set; }
    public string StaffID { get; set; }
    public bool PersonelClosed { get; set; }
    public bool Free { get; set; }
    public decimal Pricefree { get; set; }
    public bool Cancelled { get; set; }
    public string OrderBy { get; set; }
    public int ServingRow { get; set; }
    public bool HasExtra { get; set; }
    public bool Served { get; set; }
    public bool PartECR { get; set; }
    public ObservableCollection<Extra> OrderExtras { get; set; } = new ObservableCollection<Extra>();


   public async Task LoadOrderExtrasAsync(OrderDataService service, long orderItemId)
{
    OrderExtras.Clear(); 
    var extras = await service.GetOrderExtrasForOrderItemAsync(orderItemId);
    foreach (var extra in extras)
    {
        OrderExtras.Add(extra);
            Console.WriteLine($"Finished loading fucking extras");
        }
}
}
