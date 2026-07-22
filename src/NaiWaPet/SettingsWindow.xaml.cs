using System.Windows;
using System.Windows.Controls;
using NaiWaPet.Core.Configuration;

namespace NaiWaPet;

internal sealed partial class SettingsWindow : Window
{
    private readonly PetSettings settings;
    private bool initialized;

    public SettingsWindow(PetSettings settings)
    {
        InitializeComponent();
        this.settings = settings;
        ScaleSlider.Value = settings.Scale;
        SoundCheckBox.IsChecked = settings.SoundEnabled;
        RoamingCheckBox.IsChecked = settings.RoamingEnabled;
        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        ClickThroughCheckBox.IsChecked = settings.ClickThrough;
        StartupCheckBox.IsChecked = settings.StartWithWindows;
        UpdateScaleLabel();
        initialized = true;
    }

    public event EventHandler? SettingsChanged;

    public event EventHandler? ResetPositionRequested;

    public event EventHandler? PlayRequested;

    public event EventHandler? NoticesRequested;

    public void RefreshFromSettings()
    {
        initialized = false;
        ScaleSlider.Value = settings.Scale;
        SoundCheckBox.IsChecked = settings.SoundEnabled;
        RoamingCheckBox.IsChecked = settings.RoamingEnabled;
        AlwaysOnTopCheckBox.IsChecked = settings.AlwaysOnTop;
        ClickThroughCheckBox.IsChecked = settings.ClickThrough;
        StartupCheckBox.IsChecked = settings.StartWithWindows;
        UpdateScaleLabel();
        initialized = true;
    }

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateScaleLabel();
        ApplyControls();
    }

    private void OnOptionChanged(object sender, RoutedEventArgs e) => ApplyControls();

    private void ApplyControls()
    {
        if (!initialized)
        {
            return;
        }

        settings.Scale = ScaleSlider.Value;
        settings.SoundEnabled = SoundCheckBox.IsChecked == true;
        settings.RoamingEnabled = RoamingCheckBox.IsChecked == true;
        settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        settings.ClickThrough = ClickThroughCheckBox.IsChecked == true;
        settings.StartWithWindows = StartupCheckBox.IsChecked == true;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateScaleLabel()
    {
        if (ScaleValueText is not null && ScaleSlider is not null)
        {
            ScaleValueText.Text = $"{ScaleSlider.Value:P0}";
        }
    }

    private void OnResetPosition(object sender, RoutedEventArgs e) => ResetPositionRequested?.Invoke(this, EventArgs.Empty);

    private void OnPlay(object sender, RoutedEventArgs e) => PlayRequested?.Invoke(this, EventArgs.Empty);

    private void OnNotices(object sender, RoutedEventArgs e) => NoticesRequested?.Invoke(this, EventArgs.Empty);

    private void OnDone(object sender, RoutedEventArgs e) => Close();
}
