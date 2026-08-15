using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    public partial class ServerBrowserPage
    {
        private readonly List<RobloxServerEntry> _allServers = new();
        private readonly List<RobloxServerEntry> _visibleServers = new();
        private CancellationTokenSource? _loadCancellation;
        private CancellationTokenSource? _placeIdChangeCancellation;
        private DateTime _nextAllowedLoad = DateTime.MinValue;
        private int _limit = 50;
        private string _sort = "ping";
        private int _backoffSeconds = 20;

        public ServerBrowserPage()
        {
            InitializeComponent();

            // Keep the grid bound even while the page is empty. SelectionChanged
            // events can fire during InitializeComponent before the field exists,
            // so ApplySorting also guards the control below.
            if (ServersGrid is not null)
                ServersGrid.ItemsSource = _visibleServers;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string? lastPlaceId = App.Settings.Prop?.LastServerPlaceId;
            PlaceIdTextBox.Text = lastPlaceId ?? String.Empty;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _placeIdChangeCancellation?.Cancel();
            _placeIdChangeCancellation?.Dispose();
            _placeIdChangeCancellation = null;

            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = null;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadServersAsync(true);
        }

        private async void PlaceIdTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            string value = PlaceIdTextBox.Text?.Trim() ?? String.Empty;
            _placeIdChangeCancellation?.Cancel();

            if (String.IsNullOrEmpty(value))
            {
                _allServers.Clear();
                _visibleServers.Clear();
                ApplySorting();
                StatusTextBlock.Text = "Enter a Place ID to load public servers.";
                return;
            }

            if (!value.All(Char.IsDigit) || value.Length < 3)
            {
                StatusTextBlock.Text = "Place ID must contain only numbers.";
                return;
            }

            _placeIdChangeCancellation = new CancellationTokenSource();
            CancellationToken token = _placeIdChangeCancellation.Token;

            try
            {
                // Wait until the user has finished typing instead of querying
                // Roblox with every partial Place ID.
                await Task.Delay(400, token);
                if (String.Equals(value, PlaceIdTextBox.Text?.Trim(), StringComparison.Ordinal))
                    await LoadServersAsync(false);
            }
            catch (OperationCanceledException)
            {
                // A newer text change replaced this pending request.
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SortComboBox.SelectedItem is ComboBoxItem item)
            {
                _sort = item.Tag?.ToString() ?? "ping";
                ApplySorting();
            }
        }

        private void LimitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LimitComboBox.SelectedItem is ComboBoxItem item
                && Int32.TryParse(item.Tag?.ToString(), out int limit)
                && limit > 0)
            {
                _limit = limit;
                ApplySorting();
            }
        }

        private bool TryGetPlaceId(out string placeId)
        {
            placeId = PlaceIdTextBox.Text?.Trim() ?? String.Empty;

            if (String.IsNullOrEmpty(placeId))
            {
                StatusTextBlock.Text = "Enter a Place ID to load public servers.";
                return false;
            }

            if (!placeId.All(Char.IsDigit))
            {
                StatusTextBlock.Text = "Place ID must contain only numbers.";
                return false;
            }

            return true;
        }

        private async Task LoadServersAsync(bool manual)
        {
            if (!TryGetPlaceId(out string placeId))
                return;

            if (!manual && DateTime.UtcNow < _nextAllowedLoad)
                return;

            if (manual && DateTime.UtcNow < _nextAllowedLoad)
            {
                StatusTextBlock.Text = $"Please wait {(int)(_nextAllowedLoad - DateTime.UtcNow).TotalSeconds + 1}s before refreshing again.";
                return;
            }

            _nextAllowedLoad = DateTime.UtcNow.AddSeconds(20);
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = new CancellationTokenSource();
            CancellationToken token = _loadCancellation.Token;
            StatusTextBlock.Text = "Loading public servers...";

            try
            {
                string url = $"https://games.roblox.com/v1/games/{placeId}/servers/Public?sortOrder=Asc&limit=100";
                using HttpResponseMessage response = await App.HttpClient.GetAsync(url, token);
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _backoffSeconds = Math.Min(_backoffSeconds * 2, 120);
                    _nextAllowedLoad = DateTime.UtcNow.AddSeconds(_backoffSeconds);
                    StatusTextBlock.Text = $"Roblox rate-limited the request. Try again in {_backoffSeconds}s.";
                    return;
                }

                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync(token);
                RobloxServerResponse? result = JsonSerializer.Deserialize<RobloxServerResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data is null)
                {
                    StatusTextBlock.Text = "Roblox returned no server data.";
                    return;
                }

                _backoffSeconds = 20;
                App.Settings.Prop.LastServerPlaceId = placeId;
                App.Settings.Save();

                _allServers.Clear();
                _allServers.AddRange(result.Data.Where(x => x is not null).Select(x => x!));
                ApplySorting();
                StatusTextBlock.Text = $"Loaded {_allServers.Count} public servers. Double-click a row or use Join selected.";
            }
            catch (OperationCanceledException)
            {
                // A newer request replaced this one or the page was unloaded.
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerBrowserPage::LoadServers", ex);
                StatusTextBlock.Text = "Could not load servers. Check your connection and try again.";
            }
        }

        private void ApplySorting()
        {
            // ComboBox selection events can fire while InitializeComponent is
            // still building the visual tree. The DataGrid is declared later in
            // XAML, so it is not safe to refresh it until construction is done.
            if (ServersGrid is null)
                return;

            IEnumerable<RobloxServerEntry> ordered = _sort switch
            {
                "players" => _allServers.OrderBy(x => x.Playing).ThenBy(x => x.Ping),
                "mostPlayers" => _allServers.OrderByDescending(x => x.Playing).ThenBy(x => x.Ping),
                _ => _allServers.OrderBy(x => x.Ping <= 0 ? Int32.MaxValue : x.Ping).ThenBy(x => x.Playing)
            };

            List<RobloxServerEntry> visible = ordered.Take(_limit).ToList();
            _visibleServers.Clear();
            _visibleServers.AddRange(visible);
            ServersGrid.Items.Refresh();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServersGrid.SelectedItem is RobloxServerEntry server && !String.IsNullOrWhiteSpace(server.Id))
                Clipboard.SetText(server.Id);
        }

        private void JoinButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServersGrid.SelectedItem is RobloxServerEntry server)
                JoinServer(server.Id);
        }

        private void ServersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ServersGrid.SelectedItem is RobloxServerEntry server)
                JoinServer(server.Id);
        }

        private void JoinServer(string serverId)
        {
            if (!TryGetPlaceId(out string placeId) || String.IsNullOrWhiteSpace(serverId))
                return;

            try
            {
                string uri = $"roblox://experiences/start?placeId={Uri.EscapeDataString(placeId)}&serverId={Uri.EscapeDataString(serverId)}";
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox($"Roblox could not be launched.\n\n{ex.Message}", MessageBoxImage.Error);
            }
        }
    }

    public sealed class RobloxServerResponse
    {
        public List<RobloxServerEntry> Data { get; set; } = new();
    }

    public sealed class RobloxServerEntry
    {
        public string Id { get; set; } = "";
        public int MaxPlayers { get; set; }
        public int Playing { get; set; }
        public double Fps { get; set; }
        public int Ping { get; set; }
        public string PlayerSummary => $"{Playing} / {MaxPlayers}";
        public string PingSummary => Ping > 0 ? $"{Ping} ms" : "n/a";
        public string FpsSummary => Fps > 0 ? $"{Fps:0}" : "n/a";
    }
}
