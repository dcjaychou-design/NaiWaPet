using System.IO;
using System.Security;
using System.Text.Json;
using NaiWaPet.Core.Configuration;

namespace NaiWaPet.Services;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SettingsStore()
    {
        var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NaiWaPet");
        Path = System.IO.Path.Combine(directory, "settings.json");
    }

    public string Path { get; }

    public PetSettings Load()
    {
        try
        {
            if (!File.Exists(Path))
            {
                return new PetSettings();
            }

            var settings = JsonSerializer.Deserialize<PetSettings>(File.ReadAllText(Path), JsonOptions) ?? new PetSettings();
            settings.Normalize();
            return settings;
        }
        catch (JsonException)
        {
            TryPreserveCorruptSettings();
            return new PetSettings();
        }
        catch (IOException)
        {
            return new PetSettings();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
        {
            return new PetSettings();
        }
    }

    public void Save(PetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string? temporaryPath = null;
        try
        {
            settings.Normalize();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            temporaryPath = Path + $".{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, settings, JsonOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, Path, overwrite: true);
            temporaryPath = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            // A locked or policy-protected settings folder must not crash the pet.
        }
        finally
        {
            TryDeleteTemporaryFile(temporaryPath);
        }
    }

    private void TryPreserveCorruptSettings()
    {
        try
        {
            var backup = Path + $".invalid-{DateTime.Now:yyyyMMdd-HHmmss-fff}";
            File.Move(Path, backup, overwrite: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            // A corrupt file should never prevent the pet from starting.
        }
    }

    private static void TryDeleteTemporaryFile(string? temporaryPath)
    {
        if (temporaryPath is null)
        {
            return;
        }

        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            // A leftover temporary file is harmless; later saves use unique names.
        }
    }
}
