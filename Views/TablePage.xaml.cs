using OlymPOS.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OlymPOS
{
    public partial class TablePage : ContentPage
    {
        private TableViewModel _viewModel;

        public TablePage()
        {
            InitializeComponent();

            // Get the view model from DI
            _viewModel = Application.Current.Handler.MauiContext.Services.GetService<TableViewModel>();
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

        // For backwards compatibility with existing code
        private void Button1_Clicked(object sender, EventArgs e)
        {
            // Forward to ViewModel's OrdersCommand
            _viewModel?.StatisticsCommand?.Execute(null);
        }

        private void Button2_Clicked(object sender, EventArgs e)
        {
            // Forward to ViewModel's StatisticsCommand
            _viewModel?.StatisticsCommand?.Execute(null);
        }

        private void Button3_Clicked(object sender, EventArgs e)
        {
            // Forward to ViewModel's SettingsCommand
            _viewModel?.SettingsCommand?.Execute(null);
        }

        private void Button4_Clicked(object sender, EventArgs e)
        {
            // Forward to ViewModel's LogoutCommand
            _viewModel?.LogoutCommand?.Execute(null);
        }

        private void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                // Forward to ViewModel
                _viewModel?.SelectTableCommand.Execute(e.CurrentSelection[0]);

                // Clear selection
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private void OnAreaSegmentedSelectionChanged(object sender, Syncfusion.Maui.Buttons.SelectionChangedEventArgs e)
        {
            if (e.Index >= 0 && _viewModel?.Areas != null && e.Index < _viewModel.Areas.Count)
            {
                // Forward to ViewModel
                _viewModel.SelectAreaCommand.Execute(_viewModel.Areas[e.Index].Id);
            }
        }
    }
}
