using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OlymPOS;

namespace OlymPOS;

public class OrderActualView : INotifyPropertyChanged
{
    public ObservableCollection<Product> AllProducts => DataService.Instance.AllProducts;

    public OrderActualView()
    {
        // Assuming DataService.Instance.LoadAllDataAsync has been called post-login, no need to load again
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

