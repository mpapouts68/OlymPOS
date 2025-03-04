using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OlymPOS
{
    public partial class ItemsPage : ContentPage
    {
        private ProductViewModel _viewModel;

        public ItemsPage()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<ProductViewModel>();
            BindingContext = _viewModel;

            // For speech to text results
            MessagingCenter.Subscribe<Application, string>(Application.Current, "SpeechToText", (sender, result) => {
                if (!string.IsNullOrEmpty(result))
                {
                    _viewModel.SearchQuery = result;
                    _viewModel.SearchCommand.Execute(null);
                }
            });
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from messages
            MessagingCenter.Unsubscribe<Application, string>(Application.Current, "SpeechToText");
        }

        // For backwards compatibility with existing code
        private void OnSearchClicked(object sender, EventArgs e)
        {
            _viewModel?.SearchCommand?.Execute(null);
        }

        private void OnIncreaseClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.CommandParameter is Product product)
            {
                _viewModel?.IncreaseQuantityCommand?.Execute(product);
            }
        }

        private void OnDecreaseClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.CommandParameter is Product product)
            {
                _viewModel?.DecreaseQuantityCommand?.Execute(product);
            }
        }

        private void OnImageTapped(object sender, EventArgs e)
        {
            if (sender is Image image && image.BindingContext is Product product)
            {
                _viewModel?.IncreaseQuantityCommand?.Execute(product);
            }
        }

        private async void OnSpeakButtonClicked(object sender, EventArgs e)
        {
            await _viewModel?.SpeechToTextCommand?.ExecuteAsync();
        }
    }
}

