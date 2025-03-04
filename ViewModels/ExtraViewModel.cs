using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.ViewModels
{
    public class ExtrasViewModel : BaseViewModel
    {
        private readonly IOrderService _orderService;
        private readonly IProductRepository _productRepository;

        private ObservableCollection<Extra> _extras;
        private ObservableCollection<Course> _courses;
        private Product _currentProduct;
        private int _selectedCourseId;
        private bool _hasCourseOptions;

        public ObservableCollection<Extra> Extras
        {
            get => _extras;
            set => SetProperty(ref _extras, value);
        }

        public ObservableCollection<Course> Courses
        {
            get => _courses;
            set => SetProperty(ref _courses, value);
        }

        public Product CurrentProduct
        {
            get => _currentProduct;
            set => SetProperty(ref _currentProduct, value);
        }

        public int SelectedCourseId
        {
            get => _selectedCourseId;
            set => SetProperty(ref _selectedCourseId, value);
        }

        public bool HasCourseOptions
        {
            get => _hasCourseOptions;
            set => SetProperty(ref _hasCourseOptions, value);
        }

        // Commands
        public ICommand SelectExtraCommand => GetCommand<Extra>(nameof(SelectExtraCommand), SelectExtraAsync);
        public ICommand SelectCourseCommand => GetCommand<Course>(nameof(SelectCourseCommand), SelectCourseAsync);
        public ICommand SaveCommand => GetCommand(nameof(SaveCommand), SaveExtrasAsync);
        public ICommand CancelCommand => GetCommand(nameof(CancelCommand), CancelExtrasAsync);
        public ICommand AddPrefixCommand => GetCommand<string>(nameof(AddPrefixCommand), AddPrefixAsync);

        public ExtrasViewModel(
            IOrderService orderService,
            IProductRepository productRepository)
        {
            _orderService = orderService;
            _productRepository = productRepository;

            Title = "Product Options";
            Extras = new ObservableCollection<Extra>();
            Courses = new ObservableCollection<Course>();
        }

        protected override async Task OnInitializeAsync()
        {
            // Load courses from DataService
            LoadCourses();

            // Load current product
            await LoadCurrentProductAsync();

            // Load extras for the product
            await LoadExtrasAsync();

            await base.OnInitializeAsync();
        }

        private void LoadCourses()
        {
            try
            {
                Courses.Clear();

                // Get courses from DataService
                var courses = DataService.Instance.Courses;
                if (courses != null && courses.Any())
                {
                    foreach (var course in courses)
                    {
                        Courses.Add(course);
                    }

                    HasCourseOptions = Courses.Count > 0;

                    // Set the selected course to the one from settings
                    if (ProgSettings.courseid > 0)
                    {
                        SelectedCourseId = ProgSettings.courseid;
                    }
                    else if (Courses.Any())
                    {
                        SelectedCourseId = Courses.First().CourseId;
                    }
                }
                else
                {
                    HasCourseOptions = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading courses: {ex.Message}");
                HasCourseOptions = false;
            }
        }

        private async Task LoadCurrentProductAsync()
        {
            if (ProgSettings.Actprodrid <= 0)
                return;

            try
            {
                CurrentProduct = await _productRepository.GetByIdAsync(ProgSettings.Actprodrid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading current product: {ex.Message}");
            }
        }

        private async Task LoadExtrasAsync()
        {
            if (CurrentProduct == null)
                return;

            IsBusy = true;

            try
            {
                Extras.Clear();

                // Load extras from different sources
                await LoadProductExtrasAsync();
                await LoadProductGroupExtrasAsync();
                await LoadGeneralExtrasAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading extras", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadProductExtrasAsync()
        {
            // Load extras specific to this product
            try
            {
                if (CurrentProduct.Extra_ID.HasValue)
                {
                    // This would normally use a repository, but using direct DB access for now
                    using var connection = new MySqlConnection(GlobalConString.ConnStr);
                    await connection.OpenAsync();

                    var query = $"SELECT * FROM Qry_Extra WHERE Product_ID = {CurrentProduct.ProductID}";
                    using var command = new MySqlCommand(query, connection);
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        Extras.Add(new Extra
                        {
                            ExtraId = reader.GetInt32("Extra_ID"),
                            Description = reader.GetString("Description"),
                            Price = reader.GetDecimal("Price"),
                            IsSelected = false,
                            quantity = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product extras: {ex.Message}");
            }
        }

        private async Task LoadProductGroupExtrasAsync()
        {
            // Load extras for the product group
            try
            {
                using var connection = new MySqlConnection(GlobalConString.ConnStr);
                await connection.OpenAsync();

                var query = $"SELECT * FROM qry_extra_group WHERE ProductGroup_ID = {CurrentProduct.ProductGroupID}";
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    // Check if this extra is already added
                    int extraId = reader.GetInt32("Extra_ID");
                    if (!Extras.Any(e => e.ExtraId == extraId))
                    {
                        Extras.Add(new Extra
                        {
                            ExtraId = extraId,
                            Description = reader.GetString("Description"),
                            Price = reader.GetDecimal("Price"),
                            IsSelected = false,
                            quantity = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading product group extras: {ex.Message}");
            }
        }

        private async Task LoadGeneralExtrasAsync()
        {
            // Load general extras
            try
            {
                using var connection = new MySqlConnection(GlobalConString.ConnStr);
                await connection.OpenAsync();

                var query = "SELECT * FROM hypergrp_relation_extra";
                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    // Check if this extra is already added
                    int extraId = reader.GetInt32("Extra_ID");
                    if (!Extras.Any(e => e.ExtraId == extraId))
                    {
                        Extras.Add(new Extra
                        {
                            ExtraId = extraId,
                            Description = reader.GetString("Description"),
                            Price = reader.GetDecimal("Price"),
                            IsSelected = false,
                            quantity = 0
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading general extras: {ex.Message}");
            }
        }

        private async Task SelectExtraAsync(Extra extra)
        {
            if (extra == null)
                return;

            // Toggle selection
            extra.IsSelected = !extra.IsSelected;

            // Update quantity
            if (extra.IsSelected && extra.quantity == 0)
            {
                extra.quantity = 1;
            }
            else if (!extra.IsSelected)
            {
                extra.quantity = 0;
            }

            // Refresh display
            OnPropertyChanged(nameof(Extras));

            await Task.CompletedTask;
        }

        private async Task SelectCourseAsync(Course course)
        {
            if (course == null)
                return;

            SelectedCourseId = course.CourseId;
            ProgSettings.courseid = course.CourseId;

            await Task.CompletedTask;
        }

        private async Task SaveExtrasAsync()
        {
            if (CurrentProduct == null || ProgSettings.Actordrid <= 0)
                return;

            IsBusy = true;

            try
            {
                // Get the selected extras
                var selectedExtras = Extras.Where(e => e.IsSelected && e.quantity > 0).ToList();

                if (selectedExtras.Any())
                {
                    // Mark the product as having extras
                    // This would use OrderRepository in a proper implementation
                    using var connection = new MySqlConnection(GlobalConString.ConnStr);
                    await connection.OpenAsync();

                    // Get the order item
                    var query = $"SELECT Order_ID_Sub FROM Orders_Actual WHERE Order_ID = {ProgSettings.Actordrid} AND Product_ID = {CurrentProduct.ProductID} ORDER BY Order_ID_Sub DESC LIMIT 1";
                    using var command = new MySqlCommand(query, connection);
                    var orderItemId = (int)await command.ExecuteScalarAsync();

                    if (orderItemId > 0)
                    {
                        // Mark the order item as having extras
                        var updateQuery = $"UPDATE Orders_Actual SET Has_Extra = 1, Serving_Row = {SelectedCourseId} WHERE Order_ID_Sub = {orderItemId}";
                        using var updateCommand = new MySqlCommand(updateQuery, connection);
                        await updateCommand.ExecuteNonQueryAsync();

                        // Add the extras
                        foreach (var extra in selectedExtras)
                        {
                            var extraQuery = $"INSERT INTO Order_Extras_Sub (Order_ID_Sub, Quantity, Prefix, Extra_ID) VALUES ({orderItemId}, {extra.quantity}, @prefix, {extra.ExtraId})";
                            using var extraCommand = new MySqlCommand(extraQuery, connection);
                            extraCommand.Parameters.AddWithValue("@prefix", extra.Description ?? "");
                            await extraCommand.ExecuteNonQueryAsync();
                        }
                    }
                }

                // Close the extras page
                await ClosePageAsync();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error saving extras", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CancelExtrasAsync()
        {
            await ClosePageAsync();
        }

        private async Task AddPrefixAsync(string prefix)
        {
            // Add prefix to selected extras
            var selectedExtras = Extras.Where(e => e.IsSelected).ToList();

            foreach (var extra in selectedExtras)
            {
                extra.Description = $"{prefix} {extra.Description}";
            }

            // Refresh display
            OnPropertyChanged(nameof(Extras));

            await Task.CompletedTask;
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
