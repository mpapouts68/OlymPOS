using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using OlymPOS;


namespace OlymPOS;

public class OrderViewModel : BindableObject
{
    private readonly OrderDataService _orderDataService;
    private readonly OrderDataService _orderExtraService;

    public ICommand SendCommand { get; private set; }
    public ICommand MoveCommand { get; private set; }
    public ICommand DiscountCommand { get; private set; }
    public ICommand SplitCommand { get; private set; }
    public ICommand PayCommand { get; private set; }
    public ICommand ExitCommand { get; private set; }

    public ObservableCollection<Order> Orders { get; } = new ObservableCollection<Order>();
    public ObservableCollection<Extra> OrderExtras { get; } = new ObservableCollection<Extra>();
    public OrderViewModel()
    {
        _orderDataService = new OrderDataService();
        _orderExtraService = new OrderDataService();
        InitializeCommands();
        LoadOrders();
    }
    public async void OnOrderItemExpanderOpened(OrderItem orderItem)
    {
        await orderItem.LoadOrderExtrasAsync(_orderExtraService, orderItem.OrderIDSub);
    }

private async void LoadOrders()
    {
        var orders = await _orderDataService.GetOrdersAsync();
        foreach (var order in orders)
        {
            await order.LoadOrderItems(_orderDataService); 
            Orders.Add(order);
        }
        

    }

    private void InitializeCommands()
    {
        // Initialize your commands with actions or methods here...
        SendCommand = new Command(async () => await SendCommand1());
        MoveCommand = new Command(() => {/* Implement move action */});
        DiscountCommand = new Command(async () => await ShowDiscountModal());
        SplitCommand = new Command(() => {/* Implement split action */});
        PayCommand =   new Command(async () => await OpenPaymentModal());

        ExitCommand = new Command(() => {/* Implement exit action */});
    }
    private async Task OpenPaymentModal()
    {
        var paymentPage = new PaymentModalPage();
        await Application.Current.MainPage.Navigation.PushModalAsync(paymentPage);
    }
    
    private async Task ShowDiscountModal()
    {
        var discountPage = new DiscountPage();
        await Application.Current.MainPage.Navigation.PushModalAsync(discountPage);
    }

    private async Task SendCommand1()
    {
        var MidPage = new MidCashReg();
        await Application.Current.MainPage.Navigation.PushModalAsync(MidPage);
    }



}
