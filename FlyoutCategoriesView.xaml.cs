namespace OlymPOS;


public partial class FlyoutCategoriesView : ContentView
{
    public FlyoutCategoriesView()
    {
        InitializeComponent();
    }

    private async void OnFlyoutBackgroundTapped(object sender, EventArgs e)
    {
        await this.TranslateTo(-300, 0, 200); // Replace with desired width
        IsVisible = false;
    }

    private void OnFlyoutSwiped(object sender, SwipedEventArgs e)
    {
        // ... (Same as OnFlyoutBackgroundTapped)
    }

    private void OnCategorySelected(object sender, SelectionChangedEventArgs e)
    {
        //var selectedCategory = e.AddedItems[0] as ProductGroup;
      //  MessagingCenter.Send<FlyoutCategoriesView, ProductGroup>(this, "CategorySelected", selectedCategory);
    }
}
