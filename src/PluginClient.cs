using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;

namespace RamOcr;

public sealed class PluginClient : IAsyncDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly string _token; private readonly string _pluginId; private readonly string _manifestPath; private readonly string[] _capabilities;
    public PluginClient(string pipeName, string token, string pluginId, string manifestPath)
    { _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous); _token = token; _pluginId = pluginId; _manifestPath = manifestPath; using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath)); _capabilities = doc.RootElement.GetProperty("capabilities").EnumerateArray().Select(item => item.GetString()!).ToArray(); }
    public static PluginClient? FromArgs(string[] args)
    {
        try
        {
            if (args is null) return null;
            if (!TryParseArgs(args, out var values)) return null;
            if (!TryGetValue(values, "pipe", out var pipe) || !TryGetValue(values, "plugin-id", out var id)) return null;

            string? token;
            if (values.TryGetValue("token", out var inlineToken)) token = inlineToken;
            else if (TryGetValue(values, "token-file", out var tokenFile))
            {
                token = File.ReadAllText(tokenFile).Trim();
                try { File.Delete(tokenFile); } catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            else return null;

            if (string.IsNullOrWhiteSpace(token)) return null;
            var manifest = Path.Combine(AppContext.BaseDirectory, "plugin.json");
            if (!File.Exists(manifest)) manifest = Path.Combine(AppContext.BaseDirectory, "manifest.json");
            return File.Exists(manifest) ? new PluginClient(pipe, token, id, manifest) : null;
        }
        catch (ArgumentException) { return null; }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (KeyNotFoundException) { return null; }
    }
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    { await _pipe.ConnectAsync(5000, cancellationToken); var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(_manifestPath, cancellationToken))).ToLowerInvariant(); using var currentProcess = System.Diagnostics.Process.GetCurrentProcess(); await SendAsync("plugin.hello", new { pluginId = _pluginId, token = _token, protocolMajor = 1, protocolMinor = 0, manifestSha256 = hash, declaredCapabilities = _capabilities, processId = Environment.ProcessId, processStartTimeUtcTicks = currentProcess.StartTime.ToUniversalTime().Ticks }, cancellationToken); var accepted = await ReadAsync(cancellationToken) ?? throw new InvalidDataException("Plugin host closed the handshake."); if (!string.Equals(accepted.Type, "host.accept", StringComparison.Ordinal)) throw new InvalidDataException("Plugin host rejected the handshake."); }
    public async Task SendAsync(string type, object payload, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new Envelope(type, Guid.NewGuid().ToString("N"), JsonSerializer.SerializeToElement(payload, Json.Options)), Json.Options);
        if (bytes.Length > 1024 * 1024) throw new InvalidDataException("Plugin message too large.");
        var header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        await _writeGate.WaitAsync(cancellationToken); try { await _pipe.WriteAsync(header, cancellationToken); await _pipe.WriteAsync(bytes, cancellationToken); await _pipe.FlushAsync(cancellationToken); } finally { _writeGate.Release(); }
    }
    private async Task<Envelope?> ReadAsync(CancellationToken cancellationToken)
    { var header = new byte[4]; if (!await ReadExactlyAsync(header, cancellationToken)) return null; var length = BinaryPrimitives.ReadInt32LittleEndian(header); if (length <= 0 || length > 1024 * 1024) throw new InvalidDataException("Plugin message too large."); var bytes = new byte[length]; if (!await ReadExactlyAsync(bytes, cancellationToken)) return null; return JsonSerializer.Deserialize<Envelope>(bytes, Json.Options); }
    private async Task<bool> ReadExactlyAsync(byte[] buffer, CancellationToken cancellationToken)
    { var offset = 0; while (offset < buffer.Length) { var read = await _pipe.ReadAsync(buffer.AsMemory(offset), cancellationToken); if (read == 0) return false; offset += read; } return true; }
    private static bool TryParseArgs(string[] args, out Dictionary<string, string> result)
    {
        result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var argument = args[i];
            if (argument is null) return false;
            if (!argument.StartsWith("--", StringComparison.Ordinal)) continue;
            var key = argument[2..];
            if (key.Length == 0) return false;
            if (key.Equals("ram-plugin", StringComparison.OrdinalIgnoreCase)) { result[key] = "true"; continue; }
            if (i + 1 >= args.Length || args[i + 1] is null || args[i + 1].StartsWith("--", StringComparison.Ordinal)) return false;
            result[key] = args[++i];
        }
        return true;
    }
    private static bool TryGetValue(IReadOnlyDictionary<string, string> values, string key, out string value)
        => values.TryGetValue(key, out value!) && !string.IsNullOrWhiteSpace(value);
    public async ValueTask DisposeAsync() { _writeGate.Dispose(); await _pipe.DisposeAsync(); }
    private sealed record Envelope(string Type, string RequestId, JsonElement Payload, int ProtocolMajor = 1, int ProtocolMinor = 0);
    private static class Json { public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web); }
}
