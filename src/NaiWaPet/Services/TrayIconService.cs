using System.Drawing;
using System.Windows.Forms;
using NaiWaPet.Core.Configuration;

namespace NaiWaPet.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon notifyIcon;
    private readonly ContextMenuStrip menu;
    private readonly Icon icon;
    private readonly ToolStripMenuItem soundItem;
    private readonly ToolStripMenuItem roamingItem;
    private readonly ToolStripMenuItem topmostItem;
    private readonly ToolStripMenuItem clickThroughItem;
    private readonly ToolStripMenuItem startupItem;

#pragma warning disable CA2000 // ContextMenuStrip owns and disposes every item added to its Items collection.
    public TrayIconService()
    {
        menu = new ContextMenuStrip();
        var title = new ToolStripMenuItem("奶蛙桌宠") { Enabled = false };
        var playItem = new ToolStripMenuItem("开始捧腹大笑", null, (_, _) => PlayRequested?.Invoke(this, EventArgs.Empty));
        var visibilityItem = new ToolStripMenuItem("显示 / 隐藏奶蛙", null, (_, _) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty));
        soundItem = CreateToggle("播放笑声", PetSetting.SoundEnabled);
        roamingItem = CreateToggle("桌面漫游", PetSetting.RoamingEnabled);
        topmostItem = CreateToggle("始终置顶", PetSetting.AlwaysOnTop);
        clickThroughItem = CreateToggle("鼠标穿透", PetSetting.ClickThrough);
        startupItem = CreateToggle("开机启动", PetSetting.StartWithWindows);
        var settingsItem = new ToolStripMenuItem("设置…", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
        var resetItem = new ToolStripMenuItem("重置位置", null, (_, _) => ResetPositionRequested?.Invoke(this, EventArgs.Empty));
        var noticesItem = new ToolStripMenuItem("关于与开源许可…", null, (_, _) => NoticesRequested?.Invoke(this, EventArgs.Empty));
        var exitItem = new ToolStripMenuItem("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        menu.Items.AddRange([
            title,
            new ToolStripSeparator(),
            playItem,
            visibilityItem,
            new ToolStripSeparator(),
            soundItem,
            roamingItem,
            topmostItem,
            clickThroughItem,
            startupItem,
            new ToolStripSeparator(),
            settingsItem,
            resetItem,
            noticesItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        icon = LoadIcon();
#pragma warning disable CA1303 // This utility intentionally ships with a Simplified Chinese interface.
        notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "奶蛙桌宠（声音默认开启）",
            ContextMenuStrip = menu,
            Visible = true,
        };
#pragma warning restore CA1303
        notifyIcon.DoubleClick += OnDoubleClick;
    }
#pragma warning restore CA2000

    public event EventHandler? PlayRequested;

    public event EventHandler? ToggleVisibilityRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ResetPositionRequested;

    public event EventHandler? NoticesRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler<SettingToggleEventArgs>? ToggleRequested;

    public void Update(PetSettings settings)
    {
        soundItem.Checked = settings.SoundEnabled;
        roamingItem.Checked = settings.RoamingEnabled;
        topmostItem.Checked = settings.AlwaysOnTop;
        clickThroughItem.Checked = settings.ClickThrough;
        startupItem.Checked = settings.StartWithWindows;
#pragma warning disable CA1303 // This utility intentionally ships with a Simplified Chinese interface.
        notifyIcon.Text = settings.SoundEnabled ? "奶蛙桌宠（声音已开启）" : "奶蛙桌宠（声音已关闭）";
#pragma warning restore CA1303
    }

    public void ShowMenu() => menu.Show(Cursor.Position);

    private ToolStripMenuItem CreateToggle(string text, PetSetting setting)
    {
        var item = new ToolStripMenuItem(text) { CheckOnClick = false };
        item.Click += (_, _) => ToggleRequested?.Invoke(this, new SettingToggleEventArgs(setting, !item.Checked));
        return item;
    }

    private static Icon LoadIcon()
    {
        var executable = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executable))
        {
            using var associated = Icon.ExtractAssociatedIcon(executable);
            if (associated is not null)
            {
                return (Icon)associated.Clone();
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    private void OnDoubleClick(object? sender, EventArgs e) => ToggleVisibilityRequested?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
        notifyIcon.DoubleClick -= OnDoubleClick;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        menu.Dispose();
        soundItem.Dispose();
        roamingItem.Dispose();
        topmostItem.Dispose();
        clickThroughItem.Dispose();
        startupItem.Dispose();
        icon.Dispose();
    }
}

internal enum PetSetting
{
    SoundEnabled,
    RoamingEnabled,
    AlwaysOnTop,
    ClickThrough,
    StartWithWindows,
}

internal sealed class SettingToggleEventArgs(PetSetting setting, bool enabled) : EventArgs
{
    public PetSetting Setting { get; } = setting;

    public bool Enabled { get; } = enabled;
}
