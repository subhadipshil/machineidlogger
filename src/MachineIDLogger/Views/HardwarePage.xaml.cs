using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class HardwarePage : Page
{
    public OverviewViewModel ViewModel { get; }

    public HardwarePage()
    {
        ViewModel = App.Services.GetRequiredService<OverviewViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }
}
