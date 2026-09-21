using System.Diagnostics;
using System.Text.Json;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;

namespace MachineIDLogger.Services;

public class SettingsService : ISettingsService
{
    private readonly string _configDirectory;
    private readonly string _settingsFilePath;
    private ApplicationSettings _settings = new();

    public ApplicationSettings Settings => _settings;

    public SettingsService(string configDirectory = @"C:\MachineIDLogger\Config")
    {
        _configDirectory = configDirectory;
        _settingsFilePath = Path.Combine(_configDirectory, "settings.json");
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = await File.ReadAllTextAsync(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<ApplicationSettings>(json);
                if (loaded != null)
                {
                    _settings = loaded;
                    return;
                }
            }
        }
        catch
        {
            // Fallback to defaults
        }

        _settings = new ApplicationSettings();
    }

    public async Task SaveSettingsAsync(ApplicationSettings settings)
    {
        _settings = settings;
        try
        {
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_settings, options);
            string tempFile = _settingsFilePath + ".tmp";
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _settingsFilePath, overwrite: true);
        }
        catch
        {
            // Ignore write errors to protected directory if not elevated
        }
    }
}

public class UpdateService : IUpdateService
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/subhadipshil/machineidlogger/releases/latest";
    private const string ReleasesPageUrl = "https://github.com/subhadipshil/machineidlogger/releases";
    private readonly ILoggingService _loggingService;

    public UpdateService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        string currentVersion = "1.0.0";
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MachineIDLogger-UpdateChecker/1.0");

            var response = await client.GetAsync(ReleaseApiUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? currentVersion;
                string notes = doc.RootElement.GetProperty("body").GetString() ?? "No release notes available.";
                string htmlUrl = doc.RootElement.GetProperty("html_url").GetString() ?? ReleasesPageUrl;

                string cleanTag = tag.TrimStart('v');
                bool isNewer = IsVersionGreater(cleanTag, currentVersion);

                return new UpdateCheckResult(isNewer, currentVersion, cleanTag, notes, htmlUrl);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("UpdateCheck", "Could not check GitHub releases API", ex.Message);
        }

        return new UpdateCheckResult(false, currentVersion, currentVersion, "Up to date or offline.", ReleasesPageUrl);
    }

    public void OpenReleasePage(string? url = null)
    {
        string target = string.IsNullOrWhiteSpace(url) ? ReleasesPageUrl : url;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Update", $"Failed to open release page: {ex.Message}");
        }
    }

    private static bool IsVersionGreater(string latest, string current)
    {
        if (Version.TryParse(latest, out var vLatest) && Version.TryParse(current, out var vCurrent))
        {
            return vLatest > vCurrent;
        }
        return false;
    }
}
