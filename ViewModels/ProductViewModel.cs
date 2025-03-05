using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using OlymPOS.Services.Interfaces;
using OlymPOS.ViewModels.Base;

namespace OlymPOS.ViewModels
{
    public class ProductViewModel : BaseViewModel
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductGroupRepository _productGroupRepository;
        private readonly IOrderService _orderService;

        private ObservableCollection<Product> _products;
        private ObservableCollection<ProductGroup> _productGroups;
        private ProductGroup _selectedProductGroup;
        private string _searchQuery;
        private bool _isSearching;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<ProductGroup> ProductGroups
        {
            get => _productGroups;
            set => SetProperty(ref _productGroups, value);
        }

        public ProductGroup SelectedProductGroup
        {
            get => _selectedProductGroup;
            set
            {
                if (SetProperty(ref _selectedProductGroup, value) && value != null)
                {
                    LoadProductsForGroupAsync(value.ProductGroupID).ConfigureAwait(false);
                    ProgSettings.ActGrpid = value.ProductGroupID;
                }
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public bool IsSearching
        {
            get => _isSearching;
            set => SetProperty(ref _isSearching, value);
        }

        // Commands
        public ICommand SearchCommand => GetCommand(nameof(SearchCommand), SearchProductsAsync);
        public ICommand IncreaseQuantityCommand => GetCommand<Product>(nameof(IncreaseQuantityCommand), IncreaseQuantityAsync);
        public ICommand DecreaseQuantityCommand => GetCommand<Product>(nameof(DecreaseQuantityCommand), DecreaseQuantityAsync);
        public ICommand ShowExtrasCommand => GetCommand<Product>(nameof(ShowExtrasCommand), ShowExtrasAsync);
        public ICommand SpeechToTextCommand => GetCommand(nameof(SpeechToTextCommand), StartSpeechToTextAsync);

        public ProductViewModel(
            IProductRepository productRepository,
            IProductGroupRepository productGroupRepository,
            IOrderService orderService)
        {
            _productRepository = productRepository;
            _productGroupRepository = productGroupRepository;
            _orderService = orderService;

            Title = "Products";
            Products = new ObservableCollection<Product>();
            ProductGroups = new ObservableCollection<ProductGroup>();
            SearchQuery = string.Empty;
        }

        protected override async Task OnInitializeAsync()
        {
            await LoadProductGroupsAsync();

            // Load favorite products by default
            await LoadFavoriteProductsAsync();

            await base.OnInitializeAsync();
        }

        private async Task LoadProductGroupsAsync()
        {
            IsBusy = true;

            try
            {
                ProductGroups.Clear();

                var groups = await _productGroupRepository.GetRootGroupsAsync();
                foreach (var group in groups.OrderBy(g => g.Description))
                {
                    ProductGroups.Add(group);
                }

                // If a product group is remembered in settings, select it
                if (ProgSettings.ActGrpid > 0)
                {
                    var savedGroup = ProductGroups.FirstOrDefault(g => g.ProductGroupID == ProgSettings.ActGrpid);
                    if (savedGroup != null)
                    {
                        SelectedProductGroup = savedGroup;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading product groups", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadProductsForGroupAsync(int groupId)
        {
            IsBusy = true;

            try
            {
                Products.Clear();

                var products = await _productRepository.GetByGroupIdAsync(groupId);
                foreach (var product in products.OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading products", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadFavoriteProductsAsync()
        {
            IsBusy = true;

            try
            {
                Products.Clear();

                var products = await _productRepository.GetFavoritesAsync();
                foreach (var product in products.OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error loading favorite products", ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchProductsAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 3)
                return;

            IsBusy = true;
            IsSearching = true;

            try
            {
                Products.Clear();

                var products = await _productRepository.SearchAsync(SearchQuery);
                foreach (var product in products.OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }

                // Clear search after completed
                SearchQuery = string.Empty;
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error searching products", ex.Message);
            }
            finally
            {
                IsBusy = false;
                IsSearching = false;
            }
        }

        private async Task IncreaseQuantityAsync(Product product)
        {
            if (product == null || ProgSettings.Actordrid <= 0)
                return;

            try
            {
                product.Quantity += 1;
                OnPropertyChanged(nameof(Products)); // Update UI

                // Add to current order
                await _orderService.AddProductToOrderAsync(ProgSettings.Actordrid, product.ProductID, 1);

                // Remember current product ID
                ProgSettings.Actprodrid = product.ProductID;

                // If product has extras, prompt to show extras
                if (product.Has_Options || product.Auto_Extra || product.Extra_ID.HasValue)
                {
                    bool showExtras = await Application.Current.MainPage.DisplayAlert(
                        "Product Options",
                        $"Would you like to add extras or options to {product.Description}?",
                        "Yes", "No");

                    if (showExtras)
                    {
                        await ShowExtrasAsync(product);
                    }
                }
            }
            catch (Exception ex)
            {
                product.Quantity -= 1; // Revert quantity change
                OnPropertyChanged(nameof(Products)); // Update UI

                await ShowErrorAsync("Error adding product", ex.Message);
            }
        }

        private async Task DecreaseQuantityAsync(Product product)
        {
            if (product == null || product.Quantity <= 0 || ProgSettings.Actordrid <= 0)
                return;

            try
            {
                product.Quantity -= 1;
                OnPropertyChanged(nameof(Products)); // Update UI

                // Update quantity in order
                await _orderService.UpdateProductQuantityAsync(ProgSettings.Actordrid, product.ProductID, product.Quantity);
            }
            catch (Exception ex)
            {
                product.Quantity += 1; // Revert quantity change
                OnPropertyChanged(nameof(Products)); // Update UI

                await ShowErrorAsync("Error updating product quantity", ex.Message);
            }
        }

        private async Task ShowExtrasAsync(Product product)
        {
            if (product == null)
                return;

            try
            {
                // Store current product ID for extras page
                ProgSettings.Actprodrid = product.ProductID;
                ProgSettings.courseid = product.Row_Print;

                // Navigate to extras page
                await Shell.Current.GoToAsync("ExtrasOptionsPage");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error showing extras", ex.Message);
            }
        }

        private async Task StartSpeechToTextAsync()
        {
            try
            {
                // Check permissions first
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        await ShowErrorAsync("Permission Denied", "Microphone permission is required for speech recognition.");
                        return;
                    }
                }

                // Get speech to text service - would normally use DI, but using DependencyService for now
                var speechService = DependencyService.Get<ISpeechToText>();
                speechService?.StartSpeechToText();

                // The result will come back via MessagingCenter in the page code-behind
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Speech Recognition Error", ex.Message);
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
    }
}