using MachineIDLogger.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MachineIDLogger.Views;

public sealed partial class WhatsNewPage : Page
{
    public WhatsNewViewModel ViewModel { get; }

    public WhatsNewPage()
    {
        ViewModel = App.Services.GetRequiredService<WhatsNewViewModel>();
        InitializeComponent();
    }
}
