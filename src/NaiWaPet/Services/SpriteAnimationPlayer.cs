using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NaiWaPet.Core.Animation;

namespace NaiWaPet.Services;

internal sealed class SpriteAnimationPlayer : IDisposable
{
    private readonly AnimationManifest manifest;
    private readonly AtlasCache atlasCache;
    private readonly DispatcherTimer timer;
    private readonly Stopwatch clock = new();
    private readonly MemoryStream audioStream;
    private readonly SoundPlayer soundPlayer;
    private bool playing;
    private int currentFrame = -1;
    private BitmapSource? currentImage;

    public SpriteAnimationPlayer()
    {
        manifest = EmbeddedAssets.LoadManifest();
        HitMask = EmbeddedAssets.LoadHitMask(manifest.HitMaskFile);
        if (HitMask.Width != manifest.FrameWidth || HitMask.Height != manifest.FrameHeight || HitMask.FrameCount != manifest.TotalFrames)
        {
            throw new InvalidDataException("Hit mask does not match the animation manifest.");
        }

        atlasCache = new AtlasCache(manifest);
        atlasCache.Preload(0);
        atlasCache.Preload(1);
        audioStream = new MemoryStream(EmbeddedAssets.LoadLaughAudio(), writable: false);
        soundPlayer = new SoundPlayer(audioStream);
        soundPlayer.Load();
        timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(10),
        };
        timer.Tick += OnTick;
        ShowFrame(manifest.IdleFrame);
    }

    public event EventHandler<BitmapSource>? FrameChanged;

    public event EventHandler? PlaybackCompleted;

    public AnimationManifest Manifest => manifest;

    public HitMask HitMask { get; }

    public int CurrentFrame => currentFrame < 0 ? manifest.IdleFrame : currentFrame;

    public BitmapSource CurrentImage => currentImage ?? throw new InvalidOperationException("No animation frame is loaded.");

    public bool IsPlaying => playing;

    public bool SoundEnabled { get; set; }

    public void ValidateAssets()
    {
        for (var atlasIndex = 0; atlasIndex < manifest.Atlases.Count; atlasIndex++)
        {
            var atlas = atlasCache.Get(atlasIndex);
            if (atlas.PixelWidth != manifest.FrameWidth * manifest.Columns || atlas.PixelHeight != manifest.FrameHeight * manifest.Rows)
            {
                throw new InvalidDataException($"Animation atlas {atlasIndex} has unexpected dimensions.");
            }
        }
    }

    public void PlayLaugh()
    {
        soundPlayer.Stop();
        playing = true;
        clock.Restart();
        ShowFrame(0);
        timer.Start();
        if (SoundEnabled)
        {
            audioStream.Position = 0;
            soundPlayer.Play();
        }
    }

    public void Stop()
    {
        playing = false;
        timer.Stop();
        clock.Stop();
        soundPlayer.Stop();
        ShowFrame(manifest.IdleFrame);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var frame = (int)(clock.Elapsed.TotalSeconds * manifest.FramesPerSecond);
        if (frame >= manifest.TotalFrames)
        {
            Stop();
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        ShowFrame(frame);
    }

    private void ShowFrame(int frame)
    {
        if (frame == currentFrame)
        {
            return;
        }

        var location = manifest.LocateFrame(frame);
        var atlas = atlasCache.Get(location.AtlasIndex);
        var crop = new CroppedBitmap(
            atlas,
            new System.Windows.Int32Rect(
                location.Column * manifest.FrameWidth,
                location.Row * manifest.FrameHeight,
                manifest.FrameWidth,
                manifest.FrameHeight));
        crop.Freeze();
        currentFrame = frame;
        currentImage = crop;
        FrameChanged?.Invoke(this, crop);

        if (location.LocalFrame >= 24)
        {
            atlasCache.Preload(location.AtlasIndex + 1);
        }

        if (location.LocalFrame == 0)
        {
            atlasCache.Trim(location.AtlasIndex);
        }
    }

    public void Dispose()
    {
        timer.Stop();
        timer.Tick -= OnTick;
        soundPlayer.Stop();
        soundPlayer.Dispose();
        audioStream.Dispose();
    }
}
