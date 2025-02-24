using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Syncfusion.Maui.Core.Hosting;

namespace OlymPOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureSyncfusionCore();
            // Initialise the toolkit
            builder.UseMauiApp<App>().UseMauiCommunityToolkitCore();
            builder.UseMauiApp<App>().UseMauiCommunityToolkitCore();
            builder.UseMauiCommunityToolkitCore();
            builder.UseMauiCommunityToolkit();

            return builder.Build();
        }
    }
}

