namespace RamOcr;

public interface IOcrTextRecognizer
{
    Task<string> RecognizeAsync(ReadOnlyMemory<Rgba32> pixels, CancellationToken cancellationToken);
}

public sealed record OcrAccountCycleResult(string AccountId, bool Accepted, string Code, string Message, int TriggerCount);

/// <summary>Cycles validated accounts through one foreground session for capture, matching, and trigger input.</summary>
public sealed class ForegroundOcrRunner
{
    private readonly ManagedAccountRegistry _accounts;
    private readonly ForegroundOcrInputSender _input;
    private readonly Func<ManagedAccountSnapshot, IWindowCapture> _captureFactory;
    private readonly IOcrTextRecognizer _textRecognizer;
    private readonly Dictionary<string, DateTime> _lastFiredUtc = new(StringComparer.Ordinal);
    private readonly object _cooldownGate = new();

    public ForegroundOcrRunner(
        PluginClient client,
        ManagedAccountRegistry accounts,
        Func<ManagedAccountSnapshot, IWindowCapture> captureFactory,
        IOcrTextRecognizer textRecognizer)
    {
        _accounts = accounts;
        _input = new ForegroundOcrInputSender(client);
        _captureFactory = captureFactory;
        _textRecognizer = textRecognizer;
    }

    public async Task<IReadOnlyList<OcrAccountCycleResult>> RunCycleAsync(
        IReadOnlyList<OcrTrigger> triggers,
        CancellationToken cancellationToken)
    {
        var accounts = _accounts.Snapshot()
            .Where(account => !account.IsMinimized && account.ClientWidth > 0 && account.ClientHeight > 0)
            .Where(account => triggers.Any(trigger => string.IsNullOrWhiteSpace(trigger.AccountId) || trigger.AccountId == account.AccountId))
            .ToArray();
        if (accounts.Length == 0) return [];
        var opened = await _input.OpenSessionAsync(accounts.Select(account => account.AccountId).ToArray(), "ocr", cancellationToken).ConfigureAwait(false);
        if (!opened.Accepted || opened.SessionId is null)
            return accounts.Select(account => new OcrAccountCycleResult(account.AccountId, false, opened.Code, opened.Message, 0)).ToArray();

        var results = new List<OcrAccountCycleResult>(accounts.Length);
        try
        {
            foreach (var account in accounts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var activated = await _input.ActivateAsync(opened.SessionId, account.AccountId, cancellationToken).ConfigureAwait(false);
                if (!activated.Accepted)
                {
                    results.Add(new(account.AccountId, false, activated.Code, activated.Message, 0));
                    continue;
                }
                // Refresh geometry after activation so a move/resize/DPI
                // change cannot make capture read a stale screen rectangle.
                var liveAccount = _accounts.TryGet(account.AccountId, out var refreshed) ? refreshed : account;
                var accountTriggers = triggers.Where(trigger => string.IsNullOrWhiteSpace(trigger.AccountId) || trigger.AccountId == liveAccount.AccountId).ToArray();
                await using var capture = _captureFactory(liveAccount);
                var fired = 0;
                foreach (var trigger in accountTriggers)
                {
                    var pixels = await capture.CaptureAsync(trigger.Region.Normalize(), cancellationToken).ConfigureAwait(false);
                    var evaluation = trigger.Kind == TriggerKind.Color
                        ? ColorMatcher.Evaluate(pixels, trigger)
                        : TextMatcher.Evaluate(await _textRecognizer.RecognizeAsync(pixels, cancellationToken).ConfigureAwait(false), trigger);
                    if (!evaluation.Matches || !CooldownElapsed(trigger)) continue;
                    fired++;
                    MarkFired(trigger);
                    if (trigger.Actions.Count > 0)
                        await _input.SendInSessionAsync(opened.SessionId, account.AccountId, trigger.Actions, cancellationToken).ConfigureAwait(false);
                }
                results.Add(new(account.AccountId, true, "ok", $"Capture and trigger evaluation completed; {fired} trigger(s) fired.", fired));
            }
            return results;
        }
        finally { await _input.CloseSessionAsync(opened.SessionId).ConfigureAwait(false); }
    }

    private bool CooldownElapsed(OcrTrigger trigger)
    {
        lock (_cooldownGate)
            return !_lastFiredUtc.TryGetValue(trigger.Id, out var last) || DateTime.UtcNow - last >= trigger.Cooldown;
    }

    private void MarkFired(OcrTrigger trigger)
    {
        lock (_cooldownGate) _lastFiredUtc[trigger.Id] = DateTime.UtcNow;
    }
}
