using System.Text;
using System.Text.Json;
using MachineIDLogger.Core.Interfaces;
using MachineIDLogger.Core.Models;

namespace MachineIDLogger.Services;

public class ExportService : IExportService
{
    public Task<string> GenerateExportContentAsync(ExportReport report, ExportFormat format)
    {
        return Task.Run(() =>
        {
            return format switch
            {
                ExportFormat.Json => GenerateJson(report),
                ExportFormat.Csv => GenerateCsv(report),
                ExportFormat.Txt => GenerateTxt(report),
                ExportFormat.Html => GenerateHtml(report),
                _ => GenerateTxt(report)
            };
        });
    }

    public async Task<string> SaveExportToFileAsync(ExportReport report, ExportFormat format, string destinationPath)
    {
        string content = await GenerateExportContentAsync(report, format);
        string? dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(destinationPath, content, Encoding.UTF8);
        return destinationPath;
    }

    private static string GenerateJson(ExportReport report)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(report, options);
    }

    private static string GenerateCsv(ExportReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Section,Name,Value,Category,Source,Status");

        // Identifiers
        foreach (var id in report.Identifiers)
        {
            sb.AppendLine($"\"Identifier\",\"{EscapeCsv(id.Name)}\",\"{EscapeCsv(id.Value)}\",\"{id.Category}\",\"{EscapeCsv(id.Source)}\",\"{id.FormattedStatus}\"");
        }

        // System
        var s = report.System;
        sb.AppendLine($"\"System\",\"Computer Name\",\"{EscapeCsv(s.ComputerName)}\",\"System\",\"Environment\",\"Ready\"");
        sb.AppendLine($"\"System\",\"Manufacturer\",\"{EscapeCsv(s.Manufacturer)}\",\"System\",\"CIM\",\"Ready\"");
        sb.AppendLine($"\"System\",\"Model\",\"{EscapeCsv(s.Model)}\",\"System\",\"CIM\",\"Ready\"");
        sb.AppendLine($"\"System\",\"OS Edition\",\"{EscapeCsv(s.OperatingSystem.Caption)}\",\"OS\",\"CIM\",\"Ready\"");
        sb.AppendLine($"\"System\",\"OS Build\",\"{EscapeCsv(s.OperatingSystem.BuildNumber)}\",\"OS\",\"CIM\",\"Ready\"");
        sb.AppendLine($"\"System\",\"Processor\",\"{EscapeCsv(s.Cpu.Name)}\",\"Hardware\",\"CIM\",\"Ready\"");
        sb.AppendLine($"\"System\",\"Total RAM\",\"{s.Memory.FormattedTotal}\",\"Hardware\",\"Native\",\"Ready\"");

        // History
        foreach (var h in report.History)
        {
            sb.AppendLine($"\"History\",\"{h.TimestampDisplay}\",\"{EscapeCsv(h.NewGuid)}\",\"{h.Action}\",\"{EscapeCsv(h.Initiator)}\",\"{h.Status}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string val) => (val ?? "").Replace("\"", "\"\"");

    private static string GenerateTxt(ExportReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine("MACHINEIDLOGGER SYSTEM & IDENTITY REPORT");
        sb.AppendLine($"Generated: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC} | Version: {report.AppVersion}");
        sb.AppendLine("================================================================================");
        sb.AppendLine();

        sb.AppendLine("[SYSTEM IDENTIFICATION]");
        sb.AppendLine($"  Computer Name    : {report.System.ComputerName}");
        sb.AppendLine($"  Manufacturer     : {report.System.Manufacturer}");
        sb.AppendLine($"  Model            : {report.System.Model}");
        sb.AppendLine($"  Operating System : {report.System.OperatingSystem.Caption} ({report.System.OperatingSystem.Architecture})");
        sb.AppendLine($"  Windows Build    : {report.System.OperatingSystem.BuildNumber}");
        sb.AppendLine($"  System Uptime    : {report.System.OperatingSystem.FormattedUptime}");
        sb.AppendLine($"  Current User     : {report.System.CurrentUser}");
        sb.AppendLine();

        sb.AppendLine("[PRIMARY HARDWARE]");
        sb.AppendLine($"  Processor        : {report.System.Cpu.Name} ({report.System.Cpu.PhysicalCores} cores, {report.System.Cpu.LogicalProcessors} threads)");
        sb.AppendLine($"  Total RAM        : {report.System.Memory.FormattedTotal} installed ({report.System.Memory.FormattedAvailable} available)");
        if (report.System.Gpus.Count > 0)
        {
            sb.AppendLine($"  Graphics Adapter : {report.System.Gpus[0].Name} (Driver: {report.System.Gpus[0].DriverVersion})");
        }
        sb.AppendLine($"  Motherboard      : {report.System.Motherboard.Manufacturer} {report.System.Motherboard.Product}");
        sb.AppendLine($"  BIOS/UEFI        : {report.System.Bios.Manufacturer} {report.System.Bios.Version} (Date: {report.System.Bios.ReleaseDate})");
        sb.AppendLine();

        sb.AppendLine("[TECHNICAL IDENTIFIERS]");
        foreach (var id in report.Identifiers)
        {
            sb.AppendLine($"  {id.Name.PadRight(28)} : {id.Value}");
            sb.AppendLine($"    Source: {id.Source} | Status: {id.FormattedStatus}");
        }
        sb.AppendLine();

        sb.AppendLine("[MACHINE GUID AUDIT HISTORY]");
        if (report.History.Count == 0)
        {
            sb.AppendLine("  No history recorded yet.");
        }
        else
        {
            foreach (var h in report.History)
            {
                sb.AppendLine($"  [{h.TimestampDisplay}] {h.Action.ToString().ToUpperInvariant()}: {h.NewGuid} (Previous: {h.PreviousGuid}) - Status: {h.Status}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("================================================================================");
        sb.AppendLine("End of Report. Confidential technical inspection data.");
        return sb.ToString();
    }

    private static string GenerateHtml(ExportReport report)
    {
        var s = report.System;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <title>MachineIDLogger Technical Report</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 30px; background: #0F172A; color: #E2E8F0; line-height: 1.5; }");
        sb.AppendLine("    .container { max-width: 960px; margin: 0 auto; background: #1E293B; border-radius: 8px; border: 1px solid #334155; padding: 24px; }");
        sb.AppendLine("    h1 { font-size: 20px; font-weight: 600; color: #F8FAFC; margin-bottom: 4px; display: flex; align-items: center; justify-content: space-between; }");
        sb.AppendLine("    .badge { font-size: 11px; background: #38BDF8; color: #0F172A; padding: 3px 8px; border-radius: 4px; font-weight: 600; }");
        sb.AppendLine("    .meta { font-size: 12px; color: #94A3B8; margin-bottom: 24px; border-bottom: 1px solid #334155; padding-bottom: 12px; }");
        sb.AppendLine("    h2 { font-size: 14px; text-transform: uppercase; letter-spacing: 0.05em; color: #38BDF8; margin-top: 24px; margin-bottom: 8px; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 13px; }");
        sb.AppendLine("    th, td { text-align: left; padding: 8px 12px; border-bottom: 1px solid #334155; }");
        sb.AppendLine("    th { background: #0F172A; color: #94A3B8; font-weight: 600; font-size: 12px; }");
        sb.AppendLine("    .mono { font-family: 'Cascadia Mono', 'Consolas', monospace; font-size: 12px; color: #F1F5F9; }");
        sb.AppendLine("    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px; }");
        sb.AppendLine("    .card { background: #0F172A; border: 1px solid #334155; border-radius: 6px; padding: 12px; }");
        sb.AppendLine("    .card-title { font-size: 11px; color: #94A3B8; text-transform: uppercase; font-weight: 600; margin-bottom: 4px; }");
        sb.AppendLine("    .card-val { font-size: 14px; font-weight: 600; color: #F8FAFC; }");
        sb.AppendLine("    @media print { body { background: white; color: black; } .container { border: none; background: white; color: black; } th { background: #F1F5F9; color: black; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine("    <h1>MachineIDLogger System & Identity Report <span class=\"badge\">CONFIDENTIAL</span></h1>");
        sb.AppendLine($"    <div class=\"meta\">Generated on {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC &bull; MachineIDLogger v{report.AppVersion} &bull; Host: {s.ComputerName}</div>");

        sb.AppendLine("    <h2>System Specifications</h2>");
        sb.AppendLine("    <div class=\"grid\">");
        sb.AppendLine($"      <div class=\"card\"><div class=\"card-title\">Machine</div><div class=\"card-val\">{s.Manufacturer} {s.Model}</div></div>");
        sb.AppendLine($"      <div class=\"card\"><div class=\"card-title\">Operating System</div><div class=\"card-val\">{s.OperatingSystem.Caption} (Build {s.OperatingSystem.BuildNumber})</div></div>");
        sb.AppendLine($"      <div class=\"card\"><div class=\"card-title\">Processor</div><div class=\"card-val\">{s.Cpu.Name} ({s.Cpu.PhysicalCores}C / {s.Cpu.LogicalProcessors}T)</div></div>");
        sb.AppendLine($"      <div class=\"card\"><div class=\"card-title\">Memory</div><div class=\"card-val\">{s.Memory.FormattedTotal} RAM ({s.Memory.FormattedAvailable} Free)</div></div>");
        sb.AppendLine("    </div>");

        sb.AppendLine("    <h2>Technical Machine Identifiers</h2>");
        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead><tr><th>Identifier</th><th>Value</th><th>Source</th><th>Status</th></tr></thead>");
        sb.AppendLine("      <tbody>");
        foreach (var id in report.Identifiers)
        {
            sb.AppendLine($"        <tr><td><strong>{id.Name}</strong></td><td class=\"mono\">{id.Value}</td><td>{id.Source}</td><td>{id.FormattedStatus}</td></tr>");
        }
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        sb.AppendLine("    <h2>MachineGuid Change History</h2>");
        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead><tr><th>Timestamp</th><th>Action</th><th>Target GUID</th><th>Status</th><th>Initiator</th></tr></thead>");
        sb.AppendLine("      <tbody>");
        if (report.History.Count == 0)
        {
            sb.AppendLine("        <tr><td colspan=\"5\" style=\"text-align:center; color:#94A3B8;\">No audit history entries recorded.</td></tr>");
        }
        else
        {
            foreach (var h in report.History)
            {
                sb.AppendLine($"        <tr><td>{h.TimestampDisplay}</td><td>{h.Action}</td><td class=\"mono\">{h.NewGuid}</td><td>{h.Status}</td><td>{h.Initiator}</td></tr>");
            }
        }
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }
}
