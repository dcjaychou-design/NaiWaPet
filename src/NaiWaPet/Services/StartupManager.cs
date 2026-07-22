using Microsoft.Win32;
using System.IO;
using System.Security;

namespace NaiWaPet.Services;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NaiWaPet";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            var expectedCommand = GetStartupCommand();
            return expectedCommand is not null &&
                   key?.GetValue(ValueName) is string value &&
                   string.Equals(value, expectedCommand, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is SecurityException or UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (!enabled)
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
            return;
        }

        var command = GetStartupCommand();
        if (command is null)
        {
            throw new InvalidOperationException("Cannot determine the NaiWaPet executable path.");
        }

        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    private static string? GetStartupCommand()
    {
        var executable = Environment.ProcessPath;
        return string.IsNullOrWhiteSpace(executable) ? null : $"\"{executable}\" --autostart";
    }
}
