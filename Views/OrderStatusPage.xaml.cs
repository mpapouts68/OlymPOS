using OlymPOS;

namespace OlymPOS;

public partial class OrderStatusPage : ContentPage
{
    private readonly DatabaseService _databaseService = new DatabaseService();

    public OrderStatusPage()
    {
        InitializeComponent();
        LoadOrders();
    }

    private async void LoadOrders()
    {
        var orders = await _databaseService.GetOrdersByClerkId(UserSettings.ClerkID);
        this.BindingContext = orders; // Direct assignment for simplicity, consider using a proper ViewModel
    }
}
