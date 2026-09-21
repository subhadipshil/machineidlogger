using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsViewModel ViewModel { get; }

    public DiagnosticsPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<DiagnosticsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Bindings.Update();
        if (ViewModel.Results.Count == 0 && !ViewModel.IsRunning)
        {
            await ViewModel.RunDiagnosticsAsync();
            Bindings.Update();
        }
    }

    private async void ExportReport_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.ExportReportAsync();
    }

    private async void RunDiagnostics_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.RunDiagnosticsAsync();
        Bindings.Update();
    }
}
