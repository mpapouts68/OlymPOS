using Syncfusion.Maui.TabView;
using System.ComponentModel;

namespace OlymPOS;

public partial class OrderTab : ContentPage
{
    public OrderTab()
    {
        InitializeComponent();
    }
}

    public class ViewModel : INotifyPropertyChanged
    {
        private TabItemCollection items;
        public event PropertyChangedEventHandler PropertyChanged;
        public TabItemCollection Items
        {
            get { return items; }
            set
            {
                items = value;
                OnPropertyChanged("Items");
            }
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ViewModel()
        {
            SetItems();

        }
    internal void SetItems()
    {
        Items =
        [
            //new SfTabItem { Content = new OrderPage().Content, Header = "Categories"},
            new SfTabItem { Content = new ItemsPage().Content, Header = "Items" },
            new SfTabItem { Content = new ExtrasOptionsPage().Content, Header = "Extra/Options" }
            //new SfTabItem { Content = new Orders().Content, Header = "Order" }
        ];
    }
    }


