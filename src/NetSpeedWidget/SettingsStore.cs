using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetSpeedWidget;

/// <summary>User choices that survive a restart.</summary>
/// <param name="FontSize">Text size in pixels at 100 % display scaling.</param>
public sealed record Settings(
  SpeedUnit Unit = SpeedUnit.Bits, int FontSize = Settings.DefaultFontSize)
{
  public const int DefaultFontSize = 12;
  public const int MinFontSize = 10;
  // Two lines of 18 px Segoe UI just fit a standard 48 px taskbar.
  public const int MaxFontSize = 18;

  /// <summary>One pixel larger, up to <see cref="MaxFontSize"/>.</summary>
  public Settings WithLargerFont() =>
    this with { FontSize = Math.Min(FontSize + 1, MaxFontSize) };

  /// <summary>One pixel smaller, down to <see cref="MinFontSize"/>.</summary>
  public Settings WithSmallerFont() =>
    this with { FontSize = Math.Max(FontSize - 1, MinFontSize) };

  /// <summary>Back to the default size.</summary>
  public Settings WithDefaultFont() => this with { FontSize = DefaultFontSize };
}

/// <summary>Reads and writes <see cref="Settings"/> as a JSON file.</summary>
public sealed class SettingsStore(string filePath)
{
  static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
  };

  /// <summary>Loads saved settings, or defaults if none can be read.</summary>
  public Settings Load()
  {
    try
    {
      var json = File.ReadAllText(filePath);
      return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
    {
      return new Settings();
    }
  }

  /// <summary>Saves settings, creating the folder if needed.</summary>
  public void Save(Settings settings)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    File.WriteAllText(filePath, JsonSerializer.Serialize(settings, JsonOptions));
  }
}
