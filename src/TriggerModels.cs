namespace RamOcr;

public enum TriggerKind { Text, Color }
public enum TextMatchMode { Contains, Exact, Regex }

public sealed record TriggerRegion(double X, double Y, double Width, double Height)
{
    public TriggerRegion Normalize() => new(Math.Clamp(X, 0, 1), Math.Clamp(Y, 0, 1), Math.Clamp(Width, 0, 1), Math.Clamp(Height, 0, 1));
}

public sealed record OcrTrigger
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = "New trigger";
    public string AccountId { get; init; } = string.Empty;
    public TriggerKind Kind { get; init; }
    public TriggerRegion Region { get; init; } = new(0, 0, 1, 1);
    public string Text { get; init; } = string.Empty;
    public TextMatchMode TextMode { get; init; } = TextMatchMode.Contains;
    public bool IgnoreCase { get; init; } = true;
    public Rgba32 Color { get; init; } = new(255, 255, 255, 255);
    public int Tolerance { get; init; } = 20;
    public double RequiredMatchPercentage { get; init; } = 0.6;
    public TimeSpan Cooldown { get; init; } = TimeSpan.FromSeconds(5);
    public IReadOnlyList<OcrInputAction> Actions { get; init; } = [];
}

public sealed record OcrInputAction(
    string Kind,
    int VirtualKey = 0,
    int ScanCode = 0,
    bool Extended = false,
    int Button = 0,
    int WheelDelta = 0,
    double NormalizedX = 0,
    double NormalizedY = 0,
    long OffsetMicroseconds = 0);

public readonly record struct Rgba32(byte R, byte G, byte B, byte A);
public readonly record struct TriggerEvaluation(bool Matches, double Confidence, string Detail);
