using System.IO;
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
        catch (UnauthorizedAccessException)
        {
            return new PetSettings();
        }
    }

    public void Save(PetSettings settings)
    {
        try
        {
            settings.Normalize();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var temporaryPath = Path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, Path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A locked or policy-protected settings folder must not crash the pet.
        }
    }

    private void TryPreserveCorruptSettings()
    {
        try
        {
            var backup = Path + $".invalid-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Move(Path, backup, overwrite: false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A corrupt file should never prevent the pet from starting.
        }
    }
}
