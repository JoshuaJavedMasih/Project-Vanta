# Vanta Release Guide

## 1. Verify the repository

```powershell
dotnet restore Vanta.sln
dotnet build Vanta.sln -c Debug -p:Platform=x64 --no-restore
dotnet test Vanta.Tests\Vanta.Tests.csproj -c Debug -p:Platform=x64 --no-build
```

## 2. Build the self-contained release

```powershell
dotnet build Vanta\Vanta.csproj -c Release -p:Platform=x64 --no-restore
```

The runnable output is located at:

```text
Vanta\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\
```

Use the build output rather than a generic `dotnet publish -o` directory. WinUI requires its compiled XAML (`.xbf`) and package resource index (`Vanta.pri`) beside the executable.

## 3. Package

Create a ZIP from all files inside the output directory. Confirm the package contains at minimum:

- `Vanta.exe`
- `Vanta.dll`
- `Vanta.pri`
- `App.xbf`, `MainWindow.xbf`, and `MainPage.xbf`
- `Themes/VantaTheme.xbf`
- `Assets/VantaLogo.png`
- `COPYRIGHT.txt`
- Windows App SDK and self-contained .NET runtime files

## 4. Smoke test

Extract the ZIP to a new directory and start `Vanta.exe`. Verify Overview, one detail page, About, and Export Report.

## 5. GitHub Release

Create a version tag such as `v1.0.0`, publish the release notes from `CHANGELOG.md`, and attach the runtime ZIP as a GitHub Release asset. Do not commit the large runtime ZIP to the normal repository history.

Generate a checksum:

```powershell
Get-FileHash .\Vanta-win-x64.zip -Algorithm SHA256
```
