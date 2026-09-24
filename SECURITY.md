# Security policy

Minana MAC Address Changer changes a selected Windows adapter's software MAC override. It is not an anonymity tool and does not change a public IP address.

## Supported versions

This project is in preview. Security fixes target the latest source and preview release. No long-term support versions or response-time commitments are offered yet.

## Report a vulnerability

Use GitHub's **Security → Report a vulnerability** option if available on this repository. Please do not publish exploit details or secrets in a public issue. If private reporting is unavailable, consult [minanatech.com](https://minanatech.com) for the current contact options before sending sensitive details.

Include the affected version, Windows version, a minimal reproduction, impact, and a suggested mitigation if known. Remove credentials and identifying network details.

## Design boundaries

- Ordinary viewing and address generation do not require administrator privileges.
- The elevated helper validates the operation, adapter GUID, and MAC address again.
- User-provided adapter names are never interpolated into PowerShell.
- The app modifies the selected adapter's `NetworkAddress` registry value, not firmware.
- It checks the active reported MAC after applying an override.
- No telemetry, accounts, background updates, or online vendor lookups are included.

The preview is unsigned and has not undergone an independent security audit. Hardware mutation and recovery testing are tracked in `TESTING.md`.
