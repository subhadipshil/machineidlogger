using MachineIDLogger.Core.Models;

namespace MachineIDLogger.Core.Interfaces;

public interface ISystemInformationService
{
    Task<SystemOverview> GetSystemOverviewAsync(CancellationToken cancellationToken = default);
    Task<OperatingSystemInfo> GetOperatingSystemInfoAsync(CancellationToken cancellationToken = default);
}

public interface IProcessorService
{
    Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default);
    Task<double> GetLiveCpuUsageAsync(CancellationToken cancellationToken = default);
}

public interface IMemoryService
{
    Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default);
}

public interface IGraphicsService
{
    Task<List<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken = default);
}

public interface IStorageService
{
    Task<List<StorageDriveInfo>> GetPhysicalDrivesAsync(CancellationToken cancellationToken = default);
    Task<List<VolumeInfo>> GetVolumesAsync(CancellationToken cancellationToken = default);
}

public interface IMotherboardService
{
    Task<MotherboardInfo> GetMotherboardInfoAsync(CancellationToken cancellationToken = default);
}

public interface IBiosService
{
    Task<BiosInfo> GetBiosInfoAsync(CancellationToken cancellationToken = default);
}

public interface INetworkService
{
    Task<List<NetworkAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default);
}

public interface IDisplayService
{
    Task<List<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default);
}

public interface IDeviceInventoryService
{
    Task<List<DeviceItemInfo>> GetDevicesAsync(CancellationToken cancellationToken = default);
}

public interface IDeviceDiscoveryService
{
    bool IsScanning { get; }
    string CurrentScanStep { get; }
    event EventHandler<string>? ScanStepChanged;
    event EventHandler<bool>? ScanStateChanged;
    Task RunFullDiscoveryAsync(CancellationToken cancellationToken = default);
    SystemOverview CachedOverview { get; }
}

public interface IIdentifierService
{
    Task<List<IdentifierInfo>> GetIdentifiersAsync(CancellationToken cancellationToken = default);
    Task<string> GetMachineGuidAsync(CancellationToken cancellationToken = default);
    Task<string> GetSmbiosUuidAsync(CancellationToken cancellationToken = default);
}

public record MachineGuidOperationResult(
    bool Success,
    string PreviousGuid,
    string NewGuid,
    string BackupPath,
    string Message,
    string? ErrorDetails = null
);

public interface IMachineGuidService
{
    Task<string> ReadCurrentMachineGuidAsync();
    Task<string> CreatePreModificationBackupAsync(string currentGuid);
    Task<MachineGuidOperationResult> ChangeMachineGuidAsync(string newGuid, string initiator = "GUI");
    Task<MachineGuidOperationResult> RestoreMachineGuidAsync(string targetGuid, string initiator = "GUI");
    Task<bool> VerifyMachineGuidAsync(string expectedGuid);
}

public record CursorOperationResult(
    bool Success,
    string Message,
    string? BackupPath = null,
    string? ErrorDetails = null
);

public interface ICursorIdentityService
{
    Task<CursorIdentityInfo> DetectCursorIdentityAsync(CancellationToken cancellationToken = default);
    Task<CursorOperationResult> GenerateNewIdentityAsync(CancellationToken cancellationToken = default);
    Task<CursorOperationResult> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
}

public interface IHistoryService
{
    Task InitializeAsync();
    Task<List<MachineGuidHistoryEntry>> GetHistoryAsync(int limit = 100);
    Task AddHistoryEntryAsync(MachineGuidHistoryEntry entry);
    Task<int> DiscoverAndImportExistingBackupsAsync(string backupDirectory);
    Task ClearHistoryAsync();
}

public interface ILoggingService
{
    void Log(LogSeverity severity, string category, string message, string? exception = null, string? caller = null);
    void LogInfo(string category, string message);
    void LogWarning(string category, string message, string? exception = null);
    void LogError(string category, string message, string? exception = null);
    Task<List<LogEntry>> GetLogsAsync(string? categoryFilter = null, LogSeverity? minSeverity = null, string? searchText = null, int limit = 200);
    Task ClearLogsAsync();
    string GetLogFilePath();
}

public interface IDiagnosticsService
{
    Task<List<DiagnosticResult>> RunDiagnosticsAsync(CancellationToken cancellationToken = default);
}

public enum ExportFormat
{
    Txt,
    Json,
    Csv,
    Html
}

public interface IExportService
{
    Task<string> GenerateExportContentAsync(ExportReport report, ExportFormat format);
    Task<string> SaveExportToFileAsync(ExportReport report, ExportFormat format, string destinationPath);
}

public interface ISettingsService
{
    ApplicationSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync(ApplicationSettings settings);
}

public record UpdateCheckResult(
    bool UpdateAvailable,
    string CurrentVersion,
    string LatestVersion,
    string ReleaseNotes,
    string ReleaseUrl
);

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
    void OpenReleasePage(string? url = null);
}
