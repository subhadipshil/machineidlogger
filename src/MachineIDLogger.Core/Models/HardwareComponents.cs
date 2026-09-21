namespace MachineIDLogger.Core.Models;

public class GpuInfo
{
    public string Name { get; set; } = "Not available";
    public string Manufacturer { get; set; } = "Not available";
    public string DriverVersion { get; set; } = "Not available";
    public string DriverDate { get; set; } = "Not available";
    public ulong DedicatedMemoryBytes { get; set; }
    public string FormattedDedicatedMemory => DedicatedMemoryBytes > 0 ? $"{(double)DedicatedMemoryBytes / (1024 * 1024 * 1024):F2} GB" : "Shared / Dynamic";
    public string VideoProcessor { get; set; } = "Not available";
    public string VideoArchitecture { get; set; } = "Not available";
    public string VideoModeDescription { get; set; } = "Not available";
    public uint CurrentRefreshRate { get; set; }
    public string Status { get; set; } = "OK";
    public string PnpDeviceId { get; set; } = "Not available";
    public string DeviceId { get; set; } = "Not available";
    public bool IsPrimary { get; set; }
    public string Source { get; set; } = "Win32_VideoController";
}

public class StorageDriveInfo
{
    public string Model { get; set; } = "Not available";
    public string Manufacturer { get; set; } = "Not available";
    public string SerialNumber { get; set; } = "Not available";
    public string FirmwareRevision { get; set; } = "Not available";
    public string InterfaceType { get; set; } = "Not available";
    public string MediaType { get; set; } = "Not available";
    public ulong SizeBytes { get; set; }
    public string FormattedSize => SizeBytes > 0 ? $"{(double)SizeBytes / (1000 * 1000 * 1000):F1} GB" : "Unknown";
    public uint Partitions { get; set; }
    public string DeviceId { get; set; } = "Not available";
    public string Status { get; set; } = "OK";
    public string Source { get; set; } = "Win32_DiskDrive";
}

public class VolumeInfo
{
    public string DriveLetter { get; set; } = string.Empty;
    public string VolumeLabel { get; set; } = string.Empty;
    public string FileSystem { get; set; } = "NTFS";
    public string DriveType { get; set; } = "Fixed";
    public ulong TotalSizeBytes { get; set; }
    public ulong FreeSizeBytes { get; set; }
    public ulong UsedSizeBytes => TotalSizeBytes >= FreeSizeBytes ? TotalSizeBytes - FreeSizeBytes : 0;
    public double PercentUsed => TotalSizeBytes > 0 ? ((double)UsedSizeBytes / TotalSizeBytes) * 100.0 : 0;
    public string VolumeSerialNumber { get; set; } = "Not available";
    public bool IsReady { get; set; } = true;

    public string FormattedTotal => TotalSizeBytes > 0 ? $"{(double)TotalSizeBytes / (1024 * 1024 * 1024):F1} GB" : "0 GB";
    public string FormattedFree => FreeSizeBytes > 0 ? $"{(double)FreeSizeBytes / (1024 * 1024 * 1024):F1} GB" : "0 GB";
    public string FormattedUsed => UsedSizeBytes > 0 ? $"{(double)UsedSizeBytes / (1024 * 1024 * 1024):F1} GB" : "0 GB";
    public string Source { get; set; } = "System.IO.DriveInfo / Win32_LogicalDisk";
}

public class MotherboardInfo
{
    public string Manufacturer { get; set; } = "Not available";
    public string Product { get; set; } = "Not available";
    public string Version { get; set; } = "Not available";
    public string SerialNumber { get; set; } = "Not available";
    public string Status { get; set; } = "OK";
    public string Source { get; set; } = "Win32_BaseBoard";
}

public class BiosInfo
{
    public string Manufacturer { get; set; } = "Not available";
    public string Version { get; set; } = "Not available";
    public string ReleaseDate { get; set; } = "Not available";
    public string SmbiosVersion { get; set; } = "Not available";
    public string SerialNumber { get; set; } = "Not available";
    public string BiosMode { get; set; } = "UEFI";
    public string Characteristics { get; set; } = "Not available";
    public string Source { get; set; } = "Win32_BIOS";
}

public class DisplayInfo
{
    public string Name { get; set; } = "Generic Monitor";
    public string DeviceName { get; set; } = string.Empty;
    public string Resolution { get; set; } = "Not available";
    public uint RefreshRateHz { get; set; }
    public bool IsPrimary { get; set; }
    public string HdrSupport { get; set; } = "Not available";
    public string ConnectionType { get; set; } = "Internal / DisplayPort / HDMI";
    public string Manufacturer { get; set; } = "Not available";
    public string Source { get; set; } = "Win32_DesktopMonitor / DXGI";
}

public class NetworkAdapterInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Not available";
    public string Description { get; set; } = "Not available";
    public string MacAddress { get; set; } = "Not available";
    public string InterfaceType { get; set; } = "Ethernet";
    public string OperationalStatus { get; set; } = "Down";
    public bool IsPhysical { get; set; } = true;
    public long SpeedBitsPerSec { get; set; }
    public string FormattedSpeed => SpeedBitsPerSec > 0 ? $"{(double)SpeedBitsPerSec / 1_000_000:F0} Mbps" : "Not connected";
    public List<string> Ipv4Addresses { get; set; } = [];
    public List<string> Ipv6Addresses { get; set; } = [];
    public List<string> DnsServers { get; set; } = [];
    public string DhcpServer { get; set; } = "Not available";
    public bool DhcpEnabled { get; set; } = true;
    public string Source { get; set; } = "System.Net.NetworkInformation";
}
