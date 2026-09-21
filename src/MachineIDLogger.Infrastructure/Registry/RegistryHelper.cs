using Microsoft.Win32;

namespace MachineIDLogger.Infrastructure.Registry;

public static class RegistryHelper
{
    private const string CryptoKeyPath = @"SOFTWARE\Microsoft\Cryptography";
    private const string MachineGuidValueName = "MachineGuid";

    public static string ReadMachineGuid()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var cryptoKey = baseKey.OpenSubKey(CryptoKeyPath, writable: false);
            if (cryptoKey == null)
            {
                return "Not available";
            }

            var val = cryptoKey.GetValue(MachineGuidValueName);
            return val?.ToString()?.Trim() ?? "Not available";
        }
        catch (UnauthorizedAccessException)
        {
            return "Permission denied";
        }
        catch
        {
            return "Query error";
        }
    }

    public static bool CanReadMachineGuid(out string? error)
    {
        error = null;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var cryptoKey = baseKey.OpenSubKey(CryptoKeyPath, writable: false);
            if (cryptoKey == null)
            {
                error = "Cryptography registry key does not exist.";
                return false;
            }
            var val = cryptoKey.GetValue(MachineGuidValueName);
            return val != null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool CanWriteMachineGuid(out string? error)
    {
        error = null;
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var cryptoKey = baseKey.OpenSubKey(CryptoKeyPath, writable: true);
            return cryptoKey != null;
        }
        catch (UnauthorizedAccessException)
        {
            error = "Administrator privileges required to write to registry.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static void WriteMachineGuid(string newGuid)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var cryptoKey = baseKey.OpenSubKey(CryptoKeyPath, writable: true)
            ?? throw new InvalidOperationException($"Unable to open '{CryptoKeyPath}' with write permissions.");

        cryptoKey.SetValue(MachineGuidValueName, newGuid, RegistryValueKind.String);
        cryptoKey.Flush();
    }
}
