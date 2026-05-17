using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using WarframeMarketTracker.Models;
using WarframeMarketTracker.Services;

namespace WarframeMarketTracker.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IItemCache _cache;
    private readonly ITrackedItemRegistry _registry;
    private readonly ITrackedItemStore _store;
    private readonly IOfferMediatorService _offerMediator;
    private readonly IDialogService _dialogService;
    private bool _isLoading;

    public ObservableCollection<TrackedItemViewModel> TrackedItems { get; } = new();

    public IReadOnlyList<ItemShort> AvailableItems => _cache.Items;

    public event Action<TrackedItemViewModel>? RowAdded;

    public MainWindowViewModel(
        IItemCache cache,
        ITrackedItemRegistry registry,
        ITrackedItemStore store,
        IOfferMediatorService offerMediator,
        IDialogService dialogService)
    {
        _cache = cache;
        _registry = registry;
        _store = store;
        _offerMediator = offerMediator;
        _dialogService = dialogService;

        TrackedItems.CollectionChanged += OnTrackedItemsChanged;
        _offerMediator.OfferAvailable += OnOfferAvailable;
        _offerMediator.OfferCleared += OnOfferCleared;
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

    private void OnOfferCleared(string slug)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var match = TrackedItems.FirstOrDefault(vm =>
                string.Equals(vm.Slug, slug, StringComparison.OrdinalIgnoreCase));
            match?.ClearBestOffer();
        });
    }

    [RelayCommand]
    private Task OpenAbout() => _dialogService.ShowAboutAsync();

    [RelayCommand]
    private void AddRow()
    {
        var vm = CreateTrackedItem();
        TrackedItems.Add(vm);
        RowAdded?.Invoke(vm);
    }

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
        var vm = new TrackedItemViewModel(_cache, _registry, _offerMediator, _dialogService, item => TrackedItems.Remove(item));
        vm.Modified += SaveTrackedItems;
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