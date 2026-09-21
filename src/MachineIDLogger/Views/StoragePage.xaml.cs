using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class StoragePage : Page
{
    public StorageViewModel ViewModel { get; }

    public StoragePage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<StorageViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }
}
