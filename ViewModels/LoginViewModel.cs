using System;
using System.Threading.Tasks;
using System.Windows.Input;
using OlymPOS.Services.Interfaces;
using OlymPOS.ViewModels.Base;

namespace OlymPOS.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly ISyncService _syncService;
        private string _pin;
        private string _displayPin;
        private string _errorMessage;
        private bool _showError;
        private bool _isSyncing;
        private int _syncProgress;

        public string Pin
        {
            get => _pin;
            set
            {
                if (SetProperty(ref _pin, value))
                {
                    // Update display PIN with asterisks
                    DisplayPin = new string('*', _pin.Length);
                }
            }
        }

        public string DisplayPin
        {
            get => _displayPin;
            set => SetProperty(ref _displayPin, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool ShowError
        {
            get => _showError;
            set => SetProperty(ref _showError, value);
        }

        public bool IsSyncing
        {
            get => _isSyncing;
            set => SetProperty(ref _isSyncing, value);
        }

        public int SyncProgress
        {
            get => _syncProgress;
            set => SetProperty(ref _syncProgress, value);
        }

        public ICommand LoginCommand => GetCommand(nameof(LoginCommand), LoginAsync);
        public ICommand ClearCommand => GetCommand(nameof(ClearCommand), ClearPinAsync);
        public ICommand NumberCommand => GetCommand<string>(nameof(NumberCommand), AddNumberAsync);

        public LoginViewModel(IAuthenticationService authService, ISyncService syncService)
        {
            _authService = authService;
            _syncService = syncService;

            Title = "Login";
            Pin = string.Empty;
            DisplayPin = string.Empty;

            // Subscribe to sync events
            _syncService.SyncProgress += OnSyncProgress;
            _syncService.SyncCompleted += OnSyncCompleted;
        }

        private void OnSyncProgress(object sender, SyncEventArgs e)
        {
            IsSyncing = true;
            SyncProgress = e.Progress;
        }

        private void OnSyncCompleted(object sender, SyncEventArgs e)
        {
            IsSyncing = false;
            SyncProgress = 0;

            if (!e.Success)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ShowError = true;
                    ErrorMessage = $"Sync error: {e.Message}";
                });
            }
        }

        private async Task LoginAsync()
        {
            if (IsBusy || string.IsNullOrEmpty(Pin))
                return;

            IsBusy = true;
            ShowError = false;

            try
            {
                bool success = await _authService.LoginAsync(Pin);

                if (success)
                {
                    // Navigate to main page
                    await NavigateToMainPageAsync();
                }
                else
                {
                    ShowError = true;
                    ErrorMessage = "Invalid PIN. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ShowError = true;
                ErrorMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                Pin = string.Empty; // Clear PIN for security
            }
        }

        private async Task ClearPinAsync()
        {
            Pin = string.Empty;
            await Task.CompletedTask;
        }

        private async Task AddNumberAsync(string number)
        {
            Pin += number;
            await Task.CompletedTask;
        }

        private async Task NavigateToMainPageAsync()
        {
            // Use Shell navigation if you're using Shell
            await Shell.Current.GoToAsync("//TablePage");

            // Or use page navigation if you're using NavigationPage
            // await Application.Current.MainPage.Navigation.PushAsync(new TablePage());
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            // Unsubscribe from events
            _syncService.SyncProgress -= OnSyncProgress;
            _syncService.SyncCompleted -= OnSyncCompleted;
        }
    }
}