namespace MachineIDLogger.Core.Models;

public enum HistoryAction
{
    Changed,
    Restored,
    Verified,
    Discovered
}

public enum HistoryStatus
{
    Success,
    FailedVerification,
    Rollback,
    Error
}

public class MachineGuidHistoryEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string TimestampDisplay => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string PreviousGuid { get; set; } = string.Empty;
    public string NewGuid { get; set; } = string.Empty;
    public HistoryAction Action { get; set; } = HistoryAction.Changed;
    public HistoryStatus Status { get; set; } = HistoryStatus.Success;
    public string BackupPath { get; set; } = string.Empty;
    public string Initiator { get; set; } = "GUI";
    public string Notes { get; set; } = string.Empty;
}

public class CursorIdentityInfo
{
    public bool IsInstalled { get; set; }
    public string StorageJsonPath { get; set; } = string.Empty;
    public string MachineId { get; set; } = "Not available";
    public string MacMachineId { get; set; } = "Not available";
    public string DevDeviceId { get; set; } = "Not available";
    public string SqmId { get; set; } = "Not available";
    public string VersionStatus { get; set; } = "Not detected";
    public bool IsSupported { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public List<string> AvailableBackups { get; set; } = [];
}

public enum DiagnosticStatus
{
    Pass,
    Warning,
    Error,
    NotAvailable
}

public class DiagnosticResult
{
    public string CheckName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DiagnosticStatus Status { get; set; } = DiagnosticStatus.Pass;
    public string Details { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;

    public string StatusBadge => Status switch
    {
        DiagnosticStatus.Pass => "PASS",
        DiagnosticStatus.Warning => "WARNING",
        DiagnosticStatus.Error => "ERROR",
        DiagnosticStatus.NotAvailable => "NOT AVAILABLE",
        _ => Status.ToString().ToUpperInvariant()
    };
}

public enum LogSeverity
{
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public class LogEntry
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string TimestampDisplay => TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff");
    public LogSeverity Severity { get; set; } = LogSeverity.Information;
    public string Category { get; set; } = "Application";
    public string Message { get; set; } = string.Empty;
    public string? ExceptionDetails { get; set; }
    public string? Caller { get; set; }
}

public class ApplicationSettings
{
    public string Theme { get; set; } = "System"; // System, Dark, Light
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = false;
    public int MetricRefreshIntervalSeconds { get; set; } = 2;
    public bool ConfirmBeforeModification { get; set; } = true;
    public bool AutomaticBackupBeforeModification { get; set; } = true;
    public bool AutomaticVerificationAfterModification { get; set; } = true;
    public bool MaskSensitiveIdentifiersInUI { get; set; } = false;
    public int LogRetentionDays { get; set; } = 30;
    public int MaxLogSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string DataDirectory { get; set; } = @"C:\MachineIDLogger";
    public bool CheckForUpdatesOnStartup { get; set; } = true;
}

public class ExportReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string AppVersion { get; set; } = "1.0.0";
    public SystemOverview System { get; set; } = new();
    public List<IdentifierInfo> Identifiers { get; set; } = [];
    public List<MachineGuidHistoryEntry> History { get; set; } = [];
}

public class DiscoveryResult<T>
{
    public T Data { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public string StatusMessage { get; set; } = "Ready";
    public Exception? Exception { get; set; }
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    public DiscoveryResult(T data)
    {
        Data = data;
    }

    public static DiscoveryResult<T> Success(T data, string source) =>
        new(data) { Source = source, IsAvailable = true, StatusMessage = "Available" };

    public static DiscoveryResult<T> Failed(T data, string error, string source, Exception? ex = null) =>
        new(data) { Source = source, IsAvailable = false, StatusMessage = error, Exception = ex };
}
