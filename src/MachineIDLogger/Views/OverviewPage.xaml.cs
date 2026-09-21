using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class OverviewPage : Page
{
    public OverviewViewModel ViewModel { get; }

    public OverviewPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<OverviewViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.RefreshOverview();
        Bindings.Update();
        await ViewModel.InitializeAsync();
        Bindings.Update();
    }

    private void CopyMachineGuid_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.CopyMachineGuid();
    }
}
