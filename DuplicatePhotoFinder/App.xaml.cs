using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DuplicatePhotoFinder;

public partial class App : Application
{
    // ── DWM interop for Mica backdrop ─────────────────────────────────
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd, int attribute, ref int value, int size);

    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;   // Win 11 22H2+
    private const int DWMWA_MICA_EFFECT = 1029; // Win 11 21H2

    /// <summary>
    /// Call after the window handle is available (SourceInitialized / Loaded)
    /// to enable the system Mica material behind the window.
    /// </summary>
    public static void EnableMica(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        // Preferred: Windows 11 22H2+ SystemBackdropType = Mica (2)
        int backdropType = 2;
        int hr = DwmSetWindowAttribute(
            hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

        if (hr != 0)
        {
            // Fallback: Windows 11 21H2
            int enable = 1;
            DwmSetWindowAttribute(
                hwnd, DWMWA_MICA_EFFECT, ref enable, sizeof(int));
        }
    }
}
