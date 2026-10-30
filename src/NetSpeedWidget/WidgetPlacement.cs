namespace NetSpeedWidget;

/// <summary>Works out where the widget goes on the taskbar.</summary>
public static class WidgetPlacement
{
  /// <summary>Bounds just left of the tray, as tall as the taskbar.</summary>
  public static Rectangle LeftOfTray(Rectangle taskbar, Rectangle tray, int width, int gap)
  {
    return new Rectangle(tray.Left - gap - width, taskbar.Top, width, taskbar.Height);
  }
}
