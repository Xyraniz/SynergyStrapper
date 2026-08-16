using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SynergyStrapper.Integrations;

namespace SynergyStrapper.UI.Elements.Overlay;

public sealed class GameOverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExToolwindow = 0x80;
    private const int WsExNoactivate = 0x08000000;

    private readonly ActivityWatcher? _activityWatcher;
    private readonly TextBlock _label;
    private readonly DispatcherTimer _timer;

    public GameOverlayWindow(ActivityWatcher? activityWatcher)
    {
        _activityWatcher = activityWatcher;
        Width = 360;
        Height = 56;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        IsHitTestVisible = false;
        Opacity = App.Settings.Prop.Features.OverlayDimmerEnabled ? 0.72 : 0.96;

        _label = new TextBlock
        {
            Margin = new Thickness(12, 8, 12, 8),
            Foreground = Brushes.White,
            FontSize = 14,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            TextWrapping = TextWrapping.Wrap
        };
        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 20, 20, 20)),
            CornerRadius = new CornerRadius(8),
            Child = _label
        };

        SourceInitialized += OnSourceInitialized;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateOverlay();
        Loaded += (_, _) =>
        {
            PositionOverRoblox();
            UpdateOverlay();
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int style = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, style | WsExTransparent | WsExToolwindow | WsExNoactivate);
    }

    private void UpdateOverlay()
    {
        if (!App.Settings.Prop.Features.EnableGameOverlay)
        {
            Hide();
            return;
        }

        var features = App.Settings.Prop.Features;
        var parts = new List<string>();
        if (features.OverlayShowClock)
            parts.Add(DateTime.Now.ToString("HH:mm:ss"));
        if (features.OverlayShowRegion)
        {
            string region = _activityWatcher?.Data.MachineAddress is { Length: > 0 } address && GlobalCache.ServerLocation.TryGetValue(address, out string? location)
                ? location ?? "region unknown"
                : "region unknown";
            parts.Add(region);
        }
        if (features.OverlayShowPing && _activityWatcher?.Data.MachineAddressValid == true)
        {
            _ = UpdatePingAsync(_activityWatcher.Data.MachineAddress);
        }
        if (parts.Count == 0)
            parts.Add("Synergy overlay");
        _label.Text = string.Join("  ·  ", parts);
        PositionOverRoblox();
    }

    private async Task UpdatePingAsync(string address)
    {
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(address, 500);
            if (reply.Status == IPStatus.Success)
                await Dispatcher.InvokeAsync(() => _label.Text += $"  ·  {reply.RoundtripTime} ms");
        }
        catch
        {
            // Ping is informational and must never interrupt the watcher.
        }
    }

    private void PositionOverRoblox()
    {
        try
        {
            Process? process = Utilities.GetProcessesSafe().FirstOrDefault(x => x.ProcessName.Equals(App.RobloxPlayerAppName, StringComparison.OrdinalIgnoreCase));
            if (process is null || process.MainWindowHandle == IntPtr.Zero)
            {
                process?.Dispose();
                return;
            }
            if (GetWindowRect(process.MainWindowHandle, out RectNative rect))
            {
                Left = rect.Left + 16;
                Top = rect.Top + 16;
            }
            process.Dispose();
        }
        catch { }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RectNative lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectNative
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
