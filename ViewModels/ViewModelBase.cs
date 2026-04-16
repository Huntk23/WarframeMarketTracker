using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WarframeMarketTracker.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public Window? Owner { get; set; }
}