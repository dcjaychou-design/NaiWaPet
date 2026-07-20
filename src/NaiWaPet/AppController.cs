using System.IO;
using System.Windows;
using System.Security;
using NaiWaPet.Core.Configuration;
using NaiWaPet.Services;

namespace NaiWaPet;

internal sealed class AppController : IDisposable
{
    private readonly SettingsStore settingsStore = new();
    private PetSettings settings = new();
    private SpriteAnimationPlayer? player;
    private PetWindow? petWindow;
    private SettingsWindow? settingsWindow;
    private TrayIconService? trayIcon;
    private bool disposed;
    private bool exiting;

    public void Start()
    {
        settings = settingsStore.Load();
        settings.StartWithWindows = StartupManager.IsEnabled();
        player = new SpriteAnimationPlayer { SoundEnabled = settings.SoundEnabled };
        petWindow = new PetWindow(player, settings, settingsStore);
        petWindow.ContextMenuRequested += OnContextMenuRequested;
        petWindow.Show();

        trayIcon = new TrayIconService();
        trayIcon.PlayRequested += OnPlayRequested;
        trayIcon.ToggleVisibilityRequested += OnToggleVisibilityRequested;
        trayIcon.SettingsRequested += OnSettingsRequested;
        trayIcon.ResetPositionRequested += OnResetPositionRequested;
        trayIcon.ExitRequested += OnExitRequested;
        trayIcon.ToggleRequested += OnToggleRequested;
        trayIcon.Update(settings);
        settingsStore.Save(settings);
    }

    public void ShowPet()
    {
        if (petWindow is null)
        {
            return;
        }

        petWindow.Show();
        petWindow.ClampToWorkArea();
        player?.PlayLaugh();
    }

    public void Exit()
    {
        if (exiting)
        {
            return;
        }

        exiting = true;
        if (petWindow is not null)
        {
            settings.WindowLeft = petWindow.Left;
            settings.WindowTop = petWindow.Top;
        }

        settingsStore.Save(settings);
        settingsWindow?.Close();
        petWindow?.Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void OnPlayRequested(object? sender, EventArgs e)
    {
        ShowPetWithoutAnimation();
        player?.PlayLaugh();
    }

    private void OnToggleVisibilityRequested(object? sender, EventArgs e)
    {
        if (petWindow?.IsVisible == true)
        {
            petWindow.Hide();
        }
        else
        {
            ShowPetWithoutAnimation();
        }
    }

    private void OnSettingsRequested(object? sender, EventArgs e) => ShowSettings();

    private void OnResetPositionRequested(object? sender, EventArgs e)
    {
        ShowPetWithoutAnimation();
        petWindow?.ResetPosition();
    }

    private void OnExitRequested(object? sender, EventArgs e) => Exit();

    private void OnContextMenuRequested(object? sender, EventArgs e) => trayIcon?.ShowMenu();

    private void OnToggleRequested(object? sender, SettingToggleEventArgs e)
    {
        switch (e.Setting)
        {
            case PetSetting.SoundEnabled:
                settings.SoundEnabled = e.Enabled;
                break;
            case PetSetting.RoamingEnabled:
                settings.RoamingEnabled = e.Enabled;
                break;
            case PetSetting.AlwaysOnTop:
                settings.AlwaysOnTop = e.Enabled;
                break;
            case PetSetting.ClickThrough:
                settings.ClickThrough = e.Enabled;
                break;
            case PetSetting.StartWithWindows:
                settings.StartWithWindows = e.Enabled;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e), e.Setting, "Unknown pet setting.");
        }

        ApplySettings();
    }

    private void ShowSettings()
    {
        if (settingsWindow is not null)
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow = new SettingsWindow(settings);
        settingsWindow.SettingsChanged += OnSettingsChanged;
        settingsWindow.ResetPositionRequested += OnResetPositionRequested;
        settingsWindow.PlayRequested += OnPlayRequested;
        settingsWindow.Closed += OnSettingsWindowClosed;
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => ApplySettings();

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (settingsWindow is null)
        {
            return;
        }

        settingsWindow.SettingsChanged -= OnSettingsChanged;
        settingsWindow.ResetPositionRequested -= OnResetPositionRequested;
        settingsWindow.PlayRequested -= OnPlayRequested;
        settingsWindow.Closed -= OnSettingsWindowClosed;
        settingsWindow = null;
    }

    private void ApplySettings()
    {
        try
        {
            if (StartupManager.IsEnabled() != settings.StartWithWindows)
            {
                StartupManager.SetEnabled(settings.StartWithWindows);
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or SecurityException)
        {
            settings.StartWithWindows = StartupManager.IsEnabled();
            System.Windows.MessageBox.Show(
                $"无法修改开机启动设置。\n\n{exception.Message}",
                "奶蛙桌宠",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        petWindow?.ApplySettings();
        trayIcon?.Update(settings);
        settingsWindow?.RefreshFromSettings();
        settingsStore.Save(settings);
    }

    private void ShowPetWithoutAnimation()
    {
        petWindow?.Show();
        petWindow?.ClampToWorkArea();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (petWindow is not null)
        {
            petWindow.ContextMenuRequested -= OnContextMenuRequested;
        }

        trayIcon?.Dispose();
        player?.Dispose();
    }
}
