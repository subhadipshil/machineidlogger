using MachineIDLogger.Core.Models;
using Microsoft.Data.Sqlite;

namespace MachineIDLogger.Infrastructure.Storage;

public class SqliteHistoryStore
{
    private readonly string _databasePath;
    private readonly string _connectionString;
    private bool _initialized;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteHistoryStore(string dbPath)
    {
        _databasePath = dbPath;
        _connectionString = $"Data Source={_databasePath}";
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        await _semaphore.WaitAsync();
        try
        {
            if (_initialized) return;

            string? dir = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            string initSql = @"
                CREATE TABLE IF NOT EXISTS schema_version (
                    version INTEGER PRIMARY KEY,
                    applied_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS machine_guid_history (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp_utc TEXT NOT NULL,
                    previous_guid TEXT NOT NULL,
                    new_guid TEXT NOT NULL,
                    action TEXT NOT NULL,
                    status TEXT NOT NULL,
                    backup_path TEXT NOT NULL,
                    initiator TEXT NOT NULL,
                    notes TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_history_timestamp ON machine_guid_history(timestamp_utc DESC);
            ";

            using var command = new SqliteCommand(initSql, connection);
            await command.ExecuteNonQueryAsync();

            // Insert schema version 1 if absent
            string checkVersionSql = "SELECT COUNT(*) FROM schema_version WHERE version = 1";
            using var checkCmd = new SqliteCommand(checkVersionSql, connection);
            long count = (long)(await checkCmd.ExecuteScalarAsync() ?? 0);
            if (count == 0)
            {
                using var insertVersionCmd = new SqliteCommand(
                    "INSERT INTO schema_version (version, applied_at) VALUES (1, $time)", connection);
                insertVersionCmd.Parameters.AddWithValue("$time", DateTime.UtcNow.ToString("o"));
                await insertVersionCmd.ExecuteNonQueryAsync();
            }

            _initialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddEntryAsync(MachineGuidHistoryEntry entry)
    {
        await InitializeAsync();
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                INSERT INTO machine_guid_history 
                (timestamp_utc, previous_guid, new_guid, action, status, backup_path, initiator, notes)
                VALUES ($timestamp, $prev, $new, $action, $status, $backup, $initiator, $notes);
                SELECT last_insert_rowid();
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("$timestamp", entry.TimestampUtc.ToString("o"));
            command.Parameters.AddWithValue("$prev", entry.PreviousGuid);
            command.Parameters.AddWithValue("$new", entry.NewGuid);
            command.Parameters.AddWithValue("$action", entry.Action.ToString());
            command.Parameters.AddWithValue("$status", entry.Status.ToString());
            command.Parameters.AddWithValue("$backup", entry.BackupPath);
            command.Parameters.AddWithValue("$initiator", entry.Initiator);
            command.Parameters.AddWithValue("$notes", entry.Notes ?? string.Empty);

            var idObj = await command.ExecuteScalarAsync();
            if (idObj != null && long.TryParse(idObj.ToString(), out long id))
            {
                entry.Id = id;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<MachineGuidHistoryEntry>> GetHistoryAsync(int limit = 100)
    {
        await InitializeAsync();
        var list = new List<MachineGuidHistoryEntry>();

        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT id, timestamp_utc, previous_guid, new_guid, action, status, backup_path, initiator, notes
                FROM machine_guid_history
                ORDER BY id DESC
                LIMIT $limit;
            ";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("$limit", limit);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var entry = new MachineGuidHistoryEntry
                {
                    Id = reader.GetInt64(0),
                    TimestampUtc = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.UtcNow,
                    PreviousGuid = reader.GetString(2),
                    NewGuid = reader.GetString(3),
                    Action = Enum.TryParse<HistoryAction>(reader.GetString(4), out var act) ? act : HistoryAction.Changed,
                    Status = Enum.TryParse<HistoryStatus>(reader.GetString(5), out var stat) ? stat : HistoryStatus.Success,
                    BackupPath = reader.GetString(6),
                    Initiator = reader.GetString(7),
                    Notes = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                };
                list.Add(entry);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        return list;
    }

    public async Task ClearHistoryAsync()
    {
        await InitializeAsync();
        await _semaphore.WaitAsync();
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqliteCommand("DELETE FROM machine_guid_history;", connection);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
