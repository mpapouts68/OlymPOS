using OlymPOS;
using System.Collections.ObjectModel;

namespace OlymPOS;

public partial class CombinedPage : ContentPage
{
    public CombinedPage()
    {
        InitializeComponent();
    }


    private void OnSearchClicked(object sender, EventArgs e)
    {
        string searchText = searchEntry.Text;
        if (!string.IsNullOrEmpty(searchText) && searchText.Length >= 3)
        {
            if (this.BindingContext is CombinedViewModel viewModel)
            {
                viewModel.PerformSearch(searchText);
                searchEntry.Text = string.Empty; // Clear the search entry after search
            }
        }
    }
    private void OnIncreaseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.BindingContext is Product product)
        {
            var viewModel = BindingContext as CombinedViewModel;
            viewModel?.IncreaseQuantity(product);
        }
    }

    private void OnImageTapped(object sender, EventArgs e)
    {
        if (sender is Image image && image.BindingContext is Product product)
        {
            var viewModel = BindingContext as CombinedViewModel;
            viewModel?.IncreaseQuantity(product);
        }
    }

    private void OnDecreaseClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton btn && btn.BindingContext is Product product)
        {
            var viewModel = BindingContext as CombinedViewModel;
            viewModel?.DecreaseQuantity(product);
        }
    }

    private void TreeView_ItemTapped(object sender, Syncfusion.Maui.TreeView.ItemTappedEventArgs e) // Use the correct EventArgs
    {
        var item = e.Node.Content as ProductGroup;
        if (item != null)
        {
            ProgSettings.ActGrpid = item.ProductGroupID;
            // var details = $"ID: {item.ProductGroupID}\n" +
            //             $"Description: {item.Description}\n" +
            //           $"Has Subcategories: {item.Has_Sub}\n" +
            //          $"Subcategories Count: {item.Subcategories?.Count ?? 0}";
            //DisplayAlert("Item Details", details, "OK");
            Console.WriteLine(item.ProductGroupID);
            // Assuming you have a method to get products by category
            //var filteredProducts = DataService.Instance.AllProducts.Where(p => p.ProductGroupID == ProgSettings.ActGrpid).ToList();
            //DisplayedProducts = new ObservableCollection<Product>(filteredProducts);
            FilterCat();

        }
    }
    public void FilterCat()
    {
        var viewModel = BindingContext as CombinedViewModel;
        int catid = ProgSettings.ActGrpid;
        Console.WriteLine("Filter is fired");
        viewModel?.FilterProductsByCategory();

    }
    private void OnExtraClicked(object sender, EventArgs e)
    {
                   var viewModel = BindingContext as CombinedViewModel;
            Console.WriteLine(ProgSettings.Actprodrid);
           
            viewModel.ShowExtras();
         }
}


    




