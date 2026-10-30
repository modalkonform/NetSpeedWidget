namespace NetSpeedWidget.Tests;

public sealed class SettingsStoreTests : IDisposable
{
  readonly string tempDir = Path.Combine(Path.GetTempPath(), $"nsm-tests-{Guid.NewGuid()}");

  public void Dispose()
  {
    if (Directory.Exists(tempDir))
      Directory.Delete(tempDir, recursive: true);
  }

  [Fact]
  public void Load_DefaultsToBits_WhenNoFileExists()
  {
    var store = new SettingsStore(Path.Combine(tempDir, "settings.json"));

    Assert.Equal(SpeedUnit.Bits, store.Load().Unit);
  }

  [Fact]
  public void Load_ReturnsWhatWasSaved_CreatingFolderIfNeeded()
  {
    var store = new SettingsStore(Path.Combine(tempDir, "nested", "settings.json"));

    store.Save(new Settings(SpeedUnit.Bytes));

    Assert.Equal(SpeedUnit.Bytes, store.Load().Unit);
  }

  [Fact]
  public void Load_ReturnsSavedFontSize()
  {
    var store = new SettingsStore(Path.Combine(tempDir, "settings.json"));

    store.Save(new Settings(SpeedUnit.Bits, FontSize: 15));

    Assert.Equal(15, store.Load().FontSize);
  }

  [Fact]
  public void Load_UsesDefaultFontSize_ForFileSavedBeforeFontSizeExisted()
  {
    Directory.CreateDirectory(tempDir);
    var path = Path.Combine(tempDir, "settings.json");
    File.WriteAllText(path, """{ "Unit": "Bytes" }""");

    var settings = new SettingsStore(path).Load();

    Assert.Equal(new Settings(SpeedUnit.Bytes, FontSize: 12), settings);
  }

  [Fact]
  public void Load_DefaultsToBits_WhenFileIsCorrupt()
  {
    Directory.CreateDirectory(tempDir);
    var path = Path.Combine(tempDir, "settings.json");
    File.WriteAllText(path, "{ not json");

    Assert.Equal(SpeedUnit.Bits, new SettingsStore(path).Load().Unit);
  }
}
