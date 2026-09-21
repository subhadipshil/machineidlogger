using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.ViewModels;

public partial class OverviewViewModel : ObservableObject
{
    private readonly IDeviceDiscoveryService _discoveryService;
    private readonly IProcessorService _processorService;
    private readonly IMemoryService _memoryService;
    private readonly ILoggingService _loggingService;

    [ObservableProperty]
    private SystemOverview _overview = new();

    [ObservableProperty]
    private double _liveCpuUsage;

    [ObservableProperty]
    private double _liveRamUsage;

    [ObservableProperty]
    private string _liveRamFormatted = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _copyFeedback = "";

    public OverviewViewModel(
        IDeviceDiscoveryService discoveryService,
        IProcessorService processorService,
        IMemoryService memoryService,
        ILoggingService loggingService)
    {
        _discoveryService = discoveryService;
        _processorService = processorService;
        _memoryService = memoryService;
        _loggingService = loggingService;

        _overview = _discoveryService.CachedOverview;
        _discoveryService.ScanStateChanged += (s, scanning) =>
        {
            if (!scanning)
            {
                RefreshOverview();
            }
        };
    }

    public void RefreshOverview()
    {
        Overview = _discoveryService.CachedOverview;
        OnPropertyChanged(nameof(Overview));
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        RefreshOverview();

        if (Overview.PhysicalDrives.Count == 0 && Overview.Gpus.Count == 0)
        {
            await _discoveryService.RunFullDiscoveryAsync();
            RefreshOverview();
        }
        await UpdateDynamicMetricsAsync();
        RefreshOverview();
        IsLoading = false;
    }

    public async Task UpdateDynamicMetricsAsync()
    {
        try
        {
            LiveCpuUsage = await _processorService.GetLiveCpuUsageAsync();
            var mem = await _memoryService.GetMemoryInfoAsync();
            if (mem.TotalPhysicalBytes > 0)
            {
                LiveRamUsage = mem.LoadPercentage;
                LiveRamFormatted = $"{mem.FormattedUsed} / {mem.FormattedTotal} ({mem.LoadPercentage:F0}%)";
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        await _discoveryService.RunFullDiscoveryAsync();
        Overview = _discoveryService.CachedOverview;
        await UpdateDynamicMetricsAsync();
        IsLoading = false;
    }

    [RelayCommand]
    public void CopyMachineGuid()
    {
        CopyText(Overview.MachineGuid, "MachineGuid copied to clipboard");
    }

    [RelayCommand]
    public void CopySmbiosUuid()
    {
        CopyText(Overview.SmbiosUuid, "SMBIOS UUID copied to clipboard");
    }

    private void CopyText(string text, string feedback)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        Services.ClipboardHelper.Copy(text);
        CopyFeedback = feedback;
    }
}

public partial class ProcessorViewModel : ObservableObject
{
    private readonly IProcessorService _processorService;

    [ObservableProperty]
    private CpuInfo _cpu = new();

    [ObservableProperty]
    private double _liveUsage;

    [ObservableProperty]
    private bool _isLoading;

    public ProcessorViewModel(IProcessorService processorService)
    {
        _processorService = processorService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Cpu = await _processorService.GetCpuInfoAsync();
        LiveUsage = await _processorService.GetLiveCpuUsageAsync();
        IsLoading = false;
    }

    public async Task UpdateMetricsAsync()
    {
        LiveUsage = await _processorService.GetLiveCpuUsageAsync();
    }
}

public partial class MemoryViewModel : ObservableObject
{
    private readonly IMemoryService _memoryService;

    [ObservableProperty]
    private MemoryInfo _memory = new();

    [ObservableProperty]
    private bool _isLoading;

    public MemoryViewModel(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Memory = await _memoryService.GetMemoryInfoAsync();
        IsLoading = false;
    }
}

public partial class GraphicsViewModel : ObservableObject
{
    private readonly IGraphicsService _graphicsService;

    [ObservableProperty]
    private ObservableCollection<GpuInfo> _gpus = [];

    [ObservableProperty]
    private bool _isLoading;

    public GraphicsViewModel(IGraphicsService graphicsService)
    {
        _graphicsService = graphicsService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        var list = await _graphicsService.GetGpusAsync();
        Gpus.Clear();
        foreach (var g in list) Gpus.Add(g);
        IsLoading = false;
    }
}

public partial class StorageViewModel : ObservableObject
{
    private readonly IStorageService _storageService;

    [ObservableProperty]
    private ObservableCollection<StorageDriveInfo> _drives = [];

    [ObservableProperty]
    private ObservableCollection<VolumeInfo> _volumes = [];

    [ObservableProperty]
    private bool _isLoading;

    public StorageViewModel(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        var drives = await _storageService.GetPhysicalDrivesAsync();
        var volumes = await _storageService.GetVolumesAsync();

        Drives.Clear();
        foreach (var d in drives) Drives.Add(d);

        Volumes.Clear();
        foreach (var v in volumes) Volumes.Add(v);

        IsLoading = false;
    }
}

public partial class MotherboardViewModel : ObservableObject
{
    private readonly IMotherboardService _motherboardService;
    private readonly IBiosService _biosService;

    [ObservableProperty]
    private MotherboardInfo _motherboard = new();

    [ObservableProperty]
    private BiosInfo _bios = new();

    [ObservableProperty]
    private bool _isLoading;

    public MotherboardViewModel(IMotherboardService motherboardService, IBiosService biosService)
    {
        _motherboardService = motherboardService;
        _biosService = biosService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        Motherboard = await _motherboardService.GetMotherboardInfoAsync();
        Bios = await _biosService.GetBiosInfoAsync();
        IsLoading = false;
    }
}

public partial class DisplaysViewModel : ObservableObject
{
    private readonly IDisplayService _displayService;

    [ObservableProperty]
    private ObservableCollection<DisplayInfo> _displays = [];

    [ObservableProperty]
    private bool _isLoading;

    public DisplaysViewModel(IDisplayService displayService)
    {
        _displayService = displayService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        var list = await _displayService.GetDisplaysAsync();
        Displays.Clear();
        foreach (var d in list) Displays.Add(d);
        IsLoading = false;
    }
}

public partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    private List<NetworkAdapterInfo> _allAdapters = [];

    [ObservableProperty]
    private ObservableCollection<NetworkAdapterInfo> _adapters = [];

    [ObservableProperty]
    private bool _showPhysicalOnly = true;

    [ObservableProperty]
    private bool _isLoading;

    public NetworkViewModel(INetworkService networkService)
    {
        _networkService = networkService;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        _allAdapters = await _networkService.GetAdaptersAsync();
        ApplyFilter();
        IsLoading = false;
    }

    partial void OnShowPhysicalOnlyChanged(bool value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Adapters.Clear();
        var query = ShowPhysicalOnly ? _allAdapters.Where(a => a.IsPhysical) : _allAdapters;
        foreach (var a in query) Adapters.Add(a);
    }
}
