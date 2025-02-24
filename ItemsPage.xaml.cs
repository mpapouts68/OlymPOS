using Microsoft.Maui.Controls;
using System;
using Microsoft.Maui.ApplicationModel;
using OlymPOS;

namespace OlymPOS
{
    public partial class ItemsPage : ContentPage
    {
        public ItemsPage()
        {
            InitializeComponent();
            MessagingCenter.Subscribe<Application, string>(this, "SpeechToText", (sender, arg) =>
            {
                Device.BeginInvokeOnMainThread(() => searchEntry.Text = arg);
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Any additional logic needed when the page appears
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<Application, string>(this, "SpeechToText");
        }

        private void OnIncreaseClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton btn && btn.BindingContext is Product product)
            {
                var viewModel = BindingContext as ItemsViewModel;
                viewModel?.IncreaseQuantity(product);
            }
        }

        private void OnImageTapped(object sender, EventArgs e)
        {
            if (sender is Image image && image.BindingContext is Product product)
            {
                var viewModel = BindingContext as ItemsViewModel;
                viewModel?.IncreaseQuantity(product);
            }
        }

        private void OnDecreaseClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton btn && btn.BindingContext is Product product)
            {
                var viewModel = BindingContext as ItemsViewModel;
                viewModel?.DecreaseQuantity(product);
            }
        }

        private void OnSearchClicked(object sender, EventArgs e)
        {
            string searchText = searchEntry.Text;
            if (!string.IsNullOrEmpty(searchText) && searchText.Length >= 3)
            {
                if (this.BindingContext is ItemsViewModel viewModel)
                {
                    viewModel.PerformSearch(searchText);
                    searchEntry.Text = string.Empty; // Clear the search entry after search
                }
            }
        }

        private async void OnSpeakButtonClicked(object sender, EventArgs e)
        {
            Console.WriteLine("OnSpeakButtonClicked is Started");
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (status == PermissionStatus.Granted)
            {
                DependencyService.Get<ISpeechToText>().StartSpeechToText();
                Console.WriteLine("OnSpeakButtonClicked is Passed");
            }
            else
            {
                await DisplayAlert("Microphone Permission", "Permission to use the microphone is required for speech to text.", "OK");
            }
        }
    }
}
