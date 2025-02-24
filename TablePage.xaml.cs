using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using MySqlConnector;
using OlymPOS;

namespace OlymPOS
{
    public partial class TablePage : ContentPage
    {
        private readonly string ConnStr = "server=10.0.2.2;user=root;database=olympos;password=pa0k0l31;sslmode=none;";
        private readonly List<(string Description, int Yper_Main_ID)> areas;
        private readonly int defaultYperMainId;

        // Modified constructor that accepts defaultYperMainId
        public TablePage(int defaultYperMainId = 0) // Default parameter allows compatibility with calls not providing an ID
        {
            InitializeComponent();
            this.defaultYperMainId = UserSettings.Defypd;
            areas = GetAreasFromDatabase();
            LoadData();
        }

        private void LoadData()
        {
            Shell.SetNavBarIsVisible(this, false);

            CreateToggleButtons();
            if (defaultYperMainId != 0)
            {
                LoadDefaultServicingPoints();

            }
        }
        private void ReLoadData()
        {
            Shell.SetNavBarIsVisible(this, false);

            CreateToggleButtons();
            if (defaultYperMainId != 0)
            {
                LoadDefaultServicingPoints();

            }
        }
        private void CreateToggleButtons()
        {
            areaRadioButtonGroup.Children.Clear();
            foreach (var (Description, Yper_Main_ID) in areas)
            {
                Button toggleButton = new Button
                {
                    Text = Description,
                    ClassId = Yper_Main_ID.ToString(),
                    BackgroundColor = Color.FromRgb(70, 130, 180)
                };
                toggleButton.Clicked += OnAreaButtonClicked;
                areaRadioButtonGroup.Children.Add(toggleButton);
            }
            Console.WriteLine("Toggle Ok");

            if (defaultYperMainId != 0)
            {
                OnAreaButtonClicked(areaRadioButtonGroup.Children[0], EventArgs.Empty); // Simulate click on default area's button
            }
        }
        private List<(string Description, int Yper_Main_ID)> GetAreasFromDatabase()
        {
            List<(string Description, int Yper_Main_ID)> areas = new();
            try
            {
                using MySqlConnection conn = new(ConnStr);
                conn.Open();
                string query = "SELECT Yper_Description, Yper_Main_ID FROM Yper_Posts";
                MySqlCommand cmd = new(query, conn);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    areas.Add((reader["Yper_Description"].ToString(), (int)reader["Yper_Main_ID"]));
                }
                Console.WriteLine("Areas Ok");
            }
            catch (MySqlException ex)
            {
                DisplayAlert("Database Error", ex.Message, "OK");
            }
            return areas;
            
        }

        private void OnAreaButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null) return; // Early exit if the sender is not a Button
            string selectedArea = button.Text;
            var selectedAreaTuple = areas.Find(a => a.Description == selectedArea);
            if (!selectedAreaTuple.Equals(default))
            {
                int selectedYperMainId = selectedAreaTuple.Yper_Main_ID;
                var servicingPoints = GetServicingPointsFromDatabase(selectedYperMainId);
                servicingPointsCollectionView.ItemsSource = servicingPoints;
            }
        }

        private List<ServicingPoint> GetServicingPointsFromDatabase(int yperMainId)
        {
            List<ServicingPoint> points = new();
            try
            {
                using MySqlConnection conn = new(ConnStr);
                conn.Open();
                string query = "SELECT * FROM posts_main WHERE Yper_Main_ID = @yperMainId";
                MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@yperMainId", yperMainId);
                using MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    points.Add(new ServicingPoint
                    {
                        Description = reader["Description"].ToString(),
                        Active = (bool)reader["Active"],
                        ActiveOrderID = (int) reader["Active_Order_ID"],
                        PostID = (int)reader["Post_ID"],
                        PostNumber = (int)reader["Post_Number"]
                    });
                }
                Console.WriteLine("Posts Ok");
            }
            catch (MySqlException ex)
            {
                Console.WriteLine("Database Error: " + ex.Message);
            }
            return points;
        }

        private void LoadDefaultServicingPoints()
        {
            var servicingPoints = GetServicingPointsFromDatabase(defaultYperMainId);
            servicingPointsCollectionView.ItemsSource = servicingPoints;
        }

  private async void OnCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (e.CurrentSelection.FirstOrDefault() is ServicingPoint selectedPoint)
    {
        // If the table is occupied, open the order page to display the order.
        if (selectedPoint.Active)
        {
                    // Assuming OrderPage has been modified to accept an order ID.
                    //var orderPage = new OrderPage(selectedPoint.ActiveOrderID);
                    //await Navigation.PushAsync(orderPage);
                    ProgSettings.Actordrid = selectedPoint.ActiveOrderID;
                    //DisplayAlert("Item Selected", $"You selected: {selectedPoint.ActiveOrderID}", "OK");
                    Application.Current.MainPage = new OrderIntake();
                    //var mainPage = new NavigationPage(new MainPage());
                    //mainPage.BarBackgroundColor = Color.FromRgb(70, 130, 180); // Change to your desired color
                    // Set the navigation bar text color
                    //mainPage.BarTextColor = Color.FromRgb(255,255,255);




                    await Navigation.PushAsync(new OrderIntake());
                }
        else
        {
                    // For a new order, you might navigate to OrderPage differently or use another page.
                    // Here's a simple navigation to OrderPage, assuming it can handle new orders.
                    //var newOrderPage = new OrderPage(); // Modify this constructor as needed for new orders.
                    //await Navigation.PushAsync(newOrderPage);
                    ProgSettings.Actordrid = 0;
                    DisplayAlert("Item Selected", $"You selected: {selectedPoint.Active}", "OK");
                }
    }
}


        private async void Button1_Clicked(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new ItemsPage());
               //Application.Current.MainPage = new OrderFlyOut();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log or handle the exception as needed
            }
        }
        private void Button2_Clicked(object sender, EventArgs e) => Navigation.PushAsync(new Orders());
        private void Button3_Clicked(object sender, EventArgs e) => Navigation.PushAsync(new CombinedPage());
        private async void Button4_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Exit", "Do you really want to exit?", "Yes", "No");
            if (answer)
            {
                // This will attempt to close the app
                Environment.Exit(0);

            }
        }



    }
}
