using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OlymPOS;

namespace OlymPOS
{
    public class CombinedViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ProductGroup> productCategories;
        private ObservableCollection<Product> _allProducts;
        private ObservableCollection<Product> _displayedProducts;
        private ProductGroup _selectedProductGroup;

        public ObservableCollection<ProductGroup> ProductCategories
        {
            get => productCategories;
            set
            {
                productCategories = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Product> DisplayedProducts
        {
            get => _displayedProducts;
            set
            {
                _displayedProducts = value;
                OnPropertyChanged();
            }
        }

        public ProductGroup SelectedProductGroup
        {
            get => _selectedProductGroup;
            set
            {
                _selectedProductGroup = value;
                OnPropertyChanged();
                FilterProductsByCategory();
            }
        }

        public ICommand OnSearchCommand { get; }
        public ICommand OnSpeechToTextCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand ShowExtrasCommand { get; }

        public CombinedViewModel()
        {
            ProductCategories = DataService.Instance.ProductCategories;
            _allProducts = new ObservableCollection<Product>(DataService.Instance.AllProducts); // Assuming this is your data source
            DisplayedProducts = new ObservableCollection<Product>(_allProducts.Where(p => p.Favorite));
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            //OnSearchCommand = new Command<string>(PerformSearch);
            // OnSpeakButtonClicked = new Command(StartSpeechToText);
        }

        public void FilterProductsByCategory()
        {
            //if (SelectedProductGroup != null)
            {
                // Assuming you have a method to get products by category
                var filteredProducts = DataService.Instance.AllProducts.Where(p => p.ProductGroupID == ProgSettings.ActGrpid).ToList();
                DisplayedProducts = new ObservableCollection<Product>(filteredProducts);
            }
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

        private void StartSpeechToText()
        {
            // Implement speech to text functionality
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public void IncreaseQuantity(Product product)
        {
            // Ensure this method is public and correctly implemented
            MainThread.BeginInvokeOnMainThread(() =>
            {
                product.Quantity++;
                Console.WriteLine("Quantity increased for: " + product.Description);
                ProgSettings.Actprodrid = product.ProductID;
                ProgSettings.ActGrpid = product.ProductGroupID;
                ProgSettings.courseid = product.Row_Print;

            });
        }
        public void DecreaseQuantity(Product product)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (product.Quantity > 0) product.Quantity--;
                Console.WriteLine("Quantity decreased for: " + product.Description);
                ProgSettings.Actprodrid = product.ProductID;
                ProgSettings.ActGrpid = product.ProductGroupID;

            });

        }

        public void ShowExtras()
        {
            var ExtraPage = new ExtrasOptionsPage();
            Application.Current.MainPage.Navigation.PushModalAsync(ExtraPage);
        }

    }
    }


