using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Views;

using CommunityToolkit.Maui;
using OlymPOS;

namespace OlymPOS
{
    public partial class Orders : ContentPage
    {
        public Orders()
        {
            InitializeComponent();
            BindingContext = new OrderViewModel();
        }

        // Example expander event handler - adjust as needed based on your setup
        private void orderExpander_Expanded(object sender, EventArgs e)
        {
            var expander = (Expander)sender;
            var orderItem = (OrderItem)expander.BindingContext;
            ((OrderViewModel)BindingContext).OnOrderItemExpanderOpened(orderItem);
        }
    }
}
