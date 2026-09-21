using System.Management;
using System.Runtime.InteropServices;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Cim;
using MachineIDLogger.Infrastructure.Registry;
using MachineIDLogger.Infrastructure.Windows;
using Microsoft.Win32;

namespace MachineIDLogger.Services;

public class SystemInformationService : ISystemInformationService
{
    private readonly IProcessorService _processorService;
    private readonly IMemoryService _memoryService;
    private readonly IGraphicsService _graphicsService;
    private readonly IStorageService _storageService;
    private readonly IMotherboardService _motherboardService;
    private readonly IBiosService _biosService;
    private readonly INetworkService _networkService;
    private readonly IIdentifierService _identifierService;

    public SystemInformationService(
        IProcessorService processorService,
        IMemoryService memoryService,
        IGraphicsService graphicsService,
        IStorageService storageService,
        IMotherboardService motherboardService,
        IBiosService biosService,
        INetworkService networkService,
        IIdentifierService identifierService)
    {
        _processorService = processorService;
        _memoryService = memoryService;
        _graphicsService = graphicsService;
        _storageService = storageService;
        _motherboardService = motherboardService;
        _biosService = biosService;
        _networkService = networkService;
        _identifierService = identifierService;
    }

    public Task<OperatingSystemInfo> GetOperatingSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var os = new OperatingSystemInfo();
            var rows = WmiHelper.Query("Win32_OperatingSystem",
                "Caption", "Version", "BuildNumber", "OSArchitecture",
                "InstallDate", "LastBootUpTime", "RegisteredUser",
                "Organization", "WindowsDirectory", "SystemDirectory",
                "ProductType", "SerialNumber");

            if (rows.Count > 0)
            {
                var row = rows[0];
                os.Caption = row.GetString("Caption", Environment.OSVersion.ToString());
                os.Version = row.GetString("Version", Environment.OSVersion.Version.ToString());
                os.BuildNumber = row.GetString("BuildNumber", Environment.OSVersion.Version.Build.ToString());
                os.Architecture = row.GetString("OSArchitecture", RuntimeInformation.OSArchitecture.ToString());
                os.RegisteredUser = row.GetString("RegisteredUser", Environment.UserName);
                os.Organization = row.GetString("Organization", "Workgroup");
                os.WindowsFolder = row.GetString("WindowsDirectory", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                os.SystemDirectory = row.GetString("SystemDirectory", Environment.SystemDirectory);
                os.SerialNumber = row.GetString("SerialNumber", "Not available");

                // Format Install Date
                string rawInstall = row.GetString("InstallDate", "");
                if (rawInstall.Length >= 8)
                {
                    os.InstallDate = $"{rawInstall.Substring(0, 4)}-{rawInstall.Substring(4, 2)}-{rawInstall.Substring(6, 2)}";
                }

                // Format Last Boot & Uptime
                string rawBoot = row.GetString("LastBootUpTime", "");
                if (rawBoot.Length >= 14)
                {
                    os.LastBootUpTime = $"{rawBoot.Substring(0, 4)}-{rawBoot.Substring(4, 2)}-{rawBoot.Substring(6, 2)} {rawBoot.Substring(8, 2)}:{rawBoot.Substring(10, 2)}:{rawBoot.Substring(12, 2)}";
                }
            }
            else
            {
                os.Caption = RuntimeInformation.OSDescription;
                os.Architecture = RuntimeInformation.OSArchitecture.ToString();
                os.Version = Environment.OSVersion.Version.ToString();
                os.BuildNumber = Environment.OSVersion.Version.Build.ToString();
            }

            // Uptime via GetTickCount64
            ulong tickMs = NativeMethods.GetTickCount64();
            os.Uptime = TimeSpan.FromMilliseconds(tickMs);

            // Read UBR (Update Build Revision) from Registry
            try
            {
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    var ubr = key.GetValue("UBR");
                    if (ubr != null)
                    {
                        os.Ubr = ubr.ToString() ?? "";
                        os.BuildNumber = $"{os.BuildNumber}.{os.Ubr}";
                    }
                }
            }
            catch { }

            // Secure Boot status from Registry
            try
            {
                using var sbKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SecureBoot\State");
                if (sbKey != null)
                {
                    int? sbState = sbKey.GetValue("UEFISecureBootEnabled") as int?;
                    os.SecureBootState = sbState == 1 ? "Enabled" : "Disabled";
                }
                else
                {
                    os.SecureBootState = "Not supported / Legacy BIOS";
                }
            }
            catch
            {
                os.SecureBootState = "Not available";
            }

            // TPM Presence check
            try
            {
                using var tpmSearcher = new ManagementObjectSearcher(@"root\cimv2\security\microsofttpm", "SELECT IsActivated_InitialValue, IsEnabled_InitialValue FROM Win32_Tpm");
                using var tpmCol = tpmSearcher.Get();
                bool found = false;
                foreach (ManagementObject obj in tpmCol)
                {
                    bool activated = (bool)(obj["IsActivated_InitialValue"] ?? false);
                    bool enabled = (bool)(obj["IsEnabled_InitialValue"] ?? false);
                    os.TpmStatus = (activated && enabled) ? "TPM 2.0 Present & Enabled" : "TPM Detected";
                    found = true;
                    break;
                }
                if (!found) os.TpmStatus = "Not detected";
            }
            catch
            {
                os.TpmStatus = "Not available or restricted";
            }

            return os;
        }, cancellationToken);
    }

    public async Task<SystemOverview> GetSystemOverviewAsync(CancellationToken cancellationToken = default)
    {
        var overview = new SystemOverview();

        // 1. Basic Computer System info
        var csRows = WmiHelper.Query("Win32_ComputerSystem", "Manufacturer", "Model", "SystemFamily", "SystemSKUNumber", "SystemType");
        if (csRows.Count > 0)
        {
            var r = csRows[0];
            overview.Manufacturer = r.GetString("Manufacturer", "Standard PC Manufacturer");
            overview.Model = r.GetString("Model", "System Model");
            overview.SystemFamily = r.GetString("SystemFamily", "Desktop");
            overview.SystemSku = r.GetString("SystemSKUNumber", "Not available");
            overview.SystemType = r.GetString("SystemType", "x64-based PC");
        }

        // 2. Query subsystems in parallel
        var osTask = GetOperatingSystemInfoAsync(cancellationToken);
        var cpuTask = _processorService.GetCpuInfoAsync(cancellationToken);
        var memTask = _memoryService.GetMemoryInfoAsync(cancellationToken);
        var gpuTask = _graphicsService.GetGpusAsync(cancellationToken);
        var diskTask = _storageService.GetPhysicalDrivesAsync(cancellationToken);
        var volTask = _storageService.GetVolumesAsync(cancellationToken);
        var netTask = _networkService.GetAdaptersAsync(cancellationToken);
        var moboTask = _motherboardService.GetMotherboardInfoAsync(cancellationToken);
        var biosTask = _biosService.GetBiosInfoAsync(cancellationToken);
        var guidTask = _identifierService.GetMachineGuidAsync(cancellationToken);
        var smbiosTask = _identifierService.GetSmbiosUuidAsync(cancellationToken);

        await Task.WhenAll(osTask, cpuTask, memTask, gpuTask, diskTask, volTask, netTask, moboTask, biosTask, guidTask, smbiosTask);

        overview.OperatingSystem = await osTask;
        overview.Cpu = await cpuTask;
        overview.Memory = await memTask;
        overview.Gpus = await gpuTask;
        overview.PhysicalDrives = await diskTask;
        overview.Volumes = await volTask;
        overview.NetworkAdapters = await netTask;
        overview.Motherboard = await moboTask;
        overview.Bios = await biosTask;
        overview.MachineGuid = await guidTask;
        overview.SmbiosUuid = await smbiosTask;
        overview.BiosSerial = overview.Bios.SerialNumber;
        overview.MotherboardSerial = overview.Motherboard.SerialNumber;

        if (overview.PhysicalDrives.Count > 0)
        {
            overview.PrimaryDiskSerial = overview.PhysicalDrives[0].SerialNumber;
        }

        var activeNic = overview.NetworkAdapters.FirstOrDefault(n => n.OperationalStatus.Equals("Up", StringComparison.OrdinalIgnoreCase) && n.IsPhysical);
        if (activeNic != null)
        {
            overview.PrimaryMacAddress = activeNic.MacAddress;
        }
        else if (overview.NetworkAdapters.Count > 0)
        {
            overview.PrimaryMacAddress = overview.NetworkAdapters[0].MacAddress;
        }

        overview.DiscoveredAt = DateTime.UtcNow;
        return overview;
    }
}

public class DeviceDiscoveryService : IDeviceDiscoveryService
{
    private readonly ISystemInformationService _systemInfoService;
    private readonly ILoggingService _loggingService;

    public bool IsScanning { get; private set; }
    public string CurrentScanStep { get; private set; } = "Ready";
    public event EventHandler<string>? ScanStepChanged;
    public event EventHandler<bool>? ScanStateChanged;
    public SystemOverview CachedOverview { get; private set; } = new();

    public DeviceDiscoveryService(ISystemInformationService systemInfoService, ILoggingService loggingService)
    {
        _systemInfoService = systemInfoService;
        _loggingService = loggingService;
    }

    public async Task RunFullDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        if (IsScanning) return;

        IsScanning = true;
        ScanStateChanged?.Invoke(this, true);
        _loggingService.LogInfo("Discovery", "Starting full device and system discovery scan...");

        try
        {
            UpdateStep("Scanning operating system and platform...");
            var overview = await _systemInfoService.GetSystemOverviewAsync(cancellationToken);
            CachedOverview = overview;

            UpdateStep("System discovery complete");
            _loggingService.LogInfo("Discovery", $"Discovery completed successfully for machine: {overview.ComputerName} ({overview.Manufacturer} {overview.Model})");
        }
        catch (Exception ex)
        {
            UpdateStep("Discovery encountered warnings");
            _loggingService.LogError("Discovery", "Error during full discovery scan", ex.ToString());
        }
        finally
        {
            IsScanning = false;
            ScanStateChanged?.Invoke(this, false);
        }
    }

    private void UpdateStep(string step)
    {
        CurrentScanStep = step;
        ScanStepChanged?.Invoke(this, step);
    }
}
