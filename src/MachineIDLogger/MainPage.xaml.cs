using MachineIDLogger.ViewModels;
using MachineIDLogger.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Default to OverviewPage
        NavigateToTag("Overview");
        await ViewModel.StartInitialScanAsync();
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateToTag(tag);
        }
    }

    private void NavigateToTag(string tag)
    {
        Type? pageType = tag switch
        {
            "Overview" => typeof(OverviewPage),
            "Hardware" => typeof(HardwarePage),
            "Processor" => typeof(ProcessorPage),
            "Memory" => typeof(MemoryPage),
            "Graphics" => typeof(GraphicsPage),
            "Storage" => typeof(StoragePage),
            "Motherboard" => typeof(MotherboardPage),
            "Displays" => typeof(DisplaysPage),
            "Network" => typeof(NetworkPage),
            "DeviceInventory" => typeof(DeviceInventoryPage),
            "MachineGuid" => typeof(MachineGuidPage),
            "History" => typeof(HistoryPage),
            "Logs" => typeof(LogsPage),
            "CursorIdentity" => typeof(CursorIdentityPage),
            "Diagnostics" => typeof(DiagnosticsPage),
            "WhatsNew" => typeof(WhatsNewPage),
            "BuyMeCoffee" => typeof(CoffeePage),
            "Settings" => typeof(SettingsPage),
            "About" => typeof(AboutPage),
            _ => typeof(OverviewPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void GlobalSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string query = (args.QueryText ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(query)) return;

        if (query.Contains("proc") || query.Contains("cpu") || query.Contains("core") || query.Contains("intel") || query.Contains("amd"))
        {
            NavigateToTag("Processor");
        }
        else if (query.Contains("mem") || query.Contains("ram") || query.Contains("dimm") || query.Contains("ddr"))
        {
            NavigateToTag("Memory");
        }
        else if (query.Contains("gpu") || query.Contains("graph") || query.Contains("nvidia") || query.Contains("radeon") || query.Contains("video"))
        {
            NavigateToTag("Graphics");
        }
        else if (query.Contains("disk") || query.Contains("stor") || query.Contains("drive") || query.Contains("nvme") || query.Contains("sata") || query.Contains("volume"))
        {
            NavigateToTag("Storage");
        }
        else if (query.Contains("mobo") || query.Contains("mother") || query.Contains("board") || query.Contains("bios") || query.Contains("uefi"))
        {
            NavigateToTag("Motherboard");
        }
        else if (query.Contains("disp") || query.Contains("monit") || query.Contains("screen") || query.Contains("resolut"))
        {
            NavigateToTag("Displays");
        }
        else if (query.Contains("net") || query.Contains("mac") || query.Contains("ip") || query.Contains("ether") || query.Contains("wifi") || query.Contains("wi-fi"))
        {
            NavigateToTag("Network");
        }
        else if (query.Contains("guid") || query.Contains("mach") || query.Contains("ident") || query.Contains("uuid") || query.Contains("serial"))
        {
            NavigateToTag("MachineGuid");
        }
        else if (query.Contains("hist") || query.Contains("backup") || query.Contains("audit"))
        {
            NavigateToTag("History");
        }
        else if (query.Contains("log"))
        {
            NavigateToTag("Logs");
        }
        else if (query.Contains("cursor"))
        {
            NavigateToTag("CursorIdentity");
        }
        else if (query.Contains("diag") || query.Contains("test") || query.Contains("check"))
        {
            NavigateToTag("Diagnostics");
        }
        else if (query.Contains("set") || query.Contains("theme") || query.Contains("pref"))
        {
            NavigateToTag("Settings");
        }
        else if (query.Contains("about") || query.Contains("coffee") || query.Contains("upi") || query.Contains("update") || query.Contains("support"))
        {
            NavigateToTag("About");
        }
        else
        {
            // Default to Device Inventory for technical search
            NavigateToTag("DeviceInventory");
        }
    }

    private void GlobalSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(sender.Text))
        {
            var suggestions = new List<string>();
            string text = sender.Text.ToLowerInvariant();
            if ("processor cpu cores clock".Contains(text)) suggestions.Add("Processor (CPU)");
            if ("memory ram dimm capacity".Contains(text)) suggestions.Add("Memory (RAM)");
            if ("graphics gpu video vram".Contains(text)) suggestions.Add("Graphics (GPU)");
            if ("storage disk drive nvme ssd volume".Contains(text)) suggestions.Add("Storage Drives");
            if ("motherboard bios uefi smbios".Contains(text)) suggestions.Add("Motherboard & BIOS");
            if ("displays monitors resolution refresh".Contains(text)) suggestions.Add("Displays & Monitors");
            if ("network mac ethernet wifi ip".Contains(text)) suggestions.Add("Network Adapters");
            if ("windows machine id machineguid identity".Contains(text)) suggestions.Add("Windows Machine ID");
            if ("history restore audit".Contains(text)) suggestions.Add("Machine ID History");
            if ("cursor local identity telemetry".Contains(text)) suggestions.Add("Cursor Local Identity");
            if ("diagnostics health checks export".Contains(text)) suggestions.Add("System Diagnostics");
            sender.ItemsSource = suggestions;
        }
    }
}
