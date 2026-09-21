using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Views;

public sealed partial class CursorIdentityPage : Page
{
    public CursorIdentityViewModel ViewModel { get; }

    public CursorIdentityPage()
    {
        ViewModel = App.Services.GetRequiredService<CursorIdentityViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private void CopyMachineId_Click(object sender, RoutedEventArgs e) =>
        CopyText(ViewModel.CursorInfo.MachineId);

    private void CopyMacMachineId_Click(object sender, RoutedEventArgs e) =>
        CopyText(ViewModel.CursorInfo.MacMachineId);

    private void CopyDevDeviceId_Click(object sender, RoutedEventArgs e) =>
        CopyText(ViewModel.CursorInfo.DevDeviceId);

    private void CopySqmId_Click(object sender, RoutedEventArgs e) =>
        CopyText(ViewModel.CursorInfo.SqmId);

    private void CopyText(string text)
    {
        Services.ClipboardHelper.Copy(text);
    }

    private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var backupPath = (sender as FrameworkElement)?.Tag as string 
                      ?? (sender as FrameworkElement)?.DataContext as string;
        if (!string.IsNullOrWhiteSpace(backupPath))
        {
            await ViewModel.RestoreBackupAsync(backupPath);
        }
    }
}
