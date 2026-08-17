using System.Windows;
namespace RamOcr;
public partial class App : Application
{
    private PluginClient? _client;
    protected override void OnStartup(StartupEventArgs e)
    { base.OnStartup(e); MainWindow = new MainWindow(); MainWindow.Show(); _client = PluginClient.FromArgs(e.Args); if (_client is not null) _ = ConnectHostAsync(_client); }
    private static async Task ConnectHostAsync(PluginClient client) { try { await client.ConnectAsync(); } catch { await client.DisposeAsync(); } }
    protected override void OnExit(ExitEventArgs e) { _client?.DisposeAsync().AsTask().GetAwaiter().GetResult(); base.OnExit(e); }
}
