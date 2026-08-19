namespace RamOcr;

public sealed class ManagedAccountRegistry
{
    private readonly object _gate = new();
    private IReadOnlyList<ManagedAccountSnapshot> _accounts = [];

    public void Replace(IReadOnlyList<ManagedAccountSnapshot> accounts)
    {
        lock (_gate) _accounts = accounts.Where(account => account.IsRunning && !string.IsNullOrWhiteSpace(account.AccountId)).ToArray();
    }

    public IReadOnlyList<ManagedAccountSnapshot> Snapshot()
    {
        lock (_gate) return _accounts.ToArray();
    }

    public bool TryGet(string accountId, out ManagedAccountSnapshot account)
    {
        lock (_gate)
        {
            account = _accounts.FirstOrDefault(item => string.Equals(item.AccountId, accountId, StringComparison.Ordinal))!;
            return account is not null;
        }
    }
}

public sealed record ManagedAccountSnapshot(
    string AccountId,
    string Label,
    int ProcessId,
    long ProcessStartTimeUtcTicks,
    nint WindowHandle,
    int ClientX,
    int ClientY,
    int ClientWidth,
    int ClientHeight,
    uint Dpi,
    bool IsMinimized,
    DateTime LastActivityUtc,
    bool IsRunning,
    nint RootWindowHandle = 0);
