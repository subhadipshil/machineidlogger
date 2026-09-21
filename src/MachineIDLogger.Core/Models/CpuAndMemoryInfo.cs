namespace MachineIDLogger.Core.Models;

public class CpuInfo
{
    public string Name { get; set; } = "Not available";
    public string Manufacturer { get; set; } = "Not available";
    public string Architecture { get; set; } = "Not available";
    public int PhysicalCores { get; set; }
    public int LogicalProcessors { get; set; }
    public uint BaseClockSpeedMhz { get; set; }
    public uint MaxClockSpeedMhz { get; set; }
    public uint CurrentClockSpeedMhz { get; set; }
    public int SocketCount { get; set; } = 1;
    public string SocketDesignation { get; set; } = "Not available";
    public string L2CacheSize { get; set; } = "Not available";
    public string L3CacheSize { get; set; } = "Not available";
    public string VirtualizationEnabled { get; set; } = "Not available";
    public string ProcessorId { get; set; } = "Not available";
    public string Stepping { get; set; } = "Not available";
    public double CurrentLoadPercentage { get; set; }
    public string Source { get; set; } = "Win32_Processor";
}

public class MemoryModuleInfo
{
    public string BankLabel { get; set; } = "Not available";
    public string DeviceLocator { get; set; } = "Not available";
    public ulong CapacityBytes { get; set; }
    public string FormattedCapacity => FormatBytes(CapacityBytes);
    public uint SpeedMhz { get; set; }
    public uint ConfiguredClockSpeedMhz { get; set; }
    public string Manufacturer { get; set; } = "Not available";
    public string PartNumber { get; set; } = "Not available";
    public string SerialNumber { get; set; } = "Not available";
    public string MemoryType { get; set; } = "Not available";
    public string FormFactor { get; set; } = "Not available";

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";
        double gb = (double)bytes / (1024 * 1024 * 1024);
        return $"{gb:F1} GB";
    }
}

public class MemoryInfo
{
    public ulong TotalPhysicalBytes { get; set; }
    public ulong AvailablePhysicalBytes { get; set; }
    public ulong UsedPhysicalBytes => TotalPhysicalBytes >= AvailablePhysicalBytes ? TotalPhysicalBytes - AvailablePhysicalBytes : 0;
    public double LoadPercentage { get; set; }
    public ulong TotalPageFileBytes { get; set; }
    public ulong AvailablePageFileBytes { get; set; }
    public ulong TotalVirtualBytes { get; set; }
    public ulong AvailableVirtualBytes { get; set; }

    public string FormattedTotal => FormatBytes(TotalPhysicalBytes);
    public string FormattedAvailable => FormatBytes(AvailablePhysicalBytes);
    public string FormattedUsed => FormatBytes(UsedPhysicalBytes);

    public List<MemoryModuleInfo> Modules { get; set; } = [];
    public int ModuleCount => Modules.Count;
    public string Source { get; set; } = "GlobalMemoryStatusEx / Win32_PhysicalMemory";

    private static string FormatBytes(ulong bytes)
    {
        if (bytes == 0) return "0 B";
        double gb = (double)bytes / (1024 * 1024 * 1024);
        return $"{gb:F2} GB";
    }
}
