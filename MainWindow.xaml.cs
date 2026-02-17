using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;
using static TimeToolbar.Settings;

namespace TimeToolbar;

public sealed partial class MainWindow : Window
{
    private readonly Settings _settings;
    private readonly PerformanceCounter _cpuCounter;
    private readonly ManagementObjectSearcher _mgmtObjSearcherOS;
    private readonly DispatcherTimer _topmostTimer;
    private readonly DispatcherTimer _dataTimer;
    private readonly List<TimeZoneLabelBinding> _timeZoneBindings = new();

    private TextBlock _cpuValueText = null!;
    private TextBlock _ramValueText = null!;
    private Border _cpuRamPanel = null!;
    private Border _separator = null!;
    private bool _showCpuRam = true;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    // System tray
    private NativeMethods.NOTIFYICONDATA _notifyIconData;
    private NativeMethods.SUBCLASSPROC? _subclassProc;
    private const int WM_TRAYICON = 0x8000 + 1;
    private const int TRAY_ICON_ID = 1;

    private class TimeZoneLabelBinding
    {
        public TimeZoneSettings TimeZoneSettings { get; set; } = null!;
        public TextBlock TimeText { get; set; } = null!;
        public TextBlock ZoneText { get; set; } = null!;
    }

    public MainWindow()
    {
        _settings = Program.AppSettings ?? new Settings();

        if (_settings.TimeZones == null || _settings.TimeZones.Length == 0)
        {
            _settings.TimeZones =
            [
                new TimeZoneSettings { TimeZoneId = "UTC", TimeZoneLabel = "UTC" },
                new TimeZoneSettings { TimeZoneId = "Pacific Standard Time", TimeZoneLabel = "PST" }
            ];
        }

        this.InitializeComponent();
        this.Title = "TimeToolbar";

        // Initialize performance counters
        _cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
        _mgmtObjSearcherOS = new ManagementObjectSearcher("select * from Win32_OperatingSystem");

        // Configure window chrome, position, and behavior
        ConfigureWindow();

        // Build the dynamic UI
        BuildUI();

        // Set up system tray icon
        SetupTrayIcon();

        // Fast timer to keep window above the taskbar (matches original 100ms timer)
        _topmostTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _topmostTimer.Tick += TopmostTimer_Tick;
        _topmostTimer.Start();

        // Data update timer (time zones, CPU, RAM)
        _dataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _dataTimer.Tick += DataTimer_Tick;
        _dataTimer.Start();

        // Initial data population
        DataTimer_Tick(null!, null!);

        // Move off-screen before first render, then force a hide/show cycle
        // to initialize the transparent backdrop, then move into position.
        _appWindow.Move(new PointInt32(-10000, -10000));
        var initTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        initTimer.Tick += (s, e) =>
        {
            initTimer.Stop();
            _appWindow.Hide();
            _appWindow.Show();
            PositionOnTaskbar();
        };
        initTimer.Start();

        this.Closed += MainWindow_Closed;
    }

    private void ConfigureWindow()
    {
        _hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        // Remove title bar and borders, set always-on-top
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsAlwaysOnTop = true;
        }

        // Strip the resize frame (removes the thick white border)
        var style = NativeMethods.GetWindowLongPtr(_hWnd, NativeMethods.GWL_STYLE);
        NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWL_STYLE,
            style & ~NativeMethods.WS_THICKFRAME & ~NativeMethods.WS_CAPTION);
        NativeMethods.SetWindowPos(_hWnd, IntPtr.Zero, 0, 0, 0, 0,
            NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOMOVE |
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER);

        // Remove DWM border color
        int colorNone = NativeMethods.DWMWA_COLOR_NONE;
        NativeMethods.DwmSetWindowAttribute(_hWnd, NativeMethods.DWMWA_BORDER_COLOR,
            ref colorNone, sizeof(int));

        // Extend DWM frame into client area - this makes the surface behind
        // the acrylic transparent instead of opaque black
        var margins = new NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        NativeMethods.DwmExtendFrameIntoClientArea(_hWnd, ref margins);

        // Transparent acrylic: TintOpacity=0 + LuminosityOpacity=0 + DWM frame extension
        // = fully transparent backdrop (blur of transparent = transparent)
        this.SystemBackdrop = new TransparentBackdrop(_hWnd);

        // WS_EX_TOOLWINDOW: hide from Alt+Tab
        // WS_EX_NOACTIVATE: don't steal focus when clicked
        var exStyle = NativeMethods.GetWindowLongPtr(_hWnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWL_EXSTYLE,
            exStyle | (nint)NativeMethods.WS_EX_TOOLWINDOW | (nint)NativeMethods.WS_EX_NOACTIVATE);

        // Subclass window to receive tray icon messages
        _subclassProc = SubclassProc;
        NativeMethods.SetWindowSubclass(_hWnd, _subclassProc, 0, IntPtr.Zero);

        PositionOnTaskbar();
    }

    private class TransparentBackdrop : Microsoft.UI.Xaml.Media.SystemBackdrop
    {
        private DesktopAcrylicController? _controller;
        private readonly IntPtr _hWnd;

        public TransparentBackdrop(IntPtr hWnd) => _hWnd = hWnd;

        protected override void OnTargetConnected(
            Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop connectedTarget,
            Microsoft.UI.Xaml.XamlRoot xamlRoot)
        {
            base.OnTargetConnected(connectedTarget, xamlRoot);

            // DesktopAcrylicController with zero tint/luminosity, combined with
            // DwmExtendFrameIntoClientArea(-1), produces a transparent backdrop.
            // The acrylic blurs what's behind the window; with the extended DWM frame,
            // what's behind is the desktop compositor surface (transparent).
            _controller = new DesktopAcrylicController
            {
                TintOpacity = 0f,
                LuminosityOpacity = 0f,
                TintColor = Windows.UI.Color.FromArgb(0, 0, 0, 0),
                FallbackColor = Windows.UI.Color.FromArgb(0, 0, 0, 0)
            };

            _controller.AddSystemBackdropTarget(connectedTarget);
            _controller.SetSystemBackdropConfiguration(
                GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot));

            // Re-apply DWM frame extension AFTER the acrylic controller is connected,
            // so the DWM surface behind the acrylic is transparent from the first frame.
            var margins = new NativeMethods.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            NativeMethods.DwmExtendFrameIntoClientArea(_hWnd, ref margins);
        }

        protected override void OnTargetDisconnected(
            Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            base.OnTargetDisconnected(disconnectedTarget);
            _controller?.Dispose();
            _controller = null;
        }
    }

    private void PositionOnTaskbar()
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        var outerBounds = displayArea.OuterBounds;
        var workArea = displayArea.WorkArea;

        // Taskbar is at the bottom of the screen
        var taskbarTop = workArea.Y + workArea.Height;
        var taskbarHeight = (outerBounds.Y + outerBounds.Height) - taskbarTop;
        if (taskbarHeight <= 0) taskbarHeight = 48;

        var baseOffset = AreWindowsWidgetsEnabled() ? 200 : 0;

        var dpi = NativeMethods.GetDpiForWindow(_hWnd);
        var scale = dpi / 96.0;

        // Calculate content width in DIPs then scale to physical pixels
        var tzCount = _settings.TimeZones?.Length ?? 0;
        var cpuRamWidth = _showCpuRam ? 110 : 0;
        var contentWidthDips = cpuRamWidth + (tzCount * 95) + 24;
        var physicalWidth = (int)(contentWidthDips * scale);

        _appWindow.MoveAndResize(new RectInt32(
            workArea.X + (int)((baseOffset + _settings.XOffset) * scale),
            taskbarTop,
            physicalWidth,
            taskbarHeight));
    }

    private void BuildUI()
    {
        // CPU/RAM section
        _cpuRamPanel = CreateCpuRamPanel();
        ContentPanel.Children.Add(_cpuRamPanel);

        // Vertical separator
        _separator = new Border
        {
            Width = 1,
            Background = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumLowBrush"],
            Opacity = 0.4,
            Margin = new Thickness(6, 8, 6, 8)
        };
        ContentPanel.Children.Add(_separator);

        // Time zone panels
        foreach (var tz in _settings.TimeZones!)
        {
            var (panel, binding) = CreateTimeZonePanel(tz);
            ContentPanel.Children.Add(panel);
            _timeZoneBindings.Add(binding);
        }
    }

    private Border CreateCpuRamPanel()
    {
        var cpuLabel = new TextBlock
        {
            Text = "CPU",
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };

        _cpuValueText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36
        };

        var ramLabel = new TextBlock
        {
            Text = "RAM",
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };

        _ramValueText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36
        };

        var cpuRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        cpuRow.Children.Add(cpuLabel);
        cpuRow.Children.Add(_cpuValueText);

        var ramRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        ramRow.Children.Add(ramLabel);
        ramRow.Children.Add(_ramValueText);

        var stack = new StackPanel
        {
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(4, 0, 4, 0)
        };
        stack.Children.Add(cpuRow);
        stack.Children.Add(ramRow);

        var border = new Border
        {
            Child = stack,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2, 4, 2)
        };

        // Double-tap to launch Task Manager
        border.DoubleTapped += CpuRamPanel_DoubleTapped;

        // Hover highlight matching Windows 11 taskbar style
        border.PointerEntered += (s, e) =>
            border.Background = new SolidColorBrush(Colors.White) { Opacity = 0.08 };
        border.PointerExited += (s, e) =>
            border.Background = null;

        return border;
    }

    private (FrameworkElement Panel, TimeZoneLabelBinding Binding) CreateTimeZonePanel(TimeZoneSettings tz)
    {
        var timeText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var zoneText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 10,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };

        var stack = new StackPanel
        {
            Spacing = -2,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 75,
            Padding = new Thickness(6, 2, 6, 2)
        };
        stack.Children.Add(timeText);
        stack.Children.Add(zoneText);

        var border = new Border
        {
            Child = stack,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(2, 0, 2, 0)
        };

        // Hover highlight
        border.PointerEntered += (s, e) =>
            border.Background = new SolidColorBrush(Colors.White) { Opacity = 0.08 };
        border.PointerExited += (s, e) =>
            border.Background = null;

        var binding = new TimeZoneLabelBinding
        {
            TimeZoneSettings = tz,
            TimeText = timeText,
            ZoneText = zoneText
        };

        return (border, binding);
    }

    private void TopmostTimer_Tick(object? sender, object e)
    {
        // Aggressively re-assert topmost via Win32 to stay above the taskbar
        NativeMethods.SetWindowPos(_hWnd, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }

    private void DataTimer_Tick(object? sender, object e)
    {
        // Update time zone displays
        foreach (var binding in _timeZoneBindings)
        {
            var time = TimeZoneInfo.ConvertTime(DateTime.Now,
                TimeZoneInfo.FindSystemTimeZoneById(binding.TimeZoneSettings.TimeZoneId));
            binding.TimeText.Text = $"{time:t}";
            binding.ZoneText.Text = binding.TimeZoneSettings.TimeZoneLabel;
        }

        // Update CPU and RAM
        try
        {
            _cpuValueText.Text = $"{_cpuCounter.NextValue():F0}%";
            _ramValueText.Text = $"{GetCurrentRamUsage():F0}%";
        }
        catch
        {
            // Performance counters can occasionally throw
        }

        PositionOnTaskbar();
    }

    private double GetCurrentRamUsage()
    {
        using var collection = _mgmtObjSearcherOS.Get();
        return collection.Cast<ManagementObject>()
            .Select(mo =>
            {
                if (double.TryParse(mo["FreePhysicalMemory"]?.ToString(), out var free) &&
                    double.TryParse(mo["TotalVisibleMemorySize"]?.ToString(), out var total) &&
                    total > 0)
                {
                    return ((total - free) / total) * 100;
                }
                return 0.0;
            })
            .FirstOrDefault();
    }

    private static bool AreWindowsWidgetsEnabled()
    {
        const string keyName = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        return (Microsoft.Win32.Registry.GetValue(keyName, "TaskbarDa", 0) as int?) == 1;
    }

    private void CpuRamPanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("taskmgr") { UseShellExecute = true });
    }

    // ---- System Tray Icon ----

    private void SetupTrayIcon()
    {
        var hIcon = NativeMethods.ExtractIcon(IntPtr.Zero, Environment.ProcessPath!, 0);

        _notifyIconData = new NativeMethods.NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
            hWnd = _hWnd,
            uID = TRAY_ICON_ID,
            uFlags = NativeMethods.NIF_ICON | NativeMethods.NIF_MESSAGE | NativeMethods.NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = hIcon,
            szTip = "TimeToolbar"
        };

        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_ADD, ref _notifyIconData);
    }

    private void RemoveTrayIcon()
    {
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref _notifyIconData);
    }

    private void ShowTrayContextMenu()
    {
        var hMenu = NativeMethods.CreatePopupMenu();

        var showCpuRamFlags = NativeMethods.MF_STRING |
            (_showCpuRam ? NativeMethods.MF_CHECKED : NativeMethods.MF_UNCHECKED);
        NativeMethods.AppendMenu(hMenu, showCpuRamFlags, 1, "Show CPU and RAM");
        NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, 2, "Quit");

        NativeMethods.GetCursorPos(out var pt);
        NativeMethods.SetForegroundWindow(_hWnd);

        var cmd = NativeMethods.TrackPopupMenu(hMenu,
            NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_NONOTIFY,
            pt.X, pt.Y, 0, _hWnd, IntPtr.Zero);

        NativeMethods.DestroyMenu(hMenu);

        switch (cmd)
        {
            case 1: // Toggle CPU/RAM visibility
                _showCpuRam = !_showCpuRam;
                _cpuRamPanel.Visibility = _showCpuRam ? Visibility.Visible : Visibility.Collapsed;
                _separator.Visibility = _showCpuRam ? Visibility.Visible : Visibility.Collapsed;
                PositionOnTaskbar();
                break;
            case 2: // Quit
                RemoveTrayIcon();
                this.Close();
                break;
        }
    }

    private IntPtr SubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam,
        IntPtr uIdSubclass, IntPtr dwRefData)
    {
        if (uMsg == WM_TRAYICON && (int)wParam == TRAY_ICON_ID)
        {
            if ((uint)(int)lParam == NativeMethods.WM_RBUTTONUP)
            {
                DispatcherQueue.TryEnqueue(() => ShowTrayContextMenu());
            }
        }

        return NativeMethods.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        _topmostTimer.Stop();
        _dataTimer.Stop();
        RemoveTrayIcon();

        try { _cpuCounter.Dispose(); } catch { }
        try { _mgmtObjSearcherOS.Dispose(); } catch { }
    }
}
