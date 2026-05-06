using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using WarframeMarketTracker.Controls;
using WarframeMarketTracker.Services;
using WarframeMarketTracker.Views;

namespace WarframeMarketTracker;

public partial class App : Application
{
    private readonly IServiceProvider _services;
    private TrayIcon? _trayIcon;

    public App(IServiceProvider serviceProvider)
    {
        _services = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Keep the app alive even when all windows are hidden
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var mainWindow = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;

            var notificationService = _services.GetRequiredService<INotificationService>();
            notificationService.Initialize();

            CachedImage.Initialize(_services.GetRequiredService<IThumbnailCache>());

            SetupTrayIcon(desktop, mainWindow);
            FixTrayMenuPosition();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
    {
        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => desktop.Shutdown();

        _trayIcon = new TrayIcon
        {
            Icon = mainWindow.Icon,
            ToolTipText = Program.AppName,
            IsVisible = true,
            Menu = new NativeMenu {exitItem}
        };

        _trayIcon.Clicked += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.Activate();
        };
    }

    /// <summary>
    /// Avalonia's TrayPopupRoot positions its top-left corner at the cursor, causing the menu to open downward.
    /// This class handler catches the menu  presenter after layout and shifts the window up by its own height so
    /// the menu appears above the cursor instead. /Nit-pick
    /// </summary>
    private static void FixTrayMenuPosition()
    {
        Control.LoadedEvent.AddClassHandler<MenuFlyoutPresenter>((presenter, _) =>
        {
            if (TopLevel.GetTopLevel(presenter) is not Window {Name: { } name} window
                || !name.StartsWith("AvaloniaTrayPopupRoot"))
                return;

            Dispatcher.UIThread.Post(() =>
            {
                var height = window.ClientSize.Height * window.RenderScaling;
                if (height > 0)
                {
                    var pos = window.Position;
                    window.Position = new PixelPoint(pos.X, pos.Y - (int) height);
                }
            }, DispatcherPriority.Loaded);
        });
    }
}