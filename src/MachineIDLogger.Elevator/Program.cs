using MachineIDLogger.Core.Validation;
using MachineIDLogger.Infrastructure.Registry;
using MachineIDLogger.Infrastructure.Windows;

namespace MachineIDLogger.Elevator;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("MachineIDLogger Elevation Worker");

        if (!NativeMethods.IsProcessElevated())
        {
            Console.Error.WriteLine("Error: Elevation worker must run with administrator privileges.");
            return 5; // ERROR_ACCESS_DENIED
        }

        string? targetGuid = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--apply-guid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                targetGuid = args[i + 1];
            }
        }

        if (string.IsNullOrWhiteSpace(targetGuid))
        {
            Console.Error.WriteLine("Error: Missing --apply-guid argument.");
            return 87; // ERROR_INVALID_PARAMETER
        }

        if (!GuidValidator.IsValid(targetGuid, out var parsedGuid, out var error))
        {
            Console.Error.WriteLine($"Error: Invalid GUID supplied: {error}");
            return 87;
        }

        string normalizedGuid = parsedGuid.ToString("D").ToLowerInvariant();

        try
        {
            // Perform 64-bit Registry Write
            RegistryHelper.WriteMachineGuid(normalizedGuid);

            // Re-read and verify immediately
            string verified = RegistryHelper.ReadMachineGuid();
            if (!verified.Equals(normalizedGuid, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Error: Registry verification failed. Read: '{verified}', Expected: '{normalizedGuid}'.");
                return 13; // ERROR_INVALID_DATA
            }

            Console.WriteLine($"Success: MachineGuid successfully set and verified as {normalizedGuid}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing registry: {ex.Message}");
            return 1;
        }
    }
}
