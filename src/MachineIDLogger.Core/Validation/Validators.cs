using System.Text.RegularExpressions;

namespace MachineIDLogger.Core.Validation;

public static class GuidValidator
{
    private static readonly Regex GuidRegex = new(
        @"^(\{)?[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\})?$",
        RegexOptions.Compiled);

    public static bool IsValid(string? input, out Guid parsedGuid, out string errorMessage)
    {
        parsedGuid = Guid.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            errorMessage = "GUID cannot be empty or whitespace.";
            return false;
        }

        string trimmed = input.Trim();

        if (!GuidRegex.IsMatch(trimmed))
        {
            errorMessage = "Invalid GUID format. Expected standard 8-4-4-4-12 hex format (e.g., 12345678-1234-1234-1234-123456789abc).";
            return false;
        }

        if (!Guid.TryParse(trimmed, out parsedGuid))
        {
            errorMessage = "Unable to parse GUID value.";
            return false;
        }

        if (parsedGuid == Guid.Empty)
        {
            errorMessage = "All-zero NIL GUID (00000000-0000-0000-0000-000000000000) is not permitted as a valid machine identifier.";
            return false;
        }

        return true;
    }

    public static string Normalize(string input)
    {
        if (Guid.TryParse(input.Trim(), out var guid))
        {
            return guid.ToString("D").ToLowerInvariant();
        }
        return input.Trim();
    }

    public static string GenerateRandomUuid()
    {
        return Guid.NewGuid().ToString("D").ToLowerInvariant();
    }
}

public static class PathValidator
{
    public static bool IsSafeDirectoryPath(string? path, out string sanitizedPath)
    {
        sanitizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            sanitizedPath = Path.GetFullPath(path.Trim());
            return Path.IsPathRooted(sanitizedPath);
        }
        catch
        {
            return false;
        }
    }
}
