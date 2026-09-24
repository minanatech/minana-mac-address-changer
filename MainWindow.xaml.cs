using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace MinanaMac;
public partial class MainWindow : Window
{
    private List<AdapterInfo> adapters = [];
    private readonly List<string> activity = [];
    private bool busy;
    private Task? initialization;
    private AdapterInfo? Selected => AdapterList.SelectedItem as AdapterInfo;
    public MainWindow() { InitializeComponent(); SearchBox.ToolTip = "Search by adapter name or description"; }
    private async void Window_Loaded(object sender, RoutedEventArgs e) => await InitializeAsync();
    public Task InitializeAsync() => initialization ??= InitializeCoreAsync();
    private async Task InitializeCoreAsync()
    {
        await RefreshAsync();
        MacInput.Text = MacAddress.Generate();
        if (adapters.Count > 0) Log("Random address prepared. Review it, then apply when ready.");
    }
    private async Task RefreshAsync(string? selectedId = null)
    {
        if (busy) return;
        SetBusy(true, "Reading network adapters…");
        selectedId ??= Selected?.Id;
        try
        {
            adapters = await Task.Run(AdapterService.Read);
            Filter(selectedId);
            StatusText.Text = $"{adapters.Count} adapters detected  ·  Last refreshed {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex) { Log("Could not read adapters: " + ex.Message); }
        finally { SetBusy(false); }
    }
    private void Filter(string? selectedId = null)
    {
        if (AdapterList == null) return;
        selectedId ??= Selected?.Id;
        var query = SearchBox.Text.Trim();
        var visible = adapters.Where(a => (a.Name + " " + a.Description).Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        AdapterList.ItemsSource = visible;
        AdapterList.SelectedItem = visible.FirstOrDefault(a => a.Id == selectedId) ?? visible.FirstOrDefault();
        AdapterCount.Text = visible.Count.ToString();
        EmptyAdapters.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ShowSelected();
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) => Filter();
    private void Adapter_Changed(object sender, SelectionChangedEventArgs e) => ShowSelected();
    private void ShowSelected()
    {
        if (AdapterName == null) return;
        var a = Selected;
        AdapterName.Text = a?.Name ?? "Select an adapter";
        AdapterDescription.Text = a?.Description ?? "Connect a network adapter, then refresh the list.";
        ConnectionStatus.Text = a?.Status ?? "No adapter";
        CurrentMac.Text = a?.Mac ?? "—";
        OverrideState.Text = a == null ? "No adapter selected" : !a.CanChange ? "Read-only adapter · MAC override unavailable" : a.Override.Length > 0 ? "Configured override: " + MacAddress.Format(a.Override) : "Default configuration · no override set";
        IpAddress.Text = a?.Ip ?? "—"; LinkSpeed.Text = a?.Speed ?? "—"; Gateway.Text = a?.Gateway ?? "—";
        UpdateActions();
    }
    private void UpdateActions()
    {
        if (ApplyButton == null) return;
        var valid = MacAddress.TryNormalize(MacInput.Text, out var normalized, out var message);
        var same = Selected != null && normalized == Selected.Mac.Replace(":", "");
        Validation.Text = MacInput.Text.Length == 0 ? "Format: 02:1A:2B:3C:4D:5E" : same ? "This is already the current MAC address." : message;
        Validation.Foreground = (Brush)new BrushConverter().ConvertFromString(MacInput.Text.Length == 0 ? "#96A7BC" : valid ? "#75E5C0" : "#F1B090")!;
        ApplyButton.IsEnabled = !busy && Selected?.CanChange == true && valid && !same;
        RestoreButton.IsEnabled = !busy && Selected?.CanChange == true && Selected.Override.Length > 0;
        RandomButton.IsEnabled = !busy && Selected != null;
        MacInput.IsEnabled = !busy && Selected != null;
        CopyButton.IsEnabled = !busy && Selected != null;
    }
    private void SetBusy(bool value, string? message = null)
    {
        busy = value; RefreshButton.IsEnabled = !value; AdapterList.IsEnabled = !value; SearchBox.IsEnabled = !value;
        if (message != null) StatusText.Text = message;
        UpdateActions();
    }
    private void Mac_Changed(object sender, TextChangedEventArgs e) => UpdateActions();
    private void Random_Click(object sender, RoutedEventArgs e) { MacInput.Text = MacAddress.Generate(); Log("Random address generated. Nothing has been changed."); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        try { if (Selected is { } a) { Clipboard.SetText(a.Mac); Log("Current MAC address copied."); } }
        catch { Log("Clipboard is busy. Try again."); }
    }
    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (MacAddress.TryNormalize(MacInput.Text, out var mac, out _)) await ChangeAsync(mac);
    }
    private async void Restore_Click(object sender, RoutedEventArgs e) => await ChangeAsync(null);
    private async Task ChangeAsync(string? mac)
    {
        var a = Selected; if (busy || a == null || !a.CanChange) return;
        var restore = mac == null;
        var action = restore ? "Restore default MAC" : "Apply MAC address";
        var detail = restore ? "The configured software override will be removed. The driver will choose the default address; Windows Wi-Fi randomization may still apply." : $"New address: {MacAddress.Format(mac)}";
        if (MessageBox.Show(this, $"Adapter: {a.Name}\nCurrent address: {a.Mac}\n\n{detail}\n\nThis adapter will restart and its connection will briefly drop. Continue?", action, MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) != MessageBoxResult.OK) return;
        SetBusy(true, "Waiting for administrator approval and adapter restart…");
        string result;
        try
        {
            var code = await AdapterService.ApplyAsync(a.Id, mac);
            if (code == 0)
            {
                SetBusy(true, "Adapter restarted. Checking the reported address…");
                AdapterInfo? updated = null;
                for (var attempt = 0; attempt < 8; attempt++)
                {
                    await Task.Delay(1500);
                    updated = (await Task.Run(AdapterService.Read)).FirstOrDefault(n => n.Id == a.Id);
                    if (updated != null && (restore ? updated.Override.Length == 0 : updated.Mac.Replace(":", "") == mac)) break;
                }
                result = restore
                    ? updated?.Override.Length == 0 ? "Default configuration restored. Reported MAC: " + updated.Mac : "Restart completed, but default configuration could not be verified. Refresh to check."
                    : updated != null && updated.Mac.Replace(":", "") == mac ? "MAC address verified: " + updated.Mac : "Override saved, but the driver has not reported the requested MAC. It may be unsupported or need a reboot. Use Restore default to remove the override.";
            }
            else result = code switch
            {
                3 => "Override configuration saved, but the adapter restart failed. Check that the adapter is enabled in Windows, then reboot or restore the default.",
                2 => "No change applied. Administrator access is required and the adapter must still be available.",
                _ => "Could not save the adapter configuration. Check permissions and try again."
            };
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) { result = "Administrator approval canceled. No change applied."; }
        catch (Exception ex) { result = "Operation could not be completed: " + ex.Message + " Refresh to check the adapter state."; }
        finally { SetBusy(false); }
        await RefreshAsync(a.Id);
        Log(result);
        MessageBox.Show(this, result, "Minana MAC Address Changer", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void Log(string message)
    {
        activity.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        if (activity.Count > 4) activity.RemoveAt(activity.Count - 1);
        Activity.Text = string.Join("\n", activity); StatusText.Text = message;
    }
    private void Adapters_Click(object sender, RoutedEventArgs e) { Workspace.Visibility = Visibility.Visible; InfoPanel.Visibility = Visibility.Collapsed; }
    private void Guide_Click(object sender, RoutedEventArgs e)
    {
        InfoTitle.Text = "A new address. A familiar workflow.";
        InfoText.Text = "01   Select your adapter\nChoose the Ethernet or Wi-Fi adapter you want to configure. The current MAC is read directly from Windows. Virtual adapters may also appear.\n\n02   Choose a MAC address\nEnter six hexadecimal pairs, or select Randomize. Generated addresses use the locally administered, unicast 02 prefix. Avoid using an address already assigned to another device on the same network.\n\n03   Apply and verify\nSelect Apply MAC address and approve the Windows administrator prompt. The selected adapter restarts, interrupting its connection. Minana then checks the address reported by Windows.\n\nReturn to the default\nRestore default removes the NetworkAddress override and restarts the adapter. It does not change firmware. Windows Wi-Fi randomization can still affect the address.\n\nDriver compatibility\nSome drivers ignore software MAC overrides, especially Wi-Fi, VPN, and virtual adapters. A configured override is not proof that the active address changed. If verification fails, restore the default or consult the device documentation.\n\nScope\nChanging a MAC affects the local network identity. It does not change your public IP address or provide internet anonymity. Session activity stays in memory and is cleared when you close the app.";
        Workspace.Visibility = Visibility.Collapsed; InfoPanel.Visibility = Visibility.Visible;
    }
    private void About_Click(object sender, RoutedEventArgs e)
    {
        InfoTitle.Text = "Network tools, thoughtfully made.";
        InfoText.Text = "Minana MAC Address Changer\nVersion 0.1.0 • Preview\n\nDeveloped by Minana Tech\nminanatech.com\n\nA focused Windows utility for viewing network adapters, generating locally administered MAC addresses, applying software overrides, and returning to the default configuration.\n\nOpen source • MIT License\nBuilt with C# and Windows Presentation Foundation. No telemetry, accounts, advertisements, or online lookups. The website opens only when you click its link.\n\nThis is an independent project. It is not affiliated with Technitium or Microsoft. Driver compatibility depends on your hardware.\n\nCopyright © 2026 Minana Tech";
        Workspace.Visibility = Visibility.Collapsed; InfoPanel.Visibility = Visibility.Visible;
    }
    private void Website_Click(object sender, RoutedEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("https://minanatech.com") { UseShellExecute = true }); }
        catch (Exception ex) { Log("Could not open the website: " + ex.Message); }
    }
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (busy) { e.Cancel = true; StatusText.Text = "Please wait for the current operation to finish before closing."; }
    }
    public void TestReadOnlyUi()
    {
        MacInput.Text = "FF:FF:FF:FF:FF:FF";
        if (ApplyButton.IsEnabled) throw new Exception("Invalid MAC enabled Apply");
        Random_Click(this, new RoutedEventArgs());
        if (!MacAddress.TryNormalize(MacInput.Text, out _, out _)) throw new Exception("Randomize returned an invalid MAC");
        SearchBox.Text = "__no_adapter_can_match_this__";
        if (AdapterList.Items.Count != 0 || ApplyButton.IsEnabled || RestoreButton.IsEnabled || EmptyAdapters.Visibility != Visibility.Visible) throw new Exception("Empty search state failed");
        SearchBox.Text = "";
        if (AdapterList.Items.Count != adapters.Count) throw new Exception("Search reset failed");
        foreach (var adapter in adapters)
        {
            AdapterList.SelectedItem = adapter;
            if (CurrentMac.Text != adapter.Mac) throw new Exception("Selection detail mismatch");
            MacInput.Text = adapter.Mac;
            if (ApplyButton.IsEnabled) throw new Exception("Unchanged MAC enabled Apply");
            if (RestoreButton.IsEnabled != (adapter.CanChange && adapter.Override.Length > 0)) throw new Exception("Restore state mismatch");
        }
        Guide_Click(this, new RoutedEventArgs());
        if (InfoPanel.Visibility != Visibility.Visible) throw new Exception("Guide navigation failed");
        About_Click(this, new RoutedEventArgs());
        if (!InfoText.Text.Contains("Minana Tech")) throw new Exception("Brand attribution missing");
        Adapters_Click(this, new RoutedEventArgs());
        if (Workspace.Visibility != Visibility.Visible) throw new Exception("Workspace navigation failed");
    }
}
