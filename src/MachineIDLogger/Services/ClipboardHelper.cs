using MachineIDLogger.Infrastructure.Windows;
using Windows.ApplicationModel.DataTransfer;

namespace MachineIDLogger.Services;

public static class ClipboardHelper
{
    public static bool Copy(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        bool win32Success = false;
        try
        {
            win32Success = NativeMethods.CopyTextToClipboard(text);
        }
        catch
        {
            // Non-fatal, fallback to WinRT DataPackage
        }

        try
        {
            var pkg = new DataPackage();
            pkg.SetText(text);
            Clipboard.SetContent(pkg);
            Clipboard.Flush();
            return true;
        }
        catch
        {
            return win32Success;
        }
    }
}
