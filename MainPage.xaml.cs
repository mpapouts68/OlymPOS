using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OlymPOS
{
    public partial class MainPage : ContentPage
    {
        private LoginViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<LoginViewModel>();
            BindingContext = _viewModel;

            // Subscribe to property changes to show error alerts
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LoginViewModel.ShowError) && _viewModel.ShowError)
            {
                await DisplayAlert("Login Error", _viewModel.ErrorMessage, "OK");
                _viewModel.ShowError = false;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Clear the PIN when the page appears
            _viewModel?.ClearCommand.Execute(null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from events
            if (_viewModel != null)
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }
}
