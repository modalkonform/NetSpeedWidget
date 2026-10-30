namespace NetSpeedWidget.Tests;

public class SettingsTests
{
  [Fact]
  public void LargerFont_GrowsByOnePixel()
  {
    Assert.Equal(13, new Settings(FontSize: 12).WithLargerFont().FontSize);
  }

  [Fact]
  public void LargerFont_StopsAt18_SoBothLinesFitTheTaskbar()
  {
    Assert.Equal(18, new Settings(FontSize: 18).WithLargerFont().FontSize);
  }

  [Fact]
  public void SmallerFont_ShrinksByOnePixel()
  {
    Assert.Equal(11, new Settings(FontSize: 12).WithSmallerFont().FontSize);
  }

  [Fact]
  public void SmallerFont_StopsAt10()
  {
    Assert.Equal(10, new Settings(FontSize: 10).WithSmallerFont().FontSize);
  }

  [Fact]
  public void DefaultFont_Restores12_AndKeepsUnit()
  {
    var settings = new Settings(SpeedUnit.Bytes, FontSize: 16).WithDefaultFont();

    Assert.Equal(new Settings(SpeedUnit.Bytes, FontSize: 12), settings);
  }
}
