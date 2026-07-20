using System.IO;
using System.Globalization;
using System.Security;
using System.Text;

namespace NaiWaPet.Services;

internal static class ErrorLog
{
    public static string? Write(Exception exception, string context)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NaiWaPet",
                "Logs");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"error-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            var content = new StringBuilder()
                .AppendLine("NaiWaPet error report")
                .AppendLine(CultureInfo.InvariantCulture, $"Time: {DateTimeOffset.Now:O}")
                .AppendLine(CultureInfo.InvariantCulture, $"Context: {context}")
                .AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}")
                .AppendLine(CultureInfo.InvariantCulture, $"Runtime: {Environment.Version}")
                .AppendLine(CultureInfo.InvariantCulture, $"Process: {Environment.ProcessPath}")
                .AppendLine()
                .AppendLine(exception.ToString())
                .ToString();
            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException or SecurityException)
        {
            return null;
        }
    }
}
