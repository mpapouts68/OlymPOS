using MySqlConnector;
using System;
using Microsoft.Maui.Controls;
using OlymPOS;

namespace OlymPOS
{

    public partial class MainPage : ContentPage
    {

        private readonly string ConnStr = GlobalConString.ConnStr;

        public MainPage()
        {
            InitializeComponent();
            ClearPINEntry();
        }

        private void ClearPINEntry()
        {
            resultText.Text = "";
        }

        private async void LogonAsync(object sender, EventArgs e)
        {
            try
            {
                using MySqlConnection conn = new(ConnStr);
                await conn.OpenAsync();

                string passwordToCheck = resultText.Text;
                string query = "SELECT * FROM Staff WHERE Password = @password";
                MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@password", passwordToCheck);

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
                    ProgSettings.Actordrid = 0;
                    // Store other user settings as needed
                    activityIndicator.IsRunning = true;
                    activityIndicator.IsVisible = true;

                    //var dataService = new DataService(ConnStr);
                    // Adjusted to call the new method name LoadAllDataAsync
                    await DataService.Instance.LoadAllDataAsync();
                    Console.WriteLine($"Loaded productgroups count: {DataService.Instance.ProductCategories.Count}");
                    Console.WriteLine($"Loaded products count: {DataService.Instance.AllProducts.Count}");

                    // Assuming the navigation to the TablePage should occur after the data has been successfully loaded
                    await Navigation.PushAsync(new TablePage());

activityIndicator.IsRunning = false;
activityIndicator.IsVisible = false;
                }
                else
                {
                    await DisplayAlert("Login Error", "Invalid PIN", "OK");
                }
            }
            catch (MySqlException ex)
            {
                await DisplayAlert("Database Error", ex.Message, "OK");
            }
        }

        private void OnSelectNumber(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            string pressed = button.Text;
            resultText.Text += pressed;
        }

        private void OnClear(object sender, EventArgs e)
        {
            ClearPINEntry();
        }
    }
}
