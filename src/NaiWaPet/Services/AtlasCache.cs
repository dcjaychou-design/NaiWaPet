using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NaiWaPet.Core.Animation;

namespace NaiWaPet.Services;

internal sealed class AtlasCache
{
    private readonly AnimationManifest manifest;
    private readonly Dispatcher dispatcher;
    private readonly object gate = new();
    private readonly Dictionary<int, Task<byte[]>> byteCache = [];
    private readonly Dictionary<int, BitmapSource> bitmapCache = [];

    public AtlasCache(AnimationManifest manifest)
    {
        this.manifest = manifest;
        dispatcher = Dispatcher.CurrentDispatcher;
    }

    public BitmapSource Get(int atlasIndex)
    {
        dispatcher.VerifyAccess();
        if (bitmapCache.TryGetValue(atlasIndex, out var bitmap))
        {
            return bitmap;
        }

        Task<byte[]> task;
        lock (gate)
        {
            if (!byteCache.TryGetValue(atlasIndex, out task!))
            {
                task = LoadBytesAsync(atlasIndex);
                byteCache.Add(atlasIndex, task);
            }
        }

        // WPF imaging objects are deliberately created on the owning UI
        // dispatcher. Only thread-neutral byte arrays are loaded in advance.
        bitmap = EmbeddedAssets.DecodeAtlas(task.GetAwaiter().GetResult());
        bitmapCache.Add(atlasIndex, bitmap);
        return bitmap;
    }

    public void Preload(int atlasIndex)
    {
        dispatcher.VerifyAccess();
        if ((uint)atlasIndex >= (uint)manifest.Atlases.Count)
        {
            return;
        }

        lock (gate)
        {
            if (!byteCache.ContainsKey(atlasIndex))
            {
                byteCache.Add(atlasIndex, LoadBytesAsync(atlasIndex));
            }
        }
    }

    public void Trim(int currentAtlasIndex)
    {
        dispatcher.VerifyAccess();
        foreach (var key in bitmapCache.Keys.Where(key => key < currentAtlasIndex - 1 || key > currentAtlasIndex + 1).ToArray())
        {
            bitmapCache.Remove(key);
        }

        lock (gate)
        {
            foreach (var key in byteCache.Keys.Where(key => key < currentAtlasIndex - 1 || key > currentAtlasIndex + 1).ToArray())
            {
                byteCache.Remove(key);
            }
        }
    }

    private Task<byte[]> LoadBytesAsync(int atlasIndex)
    {
        var file = manifest.Atlases[atlasIndex].File;
        return Task.Run(() => EmbeddedAssets.LoadAtlasBytes(file));
    }
}
