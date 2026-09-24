# Contributing to Minana MAC Address Changer

Thank you for helping improve this open-source Windows network utility by [Minana Tech](https://minanatech.com).

## Getting started

1. Fork the repository and create a focused branch.
2. Install Visual Studio with .NET desktop development and the .NET 10 SDK.
3. Open `MinanaMac.sln` and build in Release mode.
4. Run `./scripts/test.ps1` before opening a pull request.

Keep contributions focused. Explain the user problem, the change, and how you verified it. Include screenshots for UI changes using sample adapter data, not personal network details.

## Areas where help is welcome

- Driver compatibility reports for Ethernet and Wi-Fi adapters.
- Accessibility, keyboard navigation, display scaling, and layout improvements.
- More isolated tests for adapter failures and recovery.
- Reproducible, signed distribution and installer work.
- Clear documentation and translations proposed in an issue first.

## Network changes need care

Automated tests must not mutate the host's real network settings. Hardware tests should run on a local test device with an alternate connection available. Never report a registry write as a verified MAC change; check the address Windows actually reports.

Do not include passwords, tokens, full logs containing secrets, personal MAC addresses, or public IP addresses in issues and pull requests. Redact identifying details.

## Code expectations

- Preserve strict input validation and GUID-based adapter targeting.
- Keep administrator privileges limited to the requested mutation.
- Do not add telemetry or outbound requests without prior discussion.
- Do not silently change another adapter or reboot the system.
- Keep UI copy in clear English and preserve the Minana Tech attribution.

Contributions are submitted under the repository's MIT License. Be respectful, constructive, and specific in all discussions.
