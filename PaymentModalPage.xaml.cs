using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Input;
using MySqlConnector;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;

using OlymPOS;

namespace OlymPOS;

public partial class PaymentModalPage : ContentPage
{
    public ICommand PrintOption1Command { get; } 
    public ICommand PrintOption2Command { get; }
    public ICommand PrintOption3Command { get; }
    public ICommand PrintOption4Command { get; }
    public ICommand PrintOption5Command { get; }
    public ICommand PrintOption6Command { get; }
    //public ICommand PrintOption1Command { get; }
    public PaymentModalPage()
	{
		InitializeComponent();
        PrintOption1Command = new Command(async () => await PrintOption1Async());
        PrintOption2Command = new Command(async () => await PrintOption2Async());
        PrintOption3Command = new Command(async () => await PrintOption3Async());
        PrintOption4Command = new Command(async () => await PrintOption4Async());
        PrintOption5Command = new Command(async () => await PrintOption5Async());
        PrintOption6Command = new Command(async () => await PrintOption6Async());
        BindingContext = this;
    }
    private async Task PrintOption1Async()
    {
        using var connection = new MySqlConnection(GlobalConString.ConnStr);
        try
        {
            Console.WriteLine("Start writing to database");
            await connection.OpenAsync();
            var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@value1", "PrintOnly");
            command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
            command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

            var result = await command.ExecuteNonQueryAsync();
            if (result >= 1)
            {
                // Display short snackbar alert for success
                await this.DisplaySnackbar("The Print Only Command is Accepted.\nWait for Print notification");
            }
            else
            {
                // Display error Alert
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
            }
        
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine(ex.Message);
            
        }
    }

        private async Task PrintOption2Async()
        {
            using var connection = new MySqlConnection(GlobalConString.ConnStr);
            try
            {
                Console.WriteLine("Start writing to database");
                await connection.OpenAsync();
                var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@value1", "CloseOnly");
                command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
                command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

                var result = await command.ExecuteNonQueryAsync();
            if (result >= 1)
            {
                // Display short snackbar alert for success
                await this.DisplaySnackbar("The Close Only Command is Accepted.\nWait for Print notification");
            }
            else
            {
                // Display error Alert
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
            }
        }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine(ex.Message);
            }
        }
    private async Task PrintOption3Async()
    {
        using var connection = new MySqlConnection(GlobalConString.ConnStr);
        try
        {
            Console.WriteLine("Start writing to database");
            await connection.OpenAsync();
            var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@value1", "ResendCashreg");
            command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
            command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

            var result = await command.ExecuteNonQueryAsync();
            if (result >= 1)
            {
                // Display short snackbar alert for success
                await this.DisplaySnackbar("The Resend to CashRegister Command is Accepted.\nWait for Print notification");
            }
            else
            {
                // Display error Alert
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine(ex.Message);
        }
    }
    private async Task PrintOption4Async()
    {
        using var connection = new MySqlConnection(GlobalConString.ConnStr);
        try
        {
            Console.WriteLine("Start writing to database");
            await connection.OpenAsync();
            var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@value1", "PrintAndClose");
            command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
            command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

            var result = await command.ExecuteNonQueryAsync();
            if (result >= 1)
            {
                // Display short snackbar alert for success
                await this.DisplaySnackbar("The Print and Close Command is Accepted.\nWait for Print notification");
            }
            else
            {
                // Display error Alert
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine(ex.Message);
        }
    }
    private async Task PrintOption5Async()
    {
        using var connection = new MySqlConnection(GlobalConString.ConnStr);
        try
        {
            Console.WriteLine("Start writing to database");
            await connection.OpenAsync();
            var query = "INSERT INTO CSOrders (CSORder, Staff_ID, Order_ID) VALUES (@value1, @value2, @value3);";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@value1", "CallAdmin");
            command.Parameters.AddWithValue("@value2", UserSettings.ClerkID);
            command.Parameters.AddWithValue("@value3", ProgSettings.Actordrid);

            var result = await command.ExecuteNonQueryAsync();
            if (result >= 1)
            {
                // Display short snackbar alert for success
                await this.DisplaySnackbar("The Adminis informed Command is Accepted.\nWait for Print notification");
            }
            else
            {
                // Display error Alert
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to insert data into the database", "OK");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions appropriately
            Console.WriteLine(ex.Message);
        }
    }
    private async Task PrintOption6Async()
    {
        await Navigation.PopModalAsync();
    }

    }

    

   

