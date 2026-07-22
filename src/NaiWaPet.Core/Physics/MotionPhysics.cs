namespace NaiWaPet.Core.Physics;

public readonly record struct PointD(double X, double Y)
{
    public static PointD operator +(PointD left, PointD right) => new(left.X + right.X, left.Y + right.Y);

    public static PointD operator *(PointD value, double factor) => new(value.X * factor, value.Y * factor);

    public static PointD Add(PointD left, PointD right) => left + right;

    public static PointD Multiply(PointD value, double factor) => value * factor;
}

public readonly record struct SizeD(double Width, double Height);

public readonly record struct RectD(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;

    public double Height => Bottom - Top;
}

public readonly record struct MotionResult(PointD Position, PointD Velocity, bool Settled);

public static class MotionPhysics
{
    public const double Gravity = 1800.0;
    private const double Bounce = 0.34;
    private const double FloorFriction = 0.78;

    public static MotionResult Step(PointD position, PointD velocity, double elapsedSeconds, RectD workArea, SizeD windowSize)
    {
        var dt = double.IsFinite(elapsedSeconds) ? Math.Clamp(elapsedSeconds, 0.0, 0.05) : 0.0;
        var nextVelocity = new PointD(velocity.X, velocity.Y + Gravity * dt);
        var nextPosition = position + nextVelocity * dt;
        var floor = Math.Max(workArea.Top, workArea.Bottom - Math.Max(0, windowSize.Height));
        var right = Math.Max(workArea.Left, workArea.Right - Math.Max(0, windowSize.Width));

        if (nextPosition.X < workArea.Left)
        {
            nextPosition = nextPosition with { X = workArea.Left };
            nextVelocity = nextVelocity with { X = Math.Abs(nextVelocity.X) * Bounce };
        }
        else if (nextPosition.X > right)
        {
            nextPosition = nextPosition with { X = right };
            nextVelocity = nextVelocity with { X = -Math.Abs(nextVelocity.X) * Bounce };
        }

        var settled = false;
        if (nextPosition.Y < workArea.Top)
        {
            nextPosition = nextPosition with { Y = workArea.Top };
            nextVelocity = nextVelocity with { Y = Math.Abs(nextVelocity.Y) * Bounce };
        }
        else if (nextPosition.Y >= floor)
        {
            nextPosition = nextPosition with { Y = floor };
            nextVelocity = new PointD(nextVelocity.X * FloorFriction, -Math.Abs(nextVelocity.Y) * Bounce);
            if (Math.Abs(nextVelocity.X) < 24 && Math.Abs(nextVelocity.Y) < 80)
            {
                nextVelocity = new PointD(0, 0);
                settled = true;
            }
        }

        return new MotionResult(nextPosition, nextVelocity, settled);
    }
}
