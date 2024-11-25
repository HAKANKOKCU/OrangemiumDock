using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices.ComTypes;
using Newtonsoft.Json;
using System.Windows.Media.Animation;

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static QuarticEase eio = new QuarticEase() {EasingMode = EasingMode.EaseInOut};
    public static readonly string appfolderpath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrangemiumDock");
    public static readonly string settingspath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrangemiumDock\\settings.json");
    
    public static App.settingsDataType settings;
    public static Dictionary<string, Object> styles = new();
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

    [DllImport("user32.dll")]
    internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_INVALID_STATE = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }
    public static Dictionary<string,BitmapSource> iconcache = new();
    public static BitmapSource GetIcon(string path, IconSize size, ItemState state)
    {
        if (iconcache.ContainsKey(path.ToLower())) {return iconcache[path.ToLower()];}
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
            var i = Imaging.CreateBitmapSourceFromHIcon(
                        shfi.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
            iconcache[path.ToLower()] = i;
            return i;
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
            var reg = regKey.GetValue("WallPaper");
            if (reg != null) pathWallpaper = reg.ToString() ?? "";
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

    public static Brush? getBrush(object brsh) {
        if (brsh is JObject) {
            var brush = (JObject)brsh;
            if (brush["type"].ToString() == "LinearGraidentBrush") {
                GradientStopCollection grad = new();
                var stops = (JArray)brush["stops"];
                foreach (JObject stp in stops) {
                    var color = getColor(stp["color"].ToString());
                    
                    if (color != null) {
                        var offset = double.Parse(stp["offset"].ToString());
                        GradientStop stop = new((Color)color,offset);
                        grad.Add(stop);
                    }
                }
                var startpoint = brush["startPoint"].ToString().Split(",");
                var endpoint = brush["endPoint"].ToString().Split(",");
                var grd = new LinearGradientBrush(grad,new Point(double.Parse(startpoint[0]), double.Parse(startpoint[1])),new Point(double.Parse(endpoint[0]), double.Parse(endpoint[1])));
                grd.Freeze();
                return grd;
            }
            if (brush["type"].ToString() == "RadialGraidentBrush") {
                GradientStopCollection grad = new();
                var stops = (JArray)brush["stops"];
                foreach (JObject stp in stops) {
                    var color = getColor(stp["color"].ToString());
                    
                    if (color != null) {
                        var offset = double.Parse(stp["offset"].ToString());
                        GradientStop stop = new((Color)color,offset);
                        grad.Add(stop);
                    }
                }
                var gradorigin = brush["gradientOrigin"].ToString().Split(",");
                var center = brush["center"].ToString().Split(",");
                
                var grd = new RadialGradientBrush(grad);
                grd.GradientOrigin = new Point(double.Parse(gradorigin[0]), double.Parse(gradorigin[1]));
                grd.Center = new Point(double.Parse(center[0]), double.Parse(center[1]));
                grd.RadiusX = double.Parse(brush["radiusX"].ToString());
                grd.RadiusY = double.Parse(brush["radiusY"].ToString());
                grd.Freeze();
                return grd;
            }
        }else {
            var color = getColor(brsh.ToString() ?? "");
            if (color == null) return null;
            var solid = new SolidColorBrush((Color)color);
            solid.Freeze();
            return solid;
        }

        //hmm maybe no idea?
        return null;
    }
    public static CornerRadius? getCornerRadius(string str) {
        if (str.Contains("|")) {
            var split = str.Split("|");
            switch (settings.dockPosition) {
                case "Bottom":
                    str = split[0];
                    break;
                case "Top":
                    str = split[1];
                    break;
                case "Left":
                    str = split[2];
                    break;
                case "Right":
                    str = split[3];
                    break;
                default:
                    throw new Exception("Invalid value for CornerRadius");
            }
        }
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

    public static Thickness? getThick(string str) {
        if (str.Contains("|")) {
            var split = str.Split("|");
            switch (settings.dockPosition) {
                case "Bottom":
                    str = split[0];
                    break;
                case "Top":
                    str = split[1];
                    break;
                case "Left":
                    str = split[2];
                    break;
                case "Right":
                    str = split[3];
                    break;
                default:
                    throw new Exception("Invalid value for Thickness");
            }
        }
        if (str.Contains(",")) {
            try {
                var split = str.Split(',');
                return new Thickness(double.Parse(split[0]), double.Parse(split[1]), double.Parse(split[2]), double.Parse(split[3]));
            }catch {
                return null;
            }
        }else {
            try {
                return new Thickness(double.Parse(str));
            }catch {
                return null;
            }
        }
    }

    public static double? getDouble(string str) {
        if (str.Contains("|")) {
            var split = str.Split("|");
            switch (settings.dockPosition) {
                case "Bottom":
                    str = split[0];
                    break;
                case "Top":
                    str = split[1];
                    break;
                case "Left":
                    str = split[2];
                    break;
                case "Right":
                    str = split[3];
                    break;
                default:
                    throw new Exception("Invalid value for Double");
            }
        }
        if (str == "NULL") return null;
        if (str == "NAN") return double.NaN;
        return double.Parse(str);
    }

    public class settingsDataType {
        public int iconSize = 48;
        public double animationSpeed = 5;
        public string dockPosition = "Bottom";
        public List<iconDataType> dockItems = new();
        public string groupRunningApps = "Executable";
        public bool registerAsAppBar = false;
        public int tickerInterval = 500;
        public bool topmost = false;
        public double docktransformY = 0;
        public double docktransformX = 0;
        public string autohide = "Off";
        public List<string> iconBlacklist = new();
        public bool automaticSeparatorAtRunningApps = true;
        public string appsDrawerTheme = "Dark";
        public string stylesPath = "";
        public bool enableAppsDrawerBlur = true;
        public bool useIconsInSubmenus = false;
        public byte appsMenuAlpha = 180;
        public string blurDock = "Off";
        public string appsDrawerItemStyle = "Grid";
    }
    public class iconDataType {
        public string name = "";
        public string target = "";
        public string icon = "";
        public string parameters = "";
    }

    internal static void EnableBlur(Window win)
    {
        var windowHelper = new WindowInteropHelper(win);
        
        var accent = new AccentPolicy();
        var accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
        
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);
        
        var data = new WindowCompositionAttributeData();
        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
        data.SizeOfData = accentStructSize;
        data.Data = accentPtr;

        SetWindowCompositionAttribute(windowHelper.Handle, ref data);
        
        Marshal.FreeHGlobal(accentPtr);
    }

    internal static void DisableBlur(Window win)
    {
        var windowHelper = new WindowInteropHelper(win);
        
        var accent = new AccentPolicy();
        var accentStructSize = Marshal.SizeOf(accent);
        accent.AccentState = AccentState.ACCENT_DISABLED;
        
        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
        Marshal.StructureToPtr(accent, accentPtr, false);
        
        var data = new WindowCompositionAttributeData();
        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
        data.SizeOfData = accentStructSize;
        data.Data = accentPtr;

        SetWindowCompositionAttribute(windowHelper.Handle, ref data);
        
        Marshal.FreeHGlobal(accentPtr);
    }
    
    public App() : base() {
        Dispatcher.UnhandledException += crash;
        if (!Directory.Exists(appfolderpath)) {
            Directory.CreateDirectory(appfolderpath);
        }
        if (!Directory.Exists(appfolderpath + "\\icons")) {
            Directory.CreateDirectory(appfolderpath + "\\icons");
        }
        if (!File.Exists(settingspath)) {
            settings = new();
            settings.iconBlacklist.Add("TextInputHost.exe");
            settings.dockItems.Add(new App.iconDataType() {name = "Apps...", target = "AppsDrawer"});
            File.WriteAllText(settingspath,JsonConvert.SerializeObject(settings));
        }else {
            var s = JsonConvert.DeserializeObject<App.settingsDataType>(File.ReadAllText(settingspath));
            if (s == null) {
                throw new Exception("Settings is null!!!");
            }
            settings = s;
        }
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        new MainWindow().Show();
    }
    
    public static void savesettings() {
        File.WriteAllText(settingspath,JsonConvert.SerializeObject(App.settings));
    }

    void crash(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
        e.Handled = MessageBox.Show(e.Exception.ToString(), "Error (CTRL + C to copy) | Click Yes to ignore", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes;
    }
}

