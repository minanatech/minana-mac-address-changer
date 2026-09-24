# Verification and release checklist

## Automated, non-mutating checks

- Release compilation must finish with zero warnings and errors.
- `--self-test`: accepted MAC formats, multicast/all-zero/global-address rejection, injection-like input rejection, formatting, and validity/uniqueness of 1,000 generated addresses.
- `--ui-test`: reads actual adapters; verifies invalid/current-address Apply states, random generation, search/no-results, adapter selection, Restore eligibility, help/about navigation, and malformed helper argument rejection. It never calls Apply with a valid mutation or triggers UAC.
- `--preview <absolute PNG path>`: renders the actual WPF window with read-only adapter data for layout inspection. The image contains local network information and should be redacted or replaced with explicitly labeled sample data before public publishing.

## Hardware tests required before a stable release

Perform these locally on test hardware with an alternate connection available. They are deliberately not automated on the development user's active network.

1. Ethernet driver that supports `NetworkAddress`: record current configuration, apply a unique locally administered address, verify active MAC and connectivity, and restore default.
2. Supported Wi-Fi driver: repeat and observe Windows randomized hardware address settings.
3. Unsupported Wi-Fi/virtual driver: ensure ignored overrides produce a verification warning; restore default afterward.
4. Cancel the UAC prompt and verify no configuration changed.
5. Remove the adapter between confirmation and execution; verify failure is reported.
6. Simulate adapter restart failure; verify the UI explains that configuration was saved and provides recovery guidance.
7. Test starting from an existing third-party `NetworkAddress` override. Restore default intentionally removes it; it does not restore a previous custom value.
8. Test disconnected and disabled adapters, then verify re-enabling/reboot recovery in Windows settings if necessary.
9. Check keyboard-only use, 125/150/200% display scaling, minimum window size, long adapter names, empty adapter list, and runtime installation on a clean Windows machine.
10. Test the release package on supported Windows versions and sign the final executable before general distribution if possible.

## Recovery

Use **Restore default** to remove the override. If automatic restart fails, ensure the adapter is enabled in Windows Network settings and restart Windows. A default restore means no `NetworkAddress` override remains; it is not a claim about firmware or Windows Wi-Fi randomization.
