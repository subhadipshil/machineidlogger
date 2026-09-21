using System.Net.NetworkInformation;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Cim;
using MachineIDLogger.Infrastructure.Registry;

namespace MachineIDLogger.Services;

public class IdentifierService : IIdentifierService
{
    public Task<string> GetMachineGuidAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RegistryHelper.ReadMachineGuid(), cancellationToken);
    }

    public Task<string> GetSmbiosUuidAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var rows = WmiHelper.Query("Win32_ComputerSystemProduct", "UUID");
            if (rows.Count > 0)
            {
                string uuid = rows[0].GetString("UUID", "Not available");
                if (!string.IsNullOrWhiteSpace(uuid) && !uuid.Equals("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF", StringComparison.OrdinalIgnoreCase))
                {
                    return uuid;
                }
            }
            return "Not exposed by firmware";
        }, cancellationToken);
    }

    public Task<List<IdentifierInfo>> GetIdentifiersAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var list = new List<IdentifierInfo>();

            // 1. Windows MachineGuid (The main editable identifier)
            string currentGuid = RegistryHelper.ReadMachineGuid();
            list.Add(new IdentifierInfo
            {
                Name = "Windows MachineGuid",
                Value = currentGuid,
                Category = IdentifierCategory.OperatingSystem,
                Source = @"HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid",
                IsEditable = true,
                IsAvailable = !string.IsNullOrEmpty(currentGuid) && currentGuid != "Not available",
                Status = IdentifierStatus.Supported,
                Description = "Primary unique Windows OS installation identifier stored in the 64-bit cryptography registry hive.",
                StatusMessage = "Fully managed and verified by MachineIDLogger"
            });

            // 2. SMBIOS System UUID
            string smbiosUuid = "Not available";
            var sysProd = WmiHelper.Query("Win32_ComputerSystemProduct", "UUID");
            if (sysProd.Count > 0)
            {
                smbiosUuid = sysProd[0].GetString("UUID", "Not available");
            }
            list.Add(new IdentifierInfo
            {
                Name = "SMBIOS System UUID",
                Value = smbiosUuid,
                Category = IdentifierCategory.Firmware,
                Source = "Win32_ComputerSystemProduct:UUID",
                IsEditable = false,
                IsAvailable = !string.IsNullOrEmpty(smbiosUuid) && smbiosUuid != "Not available",
                Status = smbiosUuid == "Not available" ? IdentifierStatus.FirmwareRestricted : IdentifierStatus.ReadOnly,
                Description = "Universal Unique Identifier flashed into system BIOS/UEFI SMBIOS tables by hardware manufacturer.",
                StatusMessage = "Immutable firmware identifier. Cannot be modified via registry."
            });

            // 3. BIOS Serial Number
            string biosSerial = "Not available";
            var biosRows = WmiHelper.Query("Win32_BIOS", "SerialNumber");
            if (biosRows.Count > 0)
            {
                biosSerial = biosRows[0].GetString("SerialNumber", "Not available");
            }
            list.Add(new IdentifierInfo
            {
                Name = "BIOS Serial Number",
                Value = biosSerial,
                Category = IdentifierCategory.Firmware,
                Source = "Win32_BIOS:SerialNumber",
                IsEditable = false,
                IsAvailable = !string.IsNullOrEmpty(biosSerial) && biosSerial != "Not available",
                Status = biosSerial == "Not available" ? IdentifierStatus.FirmwareRestricted : IdentifierStatus.ReadOnly,
                Description = "Motherboard BIOS/UEFI serial number assigned during factory assembly.",
                StatusMessage = "Hardware/firmware locked. Read-only."
            });

            // 4. Baseboard / Motherboard Serial Number
            string moboSerial = "Not available";
            var moboRows = WmiHelper.Query("Win32_BaseBoard", "SerialNumber");
            if (moboRows.Count > 0)
            {
                moboSerial = moboRows[0].GetString("SerialNumber", "Not available");
            }
            list.Add(new IdentifierInfo
            {
                Name = "Baseboard Serial Number",
                Value = moboSerial,
                Category = IdentifierCategory.Hardware,
                Source = "Win32_BaseBoard:SerialNumber",
                IsEditable = false,
                IsAvailable = !string.IsNullOrEmpty(moboSerial) && moboSerial != "Not available",
                Status = moboSerial == "Not available" ? IdentifierStatus.FirmwareRestricted : IdentifierStatus.ReadOnly,
                Description = "Printed circuit board (PCB) hardware serial number of the computer motherboard.",
                StatusMessage = "Physical hardware component identifier. Read-only."
            });

            // 5. Physical Disk Serial
            string diskSerial = "Not available";
            var diskRows = WmiHelper.Query("Win32_DiskDrive", "SerialNumber", "Model");
            if (diskRows.Count > 0)
            {
                diskSerial = diskRows[0].GetString("SerialNumber", "Not available").Trim();
            }
            list.Add(new IdentifierInfo
            {
                Name = "Primary Disk Serial",
                Value = diskSerial,
                Category = IdentifierCategory.Storage,
                Source = "Win32_DiskDrive:SerialNumber",
                IsEditable = false,
                IsAvailable = !string.IsNullOrEmpty(diskSerial) && diskSerial != "Not available",
                Status = diskSerial == "Not available" ? IdentifierStatus.NotAvailable : IdentifierStatus.ReadOnly,
                Description = "Factory hardware serial number programmed into the primary physical drive's controller firmware.",
                StatusMessage = "Drive controller hardware identifier. Read-only."
            });

            // 6. Volume Serial Number
            string volSerial = "Not available";
            var volRows = WmiHelper.Query("Win32_LogicalDisk", "DeviceID", "VolumeSerialNumber");
            foreach (var r in volRows)
            {
                string dev = r.GetString("DeviceID", "");
                if (dev.Equals("C:", StringComparison.OrdinalIgnoreCase))
                {
                    volSerial = r.GetString("VolumeSerialNumber", "Not available");
                    break;
                }
            }
            list.Add(new IdentifierInfo
            {
                Name = "System Volume Serial (C:)",
                Value = volSerial,
                Category = IdentifierCategory.Storage,
                Source = "Win32_LogicalDisk:VolumeSerialNumber",
                IsEditable = false,
                IsAvailable = !string.IsNullOrEmpty(volSerial) && volSerial != "Not available",
                Status = IdentifierStatus.ReadOnly,
                Description = "Filesystem volume identifier created when formatting the system drive partition.",
                StatusMessage = "Filesystem volume ID. Read-only."
            });

            // 7. Active Network MAC Address
            string macAddress = "Not available";
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToList();
            if (nics.Count > 0)
            {
                byte[] macBytes = nics[0].GetPhysicalAddress().GetAddressBytes();
                if (macBytes.Length == 6)
                {
                    macAddress = string.Join(":", macBytes.Select(b => b.ToString("X2")));
                }
            }
            list.Add(new IdentifierInfo
            {
                Name = "Active Adapter MAC Address",
                Value = macAddress,
                Category = IdentifierCategory.Network,
                Source = "System.Net.NetworkInformation:MAC",
                IsEditable = false,
                IsAvailable = macAddress != "Not available",
                Status = IdentifierStatus.ReadOnly,
                Description = "Media Access Control physical address of the primary active network interface card.",
                StatusMessage = "Network adapter hardware address. Read-only."
            });

            // 8. Windows OS Product Serial
            string osSerial = "Not available";
            var osRows = WmiHelper.Query("Win32_OperatingSystem", "SerialNumber");
            if (osRows.Count > 0)
            {
                osSerial = osRows[0].GetString("SerialNumber", "Not available");
            }
            list.Add(new IdentifierInfo
            {
                Name = "Windows Product ID",
                Value = osSerial,
                Category = IdentifierCategory.OperatingSystem,
                Source = "Win32_OperatingSystem:SerialNumber",
                IsEditable = false,
                IsAvailable = osSerial != "Not available",
                Status = IdentifierStatus.ReadOnly,
                Description = "Windows operating system product identification serial assigned during Windows installation.",
                StatusMessage = "Operating system license serial. Read-only."
            });

            return list;
        }, cancellationToken);
    }
}
