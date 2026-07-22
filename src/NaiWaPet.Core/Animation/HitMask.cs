using System.Buffers.Binary;

namespace NaiWaPet.Core.Animation;

public sealed class HitMask
{
    private const int HeaderSize = 24;
    private readonly byte[] data;

    private HitMask(int width, int height, int frameCount, int bytesPerFrame, byte[] data)
    {
        Width = width;
        Height = height;
        FrameCount = frameCount;
        BytesPerFrame = bytesPerFrame;
        this.data = data;
    }

    public int Width { get; }

    public int Height { get; }

    public int FrameCount { get; }

    public int BytesPerFrame { get; }

    public static HitMask Load(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        using var memory = new MemoryStream();
        source.CopyTo(memory);
        var bytes = memory.ToArray();
        if (bytes.Length < HeaderSize || !bytes.AsSpan(0, 4).SequenceEqual("NWMK"u8))
        {
            throw new InvalidDataException("Hit-mask header is invalid.");
        }

        var version = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4));
        var width = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4));
        var height = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4));
        var frameCount = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16, 4));
        var bytesPerFrame = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(20, 4));
        var expectedBytesPerFrame = ((long)width * height + 7) / 8;
        if (version != 1 ||
            width <= 0 ||
            height <= 0 ||
            frameCount <= 0 ||
            expectedBytesPerFrame > int.MaxValue ||
            bytesPerFrame != expectedBytesPerFrame)
        {
            throw new InvalidDataException("Hit-mask metadata is invalid.");
        }

        var expectedLength = HeaderSize + (long)frameCount * bytesPerFrame;
        if (expectedLength > int.MaxValue || bytes.Length != expectedLength)
        {
            throw new InvalidDataException("Hit-mask payload length is invalid.");
        }

        return new HitMask(width, height, frameCount, bytesPerFrame, bytes[HeaderSize..]);
    }

    public bool IsOpaque(int frame, int x, int y)
    {
        if ((uint)frame >= (uint)FrameCount || (uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return false;
        }

        var pixel = (long)y * Width + x;
        var offset = (long)frame * BytesPerFrame + pixel / 8;
        return (data[(int)offset] & (1 << (int)(pixel & 7))) != 0;
    }
}
