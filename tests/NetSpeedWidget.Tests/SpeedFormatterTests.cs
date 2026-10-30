using System.Globalization;

namespace NetSpeedWidget.Tests;

public class SpeedFormatterTests
{
  static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

  [Theory]
  [InlineData(0, "0,0 Kbps")]
  [InlineData(50, "0,4 Kbps")]
  [InlineData(111_037.5, "888,3 Kbps")]
  [InlineData(11_825_000, "94,6 Mbps")]
  [InlineData(125_000_000, "1,0 Gbps")]
  public void Bits_ScaleToShortestUnit(double bytesPerSecond, string expected)
  {
    Assert.Equal(expected, SpeedFormatter.Format(bytesPerSecond, SpeedUnit.Bits, German));
  }

  [Theory]
  [InlineData(0, "0,0 KB/s")]
  [InlineData(111_000, "111,0 KB/s")]
  [InlineData(11_800_000, "11,8 MB/s")]
  [InlineData(2_500_000_000, "2,5 GB/s")]
  public void Bytes_ScaleToShortestUnit(double bytesPerSecond, string expected)
  {
    Assert.Equal(expected, SpeedFormatter.Format(bytesPerSecond, SpeedUnit.Bytes, German));
  }

  [Theory]
  [InlineData(124_995, SpeedUnit.Bits, "1,0 Mbps")]
  [InlineData(999_960, SpeedUnit.Bytes, "1,0 MB/s")]
  public void ValueThatRoundsToThousand_MovesUpOneUnit(
    double bytesPerSecond, SpeedUnit unit, string expected)
  {
    Assert.Equal(expected, SpeedFormatter.Format(bytesPerSecond, unit, German));
  }

  [Fact]
  public void UsesDecimalSeparatorOfGivenCulture()
  {
    var english = CultureInfo.GetCultureInfo("en-US");

    Assert.Equal("888.3 Kbps", SpeedFormatter.Format(111_037.5, SpeedUnit.Bits, english));
  }
}
