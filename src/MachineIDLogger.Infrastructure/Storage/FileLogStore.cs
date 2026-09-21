using System.Collections.Concurrent;
using MachineIDLogger.Core.Models;

namespace MachineIDLogger.Infrastructure.Storage;

public class FileLogStore
{
    private readonly string _logDirectory;
    private readonly ConcurrentQueue<LogEntry> _recentEntries = new();
    private const int MaxInMemoryEntries = 1000;
    private long _entryCounter;
    private readonly object _fileLock = new();

    public FileLogStore(string logDirectory)
    {
        _logDirectory = logDirectory;
        EnsureDirectory();
    }

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public string CurrentLogFilePath
    {
        get
        {
            string fileName = $"machineid-{DateTime.UtcNow:yyyyMMdd}.log";
            return Path.Combine(_logDirectory, fileName);
        }
    }

    public void Append(LogSeverity severity, string category, string message, string? exception = null, string? caller = null)
    {
        var entry = new LogEntry
        {
            Id = Interlocked.Increment(ref _entryCounter),
            TimestampUtc = DateTime.UtcNow,
            Severity = severity,
            Category = category,
            Message = message,
            ExceptionDetails = exception,
            Caller = caller
        };

        _recentEntries.Enqueue(entry);
        while (_recentEntries.Count > MaxInMemoryEntries && _recentEntries.TryDequeue(out _)) { }

        // Write to rolling log file
        try
        {
            EnsureDirectory();
            string line = FormatLogLine(entry);
            lock (_fileLock)
            {
                File.AppendAllText(CurrentLogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Do not crash application on log write failure
        }
    }

    private static string FormatLogLine(LogEntry entry)
    {
        string timestamp = entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string sev = entry.Severity.ToString().ToUpperInvariant().PadRight(5);
        string cat = entry.Category.PadRight(15);
        string line = $"[{timestamp}] [{sev}] [{cat}] {entry.Message}";
        if (!string.IsNullOrEmpty(entry.ExceptionDetails))
        {
            line += $"{Environment.NewLine}  --> Exception: {entry.ExceptionDetails}";
        }
        return line;
    }

    public List<LogEntry> GetRecentEntries(
        string? categoryFilter = null,
        LogSeverity? minSeverity = null,
        string? searchText = null,
        int limit = 200)
    {
        var query = _recentEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            query = query.Where(e => e.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (minSeverity.HasValue)
        {
            query = query.Where(e => e.Severity >= minSeverity.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(e =>
                e.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (e.ExceptionDetails != null && e.ExceptionDetails.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
        }

        return query.OrderByDescending(e => e.Id).Take(limit).ToList();
    }

    public void ClearInMemory()
    {
        while (_recentEntries.TryDequeue(out _)) { }
    }
}
