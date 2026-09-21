using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MachineIDLogger.Infrastructure.Storage;

public class BackupMetadata
{
    public string IdentifierType { get; set; } = "WindowsMachineGuid";
    public string PreviousValue { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string MachineName { get; set; } = Environment.MachineName;
    public string OsVersion { get; set; } = Environment.OSVersion.ToString();
    public string ChecksumSha256 { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class JsonBackupStore
{
    private readonly string _backupsDirectory;

    public JsonBackupStore(string backupsDirectory)
    {
        _backupsDirectory = backupsDirectory;
        if (!Directory.Exists(_backupsDirectory))
        {
            Directory.CreateDirectory(_backupsDirectory);
        }
    }

    public async Task<string> CreateMachineGuidBackupAsync(string currentGuid, string notes = "")
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string safeGuid = currentGuid.Replace("{", "").Replace("}", "").Replace("-", "");
        string fileName = $"guid_backup_{timestamp}_{safeGuid}.json";
        string filePath = Path.Combine(_backupsDirectory, fileName);

        var meta = new BackupMetadata
        {
            IdentifierType = "WindowsMachineGuid",
            PreviousValue = currentGuid,
            CreatedAtUtc = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            OsVersion = Environment.OSVersion.ToString(),
            Notes = notes
        };

        // Compute checksum
        byte[] payloadBytes = Encoding.UTF8.GetBytes(currentGuid);
        meta.ChecksumSha256 = Convert.ToHexString(SHA256.HashData(payloadBytes));

        string json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });

        // Safe atomic write
        string tempPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);

        return filePath;
    }

    public List<string> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupsDirectory)) return [];
        return Directory.GetFiles(_backupsDirectory, "guid_backup_*.json")
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();
    }

    public async Task<BackupMetadata?> ReadBackupAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        string json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<BackupMetadata>(json);
    }
}
