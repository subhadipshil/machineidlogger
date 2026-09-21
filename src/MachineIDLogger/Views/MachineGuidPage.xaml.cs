using MachineIDLogger.Core.Models;
using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Views;

public sealed partial class MachineGuidPage : Page
{
    public MachineGuidViewModel ViewModel { get; }

    public MachineGuidPage()
    {
        ViewModel = App.Services.GetRequiredService<MachineGuidViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAsync();
    }

    private void CopyCurrentGuid_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CopyCurrentGuid();
    }

    private void CopyIdentifier_Click(object sender, RoutedEventArgs e)
    {
        var id = (sender as FrameworkElement)?.Tag as IdentifierInfo 
              ?? (sender as FrameworkElement)?.DataContext as IdentifierInfo;
        if (id != null && !string.IsNullOrWhiteSpace(id.Value))
        {
            Services.ClipboardHelper.Copy(id.Value);
        }
    }
}
