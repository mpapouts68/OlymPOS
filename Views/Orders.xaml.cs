using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core;

namespace OlymPOS
{
    public partial class Orders : ContentPage
    {
        private OrderViewModel _viewModel;

        public Orders()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<OrderViewModel>();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Initialize the view model
            if (_viewModel != null && !_viewModel.IsInitialized)
            {
                await _viewModel.InitializeAsync();
            }
        }

        private async void OrderItemExpander_Expanded(object sender, ExpandedChangedEventArgs e)
        {
            if (e.IsExpanded && sender is Expander expander && expander.BindingContext is OrderItem orderItem)
            {
                // Forward to view model
                await _viewModel.OnOrderItemExpanderOpenedAsync(orderItem);
            }
        }
    }
}
