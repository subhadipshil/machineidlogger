namespace MachineIDLogger.Core.Models;

public enum DeviceCategory
{
    Processor,
    Memory,
    Display,
    Storage,
    Network,
    Audio,
    USB,
    Bluetooth,
    Camera,
    Input,
    System,
    Other
}

public class DeviceItemInfo
{
    public string Name { get; set; } = "Unknown Device";
    public DeviceCategory Category { get; set; } = DeviceCategory.Other;
    public string Manufacturer { get; set; } = "Not available";
    public string Status { get; set; } = "OK";
    public string DriverVersion { get; set; } = "Not available";
    public string DriverDate { get; set; } = "Not available";
    public string DeviceId { get; set; } = string.Empty;
    public string PnpClass { get; set; } = "Unknown";
    public string Service { get; set; } = "Not available";
    public string Description { get; set; } = string.Empty;
    public List<string> HardwareIds { get; set; } = [];
    public List<string> CompatibleIds { get; set; } = [];
    public string PrimaryHardwareId => HardwareIds.Count > 0 ? HardwareIds[0] : DeviceId;
    public string Source { get; set; } = "Win32_PnPEntity";
}
