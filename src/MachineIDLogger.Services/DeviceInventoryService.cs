using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Cim;

namespace MachineIDLogger.Services;

public class DeviceInventoryService : IDeviceInventoryService
{
    public Task<List<DeviceItemInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var list = new List<DeviceItemInfo>();
            var rows = WmiHelper.Query("Win32_PnPEntity",
                "Caption", "Manufacturer", "Status", "PNPClass", "Service",
                "Description", "DeviceID", "HardWareID", "CompatibleID");

            foreach (var row in rows)
            {
                string caption = row.GetString("Caption", "");
                if (string.IsNullOrWhiteSpace(caption)) continue;

                var dev = new DeviceItemInfo
                {
                    Name = caption,
                    Manufacturer = row.GetString("Manufacturer", "Standard System Component"),
                    Status = row.GetString("Status", "OK"),
                    PnpClass = row.GetString("PNPClass", "System"),
                    Service = row.GetString("Service", "Not available"),
                    Description = row.GetString("Description", caption),
                    DeviceId = row.GetString("DeviceID", "")
                };

                // Map PNPClass to DeviceCategory
                dev.Category = dev.PnpClass.ToLowerInvariant() switch
                {
                    "processor" => DeviceCategory.Processor,
                    "memory" => DeviceCategory.Memory,
                    "display" => DeviceCategory.Display,
                    "diskdrive" or "scsiadapter" or "hdc" or "volume" => DeviceCategory.Storage,
                    "net" => DeviceCategory.Network,
                    "media" or "audioendpoint" or "sound" => DeviceCategory.Audio,
                    "usb" => DeviceCategory.USB,
                    "bluetooth" => DeviceCategory.Bluetooth,
                    "camera" or "image" => DeviceCategory.Camera,
                    "keyboard" or "mouse" or "hidclass" => DeviceCategory.Input,
                    "system" or "computesystem" => DeviceCategory.System,
                    _ => DeviceCategory.Other
                };

                // Extract Hardware IDs
                if (row.TryGetValue("HardWareID", out var hwVal) && hwVal is string[] hwArr)
                {
                    dev.HardwareIds.AddRange(hwArr.Where(s => !string.IsNullOrWhiteSpace(s)));
                }

                if (row.TryGetValue("CompatibleID", out var cmpVal) && cmpVal is string[] cmpArr)
                {
                    dev.CompatibleIds.AddRange(cmpArr.Where(s => !string.IsNullOrWhiteSpace(s)));
                }

                list.Add(dev);
            }

            return list;
        }, cancellationToken);
    }
}
