using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OlymPOS.Models;
using OlymPOS.Repositories.Interfaces;

namespace OlymPOS.ViewModels // Adjusted namespace for consistency
{
    public class OrderActualView : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;

        public ObservableCollection<Product> AllProducts => _dataService.AllProducts; // Use injected service

        public OrderActualView(IDataService dataService)
        {
            _dataService = dataService;
            // Assuming LoadAllDataAsync was called post-login (e.g., in MainPage), no additional loading needed
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

