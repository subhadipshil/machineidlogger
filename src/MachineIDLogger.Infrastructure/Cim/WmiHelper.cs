using System.Management;

namespace MachineIDLogger.Infrastructure.Cim;

public static class WmiHelper
{
    public static List<Dictionary<string, object?>> Query(string wmiClass, params string[] properties)
    {
        var results = new List<Dictionary<string, object?>>();
        try
        {
            string propList = properties.Length > 0 ? string.Join(", ", properties) : "*";
            string query = $"SELECT {propList} FROM {wmiClass}";

            using var searcher = new ManagementObjectSearcher(query);
            using var collection = searcher.Get();

            foreach (ManagementObject obj in collection)
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in obj.Properties)
                {
                    try
                    {
                        dict[prop.Name] = prop.Value;
                    }
                    catch
                    {
                        dict[prop.Name] = null;
                    }
                }
                results.Add(dict);
            }
        }
        catch
        {
            // Fault isolation: WMI may be blocked or service disabled
        }

        return results;
    }

    public static string GetString(this Dictionary<string, object?> dict, string key, string fallback = "Not available")
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            string s = val.ToString()?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return fallback;
    }

    public static ulong GetUInt64(this Dictionary<string, object?> dict, string key, ulong fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (ulong.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }

    public static uint GetUInt32(this Dictionary<string, object?> dict, string key, uint fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (uint.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }

    public static int GetInt32(this Dictionary<string, object?> dict, string key, int fallback = 0)
    {
        if (dict.TryGetValue(key, out var val) && val != null)
        {
            if (int.TryParse(val.ToString(), out var parsed)) return parsed;
        }
        return fallback;
    }
}
