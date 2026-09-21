using MachineIDLogger.Core.Models;
using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Views;

public sealed partial class HistoryPage : Page
{
    public HistoryViewModel ViewModel { get; }

    public HistoryPage()
    {
        ViewModel = App.Services.GetRequiredService<HistoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private void CopyGuid_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.Tag as MachineGuidHistoryEntry 
                 ?? (sender as FrameworkElement)?.DataContext as MachineGuidHistoryEntry;
        if (entry != null)
        {
            string g = string.IsNullOrWhiteSpace(entry.NewGuid) ? entry.PreviousGuid : entry.NewGuid;
            Services.ClipboardHelper.Copy(g);
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.Tag as MachineGuidHistoryEntry 
                 ?? (sender as FrameworkElement)?.DataContext as MachineGuidHistoryEntry;
        if (entry != null)
        {
            await ViewModel.RestoreEntryAsync(entry);
        }
    }
}
