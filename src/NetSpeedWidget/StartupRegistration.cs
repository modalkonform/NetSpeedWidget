using Microsoft.Win32;

namespace NetSpeedWidget;

/// <summary>Turns "Start with Windows" on or off for the current user.</summary>
public sealed class StartupRegistration(string runKeyPath, string valueName)
{
  /// <summary>Whether the app is set to start when the user signs in.</summary>
  public bool IsEnabled
  {
    get
    {
      using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
      return key?.GetValue(valueName) is not null;
    }
  }

  /// <summary>Registers the exe to start when the user signs in.</summary>
  public void Enable(string exePath)
  {
    using var key = Registry.CurrentUser.CreateSubKey(runKeyPath);
    key.SetValue(valueName, $"\"{exePath}\"");
  }

  /// <summary>Removes the startup entry, if there is one.</summary>
  public void Disable()
  {
    using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
    key?.DeleteValue(valueName, throwOnMissingValue: false);
  }
}
