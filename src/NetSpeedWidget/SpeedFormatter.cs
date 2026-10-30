using System.Globalization;

namespace NetSpeedWidget;

/// <summary>Whether rates are shown in bits (Kbps) or bytes (KB/s).</summary>
public enum SpeedUnit
{
  Bits,
  Bytes,
}

/// <summary>Turns a transfer rate into short display text.</summary>
public static class SpeedFormatter
{
  static readonly string[] BitLabels = ["Kbps", "Mbps", "Gbps"];
  static readonly string[] ByteLabels = ["KB/s", "MB/s", "GB/s"];

  /// <summary>Formats a rate, e.g. 111037.5 B/s in bits as "888,3 Kbps".</summary>
  public static string Format(double bytesPerSecond, SpeedUnit unit, CultureInfo culture)
  {
    var labels = unit == SpeedUnit.Bits ? BitLabels : ByteLabels;
    var value = (unit == SpeedUnit.Bits ? bytesPerSecond * 8 : bytesPerSecond) / 1000;
    var labelIndex = 0;
    // Compare the rounded value so 999,96 Kbps shows as "1,0 Mbps", not "1000,0 Kbps".
    while (Math.Round(value, 1) >= 1000 && labelIndex < labels.Length - 1)
    {
      value /= 1000;
      labelIndex++;
    }
    return $"{value.ToString("0.0", culture)} {labels[labelIndex]}";
  }
}
