using System.Text.Json;
namespace RamOcr;
public partial class App : System.Windows.Application
{
    private PluginClient? _client;
    public ManagedAccountRegistry ManagedAccounts { get; } = new();
    public ForegroundOcrRunner? OcrRunner { get; private set; }
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    { base.OnStartup(e); MainWindow = new MainWindow(ManagedAccounts); MainWindow.Show(); _client = PluginClient.FromArgs(e.Args); if (_client is not null) _ = ConnectHostAsync(_client); }
    private static async Task ConnectHostAsync(PluginClient client)
    {
        try
        {
            await client.ConnectAsync();
            using var shutdown = new CancellationTokenSource();
            await client.SendAsync("account.events.subscribe", new { }, shutdown.Token);
            ((App)Current).OcrRunner = new ForegroundOcrRunner(client, ((App)Current).ManagedAccounts,
                account => new GdiWindowCapture(account), new UnavailableOcrTextRecognizer());
            var heartbeat = SendHeartbeatsAsync(client, shutdown.Token);
            var refresh = RefreshAccountsAsync(client, shutdown.Token);
            while (true)
            {
                var envelope = await client.ReceiveAsync(shutdown.Token);
                if (envelope is null) break;
                if (envelope.Type == "accounts.result")
                {
                    var accounts = PluginClient.Deserialize<List<ManagedAccountSnapshot>>(envelope.Payload.GetProperty("accounts"));
                    if (accounts is not null) ((App)Current).ManagedAccounts.Replace(accounts);
                }
            }
            shutdown.Cancel();
            try { await Task.WhenAll(heartbeat, refresh); } catch (OperationCanceledException) { }
        }
        catch { await client.DisposeAsync(); }
    }
    private static async Task RefreshAccountsAsync(PluginClient client, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            do { await client.SendAsync("accounts.list", new { }, cancellationToken); }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    private static async Task SendHeartbeatsAsync(PluginClient client, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try { while (await timer.WaitForNextTickAsync(cancellationToken)) await client.SendAsync("plugin.heartbeat", new { utc = DateTime.UtcNow }, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }
    protected override void OnExit(System.Windows.ExitEventArgs e) { _client?.DisposeAsync().AsTask().GetAwaiter().GetResult(); base.OnExit(e); }
}
