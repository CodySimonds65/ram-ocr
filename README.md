# RAM OCR

RAM OCR is a standalone Apache-2.0 plugin for account-relative text and color triggers. It is designed around occlusion-safe capture adapters, bounded matching, two-match debounce, two-miss re-arm, and a five-second default cooldown. Actions are sent through the launcher host broker, including the RAM Macros action bridge.

No frames are retained by the trigger model. Diagnostic image export is an explicit user action. The host rejects unavailable or minimized targets without activating them.

Build with the .NET 8 Windows SDK. The release package contains `plugin.json`, `ram-ocr.exe`, `plugin.zip`, `plugin.sha256`, and a pinned Ed25519 signature.
