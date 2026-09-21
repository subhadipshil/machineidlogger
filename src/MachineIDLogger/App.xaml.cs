using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Infrastructure.Storage;
using MachineIDLogger.Services;
using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace MachineIDLogger;

public partial class App : Application
{
    private Window? _window;
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window? MainWindowInstance { get; private set; }

    public App()
    {
        this.UnhandledException += (sender, e) =>
        {
            try
            {
                string crashPath = @"C:\MachineIDLogger\Logs\crash.log";
                Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
                File.AppendAllText(crashPath, $"[{DateTime.UtcNow:u}] APP UNHANDLED: {e.Exception}\nMessage: {e.Message}\nStackTrace: {e.Exception.StackTrace}\n\n");
            }
            catch { }
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            try
            {
                string crashPath = @"C:\MachineIDLogger\Logs\crash.log";
                Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
                File.AppendAllText(crashPath, $"[{DateTime.UtcNow:u}] APPDOMAIN UNHANDLED: {e.ExceptionObject}\n\n");
            }
            catch { }
        };

        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure
        string dataDir = @"C:\MachineIDLogger";
        string historyDb = Path.Combine(dataDir, "History", "history.db");
        string logDir = Path.Combine(dataDir, "Logs");
        string backupDir = Path.Combine(dataDir, "Backups");
        string configDir = Path.Combine(dataDir, "Config");

        services.AddSingleton(new SqliteHistoryStore(historyDb));
        services.AddSingleton(new FileLogStore(logDir));
        services.AddSingleton(new JsonBackupStore(backupDir));

        // Core & System Services
        services.AddSingleton<IProcessorService, ProcessorService>();
        services.AddSingleton<IMemoryService, MemoryService>();
        services.AddSingleton<IGraphicsService, GraphicsService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<IMotherboardService, MotherboardService>();
        services.AddSingleton<IBiosService, BiosService>();
        services.AddSingleton<IDisplayService, DisplayService>();
        services.AddSingleton<INetworkService, NetworkService>();
        services.AddSingleton<IDeviceInventoryService, DeviceInventoryService>();
        services.AddSingleton<IIdentifierService, IdentifierService>();

        services.AddSingleton<ISystemInformationService, SystemInformationService>();
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<IMachineGuidService, MachineGuidService>();
        services.AddSingleton<ICursorIdentityService, CursorIdentityService>();
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ISettingsService>(new SettingsService(configDir));
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<IDeviceDiscoveryService, DeviceDiscoveryService>();

        // ViewModels (Singletons to preserve state and prevent data wipe on navigation)
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<OverviewViewModel>();
        services.AddSingleton<ProcessorViewModel>();
        services.AddSingleton<MemoryViewModel>();
        services.AddSingleton<GraphicsViewModel>();
        services.AddSingleton<StorageViewModel>();
        services.AddSingleton<MotherboardViewModel>();
        services.AddSingleton<DisplaysViewModel>();
        services.AddSingleton<NetworkViewModel>();
        services.AddSingleton<DeviceInventoryViewModel>();
        services.AddSingleton<MachineGuidViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<LogsViewModel>();
        services.AddSingleton<CursorIdentityViewModel>();
        services.AddSingleton<DiagnosticsViewModel>();
        services.AddSingleton<WhatsNewViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            _window = new MainWindow();
            MainWindowInstance = _window;
            _window.Activate();
        }
        catch (Exception ex)
        {
            try
            {
                string crashPath = @"C:\MachineIDLogger\Logs\crash.log";
                Directory.CreateDirectory(Path.GetDirectoryName(crashPath)!);
                File.AppendAllText(crashPath, $"[{DateTime.UtcNow:u}] OnLaunched FATAL: {ex}\n\n");
            }
            catch { }
            throw;
        }
    }
}
