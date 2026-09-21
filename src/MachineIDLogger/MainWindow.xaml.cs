using System;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace MachineIDLogger;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        try
        {
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }
        catch
        {
            // Graceful fallback if custom titlebar extension is not supported
        }

        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }
        }
        catch
        {
            // Non-fatal if window icon cannot be set at runtime
        }

        CenterOnScreen();

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    private void CenterOnScreen()
    {
        try
        {
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    int width = 1240;
                    int height = 800;
                    var workArea = displayArea.WorkArea;
                    int x = workArea.X + Math.Max(0, (workArea.Width - width) / 2);
                    int y = workArea.Y + Math.Max(0, (workArea.Height - height) / 2);
                    appWindow.MoveAndResize(new RectInt32(x, y, width, height));
                }
            }
        }
        catch
        {
            // Graceful fallback if centering is not supported on environment
        }
    }
}
