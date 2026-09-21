using MachineIDLogger.Core.Models;
using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }

    public LogsPage()
    {
        ViewModel = App.Services.GetRequiredService<LogsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as FrameworkElement)?.Tag as LogEntry 
                 ?? (sender as FrameworkElement)?.DataContext as LogEntry;
        if (entry != null)
        {
            Services.ClipboardHelper.Copy($"[{entry.TimestampDisplay}] [{entry.Severity}] [{entry.Category}] {entry.Message}");
        }
    }
}
