using System.Windows.Input;

namespace OlymPOS;

public partial class MidCashReg : ContentPage
{
	public MidCashReg()
	{
		InitializeComponent();
	}
    private async void SendWPrint(object sender, EventArgs e)
    {
        var dbService = new OrderDataService();
        await dbService.dbexecute("PrintCashReg");
        await this.Navigation.PopModalAsync();

    }
    private async void SendWiPrint(object sender, EventArgs e)
    {
        var dbService = new OrderDataService();
        await dbService.dbexecute("DoNotPrintCashReg");
        await this.Navigation.PopModalAsync();

    }
}
