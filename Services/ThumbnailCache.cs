using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;

namespace WarframeMarketTracker.Services;

public interface IThumbnailCache
{
    Task<Bitmap?> GetAsync(string thumbPath, CancellationToken ct = default);
}

public class ThumbnailCache : IThumbnailCache
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ThumbnailCache> _logger;
    private readonly string _diskRoot;

    // In-memory dedupe: concurrent callers for the same path share one Task. Completed tasks stay in the dictionary, so
    // subsequent in-session lookups hit memory and skip disk decode.
    private readonly Dictionary<string, Task<Bitmap?>> _inFlight = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    public ThumbnailCache(IHttpClientFactory httpClientFactory, ILogger<ThumbnailCache> logger)
    {
        _httpClient = httpClientFactory.CreateClient("WfmAssets");
        _logger = logger;
        _diskRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WarframeMarketTracker", "thumbs");
        Directory.CreateDirectory(_diskRoot);
    }

    public Task<Bitmap?> GetAsync(string thumbPath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(thumbPath))
            return Task.FromResult<Bitmap?>(null);

        lock (_gate)
        {
            if (_inFlight.TryGetValue(thumbPath, out var existing))
                return existing;

            var task = LoadAsync(thumbPath, ct);
            _inFlight[thumbPath] = task;
            return task;
        }
    }

    private async Task<Bitmap?> LoadAsync(string thumbPath, CancellationToken ct)
    {
        var diskPath = Path.Combine(_diskRoot, SanitizeFileName(thumbPath));

        try
        {
            if (File.Exists(diskPath))
            {
                try
                {
                    await using var fs = File.OpenRead(diskPath);
                    return new Bitmap(fs);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Corrupt thumb on disk, refetching: {Path}", diskPath);
                    TryDelete(diskPath);
                }
            }

            var bytes = await _httpClient.GetByteArrayAsync(thumbPath, ct).ConfigureAwait(false);

            var tempPath = diskPath + ".tmp";
            await File.WriteAllBytesAsync(tempPath, bytes, ct).ConfigureAwait(false);
            File.Move(tempPath, diskPath, overwrite: true);

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load thumb: {Path}", thumbPath);

            // Drop the failed task so a future request can retry instead of caching null forever
            lock (_gate) _inFlight.Remove(thumbPath);
            return null;
        }
    }

    private static string SanitizeFileName(string thumbPath)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(thumbPath.Length);
        foreach (var c in thumbPath)
        {
            if (c == '/' || c == '\\') sb.Append('_');
            else if (Array.IndexOf(invalid, c) >= 0) sb.Append('_');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}