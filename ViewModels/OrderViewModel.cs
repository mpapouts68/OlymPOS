using OlymPOS.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OlymPOS.ViewModels
{
    public class OrderViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IAuthenticationService _authService;
        private int _activeOrderId;
        private Order _activeOrder;
        private ObservableCollection<OrderItem> _orderItems;
        private decimal _orderTotal;
        private decimal _orderDiscount;
        private decimal _orderTotalAfterDiscount;

        public int ActiveOrderId
        {
            get => _activeOrderId;
            set
            {
                if (SetProperty(ref _activeOrderId, value))
                {
                    ProgSettings.Actordrid = value;
                    LoadOrderAsync().ConfigureAwait(false);
                }
            }
        }

        public Order ActiveOrder
        {
            get => _activeOrder;
            set => SetProperty(ref _activeOrder, value);
        }

        public ObservableCollection<OrderItem> OrderItems
        {
            get => _orderItems;
            set => SetProperty(ref _orderItems, value);
        }

        public decimal OrderTotal
        {
            get => _orderTotal;
            set => SetProperty(ref _orderTotal, value);
        }

        public decimal OrderDiscount
        {
            get => _orderDiscount;
            set => SetProperty(ref _orderDiscount, value);
        }

        public decimal OrderTotalAfterDiscount
        {
            get => _orderTotalAfterDiscount;
            set => SetProperty(ref _orderTotalAfterDiscount, value);
        }

        // Commands
        public ICommand SendCommand => GetCommand(nameof(SendCommand), SendOrderAsync);
        public ICommand MoveCommand => GetCommand(nameof(MoveCommand), MoveOrderAsync);
        public ICommand DiscountCommand => GetCommand(nameof(DiscountCommand), ApplyDiscountAsync);
        public ICommand SplitCommand => GetCommand(nameof(SplitCommand), SplitOrderAsync);
        public ICommand PayCommand => GetCommand(nameof(PayCommand), ProcessPaymentAsync);
        public ICommand ExitCommand => GetCommand(nameof(ExitCommand), ExitOrderAsync);
        public ICommand OrderItemExpanderCommand => GetCommand<OrderItem>(nameof(OrderItemExpanderCommand), OnOrderItemExpanderOpenedAsync);

        public OrderViewModel(IOrderService orderService, IAuthenticationService authService)
        {
            _orderService = orderService;
            _authService = authService;

            OrderItems = new ObservableCollection<OrderItem>();

            // Initialize from the current active order ID in the global settings
            ActiveOrderId = ProgSettings.Actordrid;
        }

        protected override async Task OnInitializeAsync()
        {
            await LoadOrderAsync();
            await base.OnInitializeAsync();
        }

        private async Task LoadOrderAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                // Clear current items
                OrderItems.Clear();

                // Load the order
                var orderRepository = DependencyService.Resolve<IOrderRepository>();
                ActiveOrder = await orderRepository.GetByIdAsync(ActiveOrderId);

                if (ActiveOrder != null)
                {
                    // Load order items
                    foreach (var item in ActiveOrder.OrderItems)
                    {
                        OrderItems.Add(item);
                    }

                    // Update totals
                    OrderTotal = ActiveOrder.OrderTotal ?? 0;
                    OrderDiscount = ActiveOrder.HasDiscount && ActiveOrder.DiscountPercentage.HasValue ?
                        (OrderTotal * ActiveOrder.DiscountPercentage.Value / 100) : 0;
                    OrderTotalAfterDiscount = ActiveOrder.OrderTotalAfterD.HasValue ?
                        (decimal)ActiveOrder.OrderTotalAfterD.Value : OrderTotal - OrderDiscount;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task OnOrderItemExpanderOpenedAsync(OrderItem orderItem)
        {
            if (orderItem.HasExtra && orderItem.OrderExtras.Count == 0)
            {
                // Load extras if not already loaded
                await orderItem.LoadOrderExtrasAsync(DependencyService.Resolve<OrderDataService>(), orderItem.OrderIDSub);
            }
        }

        private async Task SendOrderAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                // Display options to user (Print, Don't Print)
                bool withReceipt = await Application.Current.MainPage.DisplayAlert(
                    "Send Order",
                    "Do you want to print a receipt for this order?",
                    "Yes", "No");

                // Send to printers
                await _orderService.SendOrderToPrinterAsync(ActiveOrderId, withReceipt);

                // Refresh to show updated status
                await LoadOrderAsync();

                // Show confirmation
                await Application.Current.MainPage.DisplayAlert(
                    "Order Sent",
                    "The order has been sent to the kitchen.",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task MoveOrderAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                // Get available tables
                var tableRepository = DependencyService.Resolve<ITableRepository>();
                var availableTables = await tableRepository.GetAllAsync();

                // Filter to only show free tables
                var freeTables = availableTables.Where(t => !t.Active).ToList();

                if (freeTables.Count == 0)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "No Tables Available",
                        "There are no free tables available to move this order to.",
                        "OK");
                    return;
                }

                // Show table selection
                string result = await Application.Current.MainPage.DisplayActionSheet(
                    "Select Table to Move To",
                    "Cancel",
                    null,
                    freeTables.Select(t => t.FullDescription).ToArray());

                if (result == "Cancel" || string.IsNullOrEmpty(result))
                    return;

                // Find the selected table
                var selectedTable = freeTables.FirstOrDefault(t => t.FullDescription == result);
                if (selectedTable == null)
                    return;

                // Update the order with the new table
                var orderRepository = DependencyService.Resolve<IOrderRepository>();
                var order = await orderRepository.GetByIdAsync(ActiveOrderId);

                if (order != null)
                {
                    // Release the old table if there was one
                    if (order.PostID.HasValue)
                    {
                        await tableRepository.SetTableStatusAsync(order.PostID.Value, false);
                    }

                    // Update order with new table
                    order.PostID = selectedTable.PostID;
                    order.PostNumber = selectedTable.PostNumber;
                    order.Description = $"Table {selectedTable.PostNumber}";
                    await orderRepository.UpdateAsync(order);

                    // Update table status
                    await tableRepository.SetTableStatusAsync(selectedTable.PostID, true, ActiveOrderId);

                    // Refresh
                    await LoadOrderAsync();

                    // Show confirmation
                    await Application.Current.MainPage.DisplayAlert(
                        "Order Moved",
                        $"The order has been moved to {selectedTable.FullDescription}.",
                        "OK");
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyDiscountAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                // Show discount page
                var discountPage = new DiscountPage();
                await Application.Current.MainPage.Navigation.PushModalAsync(discountPage);

                // The DiscountPage will handle updating the order
                // We just need to refresh when it's done
                await LoadOrderAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SplitOrderAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Not Implemented",
                    "The split order functionality is not yet implemented.",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ProcessPaymentAsync()
        {
            if (ActiveOrderId <= 0)
                return;

            IsBusy = true;

            try
            {
                // Show payment modal
                var paymentPage = new PaymentModalPage();
                await Application.Current.MainPage.Navigation.PushModalAsync(paymentPage);

                // The PaymentModalPage will handle processing the payment
                // We just need to refresh when it's done
                await LoadOrderAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExitOrderAsync()
        {
            // Go back to the table view
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}