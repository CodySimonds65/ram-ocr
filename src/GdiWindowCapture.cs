using System.Drawing;
using System.Drawing.Imaging;

namespace RamOcr;

/// <summary>
/// Small foreground-only capture adapter. The runner activates the account
/// before calling this adapter, so it never reads another application's pixels.
/// </summary>
public sealed class GdiWindowCapture(ManagedAccountSnapshot account) : IWindowCapture
{
    public bool IsAvailable => !account.IsMinimized && account.ClientWidth > 0 && account.ClientHeight > 0;

    public Task<Rgba32[]> CaptureAsync(TriggerRegion region, CancellationToken cancellationToken)
    {
        if (!IsAvailable) return Task.FromResult<Rgba32[]>([]);
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = region.Normalize();
        var x = account.ClientX + (int)Math.Round(normalized.X * account.ClientWidth);
        var y = account.ClientY + (int)Math.Round(normalized.Y * account.ClientHeight);
        var width = Math.Clamp((int)Math.Round(normalized.Width * account.ClientWidth), 1, account.ClientWidth);
        var height = Math.Clamp((int)Math.Round(normalized.Height * account.ClientHeight), 1, account.ClientHeight);
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        var pixels = new Rgba32[width * height];
        var index = 0;
        for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
        {
            var color = bitmap.GetPixel(column, row);
            pixels[index++] = new Rgba32(color.R, color.G, color.B, color.A);
        }
        return Task.FromResult(pixels);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class UnavailableOcrTextRecognizer : IOcrTextRecognizer
{
    public bool IsAvailable => false;

    public Task<string> RecognizeAsync(ReadOnlyMemory<Rgba32> pixels, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(string.Empty);
    }
}
