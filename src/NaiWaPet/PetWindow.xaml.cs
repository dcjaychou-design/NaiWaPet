using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NaiWaPet.Core.Configuration;
using NaiWaPet.Core.Physics;
using NaiWaPet.Interop;
using NaiWaPet.Services;

namespace NaiWaPet;

internal sealed partial class PetWindow : Window
{
    private readonly SpriteAnimationPlayer player;
    private readonly PetSettings settings;
    private readonly SettingsStore settingsStore;
    private readonly DispatcherTimer roamTimer;
    private readonly Random random = new();
    private readonly Stopwatch interactionClock = new();
    private readonly List<(double Time, PointD Position)> dragSamples = [];
    private HwndSource? source;
    private bool dragging;
    private bool movedDuringDrag;
    private PointD dragStartCursor;
    private PointD dragStartWindow;
    private bool physicsActive;
    private PointD velocity;
    private TimeSpan lastRenderTime;
    private bool roamActive;
    private PointD roamStart;
    private PointD roamTarget;
    private TimeSpan roamStarted;

    public PetWindow(SpriteAnimationPlayer player, PetSettings settings, SettingsStore settingsStore)
    {
        InitializeComponent();
        this.player = player;
        this.settings = settings;
        this.settingsStore = settingsStore;
        FrameImage.Source = player.CurrentImage;
        player.FrameChanged += OnFrameChanged;
        player.PlaybackCompleted += OnPlaybackCompleted;

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closed += OnClosed;
        PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
        PreviewMouseMove += OnMouseMove;
        PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
        PreviewMouseRightButtonUp += OnMouseRightButtonUp;
        PreviewMouseWheel += OnMouseWheel;

        roamTimer = new DispatcherTimer
        {
            Interval = NextRoamInterval(),
        };
        roamTimer.Tick += OnRoamTimer;
        roamTimer.Start();
        ApplySettings(keepFeetAnchored: false);
    }

    public event EventHandler? ContextMenuRequested;

    public void ApplySettings(bool keepFeetAnchored = true)
    {
        var oldCenter = Left + Width / 2;
        var oldBottom = Top + Height;
        Width = player.Manifest.FrameWidth * settings.Scale;
        Height = player.Manifest.FrameHeight * settings.Scale;
        Topmost = settings.AlwaysOnTop;
        player.SoundEnabled = settings.SoundEnabled;
        if (keepFeetAnchored && IsLoaded)
        {
            Left = oldCenter - Width / 2;
            Top = oldBottom - Height;
            ClampToWorkArea();
        }
    }

    public void ResetPosition()
    {
        var work = ScreenService.GetWorkArea(this);
        Left = work.Right - Width - 24;
        Top = work.Bottom - Height - 12;
        SavePosition();
    }

    public void ClampToWorkArea()
    {
        var work = ScreenService.GetWorkArea(this);
        var clamped = ScreenService.Clamp(new PointD(Left, Top), new SizeD(Width, Height), work);
        Left = clamped.X;
        Top = clamped.Y;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (settings.WindowLeft is { } left && settings.WindowTop is { } top)
        {
            Left = left;
            Top = top;
            ClampToWorkArea();
        }
        else
        {
            ResetPosition();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WindowProcedure);
    }

    private nint WindowProcedure(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != NativeMethods.WmNcHitTest)
        {
            return 0;
        }

        if (settings.ClickThrough || !NativeMethods.GetWindowRect(hwnd, out var rect))
        {
            handled = true;
            return NativeMethods.HtTransparent;
        }

        var screenX = unchecked((short)((long)lParam & 0xffff));
        var screenY = unchecked((short)(((long)lParam >> 16) & 0xffff));
        var pixelWidth = rect.Right - rect.Left;
        var pixelHeight = rect.Bottom - rect.Top;
        var clientX = screenX - rect.Left;
        var clientY = screenY - rect.Top;
        if (pixelWidth <= 0 || pixelHeight <= 0 || clientX < 0 || clientY < 0 || clientX >= pixelWidth || clientY >= pixelHeight)
        {
            handled = true;
            return NativeMethods.HtTransparent;
        }

        var frameX = clientX * player.Manifest.FrameWidth / pixelWidth;
        var frameY = clientY * player.Manifest.FrameHeight / pixelHeight;
        if (!player.HitMask.IsOpaque(player.CurrentFrame, frameX, frameY))
        {
            handled = true;
            return NativeMethods.HtTransparent;
        }

        return 0;
    }

    private void OnFrameChanged(object? sender, BitmapSource frame) => FrameImage.Source = frame;

    private void OnPlaybackCompleted(object? sender, EventArgs e)
    {
        ClampToWorkArea();
        SavePosition();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (settings.ClickThrough)
        {
            return;
        }

        physicsActive = false;
        roamActive = false;
        dragging = true;
        movedDuringDrag = false;
        dragStartCursor = ScreenService.GetCursorPosition(this);
        dragStartWindow = new PointD(Left, Top);
        dragSamples.Clear();
        interactionClock.Restart();
        dragSamples.Add((0, dragStartCursor));
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!dragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var cursor = ScreenService.GetCursorPosition(this);
        var delta = new PointD(cursor.X - dragStartCursor.X, cursor.Y - dragStartCursor.Y);
        if (!movedDuringDrag && Length(delta) >= 5)
        {
            movedDuringDrag = true;
            player.Stop();
        }

        if (!movedDuringDrag)
        {
            return;
        }

        Left = dragStartWindow.X + delta.X;
        Top = dragStartWindow.Y + delta.Y;
        dragSamples.Add((interactionClock.Elapsed.TotalSeconds, cursor));
        while (dragSamples.Count > 2 && interactionClock.Elapsed.TotalSeconds - dragSamples[0].Time > 0.14)
        {
            dragSamples.RemoveAt(0);
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging)
        {
            return;
        }

        dragging = false;
        ReleaseMouseCapture();
        if (!movedDuringDrag)
        {
            player.PlayLaugh();
        }
        else
        {
            var throwVelocity = CalculateThrowVelocity();
            if (Length(throwVelocity) >= 420)
            {
                StartPhysics(throwVelocity);
            }
            else
            {
                ClampToWorkArea();
                SavePosition();
            }
        }

        e.Handled = true;
    }

    private PointD CalculateThrowVelocity()
    {
        if (dragSamples.Count < 2)
        {
            return new PointD(0, 0);
        }

        var first = dragSamples[0];
        var last = dragSamples[^1];
        var seconds = Math.Max(0.01, last.Time - first.Time);
        return new PointD(
            Math.Clamp((last.Position.X - first.Position.X) / seconds, -2200, 2200),
            Math.Clamp((last.Position.Y - first.Position.Y) / seconds, -2200, 2200));
    }

    private static double Length(PointD value) => Math.Sqrt(value.X * value.X + value.Y * value.Y);

    private void StartPhysics(PointD initialVelocity)
    {
        velocity = initialVelocity;
        physicsActive = true;
        lastRenderTime = TimeSpan.Zero;
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs rendering)
        {
            return;
        }

        if (lastRenderTime == TimeSpan.Zero)
        {
            lastRenderTime = rendering.RenderingTime;
            return;
        }

        var elapsed = (rendering.RenderingTime - lastRenderTime).TotalSeconds;
        lastRenderTime = rendering.RenderingTime;
        if (physicsActive)
        {
            var result = MotionPhysics.Step(
                new PointD(Left, Top),
                velocity,
                elapsed,
                ScreenService.GetWorkArea(this),
                new SizeD(Width, Height));
            Left = result.Position.X;
            Top = result.Position.Y;
            velocity = result.Velocity;
            if (result.Settled)
            {
                physicsActive = false;
                FinishMotion();
            }
        }
        else if (roamActive)
        {
            var duration = TimeSpan.FromMilliseconds(720);
            var progress = Math.Clamp((rendering.RenderingTime - roamStarted).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            var eased = 0.5 - Math.Cos(progress * Math.PI) / 2;
            Left = roamStart.X + (roamTarget.X - roamStart.X) * eased;
            Top = roamStart.Y + (roamTarget.Y - roamStart.Y) * eased - Math.Sin(progress * Math.PI) * 20;
            if (progress >= 1)
            {
                roamActive = false;
                FinishMotion();
            }
        }
    }

    private void FinishMotion()
    {
        if (!physicsActive && !roamActive)
        {
            CompositionTarget.Rendering -= OnRendering;
            lastRenderTime = TimeSpan.Zero;
            ClampToWorkArea();
            SavePosition();
        }
    }

    private void OnRoamTimer(object? sender, EventArgs e)
    {
        roamTimer.Interval = NextRoamInterval();
        if (!settings.RoamingEnabled || settings.ClickThrough || dragging || physicsActive || roamActive || player.IsPlaying || !IsVisible)
        {
            return;
        }

        var work = ScreenService.GetWorkArea(this);
        roamStart = new PointD(Left, Top);
#pragma warning disable CA5394 // Randomness controls visual roaming only and has no security purpose.
        var distance = random.Next(36, 96) * (random.Next(2) == 0 ? -1 : 1);
#pragma warning restore CA5394
        roamTarget = ScreenService.Clamp(new PointD(Left + distance, Top), new SizeD(Width, Height), work);
        roamActive = true;
        lastRenderTime = TimeSpan.Zero;
        roamStarted = TimeSpan.Zero;
        CompositionTarget.Rendering -= StartRoamOnFirstFrame;
        CompositionTarget.Rendering += StartRoamOnFirstFrame;
    }

    private void StartRoamOnFirstFrame(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs rendering)
        {
            return;
        }

        CompositionTarget.Rendering -= StartRoamOnFirstFrame;
        roamStarted = rendering.RenderingTime;
        lastRenderTime = rendering.RenderingTime;
        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private TimeSpan NextRoamInterval()
    {
#pragma warning disable CA5394 // Randomness controls visual roaming only and has no security purpose.
        return TimeSpan.FromSeconds(random.Next(28, 58));
#pragma warning restore CA5394
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ContextMenuRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        settings.Scale = Math.Clamp(settings.Scale + (e.Delta > 0 ? 0.1 : -0.1), PetSettings.MinimumScale, PetSettings.MaximumScale);
        ApplySettings();
        settingsStore.Save(settings);
        e.Handled = true;
    }

    private void SavePosition()
    {
        settings.WindowLeft = Left;
        settings.WindowTop = Top;
        settingsStore.Save(settings);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        roamTimer.Stop();
        roamTimer.Tick -= OnRoamTimer;
        CompositionTarget.Rendering -= StartRoamOnFirstFrame;
        CompositionTarget.Rendering -= OnRendering;
        source?.RemoveHook(WindowProcedure);
        player.FrameChanged -= OnFrameChanged;
        player.PlaybackCompleted -= OnPlaybackCompleted;
    }
}
