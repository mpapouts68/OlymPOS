using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Dispatching;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.ViewModels
{
    public class ItemsViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        private ObservableCollection<Product> _allProducts;
        public ObservableCollection<Product> AllProducts
        {
            get => _allProducts;
            set
            {
                _allProducts = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Product> _displayedProducts;
        public ObservableCollection<Product> DisplayedProducts
        {
            get => _displayedProducts;
            set
            {
                _displayedProducts = value;
                OnPropertyChanged();
            }
        }

        public ItemsViewModel(IDataService dataService)
        {
            _dataService = dataService;
            AllProducts = new ObservableCollection<Product>(_dataService.AllProducts);
            DisplayedProducts = new ObservableCollection<Product>(AllProducts.Where(p => p.Favorite));
        }

        public void IncreaseQuantity(Product product)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                product.Quantity++;
                Console.WriteLine("Quantity increased for: " + product.Description);
            });
        }

        public void DecreaseQuantity(Product product)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (product.Quantity > 0)
                {
                    product.Quantity--;
                    Console.WriteLine("Quantity decreased for: " + product.Description);
                }
            });
        }

        public Task ShowExtras(Product product)
        {
            Console.WriteLine($"Showing extras for: {product.Description}");
            return Task.CompletedTask;
        }

        public void PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                DisplayedProducts = new ObservableCollection<Product>(AllProducts.Where(p => p.Favorite));
            }
            else
            {
                var lowerQuery = query.ToLower();
                DisplayedProducts = new ObservableCollection<Product>(
                    AllProducts.Where(p => p.Description.ToLower().Contains(lowerQuery)));
            }
        }

        public void FilterCat()
        {
            int catid = ProgSettings.ActGrpid;
            Console.WriteLine("Filter is fired");
            DisplayedProducts = new ObservableCollection<Product>(
                AllProducts.Where(p => p.ProductGroupID == catid));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
