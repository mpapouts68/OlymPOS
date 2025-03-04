using OlymPOS.Views;
namespace OlymPOS
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(CombinedPage), typeof(CombinedPage));

        }
    }
}
