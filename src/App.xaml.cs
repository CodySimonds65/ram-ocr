using System.Windows;
namespace RamOcr;
public partial class App : Application
{
    private PluginClient? _client;
    protected override void OnStartup(StartupEventArgs e)
    { base.OnStartup(e); MainWindow = new MainWindow(); MainWindow.Show(); _client = PluginClient.FromArgs(e.Args); if (_client is not null) _ = ConnectHostAsync(_client); }
    private static async Task ConnectHostAsync(PluginClient client)
    {
        try
        {
            await client.ConnectAsync();
            using var shutdown = new CancellationTokenSource();
            await SendHeartbeatsAsync(client, shutdown.Token);
        }
        catch { await client.DisposeAsync(); }
    }
    private static async Task SendHeartbeatsAsync(PluginClient client, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try { while (await timer.WaitForNextTickAsync(cancellationToken)) await client.SendAsync("plugin.heartbeat", new { utc = DateTime.UtcNow }, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    protected override void OnExit(ExitEventArgs e) { _client?.DisposeAsync().AsTask().GetAwaiter().GetResult(); base.OnExit(e); }
}
