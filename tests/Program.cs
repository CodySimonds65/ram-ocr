using RamOcr;
static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
var trigger = new OcrTrigger { Text = "banner", TextMode = TextMatchMode.Contains };
Require(TextMatcher.Evaluate("A BANNER appeared", trigger).Matches, "Text matching failed.");
var color = new OcrTrigger { Color = new Rgba32(255, 0, 0, 255), Tolerance = 0, RequiredMatchPercentage = 1 };
Require(ColorMatcher.Evaluate([new Rgba32(255, 0, 0, 255)], color).Matches, "Color matching failed.");
var fired = 0; var coordinator = new TriggerCoordinator(_ => { fired++; return Task.CompletedTask; });
await coordinator.ObserveAsync(trigger, new(true, 1, "match"), DateTime.UtcNow);
await coordinator.ObserveAsync(trigger, new(true, 1, "match"), DateTime.UtcNow.AddMilliseconds(600));
Require(fired == 1, "Trigger debounce did not fire once.");
Require(!CaptureAvailability.CanCapture(true, true), "Minimized capture was not suspended.");
Console.WriteLine("RAM OCR tests passed.");
