using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Syncfusion.Maui.Core.Hosting;
using OlymPOS.Services;
using OlymPOS.Services.Interfaces;
using OlymPOS.ViewModels;
using OlymPOS.Services.Repositories;
using OlymPOS.Services.Caching;

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
            // Register singleton service for application-wide configuration
            services.AddSingleton<IAppSettings, AppSettings>();

            // Database connection factory
            services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();

            // Cache manager
            services.AddSingleton<ICacheManager, SqliteCacheManager>();

            // Data sync service
            services.AddSingleton<ISyncService, DataSyncService>();

            // Data service (for backward compatibility)
            services.AddSingleton<IDataService>(provider => DataService.Instance);

            // Repositories
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<IOrderRepository, OrderRepository>();
            services.AddSingleton<IProductGroupRepository, ProductGroupRepository>();
            services.AddSingleton<ITableRepository, TableRepository>();

            // Services
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<IPrintService, PrintService>();

            // OrderDataService (for backward compatibility)
            services.AddSingleton<OrderDataService>();
        }

        private static void RegisterViewModels(IServiceCollection services)
        {
            // Register view models
            services.AddTransient<LoginViewModel>();
            services.AddTransient<OrderViewModel>();
            services.AddTransient<TableViewModel>();
            services.AddTransient<ProductViewModel>();
            services.AddTransient<ItemsViewModel>();
            services.AddTransient<PaymentViewModel>();
            services.AddTransient<ExtrasViewModel>();
            services.AddTransient<StatisticsViewModel>();
        }
    }
}