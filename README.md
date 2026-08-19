# RAM OCR

RAM OCR is a standalone Apache-2.0 plugin for account-relative text and color triggers. It cycles validated accounts through one guarded foreground session for capture and trigger actions. Focus may switch briefly and returns to the prior client when safe; user takeover cancels rather than reclaiming focus. It is designed around bounded capture/matching, debounce, re-arm, and a five-second default cooldown.

No frames are retained by the trigger model. Diagnostic image export is an explicit user action. The host rejects unavailable or minimized targets, stale HWND identities, and legacy background-message requests (`foreground-required`).

Build with the .NET 8 Windows SDK. The release package contains `plugin.json`, `ram-ocr.exe`, `plugin.zip`, `plugin.sha256`, and a pinned Ed25519 signature.

## Official releases

After a PR is merged, the repository workflow publishes the matching semantic version automatically. If both manifests still contain the latest published version, the workflow creates a patch-only release commit and publishes the next patch version; major and minor version changes remain explicit. Configure `RAM_PLUGIN_SIGNING_KEY` (Ed25519 private PEM) and `RAM_PLUGIN_SIGNING_PUBLIC_KEY` (matching public PEM) repository secrets first. The public key must match the launcher trust anchor; missing secrets fail closed and never publish unsigned official assets. Manual dispatch remains available as a recovery path.
