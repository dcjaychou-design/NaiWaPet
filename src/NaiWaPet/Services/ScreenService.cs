using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using NaiWaPet.Core.Physics;
using NaiWaPet.Interop;

namespace NaiWaPet.Services;

internal static class ScreenService
{
    private const uint MonitorDefaultToNearest = 2;

    public static RectD GetWorkArea(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var monitor = NativeMethods.MonitorFromWindow(handle, MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { Size = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            var fallback = SystemParameters.WorkArea;
            return new RectD(fallback.Left, fallback.Top, fallback.Right, fallback.Bottom);
        }

        var topLeft = window.PointFromScreen(new System.Windows.Point(info.Work.Left, info.Work.Top));
        var bottomRight = window.PointFromScreen(new System.Windows.Point(info.Work.Right, info.Work.Bottom));
        return new RectD(
            window.Left + topLeft.X,
            window.Top + topLeft.Y,
            window.Left + bottomRight.X,
            window.Top + bottomRight.Y);
    }

    public static PointD GetCursorPosition(Window window)
    {
        if (!NativeMethods.GetCursorPos(out var point))
        {
            return new PointD(window.Left, window.Top);
        }

        var local = window.PointFromScreen(new System.Windows.Point(point.X, point.Y));
        return new PointD(window.Left + local.X, window.Top + local.Y);
    }

    public static PointD Clamp(PointD position, SizeD size, RectD workArea)
    {
        var right = Math.Max(workArea.Left, workArea.Right - size.Width);
        var bottom = Math.Max(workArea.Top, workArea.Bottom - size.Height);
        return new PointD(
            Math.Clamp(position.X, workArea.Left, right),
            Math.Clamp(position.Y, workArea.Top, bottom));
    }
}
