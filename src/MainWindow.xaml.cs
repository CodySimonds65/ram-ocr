using System.Collections.ObjectModel;
using System.Windows;

namespace RamOcr;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<OcrTrigger> _triggers = [];
    private OcrTrigger? _selected;
    private bool _paused;
    public MainWindow() { InitializeComponent(); TriggerList.ItemsSource = _triggers; }
    private void NewTrigger_Click(object sender, RoutedEventArgs e) { var trigger = new OcrTrigger { Name = $"Trigger {_triggers.Count + 1}" }; _triggers.Add(trigger); TriggerList.SelectedItem = trigger; }
    private void TriggerList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { _selected = TriggerList.SelectedItem as OcrTrigger; EditorText.Text = _selected is null ? "Select a trigger." : $"{_selected.Name}\nKind: {_selected.Kind}\nRegion: {_selected.Region}\nCooldown: {_selected.Cooldown.TotalSeconds:0}s\nActions are dispatched through the host input broker."; }
    private void Test_Click(object sender, RoutedEventArgs e) => StatusText.Text = _selected is null ? "Select a trigger first." : "Capture test requested; minimized or unavailable windows are suspended.";
    private void Save_Click(object sender, RoutedEventArgs e) => StatusText.Text = _selected is null ? "Select a trigger first." : "Trigger saved locally.";
    private void Pause_Click(object sender, RoutedEventArgs e) { _paused = !_paused; StatusText.Text = _paused ? "All triggers paused." : "Triggers resumed."; }
}
