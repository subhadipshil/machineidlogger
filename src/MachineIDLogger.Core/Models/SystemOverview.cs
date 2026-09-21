namespace MachineIDLogger.Core.Models;

public class OperatingSystemInfo
{
    public string Caption { get; set; } = "Not available";
    public string Version { get; set; } = "Not available";
    public string BuildNumber { get; set; } = "Not available";
    public string Ubr { get; set; } = string.Empty;
    public string Architecture { get; set; } = "Not available";
    public string InstallDate { get; set; } = "Not available";
    public string LastBootUpTime { get; set; } = "Not available";
    public TimeSpan Uptime { get; set; } = TimeSpan.Zero;
    public string FormattedUptime => $"{Uptime.Days}d {Uptime.Hours}h {Uptime.Minutes}m {Uptime.Seconds}s";
    public string RegisteredUser { get; set; } = "Not available";
    public string Organization { get; set; } = "Not available";
    public string WindowsFolder { get; set; } = "Not available";
    public string SystemDirectory { get; set; } = "Not available";
    public string ProductType { get; set; } = "Not available";
    public string SerialNumber { get; set; } = "Not available";
    public string SecureBootState { get; set; } = "Not available";
    public string TpmStatus { get; set; } = "Not available";
    public string Source { get; set; } = "Win32_OperatingSystem";
}

public class SystemOverview
{
    public string ComputerName { get; set; } = Environment.MachineName;
    public string CurrentUser { get; set; } = Environment.UserName;
    public string DomainOrWorkgroup { get; set; } = Environment.UserDomainName;
    public string Manufacturer { get; set; } = "Not available";
    public string Model { get; set; } = "Not available";
    public string SystemFamily { get; set; } = "Not available";
    public string SystemSku { get; set; } = "Not available";
    public string SystemType { get; set; } = "Not available";
    public string TimeZone { get; set; } = TimeZoneInfo.Local.DisplayName;
    public string Locale { get; set; } = System.Globalization.CultureInfo.CurrentCulture.DisplayName;

    public OperatingSystemInfo OperatingSystem { get; set; } = new();
    public CpuInfo Cpu { get; set; } = new();
    public MemoryInfo Memory { get; set; } = new();
    public List<GpuInfo> Gpus { get; set; } = [];
    public List<StorageDriveInfo> PhysicalDrives { get; set; } = [];
    public List<VolumeInfo> Volumes { get; set; } = [];
    public List<NetworkAdapterInfo> NetworkAdapters { get; set; } = [];
    public MotherboardInfo Motherboard { get; set; } = new();
    public BiosInfo Bios { get; set; } = new();

    // Key Identifiers for quick scanning on Overview
    public string MachineGuid { get; set; } = "Not available";
    public string SmbiosUuid { get; set; } = "Not available";
    public string BiosSerial { get; set; } = "Not available";
    public string MotherboardSerial { get; set; } = "Not available";
    public string PrimaryDiskSerial { get; set; } = "Not available";
    public string PrimaryMacAddress { get; set; } = "Not available";

    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
}
