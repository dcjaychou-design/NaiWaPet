namespace NaiWaPet.Core.Configuration;

public sealed class PetSettings
{
    public const int CurrentSchemaVersion = 2;
    public const double DefaultScale = 0.8;
    public const double MinimumScale = 0.5;
    public const double MaximumScale = 1.8;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public double Scale { get; set; } = DefaultScale;

    public bool AlwaysOnTop { get; set; } = true;

    public bool SoundEnabled { get; set; } = true;

    public bool RoamingEnabled { get; set; } = true;

    public bool ClickThrough { get; set; }

    public bool StartWithWindows { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public void Normalize()
    {
        if (SchemaVersion < 2)
        {
            // Version 2 changed the requested first-run experience. Preserve a
            // custom size, but migrate the old 100% default and muted default.
            if (Math.Abs(Scale - 1.0) < 0.0001)
            {
                Scale = DefaultScale;
            }

            SoundEnabled = true;
        }

        SchemaVersion = CurrentSchemaVersion;
        Scale = Math.Clamp(double.IsFinite(Scale) ? Scale : DefaultScale, MinimumScale, MaximumScale);
        if (WindowLeft is { } left && !double.IsFinite(left))
        {
            WindowLeft = null;
        }

        if (WindowTop is { } top && !double.IsFinite(top))
        {
            WindowTop = null;
        }
    }
}
