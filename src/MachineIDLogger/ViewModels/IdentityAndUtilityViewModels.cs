using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Core.Validation;
using MachineIDLogger.Infrastructure.Windows;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.ViewModels;

public partial class MachineGuidViewModel : ObservableObject
{
    private readonly IMachineGuidService _guidService;
    private readonly IIdentifierService _identifierService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private string _currentMachineGuid = "Loading...";

    [ObservableProperty]
    private ObservableCollection<IdentifierInfo> _identifiers = [];

    [ObservableProperty]
    private string _newGuidInput = "";

    [ObservableProperty]
    private string _validationMessage = "";

    [ObservableProperty]
    private bool _isInputValid;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isChangeDialogOpen;

    [ObservableProperty]
    private string _operationResultBanner = "";

    [ObservableProperty]
    private bool _operationSucceeded;

    [ObservableProperty]
    private bool _hasOperationResult;

    [ObservableProperty]
    private bool _isElevated;

    public MachineGuidViewModel(
        IMachineGuidService guidService,
        IIdentifierService identifierService,
        ILoggingService loggingService)
    {
        _guidService = guidService;
        _identifierService = identifierService;
        _loggingService = loggingService;
        _isElevated = NativeMethods.IsProcessElevated();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        CurrentMachineGuid = await _guidService.ReadCurrentMachineGuidAsync();
        IsElevated = NativeMethods.IsProcessElevated();

        var ids = await _identifierService.GetIdentifiersAsync();
        Identifiers.Clear();
        foreach (var id in ids) Identifiers.Add(id);

        IsLoading = false;
    }

    partial void OnNewGuidInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ValidationMessage = "";
            IsInputValid = false;
            return;
        }

        if (GuidValidator.IsValid(value, out _, out string err))
        {
            ValidationMessage = "Valid RFC 4122 GUID format.";
            IsInputValid = true;
        }
        else
        {
            ValidationMessage = err;
            IsInputValid = false;
        }
    }

    [RelayCommand]
    public void GenerateRandomGuid()
    {
        NewGuidInput = GuidValidator.GenerateRandomUuid();
    }

    [RelayCommand]
    public void CopyCurrentGuid()
    {
        CopyText(CurrentMachineGuid);
    }

    [RelayCommand]
    public void CopyIdentifier(IdentifierInfo? id)
    {
        if (id != null) CopyText(id.Value);
    }

    [RelayCommand]
    public void OpenChangeDialog()
    {
        if (string.IsNullOrEmpty(NewGuidInput))
        {
            GenerateRandomGuid();
        }
        IsChangeDialogOpen = true;
    }

    [RelayCommand]
    public void CloseChangeDialog()
    {
        IsChangeDialogOpen = false;
    }

    [RelayCommand]
    public async Task ApplyChangeAsync()
    {
        if (!IsInputValid) return;

        IsLoading = true;
        IsChangeDialogOpen = false;

        var result = await _guidService.ChangeMachineGuidAsync(NewGuidInput, "GUI");

        HasOperationResult = true;
        OperationSucceeded = result.Success;
        OperationResultBanner = result.Message;

        if (result.Success)
        {
            CurrentMachineGuid = await _guidService.ReadCurrentMachineGuidAsync();
            await LoadAsync();
        }

        IsLoading = false;
    }

    private static void CopyText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Services.ClipboardHelper.Copy(text);
    }
}

public partial class DeviceInventoryViewModel : ObservableObject
{
    private readonly IDeviceInventoryService _deviceService;
    private List<DeviceItemInfo> _allDevices = [];

    [ObservableProperty]
    private ObservableCollection<DeviceItemInfo> _filteredDevices = [];

    [ObservableProperty]
    private DeviceItemInfo? _selectedDevice;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private bool _isLoading;

    public List<string> Categories { get; } =
    [
        "All", "Processor", "Memory", "Display", "Storage", "Network", "Audio", "USB", "Bluetooth", "Camera", "Input", "System", "Other"
    ];

    public DeviceInventoryViewModel(IDeviceInventoryService deviceService)
    {
        _deviceService = deviceService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allDevices = await _deviceService.GetDevicesAsync();
        ApplyFilters();
        IsLoading = false;
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var query = _allDevices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "All")
        {
            query = query.Where(d => d.Category.ToString().Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            query = query.Where(d =>
                d.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                d.Manufacturer.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                d.DeviceId.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                d.PrimaryHardwareId.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        FilteredDevices.Clear();
        foreach (var dev in query.Take(500))
        {
            FilteredDevices.Add(dev);
        }

        if (SelectedDevice == null && FilteredDevices.Count > 0)
        {
            SelectedDevice = FilteredDevices[0];
        }
    }

    [RelayCommand]
    public void CopyHardwareId(DeviceItemInfo? dev)
    {
        if (dev != null && !string.IsNullOrWhiteSpace(dev.PrimaryHardwareId))
        {
            Services.ClipboardHelper.Copy(dev.PrimaryHardwareId);
        }
    }
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private readonly IMachineGuidService _guidService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ObservableCollection<MachineGuidHistoryEntry> _historyEntries = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    public HistoryViewModel(
        IHistoryService historyService,
        IMachineGuidService guidService,
        ILoggingService loggingService)
    {
        _historyService = historyService;
        _guidService = guidService;
        _loggingService = loggingService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        await _historyService.InitializeAsync();
        var entries = await _historyService.GetHistoryAsync(200);

        HistoryEntries.Clear();
        foreach (var e in entries) HistoryEntries.Add(e);

        IsLoading = false;
    }

    [RelayCommand]
    public async Task RestoreEntryAsync(MachineGuidHistoryEntry? entry)
    {
        if (entry == null) return;
        string target = string.IsNullOrWhiteSpace(entry.PreviousGuid) ? entry.NewGuid : entry.PreviousGuid;

        IsLoading = true;
        var result = await _guidService.RestoreMachineGuidAsync(target, "GUI History Restore");
        StatusMessage = result.Message;

        await LoadAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public void CopyGuid(string? guid)
    {
        if (!string.IsNullOrWhiteSpace(guid))
        {
            Services.ClipboardHelper.Copy(guid);
            StatusMessage = "GUID copied to clipboard.";
        }
    }

    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        await _historyService.ClearHistoryAsync();
        await LoadAsync();
        StatusMessage = "Audit history cleared.";
    }
}

public partial class LogsViewModel : ObservableObject
{
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logEntries = [];

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private string _selectedSeverity = "All";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    public List<string> SeverityOptions { get; } = ["All", "Information", "Warning", "Error", "Critical"];

    public LogsViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        LogSeverity? minSev = SelectedSeverity switch
        {
            "Information" => LogSeverity.Information,
            "Warning" => LogSeverity.Warning,
            "Error" => LogSeverity.Error,
            "Critical" => LogSeverity.Critical,
            _ => null
        };

        var logs = await _loggingService.GetLogsAsync(minSeverity: minSev, searchText: SearchQuery, limit: 300);

        LogEntries.Clear();
        foreach (var l in logs) LogEntries.Add(l);

        IsLoading = false;
    }

    partial void OnSearchQueryChanged(string value) => _ = LoadAsync();
    partial void OnSelectedSeverityChanged(string value) => _ = LoadAsync();

    [RelayCommand]
    public void CopyLog(LogEntry? entry)
    {
        if (entry != null)
        {
            Services.ClipboardHelper.Copy($"[{entry.TimestampDisplay}] [{entry.Severity}] [{entry.Category}] {entry.Message}");
            StatusMessage = "Log line copied.";
        }
    }

    [RelayCommand]
    public async Task ClearLogsAsync()
    {
        await _loggingService.ClearLogsAsync();
        await LoadAsync();
        StatusMessage = "In-memory logs cleared.";
    }
}

public partial class CursorIdentityViewModel : ObservableObject
{
    private readonly ICursorIdentityService _cursorService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private CursorIdentityInfo _cursorInfo = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _operationSucceeded;

    [ObservableProperty]
    private bool _hasBanner;

    public CursorIdentityViewModel(ICursorIdentityService cursorService, ILoggingService loggingService)
    {
        _cursorService = cursorService;
        _loggingService = loggingService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        CursorInfo = await _cursorService.DetectCursorIdentityAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public async Task GenerateNewIdentityAsync()
    {
        IsLoading = true;
        var result = await _cursorService.GenerateNewIdentityAsync();
        HasBanner = true;
        OperationSucceeded = result.Success;
        StatusMessage = result.Message;

        await LoadAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public async Task RestoreBackupAsync(string? backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath)) return;
        IsLoading = true;
        var result = await _cursorService.RestoreBackupAsync(backupPath);
        HasBanner = true;
        OperationSucceeded = result.Success;
        StatusMessage = result.Message;

        await LoadAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public void CopyValue(string? val)
    {
        if (!string.IsNullOrWhiteSpace(val))
        {
            Services.ClipboardHelper.Copy(val);
            StatusMessage = "Copied to clipboard.";
        }
    }
}

public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly IDiagnosticsService _diagService;
    private readonly IExportService _exportService;
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly IIdentifierService _identifierService;
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private ObservableCollection<DiagnosticResult> _results = [];

    [ObservableProperty]
    private int _passCount;

    [ObservableProperty]
    private int _warningCount;

    [ObservableProperty]
    private int _errorCount;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _statusMessage = "";

    public DiagnosticsViewModel(
        IDiagnosticsService diagService,
        IExportService exportService,
        IDeviceDiscoveryService discoveryService,
        IIdentifierService identifierService,
        IHistoryService historyService)
    {
        _diagService = diagService;
        _exportService = exportService;
        _discoveryService = discoveryService;
        _identifierService = identifierService;
        _historyService = historyService;
    }

    [RelayCommand]
    public async Task RunDiagnosticsAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        StatusMessage = "Executing system diagnostic checks...";
        try
        {
            var diagList = await Task.Run(() => _diagService.RunDiagnosticsAsync());

            var newCol = new ObservableCollection<DiagnosticResult>(diagList);
            Results = newCol;

            PassCount = Results.Count(r => r.Status == DiagnosticStatus.Pass);
            WarningCount = Results.Count(r => r.Status == DiagnosticStatus.Warning);
            ErrorCount = Results.Count(r => r.Status == DiagnosticStatus.Error);
            OnPropertyChanged(nameof(Results));
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(PassCount));
            OnPropertyChanged(nameof(WarningCount));

            StatusMessage = $"Diagnostics complete: {PassCount} Pass, {WarningCount} Warning, {ErrorCount} Error.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Diagnostics failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    public async Task ExportReportAsync()
    {
        try
        {
            var report = new ExportReport
            {
                System = _discoveryService.CachedOverview,
                Identifiers = await _identifierService.GetIdentifiersAsync(),
                History = await _historyService.GetHistoryAsync(50)
            };

            string exportDir = @"C:\MachineIDLogger\Exports";
            string fileName = $"DiagnosticReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.html";
            string path = Path.Combine(exportDir, fileName);

            await _exportService.SaveExportToFileAsync(report, ExportFormat.Html, path);
            StatusMessage = $"Report saved: {fileName}";
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }
}

public partial class WhatsNewViewModel : ObservableObject
{
    public record VersionRelease(string Version, string Date, List<string> Highlights, bool IsCurrent);

    public List<VersionRelease> Releases { get; } =
    [
        new("v1.0.0", "September 2026",
            [
                "Initial Production Release of MachineIDLogger.",
                "Real hardware discovery engine: CPU, Memory, GPUs, Disks, Motherboard, BIOS, Displays, Network, and PnP devices.",
                "Windows MachineGuid manager with 7-step verified elevation workflow.",
                "Atomic JSON backups and SQLite history persistence in C:\\MachineIDLogger\\.",
                "Featured: Version-aware Cursor local telemetry identity management.",
                "Comprehensive 12-point system diagnostic runner with printable HTML exports.",
                "Professional FaceSoter-style vector hardware icon and Windows Fluent theme.",
                "Integrated voluntary developer support with high-contrast UPI QR card."
            ], true)
    ];
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ApplicationSettings _settings = new();

    [ObservableProperty]
    private string _statusMessage = "";

    public List<string> Themes { get; } = ["System", "Dark", "Light"];

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _settings = _settingsService.Settings;
    }

    public async Task LoadAsync()
    {
        await _settingsService.LoadSettingsAsync();
        Settings = _settingsService.Settings;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        await _settingsService.SaveSettingsAsync(Settings);
        StatusMessage = "Settings saved successfully.";
    }

    [RelayCommand]
    public void OpenDataFolder()
    {
        try
        {
            if (!Directory.Exists(Settings.DataDirectory))
            {
                Directory.CreateDirectory(Settings.DataDirectory);
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = Settings.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to open folder: {ex.Message}";
        }
    }
}

public partial class AboutViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private string _appName = "MachineIDLogger";

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _buildNumber = "2026.09.21.1";

    [ObservableProperty]
    private string _targetFramework = ".NET 10.0 LTS (win-x64)";

    [ObservableProperty]
    private string _architecture = "x64 (ARM64 ready)";

    [ObservableProperty]
    private string _developer = "Subhadip Shil";

    [ObservableProperty]
    private string _upiId = "subhadipshil.pnb@ybl";

    [ObservableProperty]
    private string _upiPayUri = "upi://pay?pa=subhadipshil.pnb@ybl&pn=Subhadip%20Shil&cu=INR";

    [ObservableProperty]
    private string _gitHubRepoUrl = "https://github.com/subhadipshil/machineidlogger";

    [ObservableProperty]
    private string _buyMeCoffeeWebUrl = "https://buymeacoffee.com/subhadipshil";

    [ObservableProperty]
    private string _updateStatus = "";

    [ObservableProperty]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    private bool _isQrVisible = true;

    [ObservableProperty]
    private string _copyFeedback = "";

    public AboutViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    [RelayCommand]
    public async Task CheckForUpdatesAsync()
    {
        IsCheckingUpdate = true;
        UpdateStatus = "Checking GitHub Releases...";
        var result = await _updateService.CheckForUpdatesAsync();

        if (result.UpdateAvailable)
        {
            UpdateStatus = $"New version available: {result.LatestVersion}!";
        }
        else
        {
            UpdateStatus = $"MachineIDLogger is up to date (v{Version}).";
        }
        IsCheckingUpdate = false;
    }

    [RelayCommand]
    public void OpenGitHub()
    {
        LaunchBrowser(GitHubRepoUrl);
    }

    [RelayCommand]
    public void CopyUpiId()
    {
        Services.ClipboardHelper.Copy(UpiId);
        CopyFeedback = "UPI ID copied to clipboard: subhadipshil.pnb@ybl";
    }

    [RelayCommand]
    public void OpenUpiPayment()
    {
        Services.ClipboardHelper.Copy(UpiId);
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UpiPayUri) { UseShellExecute = true });
            CopyFeedback = "Opening UPI app... (UPI ID also copied to clipboard)";
        }
        catch
        {
            CopyFeedback = "UPI ID copied! Scan the QR code or paste 'subhadipshil.pnb@ybl' in your banking app.";
        }
    }

    [RelayCommand]
    public void OpenBuyMeCoffeeWeb()
    {
        LaunchBrowser(BuyMeCoffeeWebUrl);
        CopyFeedback = "Opening Buy Me a Coffee in browser...";
    }

    private void LaunchBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{url}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                Services.ClipboardHelper.Copy(url);
                CopyFeedback = $"URL copied to clipboard: {url}";
            }
        }
    }

    [RelayCommand]
    public void ToggleQr()
    {
        IsQrVisible = !IsQrVisible;
    }
}

public partial class MainViewModel : ObservableObject
{
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private string _statusStep = "Ready";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _globalSearchText = "";

    [ObservableProperty]
    private bool _isElevated;

    public MainViewModel(IDeviceDiscoveryService discoveryService, ILoggingService loggingService)
    {
        _discoveryService = discoveryService;
        _loggingService = loggingService;

        _discoveryService.ScanStepChanged += (s, step) => StatusStep = step;
        _discoveryService.ScanStateChanged += (s, scanning) => IsScanning = scanning;

        _isElevated = NativeMethods.IsProcessElevated();
    }

    public async Task StartInitialScanAsync()
    {
        await _discoveryService.RunFullDiscoveryAsync();
    }
}
