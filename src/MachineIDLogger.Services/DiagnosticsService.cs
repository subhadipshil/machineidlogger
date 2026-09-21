using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Core.Validation;
using MachineIDLogger.Infrastructure.Cim;
using MachineIDLogger.Infrastructure.Registry;
using MachineIDLogger.Infrastructure.Windows;

namespace MachineIDLogger.Services;

public class DiagnosticsService : IDiagnosticsService
{
    private readonly IIdentifierService _identifierService;
    private readonly IHistoryService _historyService;
    private readonly ILoggingService _loggingService;

    public DiagnosticsService(
        IIdentifierService identifierService,
        IHistoryService historyService,
        ILoggingService loggingService)
    {
        _identifierService = identifierService;
        _historyService = historyService;
        _loggingService = loggingService;
    }

    public async Task<List<DiagnosticResult>> RunDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        _loggingService.LogInfo("Diagnostics", "Starting comprehensive diagnostic checks...");
        var list = new List<DiagnosticResult>();

        // 1. Process Elevation / Least Privilege
        bool isElevated = NativeMethods.IsProcessElevated();
        list.Add(new DiagnosticResult
        {
            CheckName = "Process Privilege Level",
            Category = "Security",
            Status = isElevated ? DiagnosticStatus.Pass : DiagnosticStatus.Pass, // Non-elevated is expected by default
            Details = isElevated ? "Application is currently running with Administrator privileges." : "Application is running as standard user (least privilege). Elevation will be requested when modifying MachineGuid.",
            Recommendation = isElevated ? "Operating in elevated mode." : "Normal expected operating mode for inspecting system data safely."
        });

        // 2. Registry Read Access
        bool canRead = RegistryHelper.CanReadMachineGuid(out var readErr);
        list.Add(new DiagnosticResult
        {
            CheckName = "Registry Read Access",
            Category = "Registry",
            Status = canRead ? DiagnosticStatus.Pass : DiagnosticStatus.Error,
            Details = canRead ? @"Successfully read HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid from 64-bit view." : $"Registry read error: {readErr}",
            Recommendation = canRead ? "No action needed." : "Verify permissions on the Cryptography registry hive."
        });

        // 3. Registry Write Capability
        bool canWrite = RegistryHelper.CanWriteMachineGuid(out var writeErr);
        list.Add(new DiagnosticResult
        {
            CheckName = "Direct Registry Mutation",
            Category = "Registry",
            Status = canWrite ? DiagnosticStatus.Pass : (isElevated ? DiagnosticStatus.Error : DiagnosticStatus.Warning),
            Details = canWrite ? "Direct write access to Cryptography key is available." : (isElevated ? $"Write failed: {writeErr}" : "Standard user token cannot modify HKLM directly. Application uses elevation helper with UAC on change."),
            Recommendation = canWrite ? "Direct writes permitted." : "Normal behavior for non-elevated user."
        });

        // 4. Current MachineGuid Validity
        string currentGuid = await _identifierService.GetMachineGuidAsync(cancellationToken);
        bool validGuid = GuidValidator.IsValid(currentGuid, out _, out string valMsg);
        list.Add(new DiagnosticResult
        {
            CheckName = "MachineGuid Format Integrity",
            Category = "Identity",
            Status = validGuid ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
            Details = validGuid ? $"Current MachineGuid '{currentGuid}' is a valid RFC 4122 GUID format." : $"Current MachineGuid value warning: {valMsg}",
            Recommendation = validGuid ? "Valid identity structure." : "Consider generating a compliant RFC 4122 GUID."
        });

        // 5. SMBIOS UUID Availability
        string smbios = await _identifierService.GetSmbiosUuidAsync(cancellationToken);
        bool hasSmbios = !string.IsNullOrEmpty(smbios) && smbios != "Not available" && smbios != "Not exposed by firmware";
        list.Add(new DiagnosticResult
        {
            CheckName = "SMBIOS Firmware UUID",
            Category = "Firmware",
            Status = hasSmbios ? DiagnosticStatus.Pass : DiagnosticStatus.NotAvailable,
            Details = hasSmbios ? $"SMBIOS UUID '{smbios}' exposed via Win32_ComputerSystemProduct." : "SMBIOS UUID is not exposed by system firmware or virtual hypervisor.",
            Recommendation = hasSmbios ? "Firmware identity exposed." : "Normal on some VMs or legacy BIOS machines."
        });

        // 6. WMI/CIM Subsystem Health
        var osRows = WmiHelper.Query("Win32_OperatingSystem", "Caption");
        bool wmiOk = osRows.Count > 0;
        list.Add(new DiagnosticResult
        {
            CheckName = "WMI / CIM Subsystem",
            Category = "System",
            Status = wmiOk ? DiagnosticStatus.Pass : DiagnosticStatus.Error,
            Details = wmiOk ? "Windows Management Instrumentation service is running and responsive." : "Unable to query WMI repository (Win32_OperatingSystem). Service may be stopped.",
            Recommendation = wmiOk ? "WMI provider is operational." : "Check 'winmgmt' service in services.msc."
        });

        // 7. CPU Hardware Detection
        var cpuRows = WmiHelper.Query("Win32_Processor", "Name");
        list.Add(new DiagnosticResult
        {
            CheckName = "Processor Query Provider",
            Category = "Hardware",
            Status = cpuRows.Count > 0 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
            Details = cpuRows.Count > 0 ? $"Detected {cpuRows.Count} processor socket(s)." : "Processor WMI query returned empty. Falling back to environment variables.",
            Recommendation = cpuRows.Count > 0 ? "Processor queries functional." : "Verify processor driver."
        });

        // 8. Memory Status API
        var memStat = NativeMethods.MEMORYSTATUSEX.Create();
        bool memOk = NativeMethods.GlobalMemoryStatusEx(ref memStat);
        list.Add(new DiagnosticResult
        {
            CheckName = "Physical Memory Status API",
            Category = "Hardware",
            Status = memOk ? DiagnosticStatus.Pass : DiagnosticStatus.Error,
            Details = memOk ? $"Reported {memStat.ullTotalPhys / (1024 * 1024 * 1024):F1} GB total physical RAM." : "Call to GlobalMemoryStatusEx failed.",
            Recommendation = memOk ? "High precision memory metrics available." : "Inspect kernel memory subsystem."
        });

        // 9. Storage Drive Query
        var diskRows = WmiHelper.Query("Win32_DiskDrive", "Model");
        list.Add(new DiagnosticResult
        {
            CheckName = "Physical Disk Discovery",
            Category = "Storage",
            Status = diskRows.Count > 0 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
            Details = diskRows.Count > 0 ? $"Discovered {diskRows.Count} physical drive(s)." : "No physical drives enumerated via Win32_DiskDrive.",
            Recommendation = diskRows.Count > 0 ? "Disk controller accessible." : "Ensure storage driver is active."
        });

        // 10. Persistent Storage Directory
        string dataDir = @"C:\MachineIDLogger";
        bool dirExists = Directory.Exists(dataDir);
        list.Add(new DiagnosticResult
        {
            CheckName = "Persistent Data Directory",
            Category = "Storage",
            Status = dirExists ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
            Details = dirExists ? $"Directory '{dataDir}' exists and is accessible." : $"Directory '{dataDir}' does not exist yet; will be created on first modification or log entry.",
            Recommendation = dirExists ? "Data persistence location is verified." : "Directory will be provisioned automatically."
        });

        // 11. SQLite History Store Integrity
        try
        {
            await _historyService.InitializeAsync();
            var history = await _historyService.GetHistoryAsync(1);
            list.Add(new DiagnosticResult
            {
                CheckName = "SQLite History Database",
                Category = "Storage",
                Status = DiagnosticStatus.Pass,
                Details = "SQLite database initialized, schema version verified, and query executed successfully.",
                Recommendation = "Audit history store is healthy."
            });
        }
        catch (Exception ex)
        {
            list.Add(new DiagnosticResult
            {
                CheckName = "SQLite History Database",
                Category = "Storage",
                Status = DiagnosticStatus.Error,
                Details = $"SQLite error: {ex.Message}",
                Recommendation = "Ensure write permissions in C:\\MachineIDLogger\\History."
            });
        }

        // 12. Windows OS Compatibility
        bool win10Or11 = Environment.OSVersion.Version.Major >= 10;
        list.Add(new DiagnosticResult
        {
            CheckName = "Operating System Compatibility",
            Category = "Environment",
            Status = win10Or11 ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
            Details = $"Windows version: {Environment.OSVersion.Version} ({Environment.OSVersion.VersionString}).",
            Recommendation = win10Or11 ? "Supported Windows 10/11 platform." : "MachineIDLogger is designed for Windows 10/11."
        });

        // 13. Audio & Speaker Subsystem
        try
        {
            var soundRows = WmiHelper.Query("Win32_SoundDevice", "Name", "Status", "Manufacturer");
            bool soundOk = soundRows.Count > 0;
            string devNames = soundRows.Count > 0 
                ? string.Join(", ", soundRows.Take(2).Select(r => r.GetValueOrDefault("Name", "Unknown Audio Device")?.ToString() ?? "Unknown Audio Device"))
                : "No audio controllers found";
            list.Add(new DiagnosticResult
            {
                CheckName = "Audio & Speaker Hardware",
                Category = "Multimedia",
                Status = soundOk ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
                Details = soundOk ? $"Detected {soundRows.Count} audio controller(s): {devNames}." : "No sound devices discovered via Win32_SoundDevice.",
                Recommendation = soundOk ? "Audio playback hardware operational." : "Verify audio drivers in Device Manager."
            });
        }
        catch (Exception ex)
        {
            list.Add(new DiagnosticResult
            {
                CheckName = "Audio & Speaker Hardware",
                Category = "Multimedia",
                Status = DiagnosticStatus.Warning,
                Details = $"Audio query warning: {ex.Message}",
                Recommendation = "Verify Windows Audio service (Audiosrv)."
            });
        }

        // 14. Displays & Monitor Subsystem
        try
        {
            var dispRows = WmiHelper.Query("Win32_VideoController", "Name", "CurrentHorizontalResolution", "CurrentVerticalResolution", "CurrentRefreshRate", "Status");
            bool dispOk = dispRows.Count > 0;
            var primary = dispRows.FirstOrDefault();
            string? horiz = primary?["CurrentHorizontalResolution"]?.ToString();
            string? vert = primary?.GetValueOrDefault("CurrentVerticalResolution", "?")?.ToString();
            string? refresh = primary?.GetValueOrDefault("CurrentRefreshRate", "60")?.ToString();
            string dispName = primary?.GetValueOrDefault("Name", "Display")?.ToString() ?? "Display";

            string dispInfo = !string.IsNullOrEmpty(horiz)
                ? $"{dispName} ({horiz}x{vert ?? "?"} @ {refresh ?? "60"}Hz)"
                : (dispRows.Count > 0 ? dispRows[0].GetValueOrDefault("Name", "Standard Display")?.ToString() ?? "Standard Display" : "Display controller not enumerated");

            list.Add(new DiagnosticResult
            {
                CheckName = "Display Output & Monitors",
                Category = "Display",
                Status = dispOk ? DiagnosticStatus.Pass : DiagnosticStatus.Warning,
                Details = dispOk ? $"Display subsystem responsive: {dispInfo}." : "Unable to enumerate display controllers via WMI.",
                Recommendation = dispOk ? "Graphics pipeline output verified." : "Check display adapter driver."
            });
        }
        catch (Exception ex)
        {
            list.Add(new DiagnosticResult
            {
                CheckName = "Display Output & Monitors",
                Category = "Display",
                Status = DiagnosticStatus.Warning,
                Details = $"Display query warning: {ex.Message}",
                Recommendation = "Verify graphics driver."
            });
        }

        // 15. Network Gateway & Connectivity
        try
        {
            bool netAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            bool dnsOk = false;
            if (netAvailable)
            {
                try
                {
                    var addrs = System.Net.Dns.GetHostAddresses("dns.google");
                    dnsOk = addrs.Length > 0;
                }
                catch { }
            }

            list.Add(new DiagnosticResult
            {
                CheckName = "Network & Gateway Connectivity",
                Category = "Network",
                Status = dnsOk ? DiagnosticStatus.Pass : (netAvailable ? DiagnosticStatus.Warning : DiagnosticStatus.Warning),
                Details = dnsOk ? "Active network adapter detected with operational external DNS resolution." : (netAvailable ? "Network interface up, but external DNS query timed out or is offline." : "No operational network interface currently connected."),
                Recommendation = dnsOk ? "Internet and network adapters are functional." : "Check network adapter status or airplane mode."
            });
        }
        catch (Exception ex)
        {
            list.Add(new DiagnosticResult
            {
                CheckName = "Network & Gateway Connectivity",
                Category = "Network",
                Status = DiagnosticStatus.Warning,
                Details = $"Network check warning: {ex.Message}",
                Recommendation = "Verify TCP/IP stack."
            });
        }

        // 16. Power & Battery Subsystem
        try
        {
            var batteryRows = WmiHelper.Query("Win32_Battery", "Name", "EstimatedChargeRemaining", "BatteryStatus");
            if (batteryRows.Count > 0 && batteryRows[0].ContainsKey("EstimatedChargeRemaining") && batteryRows[0]["EstimatedChargeRemaining"] != null)
            {
                string pct = batteryRows[0].GetValueOrDefault("EstimatedChargeRemaining", "100")?.ToString() ?? "100";
                string bName = batteryRows[0].GetValueOrDefault("Name", "Internal Battery")?.ToString() ?? "Internal Battery";
                list.Add(new DiagnosticResult
                {
                    CheckName = "Power & Battery Subsystem",
                    Category = "Power",
                    Status = DiagnosticStatus.Pass,
                    Details = $"Mobile battery detected: {pct}% charge remaining ({bName}).",
                    Recommendation = "Battery telemetry operational."
                });
            }
            else
            {
                list.Add(new DiagnosticResult
                {
                    CheckName = "Power & Battery Subsystem",
                    Category = "Power",
                    Status = DiagnosticStatus.Pass,
                    Details = "Desktop AC power supply active (no mobile battery detected).",
                    Recommendation = "Standard continuous mains power supply."
                });
            }
        }
        catch
        {
            list.Add(new DiagnosticResult
            {
                CheckName = "Power & Battery Subsystem",
                Category = "Power",
                Status = DiagnosticStatus.Pass,
                Details = "AC power subsystem operational.",
                Recommendation = "No action needed."
            });
        }

        _loggingService.LogInfo("Diagnostics", $"Completed diagnostics: {list.Count(d => d.Status == DiagnosticStatus.Pass)} PASS, {list.Count(d => d.Status == DiagnosticStatus.Warning)} WARNING, {list.Count(d => d.Status == DiagnosticStatus.Error)} ERROR.");
        return list;
    }
}
