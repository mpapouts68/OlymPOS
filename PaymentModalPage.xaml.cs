using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OlymPOS
{
    public partial class PaymentModalPage : ContentPage
    {
        private PaymentViewModel _viewModel;

        public PaymentModalPage()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<PaymentViewModel>();
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
