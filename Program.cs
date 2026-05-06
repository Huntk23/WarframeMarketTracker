using System;
using Avalonia;
using Avalonia.Labs.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WarframeMarketTracker.Services;
using WarframeMarketTracker.ViewModels;
using WarframeMarketTracker.Views;

namespace WarframeMarketTracker;

internal static class Program
{
    public const string AppName = "Warframe Market Tracker";

    [STAThread]
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.File("logs/app-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((_, services) =>
                {
                    const string userAgent = $"{nameof(WarframeMarketTracker)}/{BuildInfo.AppVersion}";

                    // 1. Setup named HTTP Clients
                    services.AddHttpClient("WfmApi", c =>
                    {
                        c.BaseAddress = new Uri("https://api.warframe.market/v2/");
                        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    });

                    services.AddHttpClient("WfmAssets", c =>
                    {
                        c.BaseAddress = new Uri("https://warframe.market/static/assets/");
                        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                    });

                    services.AddHttpClient("GitHub", c =>
                    {
                        c.BaseAddress = new Uri("https://api.github.com/");
                        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                        c.Timeout = TimeSpan.FromSeconds(5);
                    });

                    // 2. Services & State
                    services.AddTransient<IWarframeMarketService, WarframeMarketService>();
                    services.AddSingleton<IItemCache, ItemCache>();
                    services.AddSingleton<ITrackedItemRegistry, TrackedItemRegistry>();
                    services.AddSingleton<ITrackedItemStore, TrackedItemStore>();
                    services.AddSingleton<IThumbnailCache, ThumbnailCache>();

                    // 3. Background Services
                    services.AddHostedService<ItemCacheHydrationService>();
                    services.AddHostedService<MarketPollingService>();

                    // 4. Components & UI
                    services.AddSingleton<IUserInterfaceNotificationService, UserInterfaceNotificationService>();
                    services.AddSingleton<INotificationService, NativeNotificationService>();
                    services.AddTransient<AboutWindowViewModel>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // Start the background services (API poller, etc.)
            host.Start();

            BuildAvaloniaApp(host.Services)
                .StartWithClassicDesktopLifetime(args);

            host.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services)
        => AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .WithAppNotifications(NativeNotificationService.AppNotificationOptions)
            .LogToTrace();
}