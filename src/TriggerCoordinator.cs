namespace RamOcr;

public sealed class TriggerCoordinator
{
    private readonly Dictionary<string, TriggerState> _states = new(StringComparer.Ordinal);
    private readonly Func<OcrTrigger, Task> _fire;

    public TriggerCoordinator(Func<OcrTrigger, Task> fire) => _fire = fire;

    public async Task<TriggerEvaluation> ObserveAsync(OcrTrigger trigger, TriggerEvaluation evaluation, DateTime utcNow)
    {
        if (!_states.TryGetValue(trigger.Id, out var state)) state = new TriggerState();
        if (evaluation.Matches)
        {
            state.ConsecutiveMatches++;
            state.ConsecutiveMisses = 0;
            if (!state.Armed || utcNow - state.LastFiredUtc < trigger.Cooldown || state.ConsecutiveMatches < 2)
            {
                _states[trigger.Id] = state;
                return evaluation;
            }
            state.Armed = false;
            state.LastFiredUtc = utcNow;
            await _fire(trigger);
        }
        else
        {
            state.ConsecutiveMisses++;
            state.ConsecutiveMatches = 0;
            if (state.ConsecutiveMisses >= 2) state.Armed = true;
        }
        _states[trigger.Id] = state;
        return evaluation;
    }

    private sealed class TriggerState { public int ConsecutiveMatches; public int ConsecutiveMisses; public bool Armed = true; public DateTime LastFiredUtc = DateTime.MinValue; }
}

public interface IWindowCapture : IAsyncDisposable
{
    bool IsAvailable { get; }
    Task<Rgba32[]> CaptureAsync(TriggerRegion region, CancellationToken cancellationToken);
}

public sealed class CaptureAvailability
{
    public static bool CanCapture(bool hasWindow, bool minimized) => hasWindow && !minimized;
}
