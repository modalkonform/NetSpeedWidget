using Microsoft.Win32;

namespace NetSpeedWidget;

/// <summary>Where the Windows taskbar is and how it looks.</summary>
static class Taskbar
{
  /// <summary>Finds the main taskbar and its tray area, in screen pixels.</summary>
  public static bool TryGetBounds(out Rectangle taskbar, out Rectangle tray)
  {
    taskbar = tray = Rectangle.Empty;
    var taskbarHandle = NativeMethods.FindWindow("Shell_TrayWnd", null);
    if (taskbarHandle == IntPtr.Zero)
      return false;

    var trayHandle = NativeMethods.FindWindowEx(taskbarHandle, IntPtr.Zero, "TrayNotifyWnd", null);
    if (trayHandle == IntPtr.Zero
      || !NativeMethods.GetWindowRect(taskbarHandle, out var taskbarRect)
      || !NativeMethods.GetWindowRect(trayHandle, out var trayRect))
      return false;

    taskbar = taskbarRect.ToRectangle();
    tray = trayRect.ToRectangle();
    return true;
  }

  /// <summary>Whether Windows is set to a light taskbar.</summary>
  public static bool UsesLightTheme()
  {
    using var key = Registry.CurrentUser.OpenSubKey(
      @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
    return key?.GetValue("SystemUsesLightTheme") is 1;
  }
}
