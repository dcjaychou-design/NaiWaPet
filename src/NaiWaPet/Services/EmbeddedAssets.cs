using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Media.Imaging;
using NaiWaPet.Core.Animation;

namespace NaiWaPet.Services;

internal static class EmbeddedAssets
{
    private const string AnimationPrefix = "NaiWaPet.Assets.Animation.";
    private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static AnimationManifest LoadManifest()
    {
        using var stream = Open(AnimationPrefix + "animation.json");
        var manifest = JsonSerializer.Deserialize<AnimationManifest>(stream, JsonOptions)
            ?? throw new InvalidDataException("Animation manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public static HitMask LoadHitMask(string file)
    {
        using var stream = Open(AnimationPrefix + file);
        return HitMask.Load(stream);
    }

    public static byte[] LoadAtlasBytes(string file)
    {
        using var stream = Open(AnimationPrefix + file);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static BitmapSource DecodeAtlas(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    public static byte[] LoadLaughAudio()
    {
        using var stream = Open("NaiWaPet.Assets.Audio.laugh.wav");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static Stream Open(string name) =>
        Assembly.GetManifestResourceStream(name)
        ?? throw new FileNotFoundException($"Embedded asset was not found: {name}");
}
