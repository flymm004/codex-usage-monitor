# Contributing

Thanks for helping improve Codex Usage Monitor.

## Before opening an issue

- Include your Windows version, display scaling, and Codex CLI version.
- Describe the steps to reproduce the problem and the result you expected.
- Remove account emails, authentication data, and live quota values from logs and screenshots.
- Check whether the issue still occurs with the latest release.

## Changes

1. Keep changes focused and explain the user-visible effect.
2. For localization changes, include the locale and check that labels fit at 100% and 200% scaling.
3. Build with `./build.ps1 -Version 0.1.0-alpha.1` on Windows.
4. Do not commit generated files from `artifacts/`, local app data, credentials, or screenshots of a real account.
5. Open a pull request with a short summary and the build result.

There are no external package dependencies. The CI workflow compiles and packages the app on Windows.
