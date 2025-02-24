using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OlymPOS;

namespace OlymPOS
{
    public class ItemsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Product> _allProducts;
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

        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand ShowExtrasCommand { get; }

        public ItemsViewModel()
        {
            _allProducts = new ObservableCollection<Product>(DataService.Instance.AllProducts); // Assuming this is your data source
            DisplayedProducts = new ObservableCollection<Product>(_allProducts.Where(p => p.Favorite));

            IncreaseQuantityCommand = new Command<Product>(IncreaseQuantity);
            DecreaseQuantityCommand = new Command<Product>(DecreaseQuantity);
            ShowExtrasCommand = new Command<Product>(ShowExtras);
        }

        public void PerformSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                // If the query is empty, filter by favorites again
                DisplayedProducts = new ObservableCollection<Product>(_allProducts.Where(p => p.Favorite));
            }
            else
            {
                var lowerQuery = query.ToLower(); // Normalize the query
                DisplayedProducts = new ObservableCollection<Product>(
                    _allProducts.Where(p => p.Description.ToLower().Contains(lowerQuery)));
            }
        }
        public void FilterCat()
        {
            int catid = ProgSettings.ActGrpid;
            Console.WriteLine("Filter is fired");
            //    var lowerQuery = query.ToLower(); // Normalize the query
            DisplayedProducts = new ObservableCollection<Product>(
        _allProducts.Where(p => p.ProductGroupID == catid).ToList());
        }

    
        public void IncreaseQuantity(Product product)
        {
            // Ensure this method is public and correctly implemented
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
                if (product.Quantity > 0) product.Quantity--;
                Console.WriteLine("Quantity decreased for: " + product.Description);
            });
        }

        private void ShowExtras(Product product)
        {
            // Logic to show extras form
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
