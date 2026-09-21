using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using MachineIDLogger.Core.Validation;
using MachineIDLogger.Infrastructure.Cursor;
using MachineIDLogger.Infrastructure.Storage;
using MachineIDLogger.Services;
using Xunit;

namespace MachineIDLogger.Tests;

public class GuidValidatorTests
{
    [Theory]
    [InlineData("d93d6535-5c43-4887-98c5-5d681b9f1e35", true)]
    [InlineData("{d93d6535-5c43-4887-98c5-5d681b9f1e35}", true)]
    [InlineData("D93D6535-5C43-4887-98C5-5D681B9F1E35", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", false)] // NIL GUID not allowed as machine ID
    [InlineData("not-a-guid", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("d93d6535-5c43-4887-98c5-5d681b9f1e35-extra", false)]
    public void Validates_Guid_Format_Correctly(string input, bool expectedValid)
    {
        bool isValid = GuidValidator.IsValid(input, out var parsedGuid, out string error);
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.NotEqual(Guid.Empty, parsedGuid);
            Assert.True(string.IsNullOrEmpty(error));
        }
        else
        {
            Assert.False(string.IsNullOrEmpty(error));
        }
    }

    [Fact]
    public void GenerateRandomUuid_Returns_Valid_Lowercase_Guid()
    {
        string guid = GuidValidator.GenerateRandomUuid();
        Assert.True(GuidValidator.IsValid(guid, out var parsed, out _));
        Assert.Equal(guid, guid.ToLowerInvariant());
        Assert.Equal(36, guid.Length);
    }

    [Fact]
    public void Normalize_Strips_Braces_And_Lowercases()
    {
        string raw = "{D93D6535-5C43-4887-98C5-5D681B9F1E35}";
        string normalized = GuidValidator.Normalize(raw);
        Assert.Equal("d93d6535-5c43-4887-98c5-5d681b9f1e35", normalized);
    }
}

public class HistoryStoreTests : IDisposable
{
    private readonly string _testDbPath;

    public HistoryStoreTests()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "MachineIDLogger_Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        _testDbPath = Path.Combine(tempDir, "test_history.db");
    }

    public void Dispose()
    {
        string? dir = Path.GetDirectoryName(_testDbPath);
        if (Directory.Exists(dir))
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public async Task Sqlite_Initializes_And_Saves_History_Entry()
    {
        var store = new SqliteHistoryStore(_testDbPath);
        await store.InitializeAsync();

        var entry = new MachineGuidHistoryEntry
        {
            PreviousGuid = "11111111-1111-1111-1111-111111111111",
            NewGuid = "22222222-2222-2222-2222-222222222222",
            Action = HistoryAction.Changed,
            Status = HistoryStatus.Success,
            BackupPath = "C:\\test\\backup.json",
            Initiator = "TestRunner",
            Notes = "Unit test insertion"
        };

        await store.AddEntryAsync(entry);
        Assert.True(entry.Id > 0);

        var history = await store.GetHistoryAsync();
        Assert.Single(history);
        Assert.Equal(entry.NewGuid, history[0].NewGuid);
        Assert.Equal(entry.PreviousGuid, history[0].PreviousGuid);
        Assert.Equal(HistoryAction.Changed, history[0].Action);
        Assert.Equal(HistoryStatus.Success, history[0].Status);
    }
}

public class ExportServiceTests
{
    private readonly ExportReport _sampleReport;

    public ExportServiceTests()
    {
        _sampleReport = new ExportReport
        {
            AppVersion = "1.0.0",
            System = new SystemOverview
            {
                ComputerName = "TEST-PC",
                Manufacturer = "TestLab",
                Model = "ProUnit-X1",
                OperatingSystem = new OperatingSystemInfo
                {
                    Caption = "Windows 11 Enterprise",
                    BuildNumber = "26100.1"
                },
                Cpu = new CpuInfo
                {
                    Name = "Intel Core i7-14700K",
                    PhysicalCores = 20,
                    LogicalProcessors = 28
                },
                Memory = new MemoryInfo
                {
                    TotalPhysicalBytes = 32UL * 1024 * 1024 * 1024,
                    AvailablePhysicalBytes = 20UL * 1024 * 1024 * 1024
                }
            },
            Identifiers =
            [
                new IdentifierInfo
                {
                    Name = "Windows MachineGuid",
                    Value = "d93d6535-5c43-4887-98c5-5d681b9f1e35",
                    Category = IdentifierCategory.OperatingSystem,
                    Source = "HKLM",
                    IsEditable = true,
                    Status = IdentifierStatus.Supported
                },
                new IdentifierInfo
                {
                    Name = "SMBIOS System UUID",
                    Value = "4c4c4544-004a-4d10-8054-c2c04f534833",
                    Category = IdentifierCategory.Firmware,
                    Source = "Win32_ComputerSystemProduct",
                    IsEditable = false,
                    Status = IdentifierStatus.ReadOnly
                }
            ],
            History =
            [
                new MachineGuidHistoryEntry
                {
                    PreviousGuid = "old-guid",
                    NewGuid = "new-guid",
                    Action = HistoryAction.Changed,
                    Status = HistoryStatus.Success,
                    Initiator = "GUI"
                }
            ]
        };
    }

    [Fact]
    public async Task Generates_Valid_Json_Export()
    {
        var exporter = new ExportService();
        string json = await exporter.GenerateExportContentAsync(_sampleReport, ExportFormat.Json);

        Assert.Contains("\"ComputerName\": \"TEST-PC\"", json);
        Assert.Contains("\"Windows MachineGuid\"", json);
        Assert.Contains("\"d93d6535-5c43-4887-98c5-5d681b9f1e35\"", json);
    }

    [Fact]
    public async Task Generates_Valid_Csv_Export()
    {
        var exporter = new ExportService();
        string csv = await exporter.GenerateExportContentAsync(_sampleReport, ExportFormat.Csv);

        Assert.Contains("Section,Name,Value,Category,Source,Status", csv);
        Assert.Contains("\"Windows MachineGuid\"", csv);
        Assert.Contains("\"Intel Core i7-14700K\"", csv);
    }

    [Fact]
    public async Task Generates_Valid_Txt_Export()
    {
        var exporter = new ExportService();
        string txt = await exporter.GenerateExportContentAsync(_sampleReport, ExportFormat.Txt);

        Assert.Contains("MACHINEIDLOGGER SYSTEM & IDENTITY REPORT", txt);
        Assert.Contains("Computer Name    : TEST-PC", txt);
        Assert.Contains("Windows MachineGuid", txt);
    }

    [Fact]
    public async Task Generates_Printable_Html_Export()
    {
        var exporter = new ExportService();
        string html = await exporter.GenerateExportContentAsync(_sampleReport, ExportFormat.Html);

        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("MachineIDLogger Technical Report", html);
        Assert.Contains("d93d6535-5c43-4887-98c5-5d681b9f1e35", html);
        Assert.Contains("Intel Core i7-14700K", html);
    }
}

public class LoggingServiceTests : IDisposable
{
    private readonly string _tempLogDir;

    public LoggingServiceTests()
    {
        _tempLogDir = Path.Combine(Path.GetTempPath(), "MachineIDLogger_LogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempLogDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempLogDir))
        {
            try { Directory.Delete(_tempLogDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Logs_And_Filters_Correctly()
    {
        var fileStore = new FileLogStore(_tempLogDir);
        var logger = new LoggingService(fileStore);

        logger.LogInfo("TestCat", "Informational test message");
        logger.LogWarning("TestCat", "Warning test message");
        logger.LogError("ErrorCat", "Error test message", "Stack trace details");

        var allLogs = await logger.GetLogsAsync();
        Assert.Equal(3, allLogs.Count);

        var errorLogs = await logger.GetLogsAsync(minSeverity: LogSeverity.Error);
        Assert.Single(errorLogs);
        Assert.Equal("ErrorCat", errorLogs[0].Category);

        var searchLogs = await logger.GetLogsAsync(searchText: "Informational");
        Assert.Single(searchLogs);
    }
}

public class DiagnosticsServiceTests
{
    [Fact]
    public async Task Runs_Full_Diagnostics_Returns_All_Checks()
    {
        var tempLogDir = Path.Combine(Path.GetTempPath(), "MachineIDLogger_DiagTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLogDir);
        try
        {
            var logger = new LoggingService(new FileLogStore(tempLogDir));
            var historyStore = new SqliteHistoryStore(Path.Combine(tempLogDir, "history.db"));
            var backupStore = new JsonBackupStore(Path.Combine(tempLogDir, "Backups"));
            var historyService = new HistoryService(historyStore, backupStore, logger);
            var identifierService = new IdentifierService();

            var diagService = new DiagnosticsService(identifierService, historyService, logger);
            var results = await diagService.RunDiagnosticsAsync();

            Assert.NotNull(results);
            Assert.True(results.Count >= 16, $"Expected at least 16 diagnostic checks, but got {results.Count}");
            Assert.Contains(results, r => r.CheckName == "Audio & Speaker Hardware");
            Assert.Contains(results, r => r.CheckName == "Display Output & Monitors");
            Assert.Contains(results, r => r.CheckName == "Network & Gateway Connectivity");
            Assert.Contains(results, r => r.CheckName == "Power & Battery Subsystem");
        }
        finally
        {
            if (Directory.Exists(tempLogDir))
            {
                try { Directory.Delete(tempLogDir, true); } catch { }
            }
        }
    }
}

