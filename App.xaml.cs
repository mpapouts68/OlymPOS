using Microsoft.Maui.Hosting;
using OlymPOS;
using Syncfusion.Licensing;

namespace OlymPOS
{
    public partial class App : Application
    {
        public App()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NMaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWX1ccHRWR2VfVkV1VkE=");
            InitializeComponent();
            MainPage = new AppShell();
        }
    }
    }