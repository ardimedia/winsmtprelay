# WinSmtpRelay MSI Installer

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
dotnet build setup/WinSmtpRelay.Setup/WinSmtpRelay.Setup.wixproj -c Release -p:PublishDir=../../publish-sc/ -p:ProductVersion=1.0.0 -p:SelfContained=true

# Framework-dependent
dotnet build setup/WinSmtpRelay.Setup/WinSmtpRelay.Setup.wixproj -c Release -p:PublishDir=../../publish-fd/ -p:ProductVersion=1.0.0 -p:SelfContained=false
```

Output: `setup/WinSmtpRelay.Setup/bin/Release/*.msi`

## MSBuild Properties

| Property | Default | Description |
|----------|---------|-------------|
| `PublishDir` | `../../publish/` | Path to `dotnet publish` output |
| `ProductVersion` | `1.0.0` | 3-part version number (no pre-release suffix) |
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

## Version convention

MSI only supports 3-part numeric versions (`major.minor.build`). Pre-release labels like "Beta 1" go in the product display name, not the version field. The display name is set in `Package.wxs` — update it when moving from beta to stable.
