using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Syncfusion.Maui.Core.Hosting;
using OlymPOS.Services;
using OlymPOS.Services.Interfaces;
using OlymPOS.ViewModels;
using OlymPOS.Repositories;
using OlymPOS.Services.Caching;
using OlymPOS.Converters;

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

            // Register services
            RegisterServices(builder.Services);

            // Register view models
            RegisterViewModels(builder.Services);

            // Register converters
            RegisterConverters(builder.Services);

            // Register Maui Community Toolkit
            builder.UseMauiCommunityToolkit();
            builder.UseMauiCommunityToolkitCore();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void RegisterServices(IServiceCollection services)
        {
            // Register global configuration services
            services.AddSingleton<IAppSettings, AppSettings>();
            services.AddSingleton<ICacheManager, SqliteCacheManager>();

            // Register database services
            services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddSingleton<ISyncService, DataSyncService>();

            // Register core application services
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<IPrintService, PrintService>();

            // Register repositories
            services.AddSingleton<IOrderRepository, OrderRepository>();
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<IProductGroupRepository, ProductGroupRepository>();
            services.AddSingleton<ITableRepository, TableRepository>();

            // Register platform-specific services
#if ANDROID
            services.AddSingleton<ISpeechToTextService, Platforms.Android.SpeechToTextImplementation>();
#elif IOS
            services.AddSingleton<ISpeechToTextService, Platforms.iOS.SpeechToTextImplementation>();
#else
            services.AddSingleton<ISpeechToTextService, DefaultSpeechToTextService>();
#endif

            // Register backward compatibility services
            services.AddSingleton<OrderDataService>();
        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            // Register view models with transient lifetime
            services.AddTransient<LoginViewModel>();
            services.AddTransient<TableViewModel>();
            services.AddTransient<OrderViewModel>();
            services.AddTransient<ProductViewModel>();
            services.AddTransient<ExtrasViewModel>();
            services.AddTransient<PaymentViewModel>();
            services.AddTransient<StatisticsViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<CombinedViewModel>();
        }

        private static void RegisterConverters(IServiceCollection services)
        {
            // Register value converters
            services.AddSingleton<QuantityToImageConverter>();
            services.AddSingleton<ReceiptStatusToImageConverter>();
            services.AddSingleton<BoolToVisibilityConverter>();
            services.AddSingleton<InvertedBoolConverter>();
            services.AddSingleton<LevelToIndentConverter>();
        }
    }

    // Default implementation for platforms that don't have specific speech-to-text
    public class DefaultSpeechToTextService : ISpeechToTextService
    {
        private readonly WeakEventManager _eventManager = new WeakEventManager();

        public event EventHandler<string> SpeechRecognized
        {
            add => _eventManager.AddEventHandler(value);
            remove => _eventManager.RemoveEventHandler(value);
        }

        public bool IsSupported => false;

        public Task<string> RecognizeSpeechAsync()
        {
            OnSpeechRecognized("Speech recognition is not supported on this platform");
            return Task.FromResult<string>(null);
        }

        public void StartSpeechToText()
        {
            OnSpeechRecognized("Speech recognition is not supported on this platform");
        }

        public void OnSpeechRecognized(string result)
        {
            _eventManager.HandleEvent(this, result, nameof(SpeechRecognized));
        }
    }
}
