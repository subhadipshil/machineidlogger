using System.Security.Cryptography;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Cursor;

namespace MachineIDLogger.Services;

public class CursorIdentityService : ICursorIdentityService
{
    private readonly ILoggingService _loggingService;

    public CursorIdentityService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<CursorIdentityInfo> DetectCursorIdentityAsync(CancellationToken cancellationToken = default)
    {
        return await CursorStorageHelper.InspectAsync(cancellationToken);
    }

    public async Task<CursorOperationResult> GenerateNewIdentityAsync(CancellationToken cancellationToken = default)
    {
        var info = await DetectCursorIdentityAsync(cancellationToken);
        if (!info.IsInstalled)
        {
            return new CursorOperationResult(false, "Cursor is not installed or storage.json is missing on this machine.");
        }

        if (!info.IsSupported)
        {
            return new CursorOperationResult(false, $"Unsupported Cursor structure: {info.VersionStatus}. Operation cancelled for safety.");
        }

        // 1. Create Backup
        string backupPath;
        try
        {
            backupPath = await CursorStorageHelper.CreateBackupAsync(info.StorageJsonPath);
            _loggingService.LogInfo("CursorIdentity", $"Cursor pre-modification backup created at: {backupPath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("CursorIdentity", "Failed to create Cursor backup", ex.ToString());
            return new CursorOperationResult(false, $"Failed to create backup: {ex.Message}");
        }

        // 2. Generate new compliant identifiers:
        // machineId: 64 hex characters (SHA-256 hash formatted)
        // macMachineId: 64 hex characters
        // devDeviceId: standard UUID format
        // sqmId: standard UUID format with braces
        string newMachineId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        string newMacMachineId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        string newDevDeviceId = Guid.NewGuid().ToString("D").ToLowerInvariant();
        string newSqmId = "{" + Guid.NewGuid().ToString("D").ToUpperInvariant() + "}";

        // 3. Write & Verify
        var (success, error) = await CursorStorageHelper.UpdateIdentityAsync(
            info.StorageJsonPath,
            newMachineId,
            newMacMachineId,
            newDevDeviceId,
            newSqmId);

        if (!success)
        {
            _loggingService.LogError("CursorIdentity", $"Failed to update Cursor identity: {error}");
            return new CursorOperationResult(false, $"Failed to update Cursor storage: {error}", backupPath);
        }

        _loggingService.LogInfo("CursorIdentity", "Successfully generated and verified new local Cursor telemetry identity.");
        return new CursorOperationResult(true, "Cursor telemetry identity was successfully updated and verified.", backupPath);
    }

    public async Task<CursorOperationResult> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        string targetPath = CursorStorageHelper.GetDefaultStorageJsonPath();
        if (!File.Exists(targetPath))
        {
            return new CursorOperationResult(false, "Target Cursor storage.json path does not exist.");
        }

        var (success, error) = await CursorStorageHelper.RestoreBackupAsync(targetPath, backupFilePath);
        if (!success)
        {
            _loggingService.LogError("CursorIdentity", $"Failed to restore Cursor backup: {error}");
            return new CursorOperationResult(false, $"Restore failed: {error}");
        }

        _loggingService.LogInfo("CursorIdentity", $"Successfully restored Cursor identity from '{backupFilePath}'.");
        return new CursorOperationResult(true, "Cursor storage.json was successfully restored from backup.");
    }
}
