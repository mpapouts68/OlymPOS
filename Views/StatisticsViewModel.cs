using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IntelliJ.Lang.Annotations;
using OlymPOS.Services.Interfaces;
using static Android.Icu.Text.CaseMap;

namespace OlymPOS.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPrintService _printService;

        private ObservableCollection<Order> _actualOrders;
        private ObservableCollection<Order> _historicalOrders;
        private decimal _actualOrdersTotal;
        private decimal _historicalOrdersTotal;
        private decimal _grandTotal;
        private DateTime _startDate;
        private DateTime _endDate;
        private bool _isDateFilterEnabled;

        public ObservableCollection<Order> ActualOrders
        {
            get => _actualOrders;
            set => SetProperty(ref _actualOrders, value);
        }

        public ObservableCollection<Order> HistoricalOrders
        {
            get => _historicalOrders;
            set => SetProperty(ref _historicalOrders, value);
        }

        public decimal ActualOrdersTotal
        {
            get => _actualOrdersTotal;
            set => SetProperty(ref _actualOrdersTotal, value);
        }

        public decimal HistoricalOrdersTotal
        {
            get => _historicalOrdersTotal;
            set => SetProperty(ref _historicalOrdersTotal, value);
        }

        public decimal GrandTotal
        {
            get => _grandTotal;
            set => SetProperty(ref _grandTotal, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value) && _isDateFilterEnabled)
                {
                    ApplyDateFilter();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value) && _isDateFilterEnabled)
                {
                    ApplyDateFilter();
                }
            }
        }

        public bool IsDateFilterEnabled
        {
            get => _isDateFilterEnabled;
            set
            {
                if (SetProperty(ref _isDateFilterEnabled, value))
                {
                    if (value)
                    {
                        ApplyDateFilter();
                    }
                    else
                    {
                        LoadOrdersAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        // Commands
        public ICommand PrintReportCommand => GetCommand(nameof(PrintReportCommand), PrintReportAsync);
        public ICommand ExportCommand => GetCommand(nameof(ExportCommand), ExportDataAsync);
        public ICommand RefreshCommand => GetCommand(nameof(RefreshCommand), RefreshDataAsync);

        public StatisticsViewModel(IOrderRepository orderRepository, IPrintService printService)
        {
            _orderRepository = orderRepository;
            _printService = printService;

            Title = "Statistics";
            ActualOrders = new ObservableCollection<Order>();
            HistoricalOrders = new ObservableCollection<Order>();

            // Set default date range to current month
            var today = DateTime.Today;
            StartDate = new DateTime(today.Year, today.Month, 1);
            EndDate = today;
            IsDateFilterEnabled = false;
        }

        protected override async Task OnInitializeAsync()
        {
            await LoadOrdersAsync();
            await base.OnInitializeAsync();
        }

        private async Task LoadOrdersAsync()
        {
            IsBusy = true;

            try
            {
                // Clear collections
                ActualOrders.Clear();
                HistoricalOrders.Clear();

                // Get clerk ID from user settings
                int clerkId = UserSettings.ClerkID;

                // Load orders for this clerk
                var orders = await _orderRepository.GetOrdersAsync(clerkId);

                // Separate actual and historical orders
                foreach (var order in orders)
                {
                    if (order.History)
                    {
                        HistoricalOrders.Add(order);
                    }
                    else
                    {
                        ActualOrders.Add(order);
                    }
                }

                // Calculate totals
                UpdateTotals();
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error Loading Data",
                    $"An error occurred: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateTotals()
        {
            // Calculate totals
            ActualOrdersTotal = ActualOrders.Sum(o => o.OrderTotal ?? 0);
            HistoricalOrdersTotal = HistoricalOrders.Sum(o => o.OrderTotal ?? 0);
            GrandTotal = ActualOrdersTotal + HistoricalOrdersTotal;
        }

        private void ApplyDateFilter()
        {
            if (!IsDateFilterEnabled)
                return;

            // Ensure end date includes the entire day
            var endDateInclusiveTime = EndDate.Date.AddDays(1).AddTicks(-1);

            // Filter actual orders by date
            var filteredActual = ActualOrders
                .Where(o => o.TimeDate >= StartDate && o.TimeDate <= endDateInclusiveTime)
                .ToList();

            // Filter historical orders by date
            var filteredHistorical = HistoricalOrders
                .Where(o => o.TimeDate >= StartDate && o.TimeDate <= endDateInclusiveTime)
                .ToList();

            // Update the displayed collections
            ActualOrders.Clear();
            foreach (var order in filteredActual)
            {
                ActualOrders.Add(order);
            }

            HistoricalOrders.Clear();
            foreach (var order in filteredHistorical)
            {
                HistoricalOrders.Add(order);
            }

            // Recalculate totals
            UpdateTotals();
        }

        private async Task PrintReportAsync()
        {
            IsBusy = true;

            try
            {
                // Build report content
                string reportTitle = "Sales Report";
                string dateRange = IsDateFilterEnabled
                    ? $"Date Range: {StartDate:d} - {EndDate:d}"
                    : "All Orders";

                string reportContent = $"{reportTitle}\n{dateRange}\n\n" +
                    $"Actual Orders: {ActualOrders.Count}, Total: {ActualOrdersTotal:C}\n" +
                    $"Historical Orders: {HistoricalOrders.Count}, Total: {HistoricalOrdersTotal:C}\n" +
                    $"Grand Total: {GrandTotal:C}";

                // In a real implementation, you would send this to a printer
                // For now, just show it in a dialog
                await Application.Current.MainPage.DisplayAlert(
                    "Print Report",
                    "Report would be printed with the following content:\n\n" + reportContent,
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error Printing Report",
                    $"An error occurred: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportDataAsync()
        {
            IsBusy = true;

            try
            {
                // In a real implementation, you would export to CSV/Excel
                // For now, just show a message
                await Application.Current.MainPage.DisplayAlert(
                    "Export Data",
                    "This would export the current data to a CSV file.",
                    "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error Exporting Data",
                    $"An error occurred: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshDataAsync()
        {
            await LoadOrdersAsync();
            if (IsDateFilterEnabled)
            {
                ApplyDateFilter();
            }
        }
    }
}
