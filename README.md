# Codex Usage Monitor

A Windows Codex CLI usage monitor that lives in the system tray and shows account limits, remaining percentages, and reset times at a glance. It supports eight interface languages and high-DPI displays.

[简体中文](README.zh-CN.md)

> **Status: Alpha.** This project depends on the local Codex CLI App Server response format, which may change between Codex versions.

## Features

- Shows service-reported short- and long-window percentages and reset times.
- Shows the short-window percentage in the notification-area icon.
- Refreshes every two minutes and supports manual refresh.
- Supports English, Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, German, and French. English is first in the language menu; System default is last.
- Handles high-DPI displays and Per-Monitor V2 scaling.

## Requirements

- Windows 10 or 11
- .NET Framework 4.8 or newer
- Codex CLI installed and signed in with a ChatGPT account. API-key-only accounts do not expose ChatGPT plan limits through this view.

## Run

The release provides a ZIP package and individual EXE/configuration assets. The ZIP is the simplest option: extract it and run `CodexUsageMonitor.exe`. If downloading the executable separately, also download the matching `.exe.config` asset and keep it beside the EXE; the configuration enables the app's high-DPI behavior. Pushing a `v*` tag builds these files and creates a draft GitHub Release for review.

Initial releases are unsigned, so Windows may show a SmartScreen prompt. Check the release notes and SHA-256 file before running a downloaded build.

## Build from source

Run this from Windows PowerShell 5.1 or PowerShell 7:

```powershell
.\build.ps1 -Version 0.1.0-alpha.1
```

The script uses the .NET Framework C# compiler included with Windows, needs no NuGet packages, and writes the executable, ZIP package, direct-download EXE/config assets, and SHA-256 checksums under the ignored `artifacts/` directory. The same script runs in GitHub Actions.

## Data and privacy

The app starts the locally installed `codex app-server` over stdio and requests account state and rate limits. The App Server uses the existing Codex sign-in and communicates with Codex services to return the account limits. This app does not contact a developer-operated server, collect telemetry, or save authentication tokens. It does not display or persist the account email. The selected language is stored under the current Windows user's application data directory.

The displayed values come from the Codex service response; this app does not estimate how many tasks remain. The app-server protocol and returned fields can change, so a Codex CLI update may temporarily break compatibility.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Please do not attach screenshots or logs containing live account limits, email addresses, tokens, or other private data to public issues.

## License and affiliation

Licensed under the [MIT License](LICENSE).

This is an independent community utility and is not affiliated with or endorsed by OpenAI. “Codex” is used only to identify the compatible product.
