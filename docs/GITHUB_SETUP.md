# GitHub Repository Setup

This repository package is ready to upload to a new GitHub repository.

## Recommended repository details

- **Name:** `Vanta`
- **Description:** Premium Windows system monitoring, performance analytics, and hardware intelligence built with C# and WinUI 3.
- **Suggested topics:** `csharp`, `dotnet`, `winui3`, `windows`, `system-monitor`, `hardware-monitoring`, `performance-analytics`

## Push with Git

After extracting the ZIP, open PowerShell in the extracted `Vanta` folder and run:

```powershell
git init
git add .
git commit -m "Initial Vanta release"
git branch -M main
git remote add origin https://github.com/YOUR-ACCOUNT/Vanta.git
git push -u origin main
```

Replace `YOUR-ACCOUNT` with the GitHub account or organization that owns the new repository.

## Upload through GitHub's website

1. Create an empty repository without adding a README, license, or `.gitignore`.
2. Extract this ZIP locally.
3. Upload the contents of the extracted `Vanta` folder.
4. Commit the uploaded files to the `main` branch.

GitHub's browser upload has per-file and request-size limits. This source repository is intentionally kept free of compiled runtime binaries.

## Publish the Windows build

Use GitHub **Releases** for the separate `Vanta-win-x64.zip` runtime package:

1. Open **Releases** and choose **Draft a new release**.
2. Create a tag such as `v1.0.0`.
3. Attach `Vanta-win-x64.zip` as the Windows x64 download.
4. Copy the relevant notes from `CHANGELOG.md` into the release description.
5. Publish the release.

This keeps the source history compact while giving users a direct downloadable application package.
