using OlymPOS;

namespace OlymPOS;

public partial class DiscountPage : ContentPage
{
	public DiscountPage()
	{
		InitializeComponent();
        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Console.WriteLine ("Running On apereance");
        // Wait for the UI to be ready
        await Task.Delay(100); // Adjust delay as needed

        // Set focus to the numeric entry
        numericEntry.Focus();
    }
    private async void OnOkClicked(object sender, EventArgs e)
    {
        // Ensure the numeric entry is not empty and is a valid number
        if (string.IsNullOrEmpty(numericEntry.Text) || !double.TryParse(numericEntry.Text, out double discountValue))
        {
            // Inform the user that the input is invalid
            await DisplayAlert("Error", "Please enter a valid discount value.", "OK");
            return;
        }

        // Determine the selected discount type
        bool isPercentage = percentageRadioButton.IsChecked;
        bool isAmount = amountRadioButton.IsChecked;

        // Perform actions based on the discount type and value
        if (isPercentage)
        {
            // Assuming you're applying the discount as a percentage
            // Example: ApplyDiscountAsPercentage(discountValue);
            await DisplayAlert("Discount", $"Applying a {discountValue}% discount.", "OK");

            var dbService = new OrderDataService();
            await dbService.ApplyPercentageDiscountAsync(discountValue, ProgSettings.Actordrid);
                    }
        else if (isAmount)
        {
            var dbService = new OrderDataService();
            var orders = await dbService.GetOrdersAsync();
            var order = orders.FirstOrDefault(); // Assuming you want the first order or there's only one

        if (order != null && order.OrderTotal > 0)
        {
                double orderTotalAsDouble = Convert.ToDouble(order.OrderTotal.Value);
                double discountPercentage = discountValue / orderTotalAsDouble * 100;

                // Apply the discount percentage to the database
                await dbService.ApplyPercentageDiscountAsync(discountPercentage, ProgSettings.Actordrid);

            // Show confirmation to the user
            await DisplayAlert("Discount", $"Applying a discount of ${discountValue} which is {discountPercentage}% of the order total.", "OK");
        }
        else
        {
            // Handle the case where the order is not found or the total is invalid
            await DisplayAlert("Error", "Order not found or has an invalid total.", "OK");
        }
    }
    // Close the modal after performing the action
    await this.Navigation.PopModalAsync();
        this.Focus();
    }


    private async void OnCancelClicked(object sender, EventArgs e)
        {
            // Close the modal without doing anything
            await this.Navigation.PopModalAsync();
        }
    }