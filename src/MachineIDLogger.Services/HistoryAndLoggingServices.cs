using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Infrastructure.Storage;

namespace MachineIDLogger.Services;

public class HistoryService : IHistoryService
{
    private readonly SqliteHistoryStore _store;
    private readonly JsonBackupStore _backupStore;
    private readonly ILoggingService _loggingService;

    public HistoryService(
        SqliteHistoryStore store,
        JsonBackupStore backupStore,
        ILoggingService loggingService)
    {
        _store = store;
        _backupStore = backupStore;
        _loggingService = loggingService;
    }

    public async Task InitializeAsync()
    {
        await _store.InitializeAsync();

        // Reinstall discovery: check for backups that might not be in the database
        try
        {
            int imported = await DiscoverAndImportExistingBackupsAsync(@"C:\MachineIDLogger\Backups");
            if (imported > 0)
            {
                _loggingService.LogInfo("History", $"Reinstall discovery: Discovered and indexed {imported} historical backup(s).");
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogWarning("History", "Error during reinstall discovery of historical backups", ex.ToString());
        }
    }

    public async Task<List<MachineGuidHistoryEntry>> GetHistoryAsync(int limit = 100)
    {
        return await _store.GetHistoryAsync(limit);
    }

    public async Task AddHistoryEntryAsync(MachineGuidHistoryEntry entry)
    {
        await _store.AddEntryAsync(entry);
    }

    public async Task<int> DiscoverAndImportExistingBackupsAsync(string backupDirectory)
    {
        if (!Directory.Exists(backupDirectory)) return 0;

        var existingHistory = await _store.GetHistoryAsync(1000);
        var knownBackups = existingHistory.Select(h => h.BackupPath).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var backupFiles = _backupStore.GetAvailableBackups();
        int importedCount = 0;

        foreach (var file in backupFiles)
        {
            if (!knownBackups.Contains(file))
            {
                var meta = await _backupStore.ReadBackupAsync(file);
                if (meta != null)
                {
                    var entry = new MachineGuidHistoryEntry
                    {
                        TimestampUtc = meta.CreatedAtUtc,
                        PreviousGuid = meta.PreviousValue,
                        NewGuid = "Discovered Historical Backup",
                        Action = HistoryAction.Discovered,
                        Status = HistoryStatus.Success,
                        BackupPath = file,
                        Initiator = "System Discovery",
                        Notes = $"Imported during reinstall discovery from backup file: {Path.GetFileName(file)}"
                    };
                    await _store.AddEntryAsync(entry);
                    importedCount++;
                }
            }
        }

        return importedCount;
    }

    public async Task ClearHistoryAsync()
    {
        await _store.ClearHistoryAsync();
        _loggingService.LogInfo("History", "Audit history was cleared by user.");
    }
}

public class LoggingService : ILoggingService
{
    private readonly FileLogStore _fileStore;

    public LoggingService(FileLogStore fileStore)
    {
        _fileStore = fileStore;
    }

    public void Log(LogSeverity severity, string category, string message, string? exception = null, string? caller = null)
    {
        _fileStore.Append(severity, category, message, exception, caller);
    }

    public void LogInfo(string category, string message) =>
        Log(LogSeverity.Information, category, message);

    public void LogWarning(string category, string message, string? exception = null) =>
        Log(LogSeverity.Warning, category, message, exception);

    public void LogError(string category, string message, string? exception = null) =>
        Log(LogSeverity.Error, category, message, exception);

    public Task<List<LogEntry>> GetLogsAsync(string? categoryFilter = null, LogSeverity? minSeverity = null, string? searchText = null, int limit = 200)
    {
        return Task.FromResult(_fileStore.GetRecentEntries(categoryFilter, minSeverity, searchText, limit));
    }

    public Task ClearLogsAsync()
    {
        _fileStore.ClearInMemory();
        return Task.CompletedTask;
    }

    public string GetLogFilePath() => _fileStore.CurrentLogFilePath;
}
