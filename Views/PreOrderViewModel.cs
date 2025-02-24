using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using OlymPOS;

namespace OlymPOS
{
    public class PreOrderViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Data Collections
        public ObservableCollection<ProductGroup> Categories { get; set; }
        public ObservableCollection<Product> FavoriteItems { get; set; }

        // Flyout State
        private bool _isFlyoutVisible = true;
        public bool IsFlyoutVisible
        {
            get => _isFlyoutVisible;
            set
            {
                _isFlyoutVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFlyoutVisible)));
            }
        }

        // Commands
        public ICommand ToggleFlyoutCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand ShowExtrasCommand { get; }
        public ICommand DeleteItemCommand { get; }

        public PreOrderViewModel()
        {
            // Load Product Categories and Favorite Items
            LoadCategories();
            LoadFavoriteItems();

            // Initialize Commands
            ToggleFlyoutCommand = new Command(ToggleFlyout);
            IncreaseQuantityCommand = new Command<Product>(IncreaseQuantity);
            ShowExtrasCommand = new Command<Product>(ShowExtras);
            DeleteItemCommand = new Command<Product>(DeleteItem);
        }

        // ***** Placeholder Functions ***** //

        private void LoadCategories()
        {
            // Replace with your logic to get ProductGroup data from DataService
        }

        private void LoadFavoriteItems()
        {
            // Replace with your logic to get Products from DataService
        }

        // ***** Command Implementations ***** //

        private void ToggleFlyout() => IsFlyoutVisible = !IsFlyoutVisible;

        private void IncreaseQuantity(Product item)
        {
            // Implement logic to increase the quantity of the item
        }

        private void ShowExtras(Product item)
        {
            // Implement logic to navigate to extras view and pass 'item'
        }

        private void DeleteItem(Product item)
        {
            // Implement logic to delete 'item' from FavoriteItems
        }
    }
}
