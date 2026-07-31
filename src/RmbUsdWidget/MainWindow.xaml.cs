using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace RmbUsdWidget;

public partial class MainWindow : Window
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private readonly JsonStore _store = new();
    private readonly DashboardService _dashboard;
    private readonly DispatcherTimer _dailyTimer;
    private readonly Forms.NotifyIcon _trayIcon;
    private AppSettings _settings = new();
    private bool _allowClose;
    private bool _isLoading;
    private DateOnly? _lastScheduledRefresh;

    public MainWindow()
    {
        InitializeComponent();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var handler = new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };
        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(25)
        };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) RmbUsdWidget/1.0");
        _dashboard = new DashboardService(
            new SafeOfficialRateProvider(httpClient),
            new BocBankQuoteProvider(httpClient),
            _store);

        _trayIcon = BuildTrayIcon();
        _dailyTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _dailyTimer.Tick += DailyTimer_OnTick;

        Loaded += MainWindow_OnLoaded;
        Closing += MainWindow_OnClosing;
        LocationChanged += MainWindow_OnLocationChanged;
        SystemEvents.UserPreferenceChanged += SystemEvents_OnUserPreferenceChanged;
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyRoundedCorners();
        _settings = await _store.LoadSettingsAsync();
        RestoreWindowPosition();
        Topmost = _settings.Topmost;
        UpdatePinState();
        ApplyTheme();
        _trayIcon.Visible = true;
        _dailyTimer.Start();

        await LoadDashboardAsync(refresh: false);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var needsRefresh = DateTime.Now.Hour >= 10 || !HasVisibleData();
        if (needsRefresh)
        {
            await LoadDashboardAsync(refresh: true);
            _lastScheduledRefresh = today;
        }
    }

    private bool HasVisibleData()
    {
        return BankRateText.Text != "—" && OfficialRateRun.Text != "—";
    }

    private async Task LoadDashboardAsync(bool refresh)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        if (refresh)
        {
            StatusText.Text = "正在更新 · 实际成交以银行为准";
        }

        try
        {
            var (_, snapshot, positions) = await _dashboard.LoadAsync(refresh);
            RenderSnapshot(snapshot, positions);
            if (Environment.GetCommandLineArgs().Contains("--qa", StringComparer.OrdinalIgnoreCase))
            {
                await Dispatcher.InvokeAsync(
                    CaptureQaScreenshot,
                    DispatcherPriority.ApplicationIdle);
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void RenderSnapshot(
        RateSnapshot snapshot,
        IReadOnlyList<HistoricalPosition> positions)
    {
        BankRateText.Text = snapshot.BankSpotBuy?.ToString("0.0000") ?? "—";
        OfficialRateRun.Text = snapshot.OfficialMid?.ToString("0.0000") ?? "—";
        DateText.Text = snapshot.QuoteDate?.ToString("yyyy-MM-dd") ?? "暂无数据";

        foreach (var position in positions)
        {
            var band = position.Years switch
            {
                1 => Band1Text,
                2 => Band2Text,
                3 => Band3Text,
                5 => Band5Text,
                _ => null
            };
            var percent = position.Years switch
            {
                1 => Percent1Text,
                2 => Percent2Text,
                3 => Percent3Text,
                5 => Percent5Text,
                _ => null
            };
            if (band is null || percent is null)
            {
                continue;
            }

            band.Text = position.Band;
            percent.Text = $"· {position.Percentile}%";
            var brush = position.Band == "中位"
                ? (System.Windows.Media.Brush)FindResource("MidBrush")
                : (System.Windows.Media.Brush)FindResource("AccentBrush");
            band.Foreground = brush;
            percent.Foreground = brush;
        }

        StatusText.Text = snapshot.Status switch
        {
            SourceStatus.Fresh => "实际成交以银行为准",
            SourceStatus.BankStale => "银行牌价未更新 · 显示最近缓存",
            SourceStatus.OfficialStale => "官方中间价未更新 · 显示最近缓存",
            _ => "数据未更新 · 显示最近缓存"
        };
    }

    private async void DailyTimer_OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        if (now.Hour >= 10 && _lastScheduledRefresh != today)
        {
            _lastScheduledRefresh = today;
            await LoadDashboardAsync(refresh: true);
        }
    }

    private Forms.NotifyIcon BuildTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示看板", null, (_, _) => ShowFromTray());
        menu.Items.Add("切换始终置顶", null, (_, _) => ToggleTopmost());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());

        var icon = new Forms.NotifyIcon
        {
            Text = "人民币兑美元汇率看板",
            Icon = Drawing.SystemIcons.Information,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void RestoreFromExternalActivation()
    {
        ShowFromTray();
    }

    private void ExitApplication()
    {
        _allowClose = true;
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void PinButton_OnClick(object sender, RoutedEventArgs e)
    {
        ToggleTopmost();
    }

    private void ToggleTopmost()
    {
        Topmost = !Topmost;
        _settings.Topmost = Topmost;
        UpdatePinState();
        _ = _store.SaveSettingsAsync(_settings);
    }

    private void UpdatePinState()
    {
        PinButton.Opacity = Topmost ? 1 : 0.55;
        PinButton.ToolTip = Topmost ? "取消始终置顶" : "始终置顶";
    }

    private void RootBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed &&
            e.OriginalSource is not System.Windows.Controls.Button)
        {
            DragMove();
        }
    }

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        SystemEvents.UserPreferenceChanged -= SystemEvents_OnUserPreferenceChanged;
        _dailyTimer.Stop();
    }

    private void MainWindow_OnLocationChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded || WindowState != WindowState.Normal)
        {
            return;
        }

        _settings.Left = Left;
        _settings.Top = Top;
        _ = _store.SaveSettingsAsync(_settings);
    }

    private void RestoreWindowPosition()
    {
        if (Environment.GetCommandLineArgs().Contains("--qa", StringComparer.OrdinalIgnoreCase))
        {
            Left = 60;
            Top = 60;
            return;
        }

        if (_settings.Left is double left &&
            _settings.Top is double top &&
            left >= SystemParameters.VirtualScreenLeft - Width + 40 &&
            left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 40 &&
            top >= SystemParameters.VirtualScreenTop - Height + 40 &&
            top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 40)
        {
            Left = left;
            Top = top;
            return;
        }

        Left = SystemParameters.WorkArea.Right - Width - 28;
        Top = SystemParameters.WorkArea.Bottom - Height - 28;
    }

    private void SystemEvents_OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        Dispatcher.Invoke(ApplyTheme);
    }

    private void ApplyTheme()
    {
        var dark = IsWindowsDarkMode();
        Resources["CardBackgroundBrush"] = new SolidColorBrush(
            dark ? System.Windows.Media.Color.FromArgb(239, 26, 32, 42) : System.Windows.Media.Color.FromArgb(239, 246, 250, 255));
        Resources["PrimaryTextBrush"] = new SolidColorBrush(
            dark ? System.Windows.Media.Color.FromRgb(242, 246, 251) : System.Windows.Media.Color.FromRgb(26, 31, 39));
        Resources["SecondaryTextBrush"] = new SolidColorBrush(
            dark ? System.Windows.Media.Color.FromRgb(184, 194, 207) : System.Windows.Media.Color.FromRgb(78, 88, 102));
        Resources["FrameBorderBrush"] = new SolidColorBrush(
            dark ? System.Windows.Media.Color.FromArgb(100, 255, 255, 255) : System.Windows.Media.Color.FromArgb(180, 255, 255, 255));
    }

    private static bool IsWindowsDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    private void ApplyRoundedCorners()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var preference = DwmwcpRound;
        _ = DwmSetWindowAttribute(
            handle,
            DwmwaWindowCornerPreference,
            ref preference,
            sizeof(int));
    }

    private void CaptureQaScreenshot()
    {
        UpdateLayout();
        var dpi = VisualTreeHelper.GetDpi(this);
        var pixelWidth = Math.Max(1, (int)Math.Round(ActualWidth * dpi.DpiScaleX));
        var pixelHeight = Math.Max(1, (int)Math.Round(ActualHeight * dpi.DpiScaleY));
        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            96 * dpi.DpiScaleX,
            96 * dpi.DpiScaleY,
            PixelFormats.Pbgra32);
        bitmap.Render(this);

        var folder = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "qa"));
        Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(folder, "implementation.png"));
        encoder.Save(stream);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int valueSize);
}
