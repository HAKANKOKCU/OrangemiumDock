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

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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
        //public string dockColor = "#77000000";
        //public string dockBorderRadius = "AUTO";
        //public string separatorColor = "#FFFFFF";
        public int tickerInterval = 500;
        //public string activeAppColor = "ACCENT";
        public bool topmost = false;
        public double docktransformY = 0;
        public double docktransformX = 0;
        public string autohide = "Off";
        public List<string> iconBlacklist = new();
        public bool automaticSeparatorAtRunningApps = true;
        //public string dockButtonStyleToUse = "asbs";
        public string appsDrawerTheme = "Dark";
        //public string submenuCornerRadius = "8";
        //public string submenuButtonStyleToUse = "asbs";
        //public string submenuBackground = "#FFFFFF";
        //public string submenuForeground = "#000000";
        public string stylesPath = "";
        public bool enableAppsDrawerBlur = true;
        public bool useIconsInSubmenus = false;
        public byte appsMenuAlpha = 150;
        public string blurDock = "Off";
        public string appsDrawerItemStyle = "Grid";
    }
    public class iconDataType {
        public string name = "";
        public string target = "";
        public string icon = "";
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

    public sealed class AppxPackage
    {
        private List<AppxApp> _apps = new List<AppxApp>();
        private IAppxManifestProperties _properties;

        private AppxPackage()
        {
        }

        public string FullName { get; private set; }
        public string Path { get; private set; }
        public string Publisher { get; private set; }
        public string PublisherId { get; private set; }
        public string ResourceId { get; private set; }
        public string FamilyName { get; private set; }
        public string ApplicationUserModelId { get; private set; }
        public string Logo { get; private set; }
        public string PublisherDisplayName { get; private set; }
        public string Description { get; private set; }
        public string DisplayName { get; private set; }
        public bool IsFramework { get; private set; }
        public Version Version { get; private set; }
        public AppxPackageArchitecture ProcessorArchitecture { get; private set; }

        public IReadOnlyList<AppxApp> Apps
        {
            get
            {
                return _apps;
            }
        }

        public IEnumerable<AppxPackage> DependencyGraph
        {
            get
            {
                return QueryPackageInfo(FullName, PackageConstants.PACKAGE_FILTER_ALL_LOADED).Where(p => p.FullName != FullName);
            }
        }

        public string? FindHighestScaleQualifiedImagePath(string resourceName)
        {
            if (resourceName == null)
                throw new ArgumentNullException("resourceName");

            const string scaleToken = ".scale-";
            var sizes = new List<int>();
            string name = System.IO.Path.GetFileNameWithoutExtension(resourceName);
            string ext = System.IO.Path.GetExtension(resourceName);
            foreach (var file in Directory.EnumerateFiles(System.IO.Path.Combine(Path, System.IO.Path.GetDirectoryName(resourceName)), name + scaleToken + "*" + ext))
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                int pos = fileName.IndexOf(scaleToken) + scaleToken.Length;
                string sizeText = fileName.Substring(pos);
                int size;
                if (int.TryParse(sizeText, out size))
                {
                    sizes.Add(size);
                }
            }
            if (sizes.Count == 0)
                return null;

            sizes.Sort();
            return System.IO.Path.Combine(Path, System.IO.Path.GetDirectoryName(resourceName), name + scaleToken + sizes.Last() + ext);
        }

        public override string ToString()
        {
            return FullName;
        }

        public static AppxPackage? FromWindow(IntPtr handle)
        {
            int processId;
            GetWindowThreadProcessId(handle, out processId);
            if (processId == 0)
                return null;

            return FromProcess(processId);
        }

        public static AppxPackage? FromProcess(Process process)
        {
            if (process == null)
            {
                process = Process.GetCurrentProcess();
            }

            try
            {
                return FromProcess(process.Handle);
            }
            catch
            {
                // probably access denied on .Handle
                return null;
            }
        }

        public static AppxPackage? FromProcess(int processId)
        {
            const int QueryLimitedInformation = 0x1000;
            IntPtr hProcess = OpenProcess(QueryLimitedInformation, false, processId);
            try
            {
                return FromProcess(hProcess);
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                {
                    CloseHandle(hProcess);
                }
            }
        }

        public static AppxPackage? FromProcess(IntPtr hProcess)
        {
            if (hProcess == IntPtr.Zero)
                return null;

            // hprocess must have been opened with QueryLimitedInformation
            int len = 0;
            GetPackageFullName(hProcess, ref len, null);
            if (len == 0)
                return null;

            var sb = new StringBuilder(len);
            string fullName = GetPackageFullName(hProcess, ref len, sb) == 0 ? sb.ToString() : null;
            if (string.IsNullOrEmpty(fullName)) // not an AppX
                return null;

            var package = QueryPackageInfo(fullName, PackageConstants.PACKAGE_FILTER_HEAD).First();

            len = 0;
            GetApplicationUserModelId(hProcess, ref len, null);
            sb = new StringBuilder(len);
            package.ApplicationUserModelId = GetApplicationUserModelId(hProcess, ref len, sb) == 0 ? sb.ToString() : null;
            return package;
        }

        public string GetPropertyStringValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            return GetStringValue(_properties, name);
        }

        public bool GetPropertyBoolValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            return GetBoolValue(_properties, name);
        }

        public string? LoadResourceString(string resource)
        {
            return LoadResourceString(FullName, resource);
        }

        private static IEnumerable<AppxPackage> QueryPackageInfo(string fullName, PackageConstants flags)
        {
            IntPtr infoRef;
            OpenPackageInfoByFullName(fullName, 0, out infoRef);
            if (infoRef != IntPtr.Zero)
            {
                IntPtr infoBuffer = IntPtr.Zero;
                try
                {
                    int len = 0;
                    int count;
                    GetPackageInfo(infoRef, flags, ref len, IntPtr.Zero, out count);
                    if (len > 0)
                    {
                        var factory = (IAppxFactory)new AppxFactory();
                        infoBuffer = Marshal.AllocHGlobal(len);
                        int res = GetPackageInfo(infoRef, flags, ref len, infoBuffer, out count);
                        for (int i = 0; i < count; i++)
                        {
                            var info = (PACKAGE_INFO)Marshal.PtrToStructure(infoBuffer + i * Marshal.SizeOf(typeof(PACKAGE_INFO)), typeof(PACKAGE_INFO));
                            var package = new AppxPackage();
                            package.FamilyName = Marshal.PtrToStringUni(info.packageFamilyName);
                            package.FullName = Marshal.PtrToStringUni(info.packageFullName);
                            package.Path = Marshal.PtrToStringUni(info.path);
                            package.Publisher = Marshal.PtrToStringUni(info.packageId.publisher);
                            package.PublisherId = Marshal.PtrToStringUni(info.packageId.publisherId);
                            package.ResourceId = Marshal.PtrToStringUni(info.packageId.resourceId);
                            package.ProcessorArchitecture = info.packageId.processorArchitecture;
                            package.Version = new Version(info.packageId.VersionMajor, info.packageId.VersionMinor, info.packageId.VersionBuild, info.packageId.VersionRevision);

                            // read manifest
                            string manifestPath = System.IO.Path.Combine(package.Path, "AppXManifest.xml");
                            const int STGM_SHARE_DENY_NONE = 0x40;
                            IStream strm;
                            SHCreateStreamOnFileEx(manifestPath, STGM_SHARE_DENY_NONE, 0, false, IntPtr.Zero, out strm);
                            if (strm != null)
                            {
                                var reader = factory.CreateManifestReader(strm);
                                package._properties = reader.GetProperties();
                                package.Description = package.GetPropertyStringValue("Description");
                                package.DisplayName = package.GetPropertyStringValue("DisplayName");
                                package.Logo = package.GetPropertyStringValue("Logo");
                                package.PublisherDisplayName = package.GetPropertyStringValue("PublisherDisplayName");
                                package.IsFramework = package.GetPropertyBoolValue("Framework");

                                var apps = reader.GetApplications();
                                while (apps.GetHasCurrent())
                                {
                                    var app = apps.GetCurrent();
                                    var appx = new AppxApp(app);
                                    appx.Description = GetStringValue(app, "Description");
                                    appx.DisplayName = GetStringValue(app, "DisplayName");
                                    appx.EntryPoint = GetStringValue(app, "EntryPoint");
                                    appx.Executable = GetStringValue(app, "Executable");
                                    appx.Id = GetStringValue(app, "Id");
                                    appx.Logo = GetStringValue(app, "Logo");
                                    appx.SmallLogo = GetStringValue(app, "SmallLogo");
                                    appx.StartPage = GetStringValue(app, "StartPage");
                                    appx.Square150x150Logo = GetStringValue(app, "Square150x150Logo");
                                    appx.Square30x30Logo = GetStringValue(app, "Square30x30Logo");
                                    appx.BackgroundColor = GetStringValue(app, "BackgroundColor");
                                    appx.ForegroundText = GetStringValue(app, "ForegroundText");
                                    appx.WideLogo = GetStringValue(app, "WideLogo");
                                    appx.Wide310x310Logo = GetStringValue(app, "Wide310x310Logo");
                                    appx.ShortName = GetStringValue(app, "ShortName");
                                    appx.Square310x310Logo = GetStringValue(app, "Square310x310Logo");
                                    appx.Square70x70Logo = GetStringValue(app, "Square70x70Logo");
                                    appx.MinWidth = GetStringValue(app, "MinWidth");
                                    package._apps.Add(appx);
                                    apps.MoveNext();
                                }
                                Marshal.ReleaseComObject(strm);
                            }
                            yield return package;
                        }
                        Marshal.ReleaseComObject(factory);
                    }
                }
                finally
                {
                    if (infoBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(infoBuffer);
                    }
                    ClosePackageInfo(infoRef);
                }
            }
        }

        public static string? LoadResourceString(string packageFullName, string resource)
        {
            if (packageFullName == null)
                throw new ArgumentNullException("packageFullName");

            if (string.IsNullOrWhiteSpace(resource))
                return null;

            const string resourceScheme = "ms-resource:";
            if (!resource.StartsWith(resourceScheme))
                return null;

            string part = resource.Substring(resourceScheme.Length);
            string url;

            if (part.StartsWith("/"))
            {
                url = resourceScheme + "//" + part;
            }
            else
            {
                url = resourceScheme + "///resources/" + part;
            }

            string source = string.Format("@{{{0}? {1}}}", packageFullName, url);
            var sb = new StringBuilder(1024);
            int i = SHLoadIndirectString(source, sb, sb.Capacity, IntPtr.Zero);
            if (i != 0)
                return null;

            return sb.ToString();
        }

        private static string GetStringValue(IAppxManifestProperties props, string name)
        {
            if (props == null)
                return null;

            string value;
            props.GetStringValue(name, out value);
            return value;
        }

        private static bool GetBoolValue(IAppxManifestProperties props, string name)
        {
            bool value;
            props.GetBoolValue(name, out value);
            return value;
        }

        internal static string GetStringValue(IAppxManifestApplication app, string name)
        {
            string value;
            app.GetStringValue(name, out value);
            return value;
        }

        [Guid("5842a140-ff9f-4166-8f5c-62f5b7b0c781"), ComImport]
        private class AppxFactory
        {
        }

        [Guid("BEB94909-E451-438B-B5A7-D79E767B75D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxFactory
        {
            void _VtblGap0_2(); // skip 2 methods
            IAppxManifestReader CreateManifestReader(IStream inputStream);
        }

        [Guid("4E1BD148-55A0-4480-A3D1-15544710637C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestReader
        {
            void _VtblGap0_1(); // skip 1 method
            IAppxManifestProperties GetProperties();
            void _VtblGap1_5(); // skip 5 methods
            IAppxManifestApplicationsEnumerator GetApplications();
        }

        [Guid("9EB8A55A-F04B-4D0D-808D-686185D4847A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestApplicationsEnumerator
        {
            IAppxManifestApplication GetCurrent();
            bool GetHasCurrent();
            bool MoveNext();
        }

        [Guid("5DA89BF4-3773-46BE-B650-7E744863B7E8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IAppxManifestApplication
        {
            [PreserveSig]
            int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] out string vaue);
        }

        [Guid("03FAF64D-F26F-4B2C-AAF7-8FE7789B8BCA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAppxManifestProperties
        {
            [PreserveSig]
            int GetBoolValue([MarshalAs(UnmanagedType.LPWStr)]string name, out bool value);
            [PreserveSig]
            int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] out string vaue);
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateStreamOnFileEx(string fileName, int grfMode, int attributes, bool create, IntPtr reserved, out IStream stream);

        [DllImport("user32.dll")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int OpenPackageInfoByFullName(string packageFullName, int reserved, out IntPtr packageInfoReference);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPackageInfo(IntPtr packageInfoReference, PackageConstants flags, ref int bufferLength, IntPtr buffer, out int count);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int ClosePackageInfo(IntPtr packageInfoReference);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPackageFullName(IntPtr hProcess, ref int packageFullNameLength, StringBuilder packageFullName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetApplicationUserModelId(IntPtr hProcess, ref int applicationUserModelIdLength, StringBuilder applicationUserModelId);

        [Flags]
        private enum PackageConstants
        {
            PACKAGE_FILTER_ALL_LOADED = 0x00000000,
            PACKAGE_PROPERTY_FRAMEWORK = 0x00000001,
            PACKAGE_PROPERTY_RESOURCE = 0x00000002,
            PACKAGE_PROPERTY_BUNDLE = 0x00000004,
            PACKAGE_FILTER_HEAD = 0x00000010,
            PACKAGE_FILTER_DIRECT = 0x00000020,
            PACKAGE_FILTER_RESOURCE = 0x00000040,
            PACKAGE_FILTER_BUNDLE = 0x00000080,
            PACKAGE_INFORMATION_BASIC = 0x00000000,
            PACKAGE_INFORMATION_FULL = 0x00000100,
            PACKAGE_PROPERTY_DEVELOPMENT_MODE = 0x00010000,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PACKAGE_INFO
        {
            public int reserved;
            public int flags;
            public IntPtr path;
            public IntPtr packageFullName;
            public IntPtr packageFamilyName;
            public PACKAGE_ID packageId;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PACKAGE_ID
        {
            public int reserved;
            public AppxPackageArchitecture processorArchitecture;
            public ushort VersionRevision;
            public ushort VersionBuild;
            public ushort VersionMinor;
            public ushort VersionMajor;
            public IntPtr name;
            public IntPtr publisher;
            public IntPtr resourceId;
            public IntPtr publisherId;
        }
    }

    public sealed class AppxApp
    {
        private AppxPackage.IAppxManifestApplication _app;

        internal AppxApp(AppxPackage.IAppxManifestApplication app)
        {
            _app = app;
        }

        public string GetStringValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            return AppxPackage.GetStringValue(_app, name);
        }

        // we code well-known but there are others (like Square71x71Logo, Square44x44Logo, whatever ...)
        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh446703.aspx
        public string Description { get; internal set; }
        public string DisplayName { get; internal set; }
        public string EntryPoint { get; internal set; }
        public string Executable { get; internal set; }
        public string Id { get; internal set; }
        public string Logo { get; internal set; }
        public string SmallLogo { get; internal set; }
        public string StartPage { get; internal set; }
        public string Square150x150Logo { get; internal set; }
        public string Square30x30Logo { get; internal set; }
        public string BackgroundColor { get; internal set; }
        public string ForegroundText { get; internal set; }
        public string WideLogo { get; internal set; }
        public string Wide310x310Logo { get; internal set; }
        public string ShortName { get; internal set; }
        public string Square310x310Logo { get; internal set; }
        public string Square70x70Logo { get; internal set; }
        public string MinWidth { get; internal set; }
    }

    public enum AppxPackageArchitecture
    {
        x86 = 0,
        Arm = 5,
        x64 = 9,
        Neutral = 11,
        Arm64 = 12
    }
    
    public App() : base() {
        Dispatcher.UnhandledException += crash;
    }

    void crash(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
        e.Handled = MessageBox.Show(e.Exception.ToString(), "Error (CTRL + C to copy) | Click Yes to ignore", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes;
    }
}

