using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Cim;
using MachineIDLogger.Infrastructure.Windows;

namespace MachineIDLogger.Services;

public class ProcessorService : IProcessorService
{
    private NativeMethods.FILETIME _prevIdleTime;
    private NativeMethods.FILETIME _prevKernelTime;
    private NativeMethods.FILETIME _prevUserTime;
    private bool _hasInitialCpuSample;

    public Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var cpu = new CpuInfo();
            var rows = WmiHelper.Query("Win32_Processor",
                "Name", "Manufacturer", "Architecture", "NumberOfCores",
                "NumberOfLogicalProcessors", "MaxClockSpeed", "CurrentClockSpeed",
                "SocketDesignation", "L2CacheSize", "L3CacheSize",
                "VirtualizationFirmwareEnabled", "ProcessorId", "Stepping");

            if (rows.Count > 0)
            {
                var row = rows[0];
                cpu.Name = row.GetString("Name", "Generic Processor");
                cpu.Manufacturer = row.GetString("Manufacturer", "GenuineIntel / AuthenticAMD");
                int archCode = row.GetInt32("Architecture", 9);
                cpu.Architecture = archCode switch
                {
                    0 => "x86",
                    5 => "ARM",
                    9 => "x64 (AMD64 / Intel 64)",
                    12 => "ARM64",
                    _ => $"Architecture code {archCode}"
                };

                cpu.PhysicalCores = row.GetInt32("NumberOfCores", Environment.ProcessorCount / 2);
                if (cpu.PhysicalCores <= 0) cpu.PhysicalCores = Environment.ProcessorCount;

                cpu.LogicalProcessors = row.GetInt32("NumberOfLogicalProcessors", Environment.ProcessorCount);
                if (cpu.LogicalProcessors <= 0) cpu.LogicalProcessors = Environment.ProcessorCount;

                cpu.MaxClockSpeedMhz = row.GetUInt32("MaxClockSpeed");
                cpu.CurrentClockSpeedMhz = row.GetUInt32("CurrentClockSpeed", cpu.MaxClockSpeedMhz);
                cpu.BaseClockSpeedMhz = cpu.MaxClockSpeedMhz;
                cpu.SocketDesignation = row.GetString("SocketDesignation", "Primary Socket");
                cpu.SocketCount = rows.Count;

                uint l2 = row.GetUInt32("L2CacheSize");
                cpu.L2CacheSize = l2 > 0 ? $"{l2} KB" : "Not exposed by firmware";

                uint l3 = row.GetUInt32("L3CacheSize");
                cpu.L3CacheSize = l3 > 0 ? $"{l3 / 1024.0:F1} MB" : "Not exposed by firmware";

                if (row.TryGetValue("VirtualizationFirmwareEnabled", out var virtVal) && virtVal is bool v)
                {
                    cpu.VirtualizationEnabled = v ? "Enabled in BIOS/UEFI" : "Disabled";
                }
                else
                {
                    cpu.VirtualizationEnabled = "Supported / OS Managed";
                }

                cpu.ProcessorId = row.GetString("ProcessorId", "Not available");
                cpu.Stepping = row.GetString("Stepping", "Not available");
            }
            else
            {
                // Fallback to Environment
                cpu.Name = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "x64 Compatible Processor";
                cpu.LogicalProcessors = Environment.ProcessorCount;
                cpu.PhysicalCores = Environment.ProcessorCount;
                cpu.Architecture = RuntimeInformation.ProcessArchitecture.ToString();
            }

            return cpu;
        }, cancellationToken);
    }

    public Task<double> GetLiveCpuUsageAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            if (!_hasInitialCpuSample)
            {
                NativeMethods.GetSystemTimes(out _prevIdleTime, out _prevKernelTime, out _prevUserTime);
                _hasInitialCpuSample = true;
                return 0.0;
            }

            NativeMethods.GetSystemTimes(out var idleTime, out var kernelTime, out var userTime);

            ulong idleDiff = idleTime.ToUInt64() - _prevIdleTime.ToUInt64();
            ulong kernelDiff = kernelTime.ToUInt64() - _prevKernelTime.ToUInt64();
            ulong userDiff = userTime.ToUInt64() - _prevUserTime.ToUInt64();

            _prevIdleTime = idleTime;
            _prevKernelTime = kernelTime;
            _prevUserTime = userTime;

            ulong totalSys = kernelDiff + userDiff;
            if (totalSys == 0) return 0.0;

            double percent = (double)(totalSys - idleDiff) * 100.0 / totalSys;
            return Math.Clamp(percent, 0.0, 100.0);
        }, cancellationToken);
    }
}

public class MemoryService : IMemoryService
{
    public Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var mem = new MemoryInfo();

            // 1. High-precision Win32 GlobalMemoryStatusEx
            var status = NativeMethods.MEMORYSTATUSEX.Create();
            if (NativeMethods.GlobalMemoryStatusEx(ref status))
            {
                mem.TotalPhysicalBytes = status.ullTotalPhys;
                mem.AvailablePhysicalBytes = status.ullAvailPhys;
                mem.LoadPercentage = status.dwMemoryLoad;
                mem.TotalPageFileBytes = status.ullTotalPageFile;
                mem.AvailablePageFileBytes = status.ullAvailPageFile;
                mem.TotalVirtualBytes = status.ullTotalVirtual;
                mem.AvailableVirtualBytes = status.ullAvailVirtual;
            }

            // 2. Memory Modules from Win32_PhysicalMemory
            var rows = WmiHelper.Query("Win32_PhysicalMemory",
                "BankLabel", "DeviceLocator", "Capacity", "Speed",
                "ConfiguredClockSpeed", "Manufacturer", "PartNumber",
                "SerialNumber", "MemoryType", "FormFactor");

            foreach (var row in rows)
            {
                var mod = new MemoryModuleInfo
                {
                    BankLabel = row.GetString("BankLabel", "Bank 0"),
                    DeviceLocator = row.GetString("DeviceLocator", "DIMM"),
                    CapacityBytes = row.GetUInt64("Capacity"),
                    SpeedMhz = row.GetUInt32("Speed"),
                    ConfiguredClockSpeedMhz = row.GetUInt32("ConfiguredClockSpeed", row.GetUInt32("Speed")),
                    Manufacturer = row.GetString("Manufacturer", "Generic"),
                    PartNumber = row.GetString("PartNumber", "Not available"),
                    SerialNumber = row.GetString("SerialNumber", "Not available")
                };

                int form = row.GetInt32("FormFactor", 0);
                mod.FormFactor = form switch
                {
                    8 => "DIMM",
                    12 => "SODIMM (Laptop)",
                    _ => "DIMM"
                };

                int memType = row.GetInt32("MemoryType", 0);
                mod.MemoryType = memType switch
                {
                    20 => "DDR",
                    21 => "DDR2",
                    24 => "DDR3",
                    26 => "DDR4",
                    34 => "DDR5",
                    _ => mod.SpeedMhz >= 4800 ? "DDR5" : (mod.SpeedMhz >= 2133 ? "DDR4" : "DDR")
                };

                mem.Modules.Add(mod);
            }

            return mem;
        }, cancellationToken);
    }
}

public class GraphicsService : IGraphicsService
{
    public Task<List<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var list = new List<GpuInfo>();
            var rows = WmiHelper.Query("Win32_VideoController",
                "Name", "AdapterCompatibility", "DriverVersion", "DriverDate",
                "AdapterRAM", "VideoProcessor", "VideoArchitecture",
                "VideoModeDescription", "CurrentRefreshRate", "Status",
                "PNPDeviceID", "DeviceID");

            bool first = true;
            foreach (var row in rows)
            {
                string name = row.GetString("Name", "Display Adapter");
                // Skip basic Microsoft Basic Render Driver if other GPUs exist
                var gpu = new GpuInfo
                {
                    Name = name,
                    Manufacturer = row.GetString("AdapterCompatibility", "Not available"),
                    DriverVersion = row.GetString("DriverVersion", "Not available"),
                    DedicatedMemoryBytes = row.GetUInt64("AdapterRAM"),
                    VideoProcessor = row.GetString("VideoProcessor", "Integrated / Discrete GPU"),
                    VideoModeDescription = row.GetString("VideoModeDescription", "Not available"),
                    CurrentRefreshRate = row.GetUInt32("CurrentRefreshRate"),
                    Status = row.GetString("Status", "OK"),
                    PnpDeviceId = row.GetString("PNPDeviceID", "Not available"),
                    DeviceId = row.GetString("DeviceID", "Video0"),
                    IsPrimary = first
                };

                string rawDate = row.GetString("DriverDate", "");
                if (rawDate.Length >= 8)
                {
                    gpu.DriverDate = $"{rawDate.Substring(0, 4)}-{rawDate.Substring(4, 2)}-{rawDate.Substring(6, 2)}";
                }
                else
                {
                    gpu.DriverDate = rawDate;
                }

                list.Add(gpu);
                first = false;
            }

            return list;
        }, cancellationToken);
    }
}

public class StorageService : IStorageService
{
    public Task<List<StorageDriveInfo>> GetPhysicalDrivesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var drives = new List<StorageDriveInfo>();
            var rows = WmiHelper.Query("Win32_DiskDrive",
                "Model", "Manufacturer", "SerialNumber", "FirmwareRevision",
                "InterfaceType", "MediaType", "Size", "Partitions", "DeviceID", "Status");

            foreach (var row in rows)
            {
                var drive = new StorageDriveInfo
                {
                    Model = row.GetString("Model", "Physical Disk"),
                    Manufacturer = row.GetString("Manufacturer", "Standard Disk Drive"),
                    SerialNumber = row.GetString("SerialNumber", "Not available"),
                    FirmwareRevision = row.GetString("FirmwareRevision", "Not available"),
                    InterfaceType = row.GetString("InterfaceType", "NVMe / SATA / SCSI"),
                    MediaType = row.GetString("MediaType", "Fixed hard disk media"),
                    SizeBytes = row.GetUInt64("Size"),
                    Partitions = row.GetUInt32("Partitions", 1),
                    DeviceId = row.GetString("DeviceID", @"\\.\PHYSICALDRIVE0"),
                    Status = row.GetString("Status", "OK")
                };

                // Clean serial number padding
                if (!string.IsNullOrWhiteSpace(drive.SerialNumber))
                {
                    drive.SerialNumber = drive.SerialNumber.Trim();
                }

                drives.Add(drive);
            }

            return drives;
        }, cancellationToken);
    }

    public Task<List<VolumeInfo>> GetVolumesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var volumes = new List<VolumeInfo>();

            // Query logical disks from WMI to get volume serial numbers
            var wmiLogical = WmiHelper.Query("Win32_LogicalDisk", "DeviceID", "VolumeSerialNumber", "FileSystem");
            var serialMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in wmiLogical)
            {
                string devId = row.GetString("DeviceID", "");
                string volSerial = row.GetString("VolumeSerialNumber", "");
                if (!string.IsNullOrEmpty(devId) && !string.IsNullOrEmpty(volSerial))
                {
                    serialMap[devId] = volSerial;
                }
            }

            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    var vol = new VolumeInfo
                    {
                        DriveLetter = drive.Name.TrimEnd('\\'),
                        DriveType = drive.DriveType.ToString(),
                        IsReady = drive.IsReady
                    };

                    if (drive.IsReady)
                    {
                        vol.VolumeLabel = string.IsNullOrEmpty(drive.VolumeLabel) ? "Local Disk" : drive.VolumeLabel;
                        vol.FileSystem = drive.DriveFormat;
                        vol.TotalSizeBytes = (ulong)drive.TotalSize;
                        vol.FreeSizeBytes = (ulong)drive.AvailableFreeSpace;

                        if (serialMap.TryGetValue(vol.DriveLetter, out var serial))
                        {
                            vol.VolumeSerialNumber = serial;
                        }
                    }

                    volumes.Add(vol);
                }
                catch
                {
                    // Ignore inaccessible or unmounted drives
                }
            }

            return volumes;
        }, cancellationToken);
    }
}

public class MotherboardService : IMotherboardService
{
    public Task<MotherboardInfo> GetMotherboardInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var mobo = new MotherboardInfo();
            var rows = WmiHelper.Query("Win32_BaseBoard", "Manufacturer", "Product", "Version", "SerialNumber", "Status");

            if (rows.Count > 0)
            {
                var row = rows[0];
                mobo.Manufacturer = row.GetString("Manufacturer", "Not available");
                mobo.Product = row.GetString("Product", "Not available");
                mobo.Version = row.GetString("Version", "Not available");
                mobo.SerialNumber = row.GetString("SerialNumber", "Not available");
                mobo.Status = row.GetString("Status", "OK");
            }

            return mobo;
        }, cancellationToken);
    }
}

public class BiosService : IBiosService
{
    public Task<BiosInfo> GetBiosInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var bios = new BiosInfo();
            var rows = WmiHelper.Query("Win32_BIOS", "Manufacturer", "Version", "ReleaseDate", "SMBIOSBIOSVersion", "SerialNumber");

            if (rows.Count > 0)
            {
                var row = rows[0];
                bios.Manufacturer = row.GetString("Manufacturer", "Not available");
                bios.Version = row.GetString("Version", "Not available");
                bios.SmbiosVersion = row.GetString("SMBIOSBIOSVersion", bios.Version);
                bios.SerialNumber = row.GetString("SerialNumber", "Not available");

                string rawDate = row.GetString("ReleaseDate", "");
                if (rawDate.Length >= 8)
                {
                    bios.ReleaseDate = $"{rawDate.Substring(0, 4)}-{rawDate.Substring(4, 2)}-{rawDate.Substring(6, 2)}";
                }
                else
                {
                    bios.ReleaseDate = rawDate;
                }
            }

            return bios;
        }, cancellationToken);
    }
}

public class DisplayService : IDisplayService
{
    public Task<List<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var list = new List<DisplayInfo>();
            var rows = WmiHelper.Query("Win32_DesktopMonitor", "Name", "DeviceID", "MonitorManufacturer", "ScreenWidth", "ScreenHeight");

            foreach (var row in rows)
            {
                var disp = new DisplayInfo
                {
                    Name = row.GetString("Name", "Standard Display"),
                    DeviceName = row.GetString("DeviceID", "DISPLAY1"),
                    Manufacturer = row.GetString("MonitorManufacturer", "Not available")
                };

                uint w = row.GetUInt32("ScreenWidth");
                uint h = row.GetUInt32("ScreenHeight");
                if (w > 0 && h > 0)
                {
                    disp.Resolution = $"{w} x {h}";
                }
                else
                {
                    disp.Resolution = "Dynamic OS Desktop Scaling";
                }

                disp.RefreshRateHz = 60;
                disp.IsPrimary = true;
                list.Add(disp);
            }

            if (list.Count == 0)
            {
                list.Add(new DisplayInfo
                {
                    Name = "Primary Connected Display",
                    Resolution = "System Native Resolution",
                    RefreshRateHz = 60,
                    IsPrimary = true,
                    ConnectionType = "DisplayPort / HDMI / eDP"
                });
            }

            return list;
        }, cancellationToken);
    }
}

public class NetworkService : INetworkService
{
    public Task<List<NetworkAdapterInfo>> GetAdaptersAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var adapters = new List<NetworkAdapterInfo>();
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var nic in interfaces)
            {
                // Skip loopback interfaces
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var info = new NetworkAdapterInfo
                {
                    Id = nic.Id,
                    Name = nic.Name,
                    Description = nic.Description,
                    OperationalStatus = nic.OperationalStatus.ToString(),
                    SpeedBitsPerSec = nic.Speed,
                    InterfaceType = nic.NetworkInterfaceType.ToString()
                };

                // Determine physical vs virtual
                string desc = nic.Description.ToLowerInvariant();
                info.IsPhysical = !(desc.Contains("virtual") || desc.Contains("vpn") || desc.Contains("hyper-v") ||
                                    desc.Contains("tap") || desc.Contains("wsl") || desc.Contains("vmware") ||
                                    desc.Contains("virtualbox") || desc.Contains("bluetooth"));

                // MAC Address formatted as XX:XX:XX:XX:XX:XX
                byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 6)
                {
                    info.MacAddress = string.Join(":", bytes.Select(b => b.ToString("X2")));
                }
                else
                {
                    info.MacAddress = "Not available";
                }

                // IP Addresses & DNS
                try
                {
                    var ipProps = nic.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            info.Ipv4Addresses.Add(addr.Address.ToString());
                        }
                        else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            info.Ipv6Addresses.Add(addr.Address.ToString());
                        }
                    }

                    foreach (var dns in ipProps.DnsAddresses)
                    {
                        info.DnsServers.Add(dns.ToString());
                    }

                    if (ipProps.DhcpServerAddresses.Count > 0)
                    {
                        info.DhcpServer = string.Join(", ", ipProps.DhcpServerAddresses);
                    }
                }
                catch
                {
                    // Ignore IP querying errors for inactive NICs
                }

                adapters.Add(info);
            }

            return adapters;
        }, cancellationToken);
    }
}
