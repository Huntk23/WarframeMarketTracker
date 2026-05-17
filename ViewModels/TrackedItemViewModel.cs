using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarframeMarketTracker.Models;
using WarframeMarketTracker.Services;

namespace WarframeMarketTracker.ViewModels;

public partial class TrackedItemViewModel : ViewModelBase
{
    private readonly IItemCache _cache;
    private readonly ITrackedItemRegistry _registry;
    private readonly IOfferMediatorService _offerMediator;
    private readonly IDialogService _dialogService;
    private readonly Action<TrackedItemViewModel> _removeCallback;
    private ItemShort? _resolvedItem;
    private string? _registeredKey;
    private bool _isApplyingItemName;

    public event Action? Modified;

    [ObservableProperty]
    public partial string ItemName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TargetPlatinum { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsValid { get; set; }

    [ObservableProperty]
    public partial int? TargetRank { get; set; }

    [ObservableProperty]
    public partial int? MaxRank { get; set; }

    [ObservableProperty]
    public partial bool HasRanks { get; set; }

    [ObservableProperty]
    public partial Uri? MarketUrl { get; set; }

    [ObservableProperty]
    public partial MarketOffer? BestOffer { get; set; }

    public string? Slug => _resolvedItem?.Slug;

    public TrackedItemViewModel(
        IItemCache cache,
        ITrackedItemRegistry registry,
        IOfferMediatorService offerMediator,
        IDialogService dialogService,
        Action<TrackedItemViewModel> removeCallback)
    {
        _cache = cache;
        _registry = registry;
        _offerMediator = offerMediator;
        _dialogService = dialogService;
        _removeCallback = removeCallback;
    }

    public void SetBestOffer(MarketOffer offer) => BestOffer = offer;

    public void ClearBestOffer() => BestOffer = null;

    [RelayCommand]
    private void Remove()
    {
        UnregisterIfNeeded();
        _removeCallback(this);
    }

    [RelayCommand]
    private async Task CopyWhisper()
    {
        if (BestOffer is null) return;
        await _dialogService.CopyWhisperAsync(BestOffer.Whisper);
    }

    [RelayCommand]
    private void IgnoreOffer()
    {
        if (BestOffer is null) return;
        _offerMediator.IgnoreOffer(BestOffer);
    }

    partial void OnItemNameChanged(string value)
    {
        _isApplyingItemName = true;
        try
        {
            UnregisterIfNeeded();
            BestOffer = null;

            _cache.TryGetByName(value, out _resolvedItem);

            IsValid = _resolvedItem != null;
            MarketUrl = _resolvedItem != null
                ? MarketUrls.SaleLink(_resolvedItem.Slug)
                : null;

            if (_resolvedItem != null)
            {
                MaxRank = _resolvedItem.MaxRank;
                HasRanks = _resolvedItem.MaxRank is > 0;
                TargetRank = HasRanks ? MaxRank : null;
            }
            else
            {
                MaxRank = null;
                HasRanks = false;
                TargetRank = null;
            }

            if (!IsValid) IsEnabled = false;
            else if (IsEnabled) RegisterEntry();
        }
        finally
        {
            _isApplyingItemName = false;
        }

        Modified?.Invoke();
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isApplyingItemName) return;

        if (value && IsValid)
        {
            RegisterEntry();
        }
        else
        {
            UnregisterIfNeeded();
            BestOffer = null;
        }

        Modified?.Invoke();
    }

    partial void OnTargetPlatinumChanged(int value)
    {
        if (_isApplyingItemName) return;

        BestOffer = null;
        if (IsEnabled && IsValid) RegisterEntry();

        Modified?.Invoke();
    }

    partial void OnTargetRankChanged(int? value)
    {
        if (_isApplyingItemName) return;

        BestOffer = null;
        if (IsEnabled && IsValid) RegisterEntry();

        Modified?.Invoke();
    }

    private void RegisterEntry()
    {
        if (_resolvedItem == null) return;

        _registry.Register(ItemName, new TrackedItemEntry(
            _resolvedItem.Slug, ItemName, TargetPlatinum, HasRanks ? TargetRank : null));
        _registeredKey = ItemName;
    }

    private void UnregisterIfNeeded()
    {
        if (_registeredKey == null) return;
        _registry.Unregister(_registeredKey);
        _registeredKey = null;
    }
}