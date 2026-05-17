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
    private readonly IUserInterfaceNotificationService _uiNotificationService;
    private readonly Action<TrackedItemViewModel> _removeCallback;
    private ItemShort? _resolvedItem;
    private string? _registeredKey;

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
        IUserInterfaceNotificationService uiNotificationService,
        Action<TrackedItemViewModel> removeCallback)
    {
        _cache = cache;
        _registry = registry;
        _uiNotificationService = uiNotificationService;
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
        await _uiNotificationService.CopyWhisperAsync(BestOffer.Whisper);
    }

    [RelayCommand]
    private void IgnoreOffer()
    {
        if (BestOffer is null) return;
        _uiNotificationService.IgnoreOffer(BestOffer);
    }

    partial void OnItemNameChanged(string value)
    {
        UnregisterIfNeeded();
        BestOffer = null;

        _cache.TryGetByName(value, out _resolvedItem);

        IsValid = _resolvedItem != null;
        MarketUrl = _resolvedItem != null
            ? new Uri($"https://warframe.market/items/{_resolvedItem.Slug}?type=sell")
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

    partial void OnIsEnabledChanged(bool value)
    {
        if (value && IsValid)
        {
            RegisterEntry();
        }
        else
        {
            UnregisterIfNeeded();
            BestOffer = null;
        }
    }

    partial void OnTargetPlatinumChanged(int value)
    {
        BestOffer = null;
        if (IsEnabled && IsValid) RegisterEntry();
    }

    partial void OnTargetRankChanged(int? value)
    {
        BestOffer = null;
        if (IsEnabled && IsValid) RegisterEntry();
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