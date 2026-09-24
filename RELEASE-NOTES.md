# 0.1.0 Preview

Initial Windows desktop preview of Minana MAC Address Changer by Minana Tech.

- Original dark interface with mint accents, keyboard focus states, searchable adapters, and built-in help.
- Adapter discovery, current MAC, configured override, IPv4, gateway, link speed, and status.
- Random locally administered unicast addresses, strict input validation, clipboard copy, apply, and default restore.
- Narrow elevation for registry updates and adapter restart, followed by reported-address verification.
- MIT source, Visual Studio solution, non-mutating tests, and Windows CI configuration.

## Verification on development machine

- .NET SDK 10.0.401 on Windows x64.
- Release compilation: zero warnings and errors.
- Address validation and 1,000 generation checks: passed.
- Read-only UI and malformed helper argument checks: passed.
- Published executable launch and rendered UI: passed.
- Layout reviewed at default and minimum window sizes. Smaller windows use vertical scrolling.
- Actual adapter mutation, restart, and hardware recovery: not executed. The active network configuration was left unchanged.
- CI workflow supplied but not yet run on GitHub.

## Distribution status

Framework-dependent portable folder; requires .NET 10 Windows Desktop Runtime. Unsigned preview. No installer, self-contained runtime, or website deployment is included. Source and preview distributions are published through the Minana Tech GitHub repository. Source documentation includes self-contained publishing instructions and the hardware validation checklist.

