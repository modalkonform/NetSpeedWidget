using System.Runtime.InteropServices;

namespace NetSpeedWidget;

/// <summary>Windows API calls the widget needs.</summary>
static class NativeMethods
{
  public const int WS_EX_TOPMOST = 0x8;
  public const int WS_EX_TOOLWINDOW = 0x80;
  public const int WS_EX_LAYERED = 0x80000;
  public const int WS_EX_NOACTIVATE = 0x08000000;

  public const uint EVENT_SYSTEM_FOREGROUND = 3;
  public const uint WINEVENT_OUTOFCONTEXT = 0;

  public static readonly IntPtr HWND_TOPMOST = new(-1);
  public const uint SWP_NOSIZE = 0x1;
  public const uint SWP_NOMOVE = 0x2;
  public const uint SWP_NOACTIVATE = 0x10;

  public const int ULW_ALPHA = 2;
  public const byte AC_SRC_ALPHA = 1;

  // SHQueryUserNotificationState results that mean a fullscreen app or presentation is up.
  const int QUNS_BUSY = 2;
  const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
  const int QUNS_PRESENTATION_MODE = 4;

  [StructLayout(LayoutKind.Sequential)]
  public struct RECT
  {
    public int Left, Top, Right, Bottom;

    public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct POINT
  {
    public int X, Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SIZE
  {
    public int Width, Height;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct BLENDFUNCTION
  {
    public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
  }

  public delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr hwnd,
    int idObject, int idChild, uint threadId, uint timeMs);

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  public static extern IntPtr FindWindow(string className, string? windowName);

  [DllImport("user32.dll", CharSet = CharSet.Unicode)]
  public static extern IntPtr FindWindowEx(
    IntPtr parent, IntPtr childAfter, string className, string? windowName);

  [DllImport("user32.dll")]
  public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

  [DllImport("user32.dll")]
  public static extern bool SetWindowPos(
    IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

  [DllImport("user32.dll")]
  public static extern bool SetForegroundWindow(IntPtr hwnd);

  [DllImport("user32.dll")]
  public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module,
    WinEventProc callback, uint processId, uint threadId, uint flags);

  [DllImport("user32.dll")]
  public static extern bool UnhookWinEvent(IntPtr hook);

  [DllImport("user32.dll")]
  public static extern IntPtr GetDC(IntPtr hwnd);

  [DllImport("user32.dll")]
  public static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);

  [DllImport("user32.dll")]
  public static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr screenDc, ref POINT position,
    ref SIZE size, IntPtr sourceDc, ref POINT sourcePosition, int colorKey,
    ref BLENDFUNCTION blend, int flags);

  [DllImport("gdi32.dll")]
  public static extern IntPtr CreateCompatibleDC(IntPtr dc);

  [DllImport("gdi32.dll")]
  public static extern bool DeleteDC(IntPtr dc);

  [DllImport("gdi32.dll")]
  public static extern IntPtr SelectObject(IntPtr dc, IntPtr gdiObject);

  [DllImport("gdi32.dll")]
  public static extern bool DeleteObject(IntPtr gdiObject);

  [DllImport("iphlpapi.dll")]
  public static extern int GetBestInterface(uint destinationAddress, out uint interfaceIndex);

  [DllImport("shell32.dll")]
  static extern int SHQueryUserNotificationState(out int state);

  /// <summary>Whether a fullscreen app (video, game, presentation) is in front.</summary>
  public static bool IsFullscreenAppRunning()
  {
    return SHQueryUserNotificationState(out var state) == 0
      && state is QUNS_BUSY or QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE;
  }
}
