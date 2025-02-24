using OlymPOS;

namespace OlymPOS;

public partial class StatisticsPage : ContentPage
    
{
   

    private readonly DatabaseService _databaseService = new DatabaseService();

    public StatisticsPage()
    {
        InitializeComponent();
        LoadOrdersDataAsync();
    }

    private async void LoadOrdersDataAsync()
    {
        int clerkId = UserSettings.ClerkID;
        var orders = await _databaseService.GetOrdersByClerkId(clerkId);

        // Filter orders based on IsHistorical
        var actualOrders = orders.Where(o => !o.IsHistorical).ToList();
        var historicalOrders = orders.Where(o => o.IsHistorical).ToList();

        actualOrdersList.ItemsSource = actualOrders;
        historyOrdersList.ItemsSource = historicalOrders;

        // Calculate subtotals and grand total
        UpdateSubtotalsAndGrandTotal(actualOrders, historicalOrders);
    }

    private void UpdateSubtotalsAndGrandTotal(List<OrderModel> actualOrders, List<OrderModel> historicalOrders)
    {
        var actualSubtotal = actualOrders.Sum(o => o.OrderTotalAfterDiscount);
        var historySubtotal = historicalOrders.Sum(o => o.OrderTotalAfterDiscount);
        var grandTotalAmount = actualSubtotal + historySubtotal;

        actualOrdersSubtotal.Text = $"Actual Orders Subtotal: {actualSubtotal:C}";
        historyOrdersSubtotal.Text = $"Historical Orders Subtotal: {historySubtotal:C}";
        grandTotal.Text = $"Grand Total: {grandTotalAmount:C}";
    }
}
