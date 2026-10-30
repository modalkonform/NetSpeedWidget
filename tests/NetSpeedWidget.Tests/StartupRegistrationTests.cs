using Microsoft.Win32;

namespace NetSpeedWidget.Tests;

public sealed class StartupRegistrationTests : IDisposable
{
  const string TestRoot = @"Software\NetSpeedWidget.Tests";
  const string runKeyPath = $@"{TestRoot}\Run";

  public void Dispose()
  {
    Registry.CurrentUser.DeleteSubKeyTree(TestRoot, throwOnMissingSubKey: false);
  }

  [Fact]
  public void Enable_WritesQuotedExePathUnderRunKey()
  {
    var startup = new StartupRegistration(runKeyPath, "NetSpeedWidget");

    startup.Enable(@"C:\Tools\NetSpeedWidget.exe");

    using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
    Assert.Equal("\"C:\\Tools\\NetSpeedWidget.exe\"", key?.GetValue("NetSpeedWidget"));
    Assert.True(startup.IsEnabled);
  }

  [Fact]
  public void Disable_RemovesEntry()
  {
    var startup = new StartupRegistration(runKeyPath, "NetSpeedWidget");
    startup.Enable(@"C:\Tools\NetSpeedWidget.exe");

    startup.Disable();

    Assert.False(startup.IsEnabled);
  }

  [Fact]
  public void Disable_WhenNeverEnabled_DoesNotThrow()
  {
    var startup = new StartupRegistration(runKeyPath, "NetSpeedWidget");

    startup.Disable();

    Assert.False(startup.IsEnabled);
  }
}
