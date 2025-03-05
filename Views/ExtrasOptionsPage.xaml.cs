using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OlymPOS
{
    public partial class ExtrasOptionsPage : ContentPage
    {
        private ExtrasViewModel _viewModel;

        public ExtrasOptionsPage()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<ExtrasViewModel>();
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
    }
}
