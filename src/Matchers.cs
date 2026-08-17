using System.Text.RegularExpressions;

namespace RamOcr;

public static class TextMatcher
{
    public static TriggerEvaluation Evaluate(string actual, OcrTrigger trigger)
    {
        var comparison = trigger.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var matches = trigger.TextMode switch
        {
            TextMatchMode.Exact => string.Equals(actual.Trim(), trigger.Text.Trim(), comparison),
            TextMatchMode.Contains => actual.Contains(trigger.Text, comparison),
            TextMatchMode.Regex => Regex.IsMatch(actual, trigger.Text, trigger.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None, TimeSpan.FromMilliseconds(100)),
            _ => false
        };
        return new TriggerEvaluation(matches, matches ? 1 : 0, matches ? "text matched" : "text did not match");
    }
}

public static class ColorMatcher
{
    public static TriggerEvaluation Evaluate(ReadOnlySpan<Rgba32> pixels, OcrTrigger trigger)
    {
        if (pixels.Length == 0) return new(false, 0, "capture contained no pixels");
        var matched = 0;
        foreach (var pixel in pixels)
        {
            var distance = Math.Abs(pixel.R - trigger.Color.R) + Math.Abs(pixel.G - trigger.Color.G) + Math.Abs(pixel.B - trigger.Color.B);
            if (distance <= trigger.Tolerance * 3) matched++;
        }
        var percentage = matched / (double)pixels.Length;
        return new(percentage >= trigger.RequiredMatchPercentage, percentage, $"{percentage:P0} of pixels matched");
    }
}
