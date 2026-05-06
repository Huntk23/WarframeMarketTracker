using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Labs.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
                    const string httpClientName = "WfmApi";
                    const string warframeMarketEndpoint = "https://api.warframe.market/v2/";
                    const string userAgent = $"{nameof(WarframeMarketTracker)}/{BuildInfo.AppVersion}";

                    // 1. Setup named HTTP Clients
                    services.AddHttpClient(httpClientName, c =>
                    {
                        c.BaseAddress = new Uri(warframeMarketEndpoint);
                        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                        c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                    });

                    services.AddHttpClient("GitHub", c =>
                    {
                        c.BaseAddress = new Uri("https://api.github.com/");
                        c.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                        c.Timeout = TimeSpan.FromSeconds(5);
                    });

                    // 2. Business Logic & API
                    services.AddTransient<IWarframeMarketService>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var client = factory.CreateClient(httpClientName);
                        var logger = sp.GetRequiredService<ILogger<WarframeMarketService>>();
                        return new WarframeMarketService(client, logger);
                    });

                    // 3. The Cache (must be a singleton or why else cache)
                    services.AddSingleton<IItemCache>(sp =>
                    {
                        var factory = sp.GetRequiredService<IHttpClientFactory>();
                        var client = factory.CreateClient(httpClientName);
                        return new ItemCache(client);
                    });

                    // 3b. Thread-safe registry for tracked items (shared between UI and poller)
                    services.AddSingleton<ITrackedItemRegistry, TrackedItemRegistry>();
                    services.AddSingleton<ITrackedItemStore, TrackedItemStore>();

                    // 4. Background Services
                    services.AddHostedService<ItemCacheHydrationService>();
                    services.AddHostedService<MarketPollingService>();

                    // 5. Components & UI
                    services.AddSingleton<IUserInterfaceNotificationService, UserInterfaceNotificationService>();
                    services.AddSingleton<INotificationService, NativeNotificationService>();
                    services.AddTransient<AboutWindowViewModel>();
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<MainWindow>(sp =>
                    {
                        var vm = sp.GetRequiredService<MainWindowViewModel>();
                        var window = new MainWindow { DataContext = vm };
                        vm.Owner = window;
                        return window;
                    });
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