using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WarframeMarketTracker.ViewModels;

public partial class AboutWindowViewModel : ViewModelBase
{
    private const string GitHubOwner = "Huntk23";
    private const string GitHubRepo = "WarframeMarketTracker";

    private readonly HttpClient _client;

    [ObservableProperty]
    public partial bool IsChecking { get; set; } = true;

    [ObservableProperty]
    public partial bool HasUpdate { get; set; }

    [ObservableProperty]
    public partial string VersionStatus { get; set; } = "Checking for new version...";

    [ObservableProperty]
    public partial string? ReleaseUrl { get; set; }

    [ObservableProperty]
    public partial string? LatestVersion { get; set; }

    public static string RepositoryUrl => $"https://github.com/{GitHubOwner}/{GitHubRepo}";

    public AboutWindowViewModel(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("GitHub");
    }

    public async Task CheckForUpdateAsync()
    {
        try
        {
            // Run the API call and a minimum display delay in parallel
            var delayTask = Task.Delay(TimeSpan.FromSeconds(3));
            var fetchTask = _client.GetFromJsonAsync<GitHubRelease>(
                $"repos/{GitHubOwner}/{GitHubRepo}/releases/latest");

            await Task.WhenAll(delayTask, fetchTask);

            var release = fetchTask.Result;

            if (release?.TagName == null)
            {
                VersionStatus = "Could not check for updates.";
            }
            else
            {
                var latest = release.TagName.TrimStart('v', 'V');
                var isNewer = Version.TryParse(latest, out var remoteVersion)
                              && Version.TryParse(BuildInfo.AppVersion, out var localVersion)
                              && remoteVersion > localVersion;

                if (isNewer)
                {
                    HasUpdate = true;
                    LatestVersion = latest;
                    ReleaseUrl = $"{RepositoryUrl}/releases/tag/{release.TagName}";
                }
                else
                {
                    VersionStatus = "You're on the latest version.";
                }
            }
        }
        catch
        {
            VersionStatus = "Could not check for updates.";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            Process.Start("open", url);
    }

    private record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName
    );
}