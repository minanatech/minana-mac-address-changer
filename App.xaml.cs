using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MinanaMac;
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.FirstOrDefault() is "--set" or "--restore")
        {
            Shutdown(await AdapterService.RunElevatedAsync(e.Args));
            return;
        }
        if (e.Args.FirstOrDefault() == "--self-test")
        {
            try { MacAddress.SelfTest(); Shutdown(0); } catch { Shutdown(1); }
            return;
        }
        var window = new MainWindow();
        MainWindow = window;
        if (e.Args.FirstOrDefault() == "--preview-small") { window.Width = 1060; window.Height = 740; }
        window.Show();
        if (e.Args.FirstOrDefault() == "--ui-test")
        {
            try
            {
                await window.InitializeAsync();
                window.TestReadOnlyUi();
                foreach (var invalid in new[] { Array.Empty<string>(), new[] { "--restore", "not-a-guid" }, new[] { "--bad", Guid.NewGuid().ToString() }, new[] { "--set", Guid.NewGuid().ToString(), "FF:FF:FF:FF:FF:FF" } })
                    if (await AdapterService.RunElevatedAsync(invalid) != 2) throw new Exception("Invalid helper input was accepted");
                Shutdown(0);
            }
            catch (Exception ex)
            {
                if (e.Args.Length == 2) File.WriteAllText(e.Args[1], ex.ToString());
                Shutdown(1);
            }
            return;
        }
        if (e.Args.Length == 2 && e.Args[0] is "--preview" or "--preview-small")
        {
            await window.InitializeAsync();
            await Task.Delay(400);
            window.UpdateLayout();
            var surface = (FrameworkElement)window.Content;
            var bitmap = new RenderTargetBitmap((int)surface.ActualWidth, (int)surface.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(surface);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(e.Args[1])) encoder.Save(stream);
            Shutdown();
        }
    }
}
