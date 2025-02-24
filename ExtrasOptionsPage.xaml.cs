using OlymPOS;
using Syncfusion.Maui.Buttons;
using Syncfusion.Maui.Core;

namespace OlymPOS;

public partial class ExtrasOptionsPage : ContentPage
{
    public ExtrasOptionsPage()
    {
        InitializeComponent();
        BindingContext = new ExtraViewModel();


    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load extras when the page appears
        LoadRadioButtons();
        await ((ExtraViewModel)BindingContext).LoadExtras();
    }
    private void LoadRadioButtons()
    {
        if (BindingContext is ExtraViewModel viewModel)
        {
            foreach (var option in viewModel._displayedcourse)
            {
                var radioButton = new SfRadioButton
                {
                    Text = option.Description,
                    TextColor = Colors.White,
                    UncheckedColor = Colors.Gray,
                    IsChecked = option.CourseId == ProgSettings.courseid // Set based on matching ID
                };
                // Assigning an event handler to change the global courseId when a radio button is checked
                // radioButton.CheckedChanged += (sender, e) =>
                //{
                //   if (e.IsChecked)
                //  {
                //     ProgSettings.courseId = option.Id;
                // }
                //};

                radioButtonsContainer.Children.Add(radioButton);
            }
            // foreach (var opex in viewModel.Extras)
            //  {
            //    var chipButton = new SfChip
            //  {
            //    Text = opex.Description,

            //  TextColor = Colors.White,
            //CornerRadius = 20,
            //HorizontalOptions = "S"
            //UncheckedColor = Colors.Gray,
            //IsChecked = option.CourseId == ProgSettings.courseid // Set based on matching ID
            //};
            //Console.WriteLine("adding chip");
            //chipsRowsContainer.Children.Add(chipButton);
        }
    }

}