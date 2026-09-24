using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
namespace MinanaMac;
public record AdapterInfo(string Id, string Name, string Description, string Mac, string Override, string Status, string Kind, string Ip, string Gateway, string Dns, string Speed, bool CanChange)
{
    public string Glyph => Kind == "Wi-Fi" ? "\uE701" : Kind == "Bluetooth" ? "\uE702" : "\uE839";
    public string Subtitle => $"{Kind}  ·  {Status}";
    public string Mode => Override.Length > 0 ? "Override configured" : "Default configuration";
}
public static class AdapterService
{
    private const string ClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    private static string? FindKey(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return null;
        using var root = Registry.LocalMachine.OpenSubKey(ClassPath);
        if (root == null) return null;
        foreach (var name in root.GetSubKeyNames().Where(n => n.Length == 4 && n.All(char.IsDigit)))
        {
            try
            {
                using var key = root.OpenSubKey(name);
                if (Guid.TryParse(key?.GetValue("NetCfgInstanceId") as string, out var candidate) && candidate == guid) return ClassPath + "\\" + name;
            }
            catch (System.Security.SecurityException) { }
            catch (UnauthorizedAccessException) { }
        }
        return null;
    }
    public static List<AdapterInfo> Read()
    {
        var result = new List<AdapterInfo>();
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel || nic.GetPhysicalAddress().GetAddressBytes().Length != 6) continue;
            try
            {
                var path = FindKey(nic.Id);
                if (path == null) continue; // Exclude protocol filter interfaces that are not independently configurable adapters.
                using var key = path == null ? null : Registry.LocalMachine.OpenSubKey(path);
                var characteristics = key?.GetValue("Characteristics") is int flags ? flags : 0;
                if ((characteristics & 8) != 0) continue; // NCF_HIDDEN: system-managed miniports and hidden adapters.
                var component = key?.GetValue("ComponentId") as string ?? "";
                var properties = nic.GetIPProperties();
                var ip = properties.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "Not assigned";
                var gateway = properties.GatewayAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address.ToString() ?? "Not assigned";
                var dns = string.Join(", ", properties.DnsAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork));
                var canChange = !component.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) && nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211;
                result.Add(new(nic.Id, nic.Name, nic.Description, MacAddress.Format(nic.GetPhysicalAddress().ToString()), key?.GetValue("NetworkAddress") as string ?? "", nic.OperationalStatus == OperationalStatus.Up ? "Connected" : "Disconnected", component.StartsWith("BTH", StringComparison.OrdinalIgnoreCase) ? "Bluetooth" : nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : "Ethernet", ip, gateway, dns.Length == 0 ? "Not assigned" : dns, nic.OperationalStatus == OperationalStatus.Up && nic.Speed > 0 ? (nic.Speed >= 1_000_000_000 ? $"{nic.Speed / 1_000_000_000d:0.#} Gbps" : $"{nic.Speed / 1_000_000d:0} Mbps") : "Unavailable", canChange));
            }
            catch (NetworkInformationException) { /* An adapter may disappear during enumeration. */ }
        }
        return result.OrderByDescending(n => n.Status == "Connected").ThenBy(n => n.Name).ToList();
    }
    public static async Task<int> ApplyAsync(string id, string? mac)
    {
        if (!Guid.TryParse(id, out var guid)) throw new ArgumentException("Invalid adapter identifier.");
        if (mac != null && !MacAddress.TryNormalize(mac, out mac, out var error)) throw new ArgumentException(error);
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Application path is unavailable.");
        var info = new ProcessStartInfo(executable) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
        info.Arguments = mac == null ? $"--restore {guid:D}" : $"--set {guid:D} {mac}";
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start the administrator helper.");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
    // Exit codes: 0 restarted; 2 invalid/precondition; 3 override saved, restart failed; 4 write failed.
    public static async Task<int> RunElevatedAsync(string[] args)
    {
        if (args.Length < 2 || args[0] is not ("--set" or "--restore") || !Guid.TryParse(args[1], out var guid)) return 2;
        if ((args[0] == "--set" && args.Length != 3) || (args[0] == "--restore" && args.Length != 2)) return 2;
        string? mac = null;
        if (args[0] == "--set" && !MacAddress.TryNormalize(args[2], out mac, out _)) return 2;
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 2;
        try
        {
            var adapter = Read().SingleOrDefault(a => Guid.TryParse(a.Id, out var g) && g == guid);
            if (adapter == null || !adapter.CanChange) return 2;
            var path = FindKey(guid.ToString());
            if (path == null) return 2;
            using var key = Registry.LocalMachine.OpenSubKey(path, true);
            if (key == null) return 4;
            if (mac == null) key.DeleteValue("NetworkAddress", false);
            else key.SetValue("NetworkAddress", mac, RegistryValueKind.String);
        }
        catch { return 4; }
        try
        {
            // Only a parsed GUID enters this script. No user-supplied names or command fragments.
            var script = "$ErrorActionPreference='Stop'; $a = @(Get-NetAdapter -IncludeHidden | Where-Object { $_.InterfaceGuid -eq [guid]'" + guid.ToString("D") + "' }); if ($a.Count -ne 1) { exit 3 }; $a[0] | Restart-NetAdapter -Confirm:$false -ErrorAction Stop";
            var powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
            var info = new ProcessStartInfo(powershell) { UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("-NoProfile"); info.ArgumentList.Add("-NonInteractive"); info.ArgumentList.Add("-EncodedCommand"); info.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(script)));
            using var process = Process.Start(info);
            if (process == null) return 3;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(true); return 3; }
            return process.ExitCode == 0 ? 0 : 3;
        }
        catch { return 3; }
    }
}
