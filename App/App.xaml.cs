using System.Configuration;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static App.settingsDataType settings;
     [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFileInfo psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    public const uint SHGFI_ICON = 0x000000100;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
    public const uint SHGFI_OPENICON = 0x000000002;
    public const uint SHGFI_SMALLICON = 0x000000001;
    public const uint SHGFI_LARGEICON = 0x000000000;
    public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    public const uint FILE_ATTRIBUTE_FILE = 0x00000100;
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFileInfo
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };
    
    public enum IconSize : short
    {
        Small,
        Large
    }

    public enum ItemState : short
    {
        Undefined,
        Open,
        Close
    }

    public static BitmapSource GetIcon(string path, IconSize size, ItemState state)
    {
        var flags = (uint)(SHGFI_ICON | SHGFI_USEFILEATTRIBUTES);
        var attribute = (uint)(FILE_ATTRIBUTE_FILE);

        if (object.Equals(size, IconSize.Small))
        {
            flags += SHGFI_SMALLICON;
        }
        else
        {
            flags += SHGFI_LARGEICON;
        }
        var shfi = new SHFileInfo();
        var res = SHGetFileInfo(path, attribute, out shfi, (uint)Marshal.SizeOf(shfi), flags);
        if (object.Equals(res, IntPtr.Zero)) throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
        try
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                        shfi.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
        }
        catch
        {
            throw;
        }
        finally
        {
            DestroyIcon(shfi.hIcon);
        }
    }


    const int WS_EX_TRANSPARENT = 0x00000020;
    const int GWL_EXSTYLE = (-20);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    public static void SetWindowExTransparent(IntPtr hwnd)
    {
        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }

    public static string GetPathWallpaper()
    {
        string pathWallpaper = "";
        RegistryKey? regKey = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
        if (regKey != null)
        {
            pathWallpaper = regKey.GetValue("WallPaper").ToString();
            regKey.Close();
        }
        return pathWallpaper;
    }

    public static Color? getColor(string str) {
        if (str == "ACCENT") {
            return SystemParameters.WindowGlassColor;
        }else {
            try {
                return (Color)ColorConverter.ConvertFromString(str);
            }catch {
                return null;
            }
        }
    }
    public static CornerRadius? getCornerRadius(string str) {
        if (str.Contains(",")) {
            try {
                var split = str.Split(',');
                return new CornerRadius(double.Parse(split[0]), double.Parse(split[1]), double.Parse(split[2]), double.Parse(split[3]));
            }catch {
                return null;
            }
        }else {
            try {
                return new CornerRadius(double.Parse(str));
            }catch {
                return null;
            }
        }
    }

    public class settingsDataType {
        public int iconSize = 48;
        public double animationSpeed = 5;
        public string dockPosition = "Bottom";
        public List<iconDataType> dockItems = new();
        public string groupRunningApps = "Executable";
        public bool registerAsAppBar = false;
        public string dockColor = "#77000000";
        public string dockBorderRadius = "AUTO";
        public string separatorColor = "#FFFFFF";
        public int tickerInterval = 500;
        public string activeAppColor = "ACCENT";
        public bool topmost = false;
        public double docktransformY = 0;
        public double docktransformX = 0;
        public string autohide = "Off";
        public List<string> iconBlacklist = new();
        public bool automaticSeparatorAtRunningApps = true;
        public string dockButtonStyleToUse = "asbs";
        public string appsDrawerTheme = "Dark";
        public string submenuCornerRadius = "8";
        public string submenuButtonStyleToUse = "asbs";
        public string submenuBackground = "#FFFFFF";
        public string submenuForeground = "#000000";
    }
    public class iconDataType {
        public string name = "";
        public string target = "";
        public string icon = "";
    }
    public App() : base() {
        Dispatcher.UnhandledException += crash;
    }

    void crash(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
        MessageBox.Show(e.Exception.ToString(), "Crash (CTRL + C to copy)", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

