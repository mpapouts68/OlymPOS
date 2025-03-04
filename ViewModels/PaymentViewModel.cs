using System;
using System.Threading.Tasks;
using System.Windows.Input;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IPrintService _printService;

        private decimal _orderTotal;
        private decimal _cashAmount;
        private decimal _cardAmount;
        private decimal _voucherAmount;
        private decimal _changeAmount;
        private bool _shouldPrintReceipt;
        private Order _currentOrder;

        public decimal OrderTotal
        {
            get => _orderTotal;
            set => SetProperty(ref _orderTotal, value);
        }

        public decimal CashAmount
        {
            get => _cashAmount;
            set
            {
                if (SetProperty(ref _cashAmount, value))
                {
                    CalculateChange();
                }
            }
        }

        public decimal CardAmount
        {
            get => _cardAmount;
            set
            {
                if (SetProperty(ref _cardAmount, value))
                {
                    CalculateChange();
                }
            }
        }

        public decimal VoucherAmount
        {
            get => _voucherAmount;
            set
            {
                if (SetProperty(ref _voucherAmount, value))
                {
                    CalculateChange();
                }
            }
        }

        public decimal ChangeAmount
        {
            get => _changeAmount;
            set => SetProperty(ref _changeAmount, value);
        }

        public bool ShouldPrintReceipt
        {
            get => _shouldPrintReceipt;
            set => SetProperty(ref _shouldPrintReceipt, value);
        }

        // Commands
        public ICommand CashPaymentCommand => GetCommand(nameof(CashPaymentCommand), ProcessCashPaymentAsync);
        public ICommand CardPaymentCommand => GetCommand(nameof(CardPaymentCommand), ProcessCardPaymentAsync);
        public ICommand VoucherPaymentCommand => GetCommand(nameof(VoucherPaymentCommand), ProcessVoucherPaymentAsync);
        public ICommand CompletePaymentCommand => GetCommand(nameof(CompletePaymentCommand), CompletePaymentAsync);
        public ICommand CancelCommand => GetCommand(nameof(CancelCommand), CancelPaymentAsync);
        public ICommand PrintOnlyCommand => GetCommand(nameof(PrintOnlyCommand), PrintOnlyAsync);
        public ICommand CloseOnlyCommand => GetCommand(nameof(CloseOnlyCommand), CloseOnlyAsync);

        public PaymentViewModel(
            IOrderService orderService,
            IPrintService printService)
        {
            _orderService = orderService;
            _printService = printService;

            Title = "Payment";
            ShouldPrintReceipt = true;
        }

        protected override async Task OnInitializeAsync()
        {
            await LoadCurrentOrderAsync();
            await base.OnInitializeAsync();
        }

        private async Task LoadCurrentOrderAsync()
        {
            if (ProgSettings.Actordrid <= 0)
                return;

            IsBusy = true;

            try
            {
                // Get the current order
                var orderRepository = DependencyService.Resolve<IOrderRepository>();
                _currentOrder = await orderRepository.GetByIdAsync(ProgSettings.Actordrid);

                if (_currentOrder != null)
                {
                    // Set the order total
                    if (_currentOrder.HasDiscount && _currentOrder.OrderTotalAfterD.HasValue)
                    {
                        OrderTotal = (decimal)_currentOrder.OrderTotalAfterD.Value;
                    }
                    else if (_currentOrder.OrderTotal.HasValue)
                    {
                        OrderTotal = _currentOrder.OrderTotal.Value;
                    }
                    else
                    {
                        // Calculate from order items
                        decimal total = 0;
                        foreach (var item in _currentOrder.OrderItems)
                        {
                            total += item.Price * item.Quantity;
                        }
                        OrderTotal = total;
                    }

                    // Initialize payment amounts
                    CashAmount = 0;
                    CardAmount = 0;
                    VoucherAmount = 0;
                    ChangeAmount = 0;
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading order", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void CalculateChange()
        {
            decimal totalPayment = CashAmount + CardAmount + VoucherAmount;
            ChangeAmount = Math.Max(0, totalPayment - OrderTotal);
        }

        private async Task ProcessCashPaymentAsync()
        {
            // Set the full amount to cash payment
            CashAmount = OrderTotal;
            CardAmount = 0;
            VoucherAmount = 0;

            // Process payment
            await CompletePaymentAsync();
        }

        private async Task ProcessCardPaymentAsync()
        {
            // Set the full amount to card payment
            CashAmount = 0;
            CardAmount = OrderTotal;
            VoucherAmount = 0;

            // Process payment
            await CompletePaymentAsync();
        }

        private async Task ProcessVoucherPaymentAsync()
        {
            // Set the full amount to voucher payment
            CashAmount = 0;
            CardAmount = 0;
            VoucherAmount = OrderTotal;

            // Process payment
            await CompletePaymentAsync();
        }

        private async Task CompletePaymentAsync()
        {
            if (_currentOrder == null)
                return;

            IsBusy = true;

            try
            {
                // Validate payment amount
                decimal totalPayment = CashAmount + CardAmount + VoucherAmount;
                if (totalPayment < OrderTotal)
                {
                    await ShowErrorAsync("Invalid Payment", "The payment amount is less than the order total.");
                    return;
                }

                // Create payment details
                var paymentDetails = new PaymentDetails
                {
                    CashAmount = CashAmount,
                    CardAmount = CardAmount,
                    VoucherAmount = VoucherAmount,
                    PrintReceipt = ShouldPrintReceipt,
                    PaymentTime = DateTime.Now
                };

                // Process payment
                bool success = await _orderService.ProcessPaymentAsync(_currentOrder.OrderID, paymentDetails);

                if (success)
                {
                    // Show success message with change amount if applicable
                    if (ChangeAmount > 0)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Payment Successful",
                            $"Change amount: {ChangeAmount:C}",
                            "OK");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Payment Successful",
                            "The payment has been processed successfully.",
                            "OK");
                    }

                    // Close payment page
                    await ClosePageAsync();
                }
                else
                {
                    await ShowErrorAsync("Payment Failed", "Failed to process the payment. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Payment Error", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelPaymentAsync()
        {
            await ClosePageAsync();
        }

        private async Task PrintOnlyAsync()
        {
            if (_currentOrder == null)
                return;

            IsBusy = true;

            try
            {
                // Just print the receipt without processing payment
                await _printService.PrintOrderAsync(_currentOrder.OrderID, true);

                await ClosePageAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Print Error", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CloseOnlyAsync()
        {
            if (_currentOrder == null)
                return;

            IsBusy = true;

            try
            {
                // Create payment details with zero amounts
                var paymentDetails = new PaymentDetails
                {
                    CashAmount = 0,
                    CardAmount = 0,
                    VoucherAmount = 0,
                    PrintReceipt = false,
                    PaymentTime = DateTime.Now
                };

                // Process payment (just close the order)
                bool success = await _orderService.ProcessPaymentAsync(_currentOrder.OrderID, paymentDetails);

                if (success)
                {
                    await ClosePageAsync();
                }
                else
                {
                    await ShowErrorAsync("Close Failed", "Failed to close the order. Please try again.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Close Error", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClosePageAsync()
        {
            await Application.Current.MainPage.Navigation.PopModalAsync();
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}
