using System.Buffers.Binary;
using NaiWaPet.Core.Animation;
using NaiWaPet.Core.Configuration;
using NaiWaPet.Core.Physics;

var tests = new (string Name, Action Run)[]
{
    ("manifest validation and frame lookup", ManifestLookup),
    ("manifest rejects gaps", ManifestRejectsGaps),
    ("manifest rejects invalid source and oversized atlases", ManifestRejectsInvalidMetadata),
    ("manifest frame lookup checks bounds", ManifestLookupBounds),
    ("hit mask loading and bounds", HitMaskRoundTrip),
    ("hit mask rejects malformed metadata", HitMaskRejectsMalformedMetadata),
    ("settings normalization", SettingsNormalization),
    ("motion physics gravity and floor", MotionPhysicsBehavior),
    ("motion physics handles constrained work areas", MotionPhysicsConstrainedWorkArea),
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
#pragma warning disable CA1031 // The runner must report every unexpected test failure and continue.
    catch (Exception exception)
    {
        failures.Add(test.Name);
        Console.Error.WriteLine($"FAIL  {test.Name}: {exception.Message}");
    }
#pragma warning restore CA1031
}

Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} tests passed.");
return failures.Count == 0 ? 0 : 1;

static AnimationManifest ValidManifest(
    int frameWidth = 10,
    int frameHeight = 20,
    int totalFrames = 70,
    int columns = 8,
    int rows = 8,
    IReadOnlyList<AtlasDefinition>? atlases = null,
    AnimationSource? source = null) => new()
{
    SchemaVersion = 1,
    FrameWidth = frameWidth,
    FrameHeight = frameHeight,
    FramesPerSecond = 30,
    TotalFrames = totalFrames,
    Columns = columns,
    Rows = rows,
    IdleFrame = 0,
    HitMaskFile = "hitmask.bin",
    Atlases = atlases ??
    [
        new AtlasDefinition { File = "first.png", FirstFrame = 0, FrameCount = 64 },
        new AtlasDefinition { File = "second.png", FirstFrame = 64, FrameCount = 6 },
    ],
    Source = source ?? new AnimationSource
    {
        File = "assets/source/Naiwa.mp4",
        Sha256 = new string('a', 64),
    },
};

static void ManifestLookup()
{
    var manifest = ValidManifest();
    manifest.Validate();
    Equal(new FrameLocation(0, 63, 7, 7), manifest.LocateFrame(63));
    Equal(new FrameLocation(1, 0, 0, 0), manifest.LocateFrame(64));
    Equal(TimeSpan.FromSeconds(70.0 / 30.0), manifest.Duration);
}

static void ManifestRejectsGaps()
{
    var manifest = ValidManifest(
        atlases: [new AtlasDefinition { File = "broken.png", FirstFrame = 1, FrameCount = 64 }]);
    Throws<InvalidDataException>(manifest.Validate);
}

static void ManifestRejectsInvalidMetadata()
{
    var missingSource = ValidManifest(source: new AnimationSource { File = string.Empty, Sha256 = "invalid" });
    Throws<InvalidDataException>(missingSource.Validate);

    var oversized = ValidManifest(
        frameWidth: int.MaxValue,
        totalFrames: 1,
        columns: 2,
        rows: 1,
        atlases: [new AtlasDefinition { File = "oversized.png", FirstFrame = 0, FrameCount = 1 }]);
    Throws<InvalidDataException>(oversized.Validate);
}

static void ManifestLookupBounds()
{
    var manifest = ValidManifest();
    manifest.Validate();
    Throws<ArgumentOutOfRangeException>(() => manifest.LocateFrame(-1));
    Throws<ArgumentOutOfRangeException>(() => manifest.LocateFrame(manifest.TotalFrames));
}

static void HitMaskRoundTrip()
{
    const int width = 3;
    const int height = 2;
    const int frameCount = 2;
    const int bytesPerFrame = 1;
    var bytes = new byte[24 + frameCount * bytesPerFrame];
    "NWMK"u8.CopyTo(bytes);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), 1);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), width);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), height);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(16), frameCount);
    BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20), bytesPerFrame);
    bytes[24] = 0b0010_0001;
    bytes[25] = 0b0000_0100;

    using var stream = new MemoryStream(bytes);
    var mask = HitMask.Load(stream);
    True(mask.IsOpaque(0, 0, 0));
    True(mask.IsOpaque(0, 2, 1));
    True(mask.IsOpaque(1, 2, 0));
    False(mask.IsOpaque(0, 1, 0));
    False(mask.IsOpaque(2, 0, 0));
}

static void HitMaskRejectsMalformedMetadata()
{
    var overflow = new byte[24];
    "NWMK"u8.CopyTo(overflow);
    BinaryPrimitives.WriteInt32LittleEndian(overflow.AsSpan(4), 1);
    BinaryPrimitives.WriteInt32LittleEndian(overflow.AsSpan(8), int.MaxValue);
    BinaryPrimitives.WriteInt32LittleEndian(overflow.AsSpan(12), int.MaxValue);
    BinaryPrimitives.WriteInt32LittleEndian(overflow.AsSpan(16), 1);
    BinaryPrimitives.WriteInt32LittleEndian(overflow.AsSpan(20), 1);
    Throws<InvalidDataException>(() =>
    {
        using var stream = new MemoryStream(overflow);
        HitMask.Load(stream);
    });

    var truncated = new byte[25];
    "NWMK"u8.CopyTo(truncated);
    BinaryPrimitives.WriteInt32LittleEndian(truncated.AsSpan(4), 1);
    BinaryPrimitives.WriteInt32LittleEndian(truncated.AsSpan(8), 3);
    BinaryPrimitives.WriteInt32LittleEndian(truncated.AsSpan(12), 2);
    BinaryPrimitives.WriteInt32LittleEndian(truncated.AsSpan(16), 2);
    BinaryPrimitives.WriteInt32LittleEndian(truncated.AsSpan(20), 1);
    Throws<InvalidDataException>(() =>
    {
        using var stream = new MemoryStream(truncated);
        HitMask.Load(stream);
    });
}

static void SettingsNormalization()
{
    var defaults = new PetSettings();
    True(defaults.SoundEnabled);
    True(defaults.RoamingEnabled);
    Equal(PetSettings.DefaultScale, defaults.Scale);

    var migrated = new PetSettings
    {
        SchemaVersion = 1,
        Scale = 1.0,
        SoundEnabled = false,
    };
    migrated.Normalize();
    Equal(PetSettings.CurrentSchemaVersion, migrated.SchemaVersion);
    Equal(PetSettings.DefaultScale, migrated.Scale);
    True(migrated.SoundEnabled);

    var customScale = new PetSettings
    {
        SchemaVersion = 1,
        Scale = 1.3,
        SoundEnabled = false,
    };
    customScale.Normalize();
    Equal(1.3, customScale.Scale);
    True(customScale.SoundEnabled);

    var settings = new PetSettings
    {
        SchemaVersion = -1,
        Scale = 99,
        WindowLeft = double.NaN,
        WindowTop = double.PositiveInfinity,
    };
    settings.Normalize();
    Equal(PetSettings.CurrentSchemaVersion, settings.SchemaVersion);
    Equal(PetSettings.MaximumScale, settings.Scale);
    Equal<double?>(null, settings.WindowLeft);
    Equal<double?>(null, settings.WindowTop);
}

static void MotionPhysicsBehavior()
{
    var work = new RectD(0, 0, 1000, 700);
    var size = new SizeD(200, 300);
    var falling = MotionPhysics.Step(new PointD(100, 100), new PointD(0, 0), 0.05, work, size);
    True(falling.Velocity.Y > 0);
    True(falling.Position.Y > 100);

    var settled = MotionPhysics.Step(new PointD(100, 400), new PointD(10, 10), 0.05, work, size);
    Equal(400.0, settled.Position.Y);
    True(settled.Settled);
}

static void MotionPhysicsConstrainedWorkArea()
{
    var work = new RectD(0, 0, 100, 100);
    var oversized = MotionPhysics.Step(new PointD(50, 50), new PointD(0, 0), 0.05, work, new SizeD(200, 200));
    Equal(0.0, oversized.Position.X);
    Equal(0.0, oversized.Position.Y);
    True(oversized.Settled);

    var invalidElapsed = MotionPhysics.Step(
        new PointD(20, 20),
        new PointD(10, 10),
        double.NaN,
        new RectD(0, 0, 1000, 700),
        new SizeD(200, 300));
    Equal(new PointD(20, 20), invalidElapsed.Position);
    Equal(new PointD(10, 10), invalidElapsed.Velocity);
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void True(bool value)
{
    if (!value)
    {
        throw new InvalidOperationException("Expected true.");
    }
}

static void False(bool value) => True(!value);

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}
