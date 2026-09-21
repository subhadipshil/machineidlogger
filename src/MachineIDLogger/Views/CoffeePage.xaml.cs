using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MachineIDLogger.Views;

public sealed partial class CoffeePage : Page
{
    public AboutViewModel ViewModel { get; }

    public CoffeePage()
    {
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Required;
        ViewModel = App.Services.GetRequiredService<AboutViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Bindings.Update();
    }

    private void CopyUpiId_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.CopyUpiId();
    }

    private void OpenUpiPayment_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.OpenUpiPayment();
    }

    private void OpenBuyMeCoffeeWeb_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.OpenBuyMeCoffeeWeb();
    }
}
