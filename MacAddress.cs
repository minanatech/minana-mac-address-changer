using System.Security.Cryptography;
using System.Text.RegularExpressions;
namespace MinanaMac;
public static class MacAddress
{
    public static bool TryNormalize(string? input, out string value, out string error)
    {
        value = ""; error = "Enter six hexadecimal pairs, for example 02:1A:2B:3C:4D:5E.";
        var text = (input ?? "").Trim();
        if (!Regex.IsMatch(text, @"\A(?:[0-9a-fA-F]{12}|(?:[0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}|(?:[0-9a-fA-F]{2}-){5}[0-9a-fA-F]{2})\z")) return false;
        var compact = text.Replace(":", "").Replace("-", "").ToUpperInvariant();
        var first = Convert.ToByte(compact[..2], 16);
        if ((first & 1) != 0) { error = "Use a unicast address. The first byte must be even."; return false; }
        if (compact == "000000000000") { error = "The all-zero address is not valid."; return false; }
        if ((first & 2) == 0) { error = "Use a locally administered address: the first byte should end in 2, 6, A or E."; return false; }
        value = compact; error = "Valid locally administered unicast address."; return true;
    }
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(6);
        bytes[0] = 0x02; // Widely accepted locally administered prefix, including many Wi-Fi drivers.
        return Format(Convert.ToHexString(bytes));
    }
    public static string Format(string? mac)
    {
        var raw = (mac ?? "").Replace(":", "").Replace("-", "");
        return raw.Length == 12 ? string.Join(":", Enumerable.Range(0, 6).Select(i => raw.Substring(i * 2, 2))).ToUpperInvariant() : "Unavailable";
    }
    public static void SelfTest()
    {
        foreach (var valid in new[] { "02:12:34:56:78:9a", "06123456789A", "0a-12-34-56-78-9a", " FE123456789A " })
            if (!TryNormalize(valid, out var normalized, out _) || normalized.Length != 12) throw new Exception("Valid address rejected");
        foreach (var invalid in new[] { "", "00:00:00:00:00:00", "FF:FF:FF:FF:FF:FF", "03:12:34:56:78:9A", "00:12:34:56:78:9A", "02:12-34:56:78:9A", "02:12:34:56:78:9G", "02123456789A;whoami", "02 12 34 56 78 9A" })
            if (TryNormalize(invalid, out _, out _)) throw new Exception("Invalid address accepted");
        var addresses = new HashSet<string>();
        for (var i = 0; i < 1000; i++) { var mac = Generate(); if (!TryNormalize(mac, out _, out _) || !addresses.Add(mac) || !mac.StartsWith("02:")) throw new Exception("Generator failure"); }
        if (Format("02123456789A") != "02:12:34:56:78:9A") throw new Exception("Formatting failure");
    }
}
