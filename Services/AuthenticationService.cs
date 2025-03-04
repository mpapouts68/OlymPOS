using System;
using System.Threading.Tasks;
using MySqlConnector;
using OlymPOS.Services.Interfaces;

namespace OlymPOS.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly ISyncService _syncService;
        private bool _isAuthenticated;

        public bool IsAuthenticated => _isAuthenticated;
        public UserSettings CurrentUser { get; private set; }

        public AuthenticationService(
            IDatabaseConnectionFactory connectionFactory,
            ISyncService syncService)
        {
            _connectionFactory = connectionFactory;
            _syncService = syncService;
            CurrentUser = null;
            _isAuthenticated = false;
        }

        public async Task<bool> LoginAsync(string pin)
        {
            try
            {
                // Try to connect to remote database first
                using var conn = await _connectionFactory.CreateRemoteConnectionAsync();
                await conn.OpenAsync();

                string query = "SELECT * FROM Staff WHERE Password = @password";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@password", pin);

                using MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    // Store user settings upon successful login
                    UserSettings.Username = reader.GetString("Name");
                    UserSettings.Role = reader.GetString("Role");
                    UserSettings.CashB = reader.GetString("CashRegister_Behaviour");
                    UserSettings.Lang = reader.GetString("Display_Language");
                    UserSettings.ClerkID = reader.GetInt32("Staff_ID");
                    UserSettings.Price_cat = reader.GetInt32("Price_Cat");
                    UserSettings.Defypd = reader.GetInt32("OrderBy");
                    UserSettings.IsAdmin = reader.GetBoolean("Admin");

                    // Set authentication state
                    _isAuthenticated = true;

                    // Check if sync is needed and perform it
                    bool syncNeeded = await _syncService.IsSyncNeededAsync();
                    if (syncNeeded)
                    {
                        await _syncService.SyncAllDataAsync();
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");

                // Try to authenticate from local cache if remote fails
                return await AuthenticateFromLocalCache(pin);
            }
        }

        private async Task<bool> AuthenticateFromLocalCache(string pin)
        {
            try
            {
                // This would need a local encrypted storage of credentials
                // For now, just use the last known user if we've authenticated before

                // This is just a fallback and not secure - in a real app, 
                // you would need to securely store credentials locally

                if (UserSettings.ClerkID > 0 && !string.IsNullOrEmpty(UserSettings.Username))
                {
                    _isAuthenticated = true;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local login error: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            UserSettings.Username = null;
            UserSettings.Role = null;
            UserSettings.ClerkID = 0;
            UserSettings.IsAdmin = false;

            _isAuthenticated = false;

            // Sync any pending changes before logout
            try
            {
                if (await _syncService.IsSyncNeededAsync())
                {
                    await _syncService.SyncOrdersAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout sync error: {ex.Message}");
                // Continue with logout even if sync fails
            }
        }
    }
}
