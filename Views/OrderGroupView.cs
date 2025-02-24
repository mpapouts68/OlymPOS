using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OlymPOS;

namespace OlymPOS;

public class OrderGroupView : INotifyPropertyChanged
{
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

    public OrderGroupView()
    {

        ProductCategories = DataService.Instance.ProductCategories;
    }



    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


