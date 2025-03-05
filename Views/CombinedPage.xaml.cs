using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using OlymPOS.ViewModels;
using OlymPOS.Models;

namespace OlymPOS.Views
{
    public partial class CombinedPage : ContentPage
    {
        private readonly CombinedViewModel _viewModel;
        private readonly ILogger<CombinedPage> _logger;
        private const int MinSearchLength = 3;

        public CombinedPage(CombinedViewModel viewModel, ILogger<CombinedPage> logger)
        {
            _viewModel = viewModel;
            _logger = logger;
            InitializeComponent();
            BindingContext = _viewModel;
            _viewModel.LoadFavorites();
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            string searchText = searchEntry.Text?.Trim();
            if (string.IsNullOrEmpty(searchText) || searchText.Length < MinSearchLength)
            {
                await DisplayAlert("Search", $"Enter at least {MinSearchLength} characters", "OK");
                return;
            }
            _viewModel.PerformSearch(searchText);
            searchEntry.Text = string.Empty;
        }

        private void AdjustQuantity(object sender, EventArgs e, bool increase)
        {
            if (sender is BindableObject bindable && bindable.BindingContext is Product product)
            {
                if (increase)
                    _viewModel.IncreaseQuantity(product);
                else
                    _viewModel.DecreaseQuantity(product);
            }
        }

        private void OnIncreaseClicked(object sender, EventArgs e) => AdjustQuantity(sender, e, true);
        private void OnImageTapped(object sender, EventArgs e) => AdjustQuantity(sender, e, true);
        private void OnDecreaseClicked(object sender, EventArgs e) => AdjustQuantity(sender, e, false);

        private void TreeView_ItemTapped(object sender, Syncfusion.Maui.TreeView.ItemTappedEventArgs e)
        {
            if (e.Node?.Content is ProductGroup item)
            {
                ProgSettings.ActGrpid = item.ProductGroupID;
                _logger?.LogInformation("Selected product group ID: {Id}", item.ProductGroupID);
                _viewModel.FilterProductsByCategory();
            }
        }
    }
}