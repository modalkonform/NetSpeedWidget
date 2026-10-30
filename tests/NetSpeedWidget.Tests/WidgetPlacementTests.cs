using System.Drawing;

namespace NetSpeedWidget.Tests;

public class WidgetPlacementTests
{
  [Fact]
  public void SitsLeftOfTrayWithGap_AndSpansTaskbarHeight()
  {
    var taskbar = new Rectangle(0, 2112, 3840, 48);
    var tray = new Rectangle(3567, 2112, 273, 48);

    var bounds = WidgetPlacement.LeftOfTray(taskbar, tray, width: 110, gap: 8);

    Assert.Equal(new Rectangle(3449, 2112, 110, 48), bounds);
  }
}
