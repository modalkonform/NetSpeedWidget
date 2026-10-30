using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;

namespace NetSpeedWidget;

/// <summary>Borderless, always-on-top window that draws the speeds over the taskbar.</summary>
sealed class TaskbarWidget : Form
{
  // Not "NetSpeedMonitor": the older tool of that name already uses that AppData folder.
  const string AppName = "NetSpeedWidget";
  const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

  // Sizes in pixels at 100 % display scaling.
  const int GapToTray = 8;
  const int TextPadding = 4;
  const int ArrowGap = 4;

  readonly record struct Palette(Color Text, Color Download, Color Upload);

  static readonly Palette DarkTaskbar = new(
    Color.White, Color.FromArgb(0x4f, 0xc3, 0xf7), Color.FromArgb(0xff, 0xb7, 0x4d));
  static readonly Palette LightTaskbar = new(
    Color.Black, Color.FromArgb(0x02, 0x77, 0xbd), Color.FromArgb(0xe6, 0x51, 0x00));

  static readonly StringFormat RightAligned =
    new(StringFormat.GenericTypographic) { Alignment = StringAlignment.Far };

  readonly SettingsStore settingsStore = new(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    AppName, "settings.json"));
  readonly StartupRegistration startup = new(RunKeyPath, AppName);
  readonly ActiveAdapterMonitor adapterMonitor = new();
  readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 1000 };
  readonly ContextMenuStrip menu = new();
  // Held in a field so the garbage collector doesn't free it while Windows still calls it.
  readonly NativeMethods.WinEventProc onForegroundChanged;
  IntPtr foregroundHook;

  Settings settings;
  TrafficSample? lastSample;
  TrafficRate rate = TrafficRate.Zero;

  public TaskbarWidget()
  {
    FormBorderStyle = FormBorderStyle.None;
    ShowInTaskbar = false;
    StartPosition = FormStartPosition.Manual;
    AutoScaleMode = AutoScaleMode.None;
    settings = settingsStore.Load();
    onForegroundChanged = (_, _, _, _, _, _, _) => KeepOnTop();
    refreshTimer.Tick += (_, _) =>
    {
      Sample();
      Redraw();
    };
    BuildMenu();
  }

  protected override bool ShowWithoutActivation => true;

  protected override CreateParams CreateParams
  {
    get
    {
      var createParams = base.CreateParams;
      createParams.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW
        | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_NOACTIVATE;
      return createParams;
    }
  }

  protected override void OnShown(EventArgs e)
  {
    base.OnShown(e);
    // Clicking the taskbar raises it above us, so climb back on top whenever focus moves.
    foregroundHook = NativeMethods.SetWinEventHook(
      NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
      IntPtr.Zero, onForegroundChanged, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
    Sample();
    Redraw();
    refreshTimer.Start();
  }

  protected override void OnFormClosed(FormClosedEventArgs e)
  {
    refreshTimer.Stop();
    NativeMethods.UnhookWinEvent(foregroundHook);
    base.OnFormClosed(e);
  }

  protected override void OnMouseUp(MouseEventArgs e)
  {
    base.OnMouseUp(e);
    if (e.Button != MouseButtons.Right)
      return;

    // The menu only closes on an outside click if our window is in the foreground.
    NativeMethods.SetForegroundWindow(Handle);
    menu.Show(Cursor.Position);
  }

  void BuildMenu()
  {
    var bitsItem = new ToolStripMenuItem("Bits (Kbps, Mbps)", null,
      (_, _) => ChangeSettings(settings with { Unit = SpeedUnit.Bits }));
    var bytesItem = new ToolStripMenuItem("Bytes (KB/s, MB/s)", null,
      (_, _) => ChangeSettings(settings with { Unit = SpeedUnit.Bytes }));
    var largerFontItem = new ToolStripMenuItem("Font size +");
    var smallerFontItem = new ToolStripMenuItem("Font size −");
    var resetFontItem = new ToolStripMenuItem("Reset font size");
    var startupItem = new ToolStripMenuItem("Start with Windows", null, (_, _) => ToggleStartup());
    var exitItem = new ToolStripMenuItem("Exit", null, (_, _) => Close());

    menu.Items.AddRange([
      bitsItem, bytesItem, new ToolStripSeparator(),
      largerFontItem, smallerFontItem, resetFontItem, new ToolStripSeparator(),
      startupItem, new ToolStripSeparator(),
      exitItem,
    ]);

    void UpdateFontItems()
    {
      largerFontItem.Enabled = settings.FontSize < Settings.MaxFontSize;
      smallerFontItem.Enabled = settings.FontSize > Settings.MinFontSize;
      resetFontItem.Enabled = settings.FontSize != Settings.DefaultFontSize;
    }

    // Keeps the menu open after a font click so + and − can be clicked repeatedly.
    var keepMenuOpen = false;
    void ResizeFont(Settings resized)
    {
      ChangeSettings(resized);
      UpdateFontItems();
      keepMenuOpen = true;
    }
    largerFontItem.Click += (_, _) => ResizeFont(settings.WithLargerFont());
    smallerFontItem.Click += (_, _) => ResizeFont(settings.WithSmallerFont());
    resetFontItem.Click += (_, _) => ResizeFont(settings.WithDefaultFont());
    menu.Closing += (_, e) =>
    {
      e.Cancel = keepMenuOpen && e.CloseReason == ToolStripDropDownCloseReason.ItemClicked;
      keepMenuOpen = false;
    };

    menu.Opening += (_, _) =>
    {
      bitsItem.Checked = settings.Unit == SpeedUnit.Bits;
      bytesItem.Checked = settings.Unit == SpeedUnit.Bytes;
      startupItem.Checked = startup.IsEnabled;
      UpdateFontItems();
    };
  }

  void ChangeSettings(Settings changed)
  {
    settings = changed;
    settingsStore.Save(settings);
    Redraw();
  }

  void ToggleStartup()
  {
    if (startup.IsEnabled)
      startup.Disable();
    else
      startup.Enable(Environment.ProcessPath!);
  }

  void Sample()
  {
    var sample = adapterMonitor.Read();
    rate = TrafficRate.Between(lastSample, sample);
    lastSample = sample;
  }

  void Redraw()
  {
    if (NativeMethods.IsFullscreenAppRunning()
      || !Taskbar.TryGetBounds(out var taskbar, out var tray))
    {
      Hide();
      return;
    }

    using var font = new Font("Segoe UI", ScaleForDpi(settings.FontSize), GraphicsUnit.Pixel);
    var gap = (int)ScaleForDpi(GapToTray);
    Bounds = WidgetPlacement.LeftOfTray(taskbar, tray, MeasureWidth(font), gap);
    Render(font);
    if (!Visible)
      Show();
    KeepOnTop();
  }

  float ScaleForDpi(float pixelsAt100Percent) => pixelsAt100Percent * DeviceDpi / 96f;

  /// <summary>Fixed width that fits the longest value, so the widget never jumps around.</summary>
  int MeasureWidth(Font font)
  {
    var longest = settings.Unit == SpeedUnit.Bits ? "↓ 888,8 Mbps" : "↓ 888,8 MB/s";
    using var graphics = Graphics.FromHwnd(IntPtr.Zero);
    var textWidth = graphics.MeasureString(longest, font, PointF.Empty, RightAligned).Width;
    return (int)Math.Ceiling(textWidth + 2 * ScaleForDpi(TextPadding));
  }

  void Render(Font font)
  {
    using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
      // Almost invisible, but non-zero alpha makes the whole widget catch right-clicks.
      graphics.Clear(Color.FromArgb(1, 0, 0, 0));
      graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

      var palette = Taskbar.UsesLightTheme() ? LightTaskbar : DarkTaskbar;
      var culture = CultureInfo.CurrentCulture;
      var lineHeight = font.GetHeight(graphics);
      var top = (Height - 2 * lineHeight) / 2;
      DrawLine(graphics, font, top, "↓", palette.Download, palette.Text,
        SpeedFormatter.Format(rate.DownloadBytesPerSecond, settings.Unit, culture));
      DrawLine(graphics, font, top + lineHeight, "↑", palette.Upload, palette.Text,
        SpeedFormatter.Format(rate.UploadBytesPerSecond, settings.Unit, culture));
    }
    PushToScreen(bitmap);
  }

  void DrawLine(Graphics graphics, Font font, float top, string arrow, Color arrowColor,
    Color textColor, string value)
  {
    var right = Width - ScaleForDpi(TextPadding);
    var lineHeight = font.GetHeight(graphics);
    var valueWidth = graphics.MeasureString(value, font, PointF.Empty, RightAligned).Width;
    using var textBrush = new SolidBrush(textColor);
    using var arrowBrush = new SolidBrush(arrowColor);
    graphics.DrawString(value, font, textBrush,
      new RectangleF(0, top, right, lineHeight), RightAligned);
    graphics.DrawString(arrow, font, arrowBrush,
      new RectangleF(0, top, right - valueWidth - ScaleForDpi(ArrowGap), lineHeight), RightAligned);
  }

  /// <summary>Shows the bitmap, with its per-pixel transparency, as the window's content.</summary>
  void PushToScreen(Bitmap bitmap)
  {
    var screenDc = NativeMethods.GetDC(IntPtr.Zero);
    var memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
    var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
    var previous = NativeMethods.SelectObject(memoryDc, hBitmap);
    try
    {
      var position = new NativeMethods.POINT { X = Left, Y = Top };
      var size = new NativeMethods.SIZE { Width = Width, Height = Height };
      var sourcePosition = new NativeMethods.POINT();
      var blend = new NativeMethods.BLENDFUNCTION
      {
        SourceConstantAlpha = 255,
        AlphaFormat = NativeMethods.AC_SRC_ALPHA,
      };
      NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref position, ref size, memoryDc,
        ref sourcePosition, 0, ref blend, NativeMethods.ULW_ALPHA);
    }
    finally
    {
      NativeMethods.SelectObject(memoryDc, previous);
      NativeMethods.DeleteObject(hBitmap);
      NativeMethods.DeleteDC(memoryDc);
      NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
    }
  }

  void KeepOnTop()
  {
    NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
      NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
  }
}
