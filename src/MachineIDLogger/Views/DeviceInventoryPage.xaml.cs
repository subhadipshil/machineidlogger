using MachineIDLogger.Core.Models;
using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Views;

public sealed partial class DeviceInventoryPage : Page
{
    public DeviceInventoryViewModel ViewModel { get; }

    public DeviceInventoryPage()
    {
        ViewModel = App.Services.GetRequiredService<DeviceInventoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        var dev = (sender as FrameworkElement)?.Tag as DeviceItemInfo 
               ?? (sender as FrameworkElement)?.DataContext as DeviceItemInfo;
        if (dev != null && !string.IsNullOrWhiteSpace(dev.PrimaryHardwareId))
        {
            Services.ClipboardHelper.Copy(dev.PrimaryHardwareId);
        }
    }
}
