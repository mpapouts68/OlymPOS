using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OlymPOS.Services.Interfaces;
using OlymPOS.ViewModels.Base;

namespace OlymPOS.ViewModels
{
    public class TableViewModel : BaseViewModel
    {
        private readonly ITableRepository _tableRepository;
        private readonly IOrderService _orderService;
        private readonly IAuthenticationService _authService;

        private ObservableCollection<ServicingPoint> _tables;
        private List<(int Id, string Description)> _areas;
        private int _selectedAreaId;
        private string _selectedAreaName;

        public ObservableCollection<ServicingPoint> Tables
        {
            get => _tables;
            set => SetProperty(ref _tables, value);
        }

        public List<(int Id, string Description)> Areas
        {
            get => _areas;
            set => SetProperty(ref _areas, value);
        }

        public int SelectedAreaId
        {
            get => _selectedAreaId;
            set
            {
                if (SetProperty(ref _selectedAreaId, value))
                {
                    var area = Areas.FirstOrDefault(a => a.Id == value);
                    SelectedAreaName = area.Description;
                    LoadTablesAsync(value).ConfigureAwait(false);
                }
            }
        }

        public string SelectedAreaName
        {
            get => _selectedAreaName;
            set => SetProperty(ref _selectedAreaName, value);
        }

        // Commands
        public ICommand RefreshCommand => GetCommand(nameof(RefreshCommand), RefreshAsync);
        public ICommand SelectTableCommand => GetCommand<ServicingPoint>(nameof(SelectTableCommand), SelectTableAsync);
        public ICommand LogoutCommand => GetCommand(nameof(LogoutCommand), LogoutAsync);
        public ICommand StatisticsCommand => GetCommand(nameof(StatisticsCommand), ShowStatisticsAsync);
        public ICommand SettingsCommand => GetCommand(nameof(SettingsCommand), ShowSettingsAsync);
        public ICommand SelectAreaCommand => GetCommand<int>(nameof(SelectAreaCommand), SelectAreaAsync);

        public TableViewModel(
            ITableRepository tableRepository,
            IOrderService orderService,
            IAuthenticationService authService)
        {
            _tableRepository = tableRepository;
            _orderService = orderService;
            _authService = authService;

            Title = "Tables";
            Tables = new ObservableCollection<ServicingPoint>();
            Areas = new List<(int Id, string Description)>();
        }

        protected override async Task OnInitializeAsync()
        {
            await LoadAreasAsync();
            await base.OnInitializeAsync();
        }

        private async Task LoadAreasAsync()
        {
            IsBusy = true;

            try
            {
                var tables = await _tableRepository.GetAllAsync();
                var areas = tables
                    .Select(t => new { t.YperMainID, Description = GetAreaDescription(t.YperMainID) })
                    .GroupBy(a => a.YperMainID)
                    .Select(g => (Id: g.Key, Description: g.First().Description))
                    .ToList();

                Areas = areas;

                if (areas.Any())
                {
                    // Set default area - use the one from user settings if available
                    int defaultAreaId = UserSettings.Defypd;
                    if (areas.Any(a => a.Id == defaultAreaId))
                    {
                        SelectedAreaId = defaultAreaId;
                    }
                    else
                    {
                        SelectedAreaId = areas.First().Id;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading areas", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadTablesAsync(int areaId)
        {
            IsBusy = true;

            try
            {
                Tables.Clear();

                var tables = await _tableRepository.GetByAreaAsync(areaId);

                foreach (var table in tables.OrderBy(t => t.PostNumber))
                {
                    Tables.Add(table);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading tables", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            if (SelectedAreaId > 0)
            {
                await LoadTablesAsync(SelectedAreaId);
            }
        }

        private async Task SelectTableAsync(ServicingPoint table)
        {
            if (table == null)
                return;

            try
            {
                if (table.Active)
                {
                    // Open existing order
                    ProgSettings.Actordrid = table.ActiveOrderID;
                    ProgSettings.Actpostid = table.PostID;

                    // Navigate to order page
                    await NavigateToOrderPageAsync();
                }
                else
                {
                    // Ask if the user wants to create a new order
                    bool createNew = await Application.Current.MainPage.DisplayAlert(
                        "Create Order",
                        $"Do you want to create a new order for {table.FullDescription}?",
                        "Yes", "No");

                    if (createNew)
                    {
                        // Create new order
                        int orderId = await _orderService.CreateOrderAsync(table.PostID);

                        // Navigate to order page
                        await NavigateToOrderPageAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error selecting table", ex.Message);
            }
        }

        private async Task LogoutAsync()
        {
            try
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes", "No");

                if (confirm)
                {
                    await _authService.LogoutAsync();

                    // Navigate to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error logging out", ex.Message);
            }
        }

        private async Task ShowStatisticsAsync()
        {
            try
            {
                // Navigate to statistics page
                await Shell.Current.GoToAsync("StatisticsPage");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error navigating to statistics", ex.Message);
            }
        }

        private async Task ShowSettingsAsync()
        {
            try
            {
                // Navigate to settings page - not implemented yet
                await Application.Current.MainPage.DisplayAlert(
                    "Settings",
                    "Settings page is not implemented yet.",
                    "OK");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error navigating to settings", ex.Message);
            }
        }

        private async Task SelectAreaAsync(int areaId)
        {
            SelectedAreaId = areaId;
            await Task.CompletedTask;
        }

        private async Task NavigateToOrderPageAsync()
        {
            // Navigate to order page
            await Shell.Current.GoToAsync("OrderIntake");
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        // Helper method to get area description
        private string GetAreaDescription(int areaId)
        {
            // Normally we would get this from a repository, but for now just use a placeholder
            return $"Area {areaId}";
        }
    }
}
