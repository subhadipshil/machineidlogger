using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class MemoryPage : Page
{
    public MemoryViewModel ViewModel { get; }

    public MemoryPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<MemoryViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }
}
