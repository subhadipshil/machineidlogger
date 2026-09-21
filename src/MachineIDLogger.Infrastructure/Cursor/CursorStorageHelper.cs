using System.Text.Json;
using System.Text.Json.Nodes;
using MachineIDLogger.Core.Models;

namespace MachineIDLogger.Infrastructure.Cursor;

public static class CursorStorageHelper
{
    public static string GetDefaultStorageJsonPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Cursor", "User", "globalStorage", "storage.json");
    }

    public static string GetBackupDirectory()
    {
        return Path.Combine(@"C:\MachineIDLogger\Backups", "cursor");
    }

    public static bool IsCursorInstalled(out string path)
    {
        path = GetDefaultStorageJsonPath();
        return File.Exists(path);
    }

    public static async Task<CursorIdentityInfo> InspectAsync(CancellationToken cancellationToken = default)
    {
        var info = new CursorIdentityInfo();
        string path = GetDefaultStorageJsonPath();
        info.StorageJsonPath = path;

        if (!File.Exists(path))
        {
            info.IsInstalled = false;
            info.VersionStatus = "Cursor storage.json not found on this system.";
            info.IsSupported = false;
            return info;
        }

        info.IsInstalled = true;
        try
        {
            string content = await File.ReadAllTextAsync(path, cancellationToken);
            var node = JsonNode.Parse(content);
            if (node is JsonObject obj)
            {
                info.MachineId = obj["telemetry.machineId"]?.ToString() ?? "Not found in file";
                info.MacMachineId = obj["telemetry.macMachineId"]?.ToString() ?? "Not found in file";
                info.DevDeviceId = obj["telemetry.devDeviceId"]?.ToString() ?? "Not found in file";
                info.SqmId = obj["telemetry.sqmId"]?.ToString() ?? "Not found in file";

                // Verify that at least machineId or devDeviceId exists to consider structure supported
                if (obj.ContainsKey("telemetry.machineId") || obj.ContainsKey("telemetry.devDeviceId"))
                {
                    info.IsSupported = true;
                    info.VersionStatus = "Supported Cursor configuration";
                }
                else
                {
                    info.IsSupported = false;
                    info.VersionStatus = "Unsupported Cursor schema (missing telemetry keys)";
                }
            }
            else
            {
                info.IsSupported = false;
                info.VersionStatus = "Invalid JSON structure in storage.json";
            }
        }
        catch (Exception ex)
        {
            info.IsSupported = false;
            info.VersionStatus = $"Error reading storage.json: {ex.Message}";
        }

        // List backups
        string backupDir = GetBackupDirectory();
        if (Directory.Exists(backupDir))
        {
            info.AvailableBackups = Directory.GetFiles(backupDir, "cursor_backup_*.json")
                .OrderByDescending(f => File.GetCreationTimeUtc(f))
                .ToList();
        }

        return info;
    }

    public static async Task<string> CreateBackupAsync(string path)
    {
        string backupDir = GetBackupDirectory();
        if (!Directory.Exists(backupDir))
        {
            Directory.CreateDirectory(backupDir);
        }

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string backupFile = Path.Combine(backupDir, $"cursor_backup_{timestamp}.json");

        string content = await File.ReadAllTextAsync(path);
        await File.WriteAllTextAsync(backupFile, content);

        return backupFile;
    }

    public static async Task<(bool Success, string? Error)> UpdateIdentityAsync(
        string path,
        string newMachineId,
        string newMacMachineId,
        string newDevDeviceId,
        string newSqmId)
    {
        try
        {
            string content = await File.ReadAllTextAsync(path);
            var node = JsonNode.Parse(content) as JsonObject
                ?? throw new InvalidOperationException("Failed to parse storage.json root as object.");

            // Update keys
            node["telemetry.machineId"] = newMachineId;
            node["telemetry.macMachineId"] = newMacMachineId;
            node["telemetry.devDeviceId"] = newDevDeviceId;
            node["telemetry.sqmId"] = newSqmId;

            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = node.ToJsonString(options);

            // Safe atomic write
            string tempFile = path + ".tmp";
            await File.WriteAllTextAsync(tempFile, updatedJson);
            File.Move(tempFile, path, overwrite: true);

            // Post-write verification
            string verifiedContent = await File.ReadAllTextAsync(path);
            var verifiedNode = JsonNode.Parse(verifiedContent) as JsonObject;
            if (verifiedNode?["telemetry.machineId"]?.ToString() != newMachineId)
            {
                return (false, "Verification failed: Written machineId does not match expected value.");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Success, string? Error)> RestoreBackupAsync(string targetStoragePath, string backupFilePath)
    {
        try
        {
            if (!File.Exists(backupFilePath))
            {
                return (false, "Specified backup file does not exist.");
            }

            string content = await File.ReadAllTextAsync(backupFilePath);
            // Verify valid JSON before restoring
            var node = JsonNode.Parse(content);
            if (node == null)
            {
                return (false, "Backup file does not contain valid JSON.");
            }

            string tempFile = targetStoragePath + ".tmp";
            await File.WriteAllTextAsync(tempFile, content);
            File.Move(tempFile, targetStoragePath, overwrite: true);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
