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
            Items = new TabItemCollection();
            OrderPage page1 = new OrderPage();
            ItemsPage page2 = new ItemsPage();
            ExtrasOptionsPage page3 = new ExtrasOptionsPage();
            Orders page4 = new Orders();
            Items.Add(new SfTabItem { Content = page1.Content, Header = "Categories"});
            Items.Add(new SfTabItem { Content = page2.Content, Header = "Items" });
            Items.Add(new SfTabItem { Content = page3.Content, Header = "Extra/Options" });
            Items.Add(new SfTabItem { Content = page4.Content, Header = "Order" });
        }
    }


