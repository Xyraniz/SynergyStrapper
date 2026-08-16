using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Wpf.Ui.Mvvm.Contracts;

using SynergyStrapper.UI.Elements.Dialogs;

namespace SynergyStrapper.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for FastFlagEditorPage.xaml
    /// </summary>
    public partial class FastFlagEditorPage
    {
        private readonly ObservableCollection<FastFlag> _fastFlagList = new();
        private readonly List<string> _validPrefixes = new()
        {
            "FFlag", "DFFlag", "SFFlag", "FInt", "DFInt", "FString", "DFString", "FLog", "DFLog"
        };
        private readonly List<string> _historyEntries = new();

        private readonly Regex _boolFilterPattern = new("^(?:true|false)(;[\\d]{1,})+$", RegexOptions.IgnoreCase);
        private readonly Regex _intFilterPattern = new("^([\\d]{1,})?(;[\\d]{1,})+$", RegexOptions.IgnoreCase);
        private readonly Regex _stringFilterPattern = new("^[^;]*(;[\\d]{1,})+$", RegexOptions.IgnoreCase);

        private bool _showPresets;
        private string _searchFilter = "";

        public FastFlagEditorPage()
        {
            InitializeComponent();
            RefreshProfiles();
            UpdateStatistics();
        }

        private void RefreshProfiles()
        {
            ProfileComboBox.Items.Clear();
            foreach (string name in App.FastFlags.GetBackupNames())
                ProfileComboBox.Items.Add(name);

            if (ProfileComboBox.Items.Count > 0)
                ProfileComboBox.SelectedIndex = 0;
        }

        private void ReloadList()
        {
            var selectedEntry = DataGrid.SelectedItem as FastFlag;
            _fastFlagList.Clear();

            var presetFlags = FastFlagManager.PresetFlags.Values;

            foreach (var pair in App.FastFlags.Prop.OrderBy(x => x.Key))
            {
                if (!_showPresets && presetFlags.Contains(pair.Key))
                    continue;

                if (!pair.Key.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var entry = new FastFlag
                {
                    Name = pair.Key,
                    Value = pair.Value?.ToString() ?? String.Empty
                };
                entry.UpdateMetadata(GetTags(entry.Name), presetFlags.Contains(entry.Name));
                _fastFlagList.Add(entry);
            }

            if (DataGrid.ItemsSource is null)
                DataGrid.ItemsSource = _fastFlagList;

            UpdateStatistics();

            if (selectedEntry is null)
                return;

            var newSelectedEntry = _fastFlagList.FirstOrDefault(x => x.Name == selectedEntry.Name);
            if (newSelectedEntry is null)
                return;

            DataGrid.SelectedItem = newSelectedEntry;
            DataGrid.ScrollIntoView(newSelectedEntry);
        }

        private IReadOnlyList<string> GetTags(string name)
        {
            var tags = new List<string>();

            if (name.StartsWith("FFlag", StringComparison.Ordinal) || name.StartsWith("DFFlag", StringComparison.Ordinal) || name.StartsWith("SFFlag", StringComparison.Ordinal))
                tags.Add("Boolean");
            else if (name.StartsWith("FInt", StringComparison.Ordinal) || name.StartsWith("DFInt", StringComparison.Ordinal))
                tags.Add("Integer");
            else if (name.StartsWith("FString", StringComparison.Ordinal) || name.StartsWith("DFString", StringComparison.Ordinal))
                tags.Add("String");
            else if (name.StartsWith("FLog", StringComparison.Ordinal) || name.StartsWith("DFLog", StringComparison.Ordinal))
                tags.Add("Log");

            if (name.StartsWith("D", StringComparison.Ordinal))
                tags.Add("Debug");

            if (FastFlagManager.PresetFlags.Values.Contains(name))
                tags.Add("Preset");

            return tags;
        }

        private void UpdateStatistics()
        {
            TotalFlagsTextBlock.Text = $"Flags: {App.FastFlags.Prop.Count}";
            HistoryCountTextBlock.Text = $"Changes: {_historyEntries.Count}";
            UpdateHealthStatus();
        }

        private void UpdateHealthStatus()
        {
            IReadOnlyList<FastFlagHealthIssue> issues = App.FastFlags.GetHealthIssues();
            var availability = SynergyStrapper.Integrations.FastFlagAvailabilityService.CheckCurrentFlags();
            int unavailable = availability.Count(x => x.Status == SynergyStrapper.Integrations.FastFlagAvailability.Unavailable);
            int errors = issues.Count(x => x.Severity == FastFlagHealthSeverity.Error);
            int warnings = issues.Count - errors;

            HealthStatusTextBlock.Text = issues.Count == 0 && unavailable == 0
                ? "Health: OK"
                : $"Health: {errors} error{(errors == 1 ? "" : "s")}, {warnings} warning{(warnings == 1 ? "" : "s")}, {unavailable} unavailable";
        }

        private void HealthCheckButton_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<FastFlagHealthIssue> issues = App.FastFlags.GetHealthIssues();
            IReadOnlyList<FastFlagChange> changes = App.FastFlags.GetPendingChanges();
            int errors = issues.Count(x => x.Severity == FastFlagHealthSeverity.Error);
            int warnings = issues.Count - errors;

            var report = new StringBuilder();
            var availability = SynergyStrapper.Integrations.FastFlagAvailabilityService.CheckCurrentFlags();
            int unavailable = availability.Count(x => x.Status == SynergyStrapper.Integrations.FastFlagAvailability.Unavailable);
            report.AppendLine($"FastFlag Health Check: {errors} error{(errors == 1 ? "" : "s")}, {warnings} warning{(warnings == 1 ? "" : "s")}, {unavailable} unavailable");
            report.AppendLine(changes.Count == 0
                ? "No pending changes since the last save."
                : $"Pending changes: {changes.Count}");

            if (changes.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Pending changes:");
                foreach (FastFlagChange change in changes.Take(40))
                {
                    string detail = change.Kind switch
                    {
                        FastFlagChangeKind.Added => $"added = {change.After}",
                        FastFlagChangeKind.Removed => $"removed = {change.Before}",
                        _ => $"{change.Before} -> {change.After}"
                    };
                    report.AppendLine($"  [{change.Kind}] {change.Name}: {detail}");
                }

                if (changes.Count > 40)
                    report.AppendLine($"  ...and {changes.Count - 40} more.");
            }

            if (issues.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Issues:");
                foreach (FastFlagHealthIssue issue in issues.Take(40))
                    report.AppendLine($"  [{issue.Severity}] {issue.Name}: {issue.Message}");

                if (issues.Count > 40)
                    report.AppendLine($"  ...and {issues.Count - 40} more.");
            }

            if (unavailable > 0)
            {
                report.AppendLine();
                report.AppendLine("Flags not present in the cached allowlist:");
                foreach (var entry in availability.Where(x => x.Status == SynergyStrapper.Integrations.FastFlagAvailability.Unavailable).Take(40))
                    report.AppendLine($"  [Unavailable] {entry.Name} (source {entry.SourceRevision})");
            }

            UpdateHealthStatus();
            Frontend.ShowMessageBox(report.ToString(), errors > 0 ? MessageBoxImage.Error : warnings > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private void AddHistory(string message)
        {
            string entry = $"{DateTime.Now:HH:mm:ss}  {message}";
            _historyEntries.Insert(0, entry);

            if (_historyEntries.Count > 200)
                _historyEntries.RemoveAt(_historyEntries.Count - 1);

            HistoryListBox.Items.Clear();
            foreach (string historyEntry in _historyEntries)
                HistoryListBox.Items.Add(historyEntry);

            UpdateStatistics();
        }

        private void ClearSearch(bool refresh = true)
        {
            SearchTextBox.Text = "";
            _searchFilter = "";

            if (refresh)
                ReloadList();
        }

        private void ShowAddDialog()
        {
            var dialog = new AddFastFlagDialog();
            dialog.ShowDialog();

            if (dialog.Result != MessageBoxResult.OK)
                return;

            if (dialog.Tabs.SelectedIndex == 0)
                AddSingle(dialog.FlagNameTextBox.Text.Trim(), dialog.FlagValueTextBox.Text);
            else if (dialog.Tabs.SelectedIndex == 1)
                ImportJSON(dialog.JsonTextBox.Text);
        }

        private void AddSingle(string name, string value)
        {
            FastFlag? entry;

            if (App.FastFlags.GetValue(name) is null)
            {
                if (!ValidateFlagEntry(name, value))
                {
                    ShowAddDialog();
                    return;
                }

                entry = new FastFlag
                {
                    Name = name,
                    Value = value
                };
                entry.UpdateMetadata(GetTags(name), FastFlagManager.PresetFlags.Values.Contains(name));

                App.FastFlags.SetValue(entry.Name, entry.Value);
                _fastFlagList.Add(entry);
                AddHistory($"Added {name}");

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                    ClearSearch();
                else
                    ReloadList();
            }
            else
            {
                Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);

                bool refresh = false;

                if (!_showPresets && FastFlagManager.PresetFlags.Values.Contains(name))
                {
                    TogglePresetsButton.IsChecked = true;
                    _showPresets = true;
                    refresh = true;
                }

                if (!name.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase))
                {
                    ClearSearch(false);
                    refresh = true;
                }

                if (refresh)
                    ReloadList();

                entry = _fastFlagList.FirstOrDefault(x => x.Name == name);
            }

            DataGrid.SelectedItem = entry;
            DataGrid.ScrollIntoView(entry);
        }

        private void ImportJSON(string json)
        {
            Dictionary<string, object>? list;

            json = json.Trim();
            if (!json.StartsWith('{'))
                json = '{' + json;

            if (!json.EndsWith('}'))
            {
                int lastIndex = json.LastIndexOf('}');
                json = lastIndex == -1 ? json + '}' : json[..(lastIndex + 1)];
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };
                list = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);
                if (list is null)
                    throw new Exception("JSON deserialization returned null");
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(String.Format(Strings.Menu_FastFlagEditor_InvalidJSON, ex.Message), MessageBoxImage.Error);
                ShowAddDialog();
                return;
            }

            if (list.Count > 16)
            {
                var result = Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_LargeConfig, MessageBoxImage.Warning, MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            var conflictingFlags = App.FastFlags.Prop.Where(x => list.ContainsKey(x.Key)).Select(x => x.Key).ToList();
            bool overwriteConflicting = false;

            if (conflictingFlags.Count > 0)
            {
                string message = String.Format(
                    Strings.Menu_FastFlagEditor_ConflictingImport,
                    conflictingFlags.Count,
                    String.Join(", ", conflictingFlags.Take(25))
                );

                if (conflictingFlags.Count > 25)
                    message += "...";

                overwriteConflicting = Frontend.ShowMessageBox(message, MessageBoxImage.Question, MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            }

            int imported = 0;
            foreach (var pair in list)
            {
                if (App.FastFlags.Prop.ContainsKey(pair.Key) && !overwriteConflicting)
                    continue;

                if (pair.Value is null)
                    continue;

                string? value = pair.Value.ToString();
                if (value is null || !ValidateFlagEntry(pair.Key, value))
                    continue;

                App.FastFlags.SetValue(pair.Key, pair.Value);
                imported++;
            }

            if (imported > 0)
                AddHistory($"Imported {imported} flag{(imported == 1 ? "" : "s")} from JSON");

            ClearSearch();
        }

        private bool ValidateFlagEntry(string name, string value)
        {
            string lowerValue = value.ToLowerInvariant();
            string errorMessage = "";

            if (!_validPrefixes.Any(name.StartsWith))
                errorMessage = Strings.Menu_FastFlagEditor_InvalidPrefix;
            else if (!name.All(x => char.IsLetterOrDigit(x) || x == '_'))
                errorMessage = Strings.Menu_FastFlagEditor_InvalidCharacter;

            if (name.EndsWith("_PlaceFilter") || name.EndsWith("_DataCenterFilter"))
                errorMessage = !ValidateFilter(name, value) ? Strings.Menu_FastFlagEditor_InvalidPlaceFilter : "";
            else if ((name.StartsWith("FInt") || name.StartsWith("DFInt")) && !Int32.TryParse(value, out _))
                errorMessage = Strings.Menu_FastFlagEditor_InvalidNumberValue;
            else if ((name.StartsWith("FFlag") || name.StartsWith("DFFlag")) && lowerValue != "true" && lowerValue != "false")
                errorMessage = Strings.Menu_FastFlagEditor_InvalidBoolValue;

            if (!String.IsNullOrEmpty(errorMessage))
            {
                Frontend.ShowMessageBox(String.Format(errorMessage, name), MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private bool ValidateFilter(string name, string value)
        {
            if (name.StartsWith("FFlag") || name.StartsWith("DFFlag"))
                return _boolFilterPattern.IsMatch(value);
            if (name.StartsWith("FInt") || name.StartsWith("DFInt"))
                return _intFilterPattern.IsMatch(value);
            if (name.StartsWith("FString") || name.StartsWith("DFString") || name.StartsWith("FLog") || name.StartsWith("DFLog"))
                return _stringFilterPattern.IsMatch(value);

            return false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e) => ReloadList();

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Row.DataContext is not FastFlag entry || e.EditingElement is not TextBox textbox)
                return;

            switch (e.Column.Header)
            {
                case "Name":
                    string oldName = entry.Name;
                    string newName = textbox.Text.Trim();

                    if (newName == oldName)
                        return;

                    if (App.FastFlags.GetValue(newName) is not null)
                    {
                        Frontend.ShowMessageBox(Strings.Menu_FastFlagEditor_AlreadyExists, MessageBoxImage.Information);
                        e.Cancel = true;
                        textbox.Text = oldName;
                        return;
                    }

                    if (!ValidateFlagEntry(newName, entry.Value))
                    {
                        e.Cancel = true;
                        textbox.Text = oldName;
                        return;
                    }

                    App.FastFlags.SetValue(oldName, null);
                    App.FastFlags.SetValue(newName, entry.Value);
                    entry.Name = newName;
                    entry.UpdateMetadata(GetTags(newName), FastFlagManager.PresetFlags.Values.Contains(newName));
                    AddHistory($"Renamed {oldName} to {newName}");
                    ReloadList();
                    break;

                case "Value":
                    string oldValue = entry.Value;
                    string newValue = textbox.Text;

                    if (!ValidateFlagEntry(entry.Name, newValue))
                    {
                        e.Cancel = true;
                        textbox.Text = oldValue;
                        return;
                    }

                    if (oldValue == newValue)
                        return;

                    App.FastFlags.SetValue(entry.Name, newValue);
                    entry.Value = newValue;
                    AddHistory($"Changed {entry.Name}");
                    break;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is INavigationWindow window)
                window.Navigate(typeof(FastFlagsPage));
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) => ShowAddDialog();

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var entries = DataGrid.SelectedItems.OfType<FastFlag>().ToList();
            if (entries.Count == 0)
                return;

            foreach (FastFlag entry in entries)
            {
                _fastFlagList.Remove(entry);
                App.FastFlags.SetValue(entry.Name, null);
                AddHistory($"Deleted {entry.Name}");
            }

            ReloadList();
        }

        private void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.FastFlags.Prop.Count == 0)
                return;

            if (Frontend.ShowMessageBox(
                    $"Delete all {App.FastFlags.Prop.Count} FastFlags? This cannot be undone unless you have a profile or backup.",
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            int count = App.FastFlags.Prop.Count;
            foreach (string name in App.FastFlags.Prop.Keys.ToList())
                App.FastFlags.SetValue(name, null);

            AddHistory($"Deleted all {count} FastFlags");
            ReloadList();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton button)
                return;

            _showPresets = button.IsChecked ?? false;
            ReloadList();
        }

        private void ExportJSONButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(BuildJson());
            Frontend.ShowMessageBox("FastFlags JSON copied to the clipboard.", MessageBoxImage.Information);
        }

        private void SaveJSONButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt",
                Title = "Save FastFlags JSON",
                FileName = "ClientAppSettings.json"
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            try
            {
                File.WriteAllText(saveFileDialog.FileName, BuildJson(), Encoding.UTF8);
                Frontend.ShowMessageBox("FastFlags JSON saved successfully.", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("FastFlagEditorPage::SaveJSON", ex);
                Frontend.ShowMessageBox($"The JSON file could not be saved: {ex.Message}", MessageBoxImage.Error);
            }
        }

        private static string BuildJson() => JsonSerializer.Serialize(
            App.FastFlags.Prop,
            new JsonSerializerOptions { WriteIndented = true }
        );

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Enter a profile name:", "Save FastFlag profile", "My profile").Trim();
            if (String.IsNullOrWhiteSpace(name))
                return;

            if (!App.FastFlags.SaveBackup(name))
            {
                Frontend.ShowMessageBox("The profile could not be saved. Use a name of up to 64 characters without path separators.", MessageBoxImage.Warning);
                return;
            }

            RefreshProfiles();
            ProfileComboBox.SelectedItem = name;
            Frontend.ShowMessageBox($"Profile '{name}' saved.", MessageBoxImage.Information);
        }

        private void LoadProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is not string name)
                return;

            MessageBoxResult result = Frontend.ShowMessageBox(
                "Replace current FastFlags with this profile? Choose No to merge its values into the current configuration.",
                MessageBoxImage.Question,
                MessageBoxButton.YesNoCancel
            );
            if (result == MessageBoxResult.Cancel)
                return;

            bool loaded = App.FastFlags.LoadBackup(name, result == MessageBoxResult.Yes);
            if (!loaded)
            {
                Frontend.ShowMessageBox("The profile could not be loaded.", MessageBoxImage.Error);
                return;
            }

            AddHistory($"Loaded profile '{name}'");
            ReloadList();
            Frontend.ShowMessageBox($"Profile '{name}' loaded.", MessageBoxImage.Information);
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem is not string name)
                return;

            if (Frontend.ShowMessageBox($"Delete profile '{name}'?", MessageBoxImage.Question, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            if (!App.FastFlags.DeleteBackup(name))
            {
                Frontend.ShowMessageBox("The profile could not be deleted.", MessageBoxImage.Warning);
                return;
            }

            RefreshProfiles();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textbox)
                return;

            _searchFilter = textbox.Text;
            ReloadList();
        }
    }
}
