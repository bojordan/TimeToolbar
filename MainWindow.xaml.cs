using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI;
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

    private TextBlock _cpuLabelText = null!;
    private TextBlock _ramLabelText = null!;
    private TextBlock _cpuValueText = null!;
    private TextBlock _ramValueText = null!;
    private Border _cpuRamPanel = null!;
    private Border _separator = null!;
    private bool _showCpuRam = true;
    private bool _use24HourFormat;

    private AppWindow _appWindow = null!;
    private IntPtr _hWnd;

    // Drag state
    private bool _isDragging;
    private int _dragStartCursorX;
    private int _dragStartWindowX;

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

        // Apply theme-dependent colors (background + accent)
        ApplyThemeColors();

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

        // Drag support
        RootGrid.PointerPressed += RootGrid_PointerPressed;
        RootGrid.PointerMoved += RootGrid_PointerMoved;
        RootGrid.PointerReleased += RootGrid_PointerReleased;
        RootGrid.PointerCaptureLost += RootGrid_PointerCaptureLost;

        // Right-click context menu
        RootGrid.RightTapped += RootGrid_RightTapped;

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

        // Strip the resize frame
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

        // DWM rounded corners for pill shape
        int cornerPref = NativeMethods.DWMWCP_ROUND;
        NativeMethods.DwmSetWindowAttribute(_hWnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
            ref cornerPref, sizeof(int));

        RootGrid.ActualThemeChanged += (s, e) => ApplyThemeColors();

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

    private void ApplyThemeColors()
    {
        var isDark = RootGrid.ActualTheme == ElementTheme.Dark;
        RootGrid.Background = new SolidColorBrush(
            isDark ? Windows.UI.Color.FromArgb(255, 40, 40, 40)
                   : Windows.UI.Color.FromArgb(255, 240, 240, 240));

        var accentBrush = new SolidColorBrush(
            isDark ? (Windows.UI.Color)Application.Current.Resources["SystemAccentColorLight2"]
                   : (Windows.UI.Color)Application.Current.Resources["SystemAccentColorDark2"]);

        _cpuLabelText.Foreground = accentBrush;
        _ramLabelText.Foreground = accentBrush;
        foreach (var binding in _timeZoneBindings)
            binding.ZoneText.Foreground = accentBrush;
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
        var cpuRamWidth = _showCpuRam ? 105 : 0;
        var tzSeparators = tzCount > 1 ? (tzCount - 1) * 13 : 0;
        var contentWidthDips = cpuRamWidth + (tzCount * 82) + tzSeparators;
        var physicalWidth = (int)(contentWidthDips * scale);

        // Inset the pill within the taskbar with padding
        var inset = (int)(5 * scale);
        _appWindow.MoveAndResize(new RectInt32(
            workArea.X + (int)((baseOffset + _settings.XOffset) * scale) + inset,
            taskbarTop + inset,
            physicalWidth,
            taskbarHeight - (inset * 2)));
    }

    private void RootGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(RootGrid).Properties.IsLeftButtonPressed) return;
        RootGrid.CapturePointer(e.Pointer);
        NativeMethods.GetCursorPos(out var pt);
        _dragStartCursorX = pt.X;
        _dragStartWindowX = _appWindow.Position.X;
        _isDragging = true;
    }

    private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        NativeMethods.GetCursorPos(out var pt);
        var newX = _dragStartWindowX + (pt.X - _dragStartCursorX);
        _appWindow.Move(new PointInt32(newX, _appWindow.Position.Y));
    }

    private void RootGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        RootGrid.ReleasePointerCapture(e.Pointer);
        FinishDrag();
    }

    private void RootGrid_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        FinishDrag();
    }

    private void FinishDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;

        // Back-calculate XOffset from the new window position so
        // PositionOnTaskbar() will keep the window in place.
        var dpi = NativeMethods.GetDpiForWindow(_hWnd);
        var scale = dpi / 96.0;
        var inset = (int)(5 * scale);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hWnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        var baseOffset = AreWindowsWidgetsEnabled() ? 200 : 0;

        _settings.XOffset = (int)((_appWindow.Position.X - displayArea.WorkArea.X - inset) / scale) - baseOffset;
        SaveSettings();
    }

    private void SaveSettings()
    {
        var wrapper = new { Settings = _settings };
        var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        File.WriteAllText(path, json);
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

        // Time zone panels with separators between them
        for (int i = 0; i < _settings.TimeZones!.Length; i++)
        {
            if (i > 0)
            {
                ContentPanel.Children.Add(new Border
                {
                    Width = 1,
                    Background = (Brush)Application.Current.Resources["SystemControlForegroundBaseMediumLowBrush"],
                    Opacity = 0.4,
                    Margin = new Thickness(6, 8, 6, 8)
                });
            }

            var (panel, binding) = CreateTimeZonePanel(_settings.TimeZones[i]);
            ContentPanel.Children.Add(panel);
            _timeZoneBindings.Add(binding);
        }
    }

    private Border CreateCpuRamPanel()
    {
        _cpuLabelText = new TextBlock
        {
            Text = "CPU",
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };
        var cpuLabel = _cpuLabelText;

        _cpuValueText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36
        };

        _ramLabelText = new TextBlock
        {
            Text = "RAM",
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center
        };
        var ramLabel = _ramLabelText;

        _ramValueText = new TextBlock
        {
            IsTextScaleFactorEnabled = false,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 36
        };

        var cpuRow = new Grid();
        cpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        cpuRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(cpuLabel, 0);
        Grid.SetColumn(_cpuValueText, 1);
        cpuRow.Children.Add(cpuLabel);
        cpuRow.Children.Add(_cpuValueText);

        var ramRow = new Grid();
        ramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ramRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(ramLabel, 0);
        Grid.SetColumn(_ramValueText, 1);
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

        // Double-tap to toggle 12h/24h format
        border.DoubleTapped += TimeZonePanel_DoubleTapped;

        var binding = new TimeZoneLabelBinding
        {
            TimeZoneSettings = tz,
            TimeText = timeText,
            ZoneText = zoneText
        };

        return (border, binding);
    }

    private Brush GetUsageBrush(double percent)
    {
        if (percent <= 90)
            return (Brush)Application.Current.Resources["DefaultTextForegroundThemeBrush"];

        // Lerp from default text color to deep red over 90–100%
        var t = Math.Clamp((percent - 90) / 10.0, 0, 1);
        var isDark = RootGrid.ActualTheme == ElementTheme.Dark;
        var defaultColor = isDark
            ? Windows.UI.Color.FromArgb(255, 255, 255, 255)
            : Windows.UI.Color.FromArgb(255, 0, 0, 0);
        var redColor = Windows.UI.Color.FromArgb(255, 180, 30, 30);

        var r = (byte)(defaultColor.R + (redColor.R - defaultColor.R) * t);
        var g = (byte)(defaultColor.G + (redColor.G - defaultColor.G) * t);
        var b = (byte)(defaultColor.B + (redColor.B - defaultColor.B) * t);

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
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
            binding.TimeText.Text = _use24HourFormat ? $"{time:HH:mm}" : $"{time:h:mm tt}";
            binding.ZoneText.Text = binding.TimeZoneSettings.TimeZoneLabel;
        }

        // Update CPU and RAM
        try
        {
            var cpu = _cpuCounter.NextValue();
            var ram = GetCurrentRamUsage();
            _cpuValueText.Text = $"{cpu:F0}%";
            _ramValueText.Text = $"{ram:F0}%";
            _cpuValueText.Foreground = GetUsageBrush(cpu);
            _ramValueText.Foreground = GetUsageBrush(ram);
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

    private void TimeZonePanel_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        _use24HourFormat = !_use24HourFormat;
        foreach (var binding in _timeZoneBindings)
        {
            var time = TimeZoneInfo.ConvertTime(DateTime.Now,
                TimeZoneInfo.FindSystemTimeZoneById(binding.TimeZoneSettings.TimeZoneId));
            binding.TimeText.Text = _use24HourFormat ? $"{time:HH:mm}" : $"{time:h:mm tt}";
        }
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

    private void RootGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        ShowTrayContextMenu();
    }

    private void ShowTrayContextMenu()
    {
        _topmostTimer.Stop();

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
        _topmostTimer.Start();

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
