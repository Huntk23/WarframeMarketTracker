using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarframeMarketTracker.Services;

namespace WarframeMarketTracker.ViewModels;

public partial class TrackedItemViewModel : ViewModelBase
{
    private readonly IItemCache _cache;
    private readonly ITrackedItemRegistry _registry;
    private readonly Action<TrackedItemViewModel> _removeCallback;

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

    public TrackedItemViewModel(IItemCache cache, ITrackedItemRegistry registry, Action<TrackedItemViewModel> removeCallback)
    {
        _cache = cache;
        _registry = registry;
        _removeCallback = removeCallback;
    }

    [RelayCommand]
    private void Remove()
    {
        if (IsEnabled)
            _registry.Unregister(ItemName);

        _removeCallback(this);
    }

    partial void OnItemNameChanged(string value)
    {
        var item = _cache.Items.FirstOrDefault(i =>
            i.EnglishName.Equals(value, StringComparison.OrdinalIgnoreCase));

        IsValid = item != null;

        if (item != null)
        {
            MaxRank = item.MaxRank;
            HasRanks = item.MaxRank is > 0;

            // Reset target rank when switching items
            TargetRank = HasRanks ? MaxRank : null;
        }
        else
        {
            MaxRank = null;
            HasRanks = false;
            TargetRank = null;
        }

        if (!IsValid) IsEnabled = false;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (value && IsValid)
            RegisterEntry();
        else
            _registry.Unregister(ItemName);
    }

    partial void OnTargetPlatinumChanged(int value)
    {
        if (IsEnabled && IsValid) RegisterEntry();
    }

    partial void OnTargetRankChanged(int? value)
    {
        if (IsEnabled && IsValid) RegisterEntry();
    }

    private void RegisterEntry()
    {
        var item = _cache.Items.FirstOrDefault(i =>
            i.EnglishName.Equals(ItemName, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            _registry.Register(ItemName, new TrackedItemEntry(
                item.Slug, ItemName, TargetPlatinum, HasRanks ? TargetRank : null));
        }
    }
}