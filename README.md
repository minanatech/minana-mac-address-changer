# Minana MAC Address Changer for Windows

[![Minana MAC Address Changer for Windows by Minana Tech](assets/repository-banner.svg)](https://minanatech.com)

[![Windows build](https://github.com/minanatech/minana-mac-address-changer/actions/workflows/build.yml/badge.svg)](https://github.com/minanatech/minana-mac-address-changer/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-75e5c0)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078d4)](https://github.com/minanatech/minana-mac-address-changer/releases)
[![Website: Minana Tech](https://img.shields.io/badge/Website-minanatech.com-75e5c0)](https://minanatech.com)

**Minana MAC Address Changer** is a free, open-source **MAC address changer for Windows 10 and Windows 11**, developed by [**Minana Tech**](https://minanatech.com). It helps you inspect network adapters, generate a random MAC address, apply a software MAC override on compatible Ethernet and Wi-Fi drivers, and restore the default configuration.

**[Download preview](https://github.com/minanatech/minana-mac-address-changer/releases)** · **[Official website](https://minanatech.com)** · **[Report an issue](https://github.com/minanatech/minana-mac-address-changer/issues/new/choose)** · **[Contribute](CONTRIBUTING.md)**

View adapters, generate locally administered MAC addresses, apply software overrides, and restore the default configuration in a clean English interface.

**Version 0.1.0 — preview.** Built for Windows 10/11 with C# and WPF on .NET 10. The current release is unsigned. Driver-specific mutation and recovery still require hardware testing before a stable public release.

## Features

- Live network adapter list with search, connection status, IPv4 address, link speed, and gateway.
- Current MAC copied directly from Windows; configured overrides displayed separately.
- Cryptographically generated, locally administered unicast addresses using an `02` prefix.
- Strict validation of colon-separated, hyphen-separated, or unseparated MAC addresses.
- An explicit review prompt before changing or restoring an adapter.
- Administrator elevation only for the requested mutation and restart.
- Active address verification after applying a change; ignored overrides are not reported as successful.
- Restore default removes the software override; no firmware modification.
- Built-in quick guide, Minana Tech branding, and session-only activity.
- No telemetry, accounts, third-party packages, or background network requests.

## Download and run on Windows

Download the Windows ZIP from [GitHub Releases](https://github.com/minanatech/minana-mac-address-changer/releases), extract it, and run `MinanaMac.exe`. Keep all extracted files together. This preview is a portable folder, not an installer.

The framework-dependent build requires the **.NET 10 Desktop Runtime for Windows x64**. The .NET SDK on the development machine already includes it. A self-contained build can be produced using the command below for users without the runtime.

Select an adapter, review the suggested address or enter your own, and click **Apply MAC address**. The selected connection will briefly drop. Windows asks for administrator approval. The app then verifies the address reported by the adapter.

**Restore default** is available when a registry override is present. It removes that override and restarts the adapter. The physical factory address is not guessed or stored by the app. Windows Wi-Fi randomization may still affect the address.

## Build in Visual Studio

1. Install the **.NET desktop development** workload and .NET 10 SDK.
2. Open `MinanaMac.sln` in Visual Studio Professional.
3. Select **Release / Any CPU**, then build or press F5 to run.

From a terminal in this directory:

```powershell
dotnet build MinanaMac.sln -c Release
dotnet publish MinanaMac.csproj -c Release --self-contained false -o artifacts/app
```

The project has no package dependencies. `NuGet.Config` disables package feeds for reproducible offline builds with the SDK installed. For a self-contained build, allow the official feed explicitly so runtime packs can be downloaded:

```powershell
dotnet publish MinanaMac.csproj -c Release -r win-x64 --self-contained true --source https://api.nuget.org/v3/index.json -o artifacts/standalone
```

## Tests

```powershell
./scripts/test.ps1
```

The tests exercise address validation, 1,000 generated addresses, invalid helper arguments, adapter selection, input validation, search, empty states, and guide/about navigation. They do not change network settings or request elevation. See `TESTING.md` for hardware release checks.

## Implementation

`AdapterService` maps adapter GUIDs to the Windows network class registry keys and changes only that adapter's `NetworkAddress` value. An elevated instance of the same executable validates the GUID and MAC again, then restarts the selected adapter with `Restart-NetAdapter`. User-supplied adapter names never enter PowerShell. Read-only operation uses ordinary user privileges.

Hidden system adapters and protocol filter interfaces are omitted. Some visible virtual adapters may still appear. Support for a MAC override depends on the driver; the app cannot make an incompatible driver accept one. A failed restart leaves the requested registry configuration in place and reports that state so the user can recover. No automatic reboot is performed.

Technical references: [NDIS NetworkAddress](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ndis/nf-ndis-ndisreadnetworkaddress), [Restart-NetAdapter](https://learn.microsoft.com/en-us/powershell/module/netadapter/restart-netadapter).

## Frequently asked questions

### Can I change my MAC address on Windows 11?

On adapters whose drivers support a software MAC override, Minana can request a new address and then verify the address reported by Windows. Compatibility is not guaranteed for every adapter.

### Does this MAC address changer work with Wi-Fi?

It lists Wi-Fi adapters and can request an override, but many wireless drivers restrict or ignore changes. If verification fails, use Restore default. Windows randomized hardware address settings may also affect the reported MAC.

### Does it permanently change the hardware MAC?

No. It changes a Windows software configuration value. Restore default removes that value; it does not rewrite adapter firmware or guarantee a particular factory address.

### Does changing a MAC address hide my public IP?

No. A MAC address identifies an interface on the local network. Changing it does not change your public IP address or provide internet anonymity.

### Is Minana MAC Address Changer free and open source?

Yes. The source is available under the MIT License. This is an independent project by Minana Tech, not an official Technitium product.

## Project and community

- **Developer and official website:** [Minana Tech — minanatech.com](https://minanatech.com)
- **Source repository:** [minanatech/minana-mac-address-changer](https://github.com/minanatech/minana-mac-address-changer)
- **Contributing:** [Development and contribution guide](CONTRIBUTING.md)
- **Security:** [Responsible vulnerability reporting](SECURITY.md)
- **Release readiness:** [Testing checklist](TESTING.md) and [release notes](RELEASE-NOTES.md)

Before a stable release, complete hardware compatibility and recovery tests, validate the final distribution on clean Windows installations, and sign the executable if a signing certificate is available.

## License and credits

MIT © 2026 Minana Tech. Independent software, not affiliated with Technitium or Microsoft. The interface and code are original to this project.
