namespace RamOcr;

public sealed class ForegroundOcrInputSender(PluginClient client)
{
    public async Task<(bool Accepted, string Code, string Message, string? SessionId)> OpenSessionAsync(IReadOnlyList<string> accountIds, string purpose, CancellationToken cancellationToken)
    {
        var response = await client.RequestAsync("input.session.open", new { accountIds = accountIds.ToArray(), purpose, restoreForeground = true }, cancellationToken).ConfigureAwait(false);
        if (response.Type != "input.session.result") return (false, "rejected", "The launcher rejected foreground automation.", null);
        var accepted = response.Payload.TryGetProperty("accepted", out var acceptedElement) && acceptedElement.GetBoolean();
        var sessionId = response.Payload.TryGetProperty("sessionId", out var id) ? id.GetString() : null;
        return (accepted && !string.IsNullOrWhiteSpace(sessionId),
            response.Payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            response.Payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "",
            sessionId);
    }

    public async Task<(bool Accepted, string Code, string Message)> ActivateAsync(string sessionId, string accountId, CancellationToken cancellationToken)
    {
        var response = await client.RequestAsync("input.session.activate", new { sessionId, accountId }, cancellationToken).ConfigureAwait(false);
        return ParseSession(response);
    }

    public async Task<(bool Accepted, string Code, string Message)> SendInSessionAsync(string sessionId, string accountId, IReadOnlyList<OcrInputAction> actions, CancellationToken cancellationToken)
    {
        if (actions.Count == 0) return (true, "no-action", "The OCR trigger has no input actions.");
        var response = await client.RequestAsync("input.post", new
        {
            accountId, sessionId, deliveryIntent = "foreground-real",
            events = actions.Select(action => new { kind = action.Kind, virtualKey = action.VirtualKey, scanCode = action.ScanCode, extended = action.Extended, button = action.Button, wheelDelta = action.WheelDelta, normalizedX = action.NormalizedX, normalizedY = action.NormalizedY, offsetMicroseconds = action.OffsetMicroseconds }).ToArray()
        }, cancellationToken).ConfigureAwait(false);
        if (response.Type != "input.result") return (false, "rejected", "The launcher rejected OCR input.");
        return (
            response.Payload.TryGetProperty("accepted", out var accepted) && accepted.GetBoolean(),
            response.Payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            response.Payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "");
    }

    public async Task CloseSessionAsync(string sessionId)
    {
        try { await client.RequestAsync("input.session.close", new { sessionId, restoreForeground = true, userInitiated = false }, CancellationToken.None).ConfigureAwait(false); }
        catch { }
    }

    public async Task<(bool Accepted, string Code, string Message)> SendAsync(string accountId, IReadOnlyList<OcrInputAction> actions, string purpose, CancellationToken cancellationToken)
    {
        var opened = await OpenSessionAsync([accountId], purpose, cancellationToken).ConfigureAwait(false);
        if (!opened.Accepted || opened.SessionId is null) return (false, opened.Code, opened.Message);
        try
        {
            var activated = await ActivateAsync(opened.SessionId, accountId, cancellationToken).ConfigureAwait(false);
            if (!activated.Accepted) return activated;
            return await SendInSessionAsync(opened.SessionId, accountId, actions, cancellationToken).ConfigureAwait(false);
        }
        finally { await CloseSessionAsync(opened.SessionId).ConfigureAwait(false); }
    }

    private static (bool Accepted, string Code, string Message) ParseSession(PluginClient.Envelope response)
    {
        if (response.Type != "input.session.result") return (false, "rejected", "The launcher rejected foreground automation.");
        return (
            response.Payload.TryGetProperty("accepted", out var accepted) && accepted.GetBoolean(),
            response.Payload.TryGetProperty("code", out var code) ? code.GetString() ?? "unknown" : "unknown",
            response.Payload.TryGetProperty("message", out var message) ? message.GetString() ?? "" : "");
    }
}
