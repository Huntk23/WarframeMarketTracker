using System;
using System.Threading.Tasks;

namespace WarframeMarketTracker.Services;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string body, string whisper, string orderId);
    event Action<string>? OrderIgnored;
    void Initialize();
}