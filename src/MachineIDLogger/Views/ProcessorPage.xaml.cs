using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MachineIDLogger.Views;

public sealed partial class ProcessorPage : Page
{
    public ProcessorViewModel ViewModel { get; }

    public ProcessorPage()
    {
        NavigationCacheMode = NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<ProcessorViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }
}
