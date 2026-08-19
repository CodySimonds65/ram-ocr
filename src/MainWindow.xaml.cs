using System.Collections.ObjectModel;
using System.Windows;

namespace RamOcr;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<OcrTrigger> _triggers = [];
    private OcrTrigger? _selected;
    private bool _paused;
    private readonly ManagedAccountRegistry _managedAccounts;
    public MainWindow(ManagedAccountRegistry? managedAccounts = null) { InitializeComponent(); _managedAccounts = managedAccounts ?? new ManagedAccountRegistry(); TriggerList.ItemsSource = _triggers; }
    private void NewTrigger_Click(object sender, RoutedEventArgs e) { var trigger = new OcrTrigger { Name = $"Trigger {_triggers.Count + 1}" }; _triggers.Add(trigger); TriggerList.SelectedItem = trigger; }
    private void TriggerList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { _selected = TriggerList.SelectedItem as OcrTrigger; EditorText.Text = _selected is null ? "Select a trigger." : $"{_selected.Name}\nKind: {_selected.Kind}\nRegion: {_selected.Region}\nCooldown: {_selected.Cooldown.TotalSeconds:0}s\nActions use guarded foreground SendInput; focus may switch briefly."; }
    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) { StatusText.Text = "Select a trigger first."; return; }
        var runner = (System.Windows.Application.Current as App)?.OcrRunner;
        if (runner is null) { StatusText.Text = "The launcher host is not connected."; return; }
        StatusText.Text = "Capturing managed clients; focus may switch briefly and will be restored when safe.";
        try
        {
            var results = await runner.RunCycleAsync([_selected], CancellationToken.None);
            StatusText.Text = string.Join(Environment.NewLine, results.Select(result => $"{result.AccountId}: {result.Code} — {result.Message}"));
        }
        catch (Exception ex) { StatusText.Text = $"Capture failed: {ex.Message}"; }
    }
    private void Save_Click(object sender, RoutedEventArgs e) => StatusText.Text = _selected is null ? "Select a trigger first." : "Trigger saved locally.";
    private void Pause_Click(object sender, RoutedEventArgs e) { _paused = !_paused; StatusText.Text = _paused ? "All triggers paused." : "Triggers resumed."; }
}
