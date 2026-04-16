using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WarframeMarketTracker.Services;

public partial class ItemCacheHydrationService : IHostedLifecycleService
{
    private readonly ILogger<ItemCacheHydrationService> _logger;
    private readonly IItemCache _cache;

    public ItemCacheHydrationService(ILogger<ItemCacheHydrationService> logger, IItemCache cache)
    {
        _cache = cache;
        _logger = logger;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Item cache hydration service initialized with {ItemCount} items")]
    private partial void LogCacheInitialized(int itemCount);

    public async Task StartingAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting item cache hydration service");
        await _cache.InitializeAsync(ct);
        LogCacheInitialized(_cache.Count);
    }

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken ct) => Task.CompletedTask;
}