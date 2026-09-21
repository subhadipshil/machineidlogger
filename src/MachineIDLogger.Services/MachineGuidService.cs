using System.ComponentModel;
using System.Diagnostics;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Core.Validation;
using MachineIDLogger.Infrastructure.Registry;
using MachineIDLogger.Infrastructure.Storage;
using MachineIDLogger.Infrastructure.Windows;

namespace MachineIDLogger.Services;

public class MachineGuidService : IMachineGuidService
{
    private readonly JsonBackupStore _backupStore;
    private readonly IHistoryService _historyService;
    private readonly ILoggingService _loggingService;

    public MachineGuidService(
        JsonBackupStore backupStore,
        IHistoryService historyService,
        ILoggingService loggingService)
    {
        _backupStore = backupStore;
        _historyService = historyService;
        _loggingService = loggingService;
    }

    public Task<string> ReadCurrentMachineGuidAsync()
    {
        return Task.Run(() => RegistryHelper.ReadMachineGuid());
    }

    public async Task<string> CreatePreModificationBackupAsync(string currentGuid)
    {
        return await _backupStore.CreateMachineGuidBackupAsync(currentGuid, "Pre-modification automatic backup");
    }

    public async Task<bool> VerifyMachineGuidAsync(string expectedGuid)
    {
        return await Task.Run(() =>
        {
            string current = RegistryHelper.ReadMachineGuid();
            return current.Equals(expectedGuid, StringComparison.OrdinalIgnoreCase);
        });
    }

    public async Task<MachineGuidOperationResult> ChangeMachineGuidAsync(string newGuid, string initiator = "GUI")
    {
        // 1. Validation
        if (!GuidValidator.IsValid(newGuid, out var parsedGuid, out string validationError))
        {
            _loggingService.LogWarning("MachineGuid", $"Validation failed for input '{newGuid}': {validationError}");
            return new MachineGuidOperationResult(false, "", newGuid, "", validationError);
        }

        string normalizedNew = parsedGuid.ToString("D").ToLowerInvariant();
        string currentGuid = await ReadCurrentMachineGuidAsync();

        // Check if already identical
        if (currentGuid.Equals(normalizedNew, StringComparison.OrdinalIgnoreCase))
        {
            return new MachineGuidOperationResult(true, currentGuid, normalizedNew, "", "The requested GUID is already the active Windows MachineGuid.");
        }

        // 2. Pre-modification atomic backup
        string backupPath = "";
        try
        {
            backupPath = await CreatePreModificationBackupAsync(currentGuid);
            _loggingService.LogInfo("MachineGuid", $"Pre-change backup created at: {backupPath}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("MachineGuid", "Failed to create pre-modification backup", ex.ToString());
            return new MachineGuidOperationResult(false, currentGuid, normalizedNew, "",
                $"Unable to create backup before modification: {ex.Message}. Operation aborted for safety.");
        }

        // 3. Execution (Direct if elevated, else launch Elevator with Verb=runas)
        bool writeSuccess = false;
        string? failureError = null;

        if (NativeMethods.IsProcessElevated())
        {
            try
            {
                RegistryHelper.WriteMachineGuid(normalizedNew);
                writeSuccess = true;
            }
            catch (Exception ex)
            {
                failureError = ex.Message;
            }
        }
        else
        {
            var elevResult = await RunElevatedWorkerAsync(normalizedNew, backupPath);
            writeSuccess = elevResult.Success;
            failureError = elevResult.Error;
        }

        if (!writeSuccess)
        {
            _loggingService.LogError("MachineGuid", $"Registry write failed: {failureError}");
            await _historyService.AddHistoryEntryAsync(new MachineGuidHistoryEntry
            {
                PreviousGuid = currentGuid,
                NewGuid = normalizedNew,
                Action = HistoryAction.Changed,
                Status = HistoryStatus.Error,
                BackupPath = backupPath,
                Initiator = initiator,
                Notes = $"Write failed: {failureError}"
            });

            return new MachineGuidOperationResult(false, currentGuid, normalizedNew, backupPath,
                $"Failed to write MachineGuid: {failureError}", failureError);
        }

        // 4. Immediate Post-Write Verification
        bool verified = await VerifyMachineGuidAsync(normalizedNew);
        if (!verified)
        {
            string actualAfterWrite = await ReadCurrentMachineGuidAsync();
            _loggingService.LogError("MachineGuid", $"CRITICAL: Verification failed! Read: {actualAfterWrite}, Expected: {normalizedNew}");

            await _historyService.AddHistoryEntryAsync(new MachineGuidHistoryEntry
            {
                PreviousGuid = currentGuid,
                NewGuid = normalizedNew,
                Action = HistoryAction.Changed,
                Status = HistoryStatus.FailedVerification,
                BackupPath = backupPath,
                Initiator = initiator,
                Notes = $"Verification mismatch. Registry returned '{actualAfterWrite}'"
            });

            return new MachineGuidOperationResult(false, currentGuid, actualAfterWrite, backupPath,
                "FAILED VERIFICATION: The value written to the registry did not match the requested GUID. Please inspect logs and restore from backup.",
                $"Mismatch: Expected {normalizedNew}, Read {actualAfterWrite}");
        }

        // 5. Success Recording
        await _historyService.AddHistoryEntryAsync(new MachineGuidHistoryEntry
        {
            PreviousGuid = currentGuid,
            NewGuid = normalizedNew,
            Action = HistoryAction.Changed,
            Status = HistoryStatus.Success,
            BackupPath = backupPath,
            Initiator = initiator,
            Notes = "Successfully updated and verified."
        });

        _loggingService.LogInfo("MachineGuid", $"MachineGuid successfully changed from '{currentGuid}' to '{normalizedNew}'. Verified: true.");

        return new MachineGuidOperationResult(true, currentGuid, normalizedNew, backupPath,
            "Windows MachineGuid was successfully changed and verified in the 64-bit registry.");
    }

    public async Task<MachineGuidOperationResult> RestoreMachineGuidAsync(string targetGuid, string initiator = "GUI")
    {
        if (!GuidValidator.IsValid(targetGuid, out var parsedGuid, out string validationError))
        {
            return new MachineGuidOperationResult(false, "", targetGuid, "", $"Cannot restore invalid GUID: {validationError}");
        }

        string normalizedTarget = parsedGuid.ToString("D").ToLowerInvariant();
        string currentGuid = await ReadCurrentMachineGuidAsync();

        // Create pre-restore backup
        string backupPath = "";
        try
        {
            backupPath = await _backupStore.CreateMachineGuidBackupAsync(currentGuid, "Pre-restore automatic backup");
        }
        catch (Exception ex)
        {
            return new MachineGuidOperationResult(false, currentGuid, normalizedTarget, "", $"Backup failed: {ex.Message}");
        }

        bool writeSuccess = false;
        string? failureError = null;

        if (NativeMethods.IsProcessElevated())
        {
            try
            {
                RegistryHelper.WriteMachineGuid(normalizedTarget);
                writeSuccess = true;
            }
            catch (Exception ex)
            {
                failureError = ex.Message;
            }
        }
        else
        {
            var elevResult = await RunElevatedWorkerAsync(normalizedTarget, backupPath);
            writeSuccess = elevResult.Success;
            failureError = elevResult.Error;
        }

        if (!writeSuccess)
        {
            return new MachineGuidOperationResult(false, currentGuid, normalizedTarget, backupPath,
                $"Restore failed: {failureError}");
        }

        bool verified = await VerifyMachineGuidAsync(normalizedTarget);
        if (!verified)
        {
            return new MachineGuidOperationResult(false, currentGuid, normalizedTarget, backupPath,
                "FAILED VERIFICATION during restore.");
        }

        await _historyService.AddHistoryEntryAsync(new MachineGuidHistoryEntry
        {
            PreviousGuid = currentGuid,
            NewGuid = normalizedTarget,
            Action = HistoryAction.Restored,
            Status = HistoryStatus.Success,
            BackupPath = backupPath,
            Initiator = initiator,
            Notes = "Successfully restored from backup."
        });

        _loggingService.LogInfo("MachineGuid", $"MachineGuid successfully restored to '{normalizedTarget}'.");

        return new MachineGuidOperationResult(true, currentGuid, normalizedTarget, backupPath,
            "Windows MachineGuid was successfully restored and verified.");
    }

    private static async Task<(bool Success, string? Error)> RunElevatedWorkerAsync(string targetGuid, string backupPath)
    {
        return await Task.Run<(bool Success, string? Error)>(() =>
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string elevatorPath = Path.Combine(appDir, "MachineIDLogger.Elevator.exe");

            // If not found in current directory, check neighboring build folder or parent
            if (!File.Exists(elevatorPath))
            {
                string devPath = Path.Combine(appDir, @"..\..\..\..\MachineIDLogger.Elevator\bin\Debug\net10.0\MachineIDLogger.Elevator.exe");
                if (File.Exists(devPath))
                {
                    elevatorPath = Path.GetFullPath(devPath);
                }
            }

            if (!File.Exists(elevatorPath))
            {
                // If the standalone exe is not built yet, fallback to powershell elevated command
                return RunElevatedPowershellAsync(targetGuid);
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = elevatorPath,
                    Arguments = $"--apply-guid {targetGuid} --backup-path \"{backupPath}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    return (false, "Failed to start elevation process.");
                }

                proc.WaitForExit();

                if (proc.ExitCode == 0)
                {
                    return (true, null);
                }
                return (false, $"Elevated process exited with error code {proc.ExitCode}.");
            }
            catch (Win32Exception wEx) when (wEx.NativeErrorCode == 1223) // ERROR_CANCELLED
            {
                return (false, "Administrator elevation was cancelled by user.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
    }

    private static (bool Success, string? Error) RunElevatedPowershellAsync(string targetGuid)
    {
        try
        {
            string command = $"Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Microsoft\\Cryptography' -Name 'MachineGuid' -Value '{targetGuid}' -Type String";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -WindowStyle Hidden -Command \"{command}\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var proc = Process.Start(psi);
            if (proc == null) return (false, "Unable to start elevated command.");
            proc.WaitForExit();
            return proc.ExitCode == 0 ? (true, null) : (false, $"PowerShell exited with code {proc.ExitCode}");
        }
        catch (Win32Exception wEx) when (wEx.NativeErrorCode == 1223)
        {
            return (false, "Administrator elevation was cancelled by user.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
