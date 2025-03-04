using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.ViewModels // Adjusted namespace for consistency
{
    public class OrderGroupView : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        private ObservableCollection<ProductGroup> productCategories;

        public ObservableCollection<ProductGroup> ProductCategories
        {
            get => productCategories;
            set
            {
                productCategories = value;
                OnPropertyChanged(nameof(ProductCategories));
            }
        }

        public OrderGroupView(IDataService dataService)
        {
            _dataService = dataService;
            ProductCategories = _dataService.ProductCategories; // Use injected service
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
