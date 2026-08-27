# Contributing to Vanta

Thank you for your interest in Vanta.

Vanta is a proprietary, source-available project owned by Merhatta Softwares. Opening an issue or pull request does not grant a license to the project, its branding, or its assets.

## Before contributing

1. Open an issue describing the problem or proposed improvement.
2. Keep changes focused and avoid unrelated formatting rewrites.
3. Do not add telemetry, remote analytics, personal-data collection, fabricated sensor readings, or always-on administrator requirements.
4. Preserve the provider → service → model → view-model → UI boundary.
5. Add or update tests for behavioral changes.

## Build and test

```powershell
dotnet restore Vanta.sln
dotnet build Vanta.sln -c Debug -p:Platform=x64 --no-restore
dotnet test Vanta.Tests\Vanta.Tests.csproj -c Debug -p:Platform=x64 --no-build
```

## Pull requests

- Explain the problem and the chosen approach.
- Include screenshots for visible UI changes.
- State which Windows version and architecture were tested.
- Confirm that build and tests pass.
- Do not include `bin`, `obj`, `work`, `outputs`, generated packages, or user-specific files.

Merhatta Softwares may accept, revise, or decline contributions at its discretion.
