using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using WarframeMarketTracker.Models;
using WarframeMarketTracker.Services;
using WarframeMarketTracker.Views;

namespace WarframeMarketTracker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IItemCache _cache;
    private readonly ITrackedItemRegistry _registry;
    private readonly ITrackedItemStore _store;
    private readonly INotificationService _notifications;
    private readonly IServiceProvider _services;
    private bool _isLoading;

    private static readonly HashSet<string> PersistedProperties =
    [
        nameof(TrackedItemViewModel.ItemName),
        nameof(TrackedItemViewModel.TargetPlatinum),
        nameof(TrackedItemViewModel.TargetRank),
        nameof(TrackedItemViewModel.IsEnabled),
    ];
    
    public ObservableCollection<TrackedItemViewModel> TrackedItems { get; } = new();

    public IEnumerable<string> AvailableItemNames => _cache.Items.Select(i => i.EnglishName);

    public MainWindowViewModel(
        IItemCache cache,
        ITrackedItemRegistry registry,
        ITrackedItemStore store,
        INotificationService notifications,
        IServiceProvider services)
    {
        _cache = cache;
        _registry = registry;
        _store = store;
        _notifications = notifications;
        _services = services;

        TrackedItems.CollectionChanged += OnTrackedItemsChanged;
        _notifications.OfferAvailable += OnOfferAvailable;
        LoadTrackedItems();
    }

    private void OnOfferAvailable(MarketOffer offer)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var match = TrackedItems.FirstOrDefault(vm =>
                string.Equals(vm.Slug, offer.Slug, StringComparison.OrdinalIgnoreCase));
            match?.SetBestOffer(offer);
        });
    }

    [RelayCommand]
    private async Task OpenAbout()
    {
        if (Owner is null) return;

        var vm = _services.GetRequiredService<AboutWindowViewModel>();
        var window = new AboutWindow { DataContext = vm };
        _ = vm.CheckForUpdateAsync();
        await window.ShowDialog(Owner);
    }

    [RelayCommand]
    private void AddRow() => TrackedItems.Add(CreateTrackedItem());

    private void LoadTrackedItems()
    {
        _isLoading = true;

        var saved = _store.Load();
        foreach (var entry in saved)
        {
            var vm = CreateTrackedItem();
            vm.ItemName = entry.ItemName;
            vm.TargetPlatinum = entry.TargetPlatinum;
            vm.TargetRank = entry.TargetRank;
            vm.IsEnabled = entry.IsEnabled;
            TrackedItems.Add(vm);
        }

        if (TrackedItems.Count == 0)
            AddRow();

        _isLoading = false;
    }
    
    private TrackedItemViewModel CreateTrackedItem()
    {
        var vm = new TrackedItemViewModel(_cache, _registry, _notifications, item => TrackedItems.Remove(item));
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null && PersistedProperties.Contains(e.PropertyName))
                SaveTrackedItems();
        };
        return vm;
    }

    private void OnTrackedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => SaveTrackedItems();

    private void SaveTrackedItems()
    {
        if (_isLoading) return;

        var items = TrackedItems
            .Where(vm => !string.IsNullOrEmpty(vm.ItemName))
            .Select(vm => new SavedTrackedItem(vm.ItemName, vm.TargetPlatinum, vm.TargetRank, vm.IsEnabled));

        _store.Save(items);
    }
}