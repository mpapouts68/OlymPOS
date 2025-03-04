using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces; // Added for IDataService
using OlymPOS.Services;

namespace OlymPOS.ViewModels // Corrected namespace
{
    public partial class CombinedViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        [ObservableProperty]
        private ObservableCollection<Product> displayedProducts;

        [ObservableProperty]
        private ObservableCollection<Extra> selectedExtras;

        [ObservableProperty]
        private ObservableCollection<OrderItem> orderItems;

        public ObservableCollection<ProductGroup> ProductCategories => _dataService.ProductCategories;

        public CombinedViewModel(IDataService dataService)
        {
            _dataService = dataService;
            DisplayedProducts = new ObservableCollection<Product>();
            SelectedExtras = new ObservableCollection<Extra>();
            OrderItems = new ObservableCollection<OrderItem>();
        }

        public void LoadFavorites()
        {
            DisplayedProducts.Clear();
            foreach (var product in _dataService.FavoriteProducts)
            {
                DisplayedProducts.Add(product);
            }
        }

        public void PerformSearch(string searchText)
        {
            DisplayedProducts.Clear();
            var results = _dataService.AllProducts
                .Where(p => p.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            foreach (var product in results)
            {
                DisplayedProducts.Add(product);
            }
        }

        public void IncreaseQuantity(Product product)
        {
            var orderItem = OrderItems.FirstOrDefault(oi => oi.ProductID == product.ProductID);
            if (orderItem == null)
            {
                orderItem = new OrderItem
                {
                    ProductID = product.ProductID,
                    Description = product.Description,
                    Price = product.Price,
                    Quantity = 0
                };
                OrderItems.Add(orderItem);
            }
            orderItem.Quantity++;
            UpdateDisplayedProduct(product);
        }

        public void DecreaseQuantity(Product product)
        {
            var orderItem = OrderItems.FirstOrDefault(oi => oi.ProductID == product.ProductID);
            if (orderItem != null && orderItem.Quantity > 0)
            {
                orderItem.Quantity--;
                if (orderItem.Quantity == 0)
                    OrderItems.Remove(orderItem);
                UpdateDisplayedProduct(product);
            }
        }

        private void UpdateDisplayedProduct(Product product)
        {
            var displayed = DisplayedProducts.FirstOrDefault(p => p.ProductID == product.ProductID);
            if (displayed != null)
            {
                var orderItem = OrderItems.FirstOrDefault(oi => oi.ProductID == product.ProductID);
                displayed.Quantity = orderItem?.Quantity ?? 0;
            }
        }

        public void FilterProductsByCategory()
        {
            DisplayedProducts.Clear();
            var filtered = _dataService.AllProducts
                .Where(p => p.ProductGroupID == ProgSettings.ActGrpid);
            foreach (var product in filtered)
            {
                DisplayedProducts.Add(product);
            }
        }

        [RelayCommand]
        public async Task ShowExtras()
        {
            SelectedExtras.Clear();
            var extras = await _dataService.QueryAsync<Extra>(
                "SELECT ExtraID AS ExtraID, Description, Price, ProductID FROM Extras WHERE ProductID = @ProductID",
                new { ProductID = ProgSettings.Actprodrid });
            foreach (var extra in extras)
            {
                SelectedExtras.Add(extra);
            }
        }
    }
}