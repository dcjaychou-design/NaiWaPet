namespace NaiWaPet.Core.Animation;

public sealed class AnimationManifest
{
    public int SchemaVersion { get; init; }

    public int FrameWidth { get; init; }

    public int FrameHeight { get; init; }

    public int FramesPerSecond { get; init; }

    public int TotalFrames { get; init; }

    public int Columns { get; init; }

    public int Rows { get; init; }

    public int IdleFrame { get; init; }

    public string HitMaskFile { get; init; } = string.Empty;

    public IReadOnlyList<AtlasDefinition> Atlases { get; init; } = [];

    public AnimationSource Source { get; init; } = new();

    public TimeSpan Duration => TimeSpan.FromSeconds((double)TotalFrames / FramesPerSecond);

    public void Validate()
    {
        if (SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported animation schema: {SchemaVersion}.");
        }

        if (FrameWidth <= 0 || FrameHeight <= 0 || FramesPerSecond is < 1 or > 120 || TotalFrames <= 0)
        {
            throw new InvalidDataException("Animation dimensions, frame rate, or frame count are invalid.");
        }

        if (Columns <= 0 ||
            Rows <= 0 ||
            IdleFrame < 0 ||
            IdleFrame >= TotalFrames ||
            string.IsNullOrWhiteSpace(HitMaskFile) ||
            Atlases is null)
        {
            throw new InvalidDataException("Animation layout metadata is invalid.");
        }

        var atlasCapacity = (long)Columns * Rows;
        var atlasPixelWidth = (long)FrameWidth * Columns;
        var atlasPixelHeight = (long)FrameHeight * Rows;
        if (atlasCapacity > int.MaxValue || atlasPixelWidth > int.MaxValue || atlasPixelHeight > int.MaxValue)
        {
            throw new InvalidDataException("Animation atlas dimensions are too large.");
        }

        if (Source is null ||
            string.IsNullOrWhiteSpace(Source.File) ||
            string.IsNullOrWhiteSpace(Source.Sha256) ||
            Source.Sha256.Length != 64 ||
            !Source.Sha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("Animation source metadata is invalid.");
        }

        long expectedFrame = 0;
        foreach (var atlas in Atlases)
        {
            if (atlas is null ||
                atlas.FirstFrame != expectedFrame ||
                atlas.FrameCount <= 0 ||
                atlas.FrameCount > atlasCapacity ||
                string.IsNullOrWhiteSpace(atlas.File))
            {
                throw new InvalidDataException("Animation atlas sequence is not contiguous.");
            }

            expectedFrame += atlas.FrameCount;
        }

        if (expectedFrame != TotalFrames)
        {
            throw new InvalidDataException("Animation atlas frame counts do not match the manifest.");
        }
    }

    public FrameLocation LocateFrame(int frameIndex)
    {
        if ((uint)frameIndex >= (uint)TotalFrames)
        {
            throw new ArgumentOutOfRangeException(nameof(frameIndex));
        }

        for (var atlasIndex = 0; atlasIndex < Atlases.Count; atlasIndex++)
        {
            var atlas = Atlases[atlasIndex];
            if (frameIndex < (long)atlas.FirstFrame + atlas.FrameCount)
            {
                var localFrame = frameIndex - atlas.FirstFrame;
                return new FrameLocation(
                    atlasIndex,
                    localFrame,
                    localFrame % Columns,
                    localFrame / Columns);
            }
        }

        throw new InvalidDataException("Frame was not found in any atlas.");
    }
}

public sealed class AtlasDefinition
{
    public string File { get; init; } = string.Empty;

    public int FirstFrame { get; init; }

    public int FrameCount { get; init; }
}

public sealed class AnimationSource
{
    public string File { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;
}

public readonly record struct FrameLocation(int AtlasIndex, int LocalFrame, int Column, int Row);
