# WinSmtpRelay MSI Installer

## Versioning and Releases

Version is defined in one place: `Directory.Build.props` at the repo root.

```xml
<Version>N.N.N</Version>
<VersionSuffix>betaN-buildN</VersionSuffix>
```

This flows to all assemblies, the Admin UI `/api/server/info` endpoint, and the MSI installer.

### How to release a new version

Lets Claude do a release on github:

```
fetch latest updates from github
bump to 1.0.0-beta1-build24 (next version, no need to change)

D:\CODE\github\ardimedia\winsmtprelay\setup\WinSmtpRelay.Setup\README.md
update all release references (readme.md, etc) to the new version, check also this:
    <Version>N.N.N</Version>
    <VersionSuffix>betaN-buildN</VersionSuffix>
    git commit -m "bump version to N.N.N-betaN-buildN"
    git tag vN.N.N-betaN-buildN

make sure copyright has the current year: Copyright (c) 2026 ARDIMEDIA
make sure the version below is higher than the current one, otherwise aboard with a message
github push
```

**1. Bump the version** in `Directory.Build.props`:

```xml
<!-- Examples: -->
<Version>N.N.N</Version>  <VersionSuffix>betaN-buildN</VersionSuffix>   <!-- pre-release, next build -->
<Version>N.N.N</Version>  <VersionSuffix></VersionSuffix>               <!-- stable release -->
<Version>N.N.N</Version>  <VersionSuffix>betaN-buildN</VersionSuffix>   <!-- next minor -->
```

**2. Commit and tag:**

```bash
git add Directory.Build.props
git commit -m "bump version to N.N.N-betaN-buildN"
git tag vN.N.N-betaN-buildN
git push origin main --tags
```

**3. CI does the rest automatically:**

- Builds and tests the solution
- Stamps all assemblies with the version
- Builds both MSI variants (self-contained + framework-dependent)
- Creates a GitHub Release (pre-release if tag contains `alpha`, `beta`, `rc`, or `preview`)
- Uploads both MSIs as release assets

### How it works internally

| Property | Example | Where it appears |
|----------|---------|-----------------|
| `Version` | `N.N.N` | MSI version, `AssemblyVersion`, `FileVersion` |
| `VersionSuffix` | `betaN-buildN` | Pre-release label |
| `InformationalVersion` | `N.N.N-betaN-buildN` | Admin UI `/api/server/info`, Windows file details |
| MSI product name | `WinSmtpRelay N.N.N Beta N Build N` | Add/Remove Programs |

MSI only supports 3-part numeric versions. The pre-release label goes in the product display name (`Package.wxs`), not the MSI version field.

## Overview

WiX v5 project that produces two MSI variants for WinSmtpRelay.

| Variant | Output | Prerequisite |
|---------|--------|-------------|
| Self-contained | `WinSmtpRelay-{version}-x64-self-contained.msi` | None |
| Framework-dependent | `WinSmtpRelay-{version}-x64.msi` | .NET 10 Runtime (x64) |

Both share the same `UpgradeCode` — installing one replaces the other.

## Building locally

**Step 1: Publish the service** (from repo root)

```bash
# Self-contained (~80-120 MB MSI)
dotnet publish src/WinSmtpRelay.Service/WinSmtpRelay.Service.csproj -c Release -r win-x64 --self-contained true -o publish-sc/

# Framework-dependent (~5-15 MB MSI)
dotnet publish src/WinSmtpRelay.Service/WinSmtpRelay.Service.csproj -c Release -r win-x64 --self-contained false -o publish-fd/
```

**Step 2: Build the MSI**

```bash
# Self-contained
dotnet build setup/WinSmtpRelay.Setup/WinSmtpRelay.Setup.wixproj -c Release -p:HarvestPath=../../publish-sc/ -p:ProductVersion=N.N.N -p:SelfContained=true

# Framework-dependent
dotnet build setup/WinSmtpRelay.Setup/WinSmtpRelay.Setup.wixproj -c Release -p:HarvestPath=../../publish-fd/ -p:ProductVersion=N.N.N -p:SelfContained=false
```

Output: `setup/WinSmtpRelay.Setup/bin/Release/*.msi`

## MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `HarvestPath` | `../../publish/` | Path to `dotnet publish` output |
| `ProductVersion` | `N.N.N` | 3-part version number (no pre-release suffix) |
| `SelfContained` | *(empty)* | Set to `true` for self-contained variant; changes output filename and skips .NET runtime check |

## What the MSI does

**Service:** Installs `WinSmtpRelay` as a Windows Service (auto-start, NetworkService account, auto-restart on failure).

**Firewall:** Opens inbound TCP ports 25, 587, 465 (SMTP) and 8025 (Admin UI).

**Permissions:** Grants NetworkService write access to the install directory (`Program Files\WinSmtpRelay`) for the SQLite database.

**Config preservation:** `appsettings.json` is never overwritten on upgrade and is kept on uninstall. Users can safely edit it without losing changes.

## File structure

| File | Purpose |
|------|---------|
| `WinSmtpRelay.Setup.wixproj` | WiX v5 project — SDK, extensions, HarvestDirectory config |
| `Package.wxs` | Package identity, UpgradeCode, MajorUpgrade, directory layout, feature |
| `ServiceComponents.wxs` | Service install, config preservation, firewall rules, directory permissions |
| `License.rtf` | MIT license in RTF format (displayed during install) |

## Important: UpgradeCode

The `UpgradeCode` GUID (`2cbb38d8-5035-40db-b30b-0e55f24ec496`) in `Package.wxs` must **never change**. It is the permanent identity that allows MSI upgrades to detect and replace previous installations.

## Verification checklist

After building locally, verify:

```powershell
# Check MSI size (self-contained should be 60-120 MB, framework-dependent 5-15 MB)
Get-Item setup\WinSmtpRelay.Setup\bin\Release\*.msi | Select-Object Name, @{N='MB';E={[math]::Round($_.Length/1MB,1)}}

# Check MSI contents (should list hundreds of files for self-contained)
msiexec /a "setup\WinSmtpRelay.Setup\bin\Release\WinSmtpRelay.Setup.msi" /qn TARGETDIR=C:\temp\msi-check
Get-ChildItem C:\temp\msi-check -Recurse -File | Measure-Object
Remove-Item C:\temp\msi-check -Recurse -Force
```

Install/upgrade tests (on a VM or test machine):

1. **Fresh install (self-contained)** — service starts, Admin UI on `:8025`, firewall rules created
2. **Fresh install (framework-dependent)** — same, but requires .NET 10 runtime
3. **FD on machine without .NET 10** — installer shows helpful error message
4. **Upgrade** — edit `appsettings.json`, install newer MSI, verify config preserved
5. **Cross-variant upgrade** — install SC, then upgrade to FD (and vice versa) — seamless replacement
6. **Uninstall** — service removed, firewall rules removed, `appsettings.json` preserved

