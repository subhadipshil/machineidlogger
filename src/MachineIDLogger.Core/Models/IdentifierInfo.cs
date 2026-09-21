namespace MachineIDLogger.Core.Models;

public enum IdentifierStatus
{
    Supported,
    ReadOnly,
    NotAvailable,
    PermissionDenied,
    FirmwareRestricted,
    Unsupported,
    Error
}

public enum IdentifierCategory
{
    OperatingSystem,
    Firmware,
    Hardware,
    Storage,
    Network,
    Application
}

public class IdentifierInfo
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public IdentifierCategory Category { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsEditable { get; set; }
    public bool IsAvailable { get; set; } = true;
    public IdentifierStatus Status { get; set; } = IdentifierStatus.Supported;
    public string Description { get; set; } = string.Empty;
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public string FormattedStatus => Status switch
    {
        IdentifierStatus.Supported => IsEditable ? "Editable" : "Read-only",
        IdentifierStatus.ReadOnly => "Read-only",
        IdentifierStatus.NotAvailable => "Not available",
        IdentifierStatus.PermissionDenied => "Permission denied",
        IdentifierStatus.FirmwareRestricted => "Not exposed by firmware",
        IdentifierStatus.Unsupported => "Unsupported on this system",
        IdentifierStatus.Error => "Query error",
        _ => Status.ToString()
    };
}
