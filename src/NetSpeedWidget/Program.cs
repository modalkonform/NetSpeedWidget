namespace NetSpeedWidget;

static class Program
{
  /// <summary>The main entry point for the application.</summary>
  [STAThread]
  static void Main()
  {
    // A second copy (e.g. started by hand while the startup copy runs) would draw on top.
    using var singleInstance = new Mutex(true, "NetSpeedWidget.SingleInstance", out var isFirst);
    if (!isFirst)
      return;

    ApplicationConfiguration.Initialize();
    Application.Run(new TaskbarWidget());
  }
}
