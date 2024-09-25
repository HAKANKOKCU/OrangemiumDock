using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using HWND = System.IntPtr;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Windows.Controls.Primitives;
using WpfAppBar;
using System.Text.Json.Nodes;
using System.Dynamic;

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern UInt32 GetWindowThreadProcessId(IntPtr hWnd, out UInt32 lpdwProcessId);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
    #endregion

    public static IDictionary<HWND, string> GetOpenWindows()
    {
        HWND shellWindow = GetShellWindow();
        Dictionary<HWND, string> windows = new Dictionary<HWND, string>();

        EnumWindows(delegate(HWND hWnd, int lParam)
        {
        if (hWnd == shellWindow) return true;
        if (!IsWindowVisible(hWnd)) return true;
        //if (!HasOwnerWindow(hWnd)) return true;

        int length = GetWindowTextLength(hWnd);
        if (length == 0) return true;

        StringBuilder builder = new StringBuilder(length);
        GetWindowText(hWnd, builder, length + 1);

        windows[hWnd] = builder.ToString();
        return true;

        }, 0);

        return windows;
  }
    string getWindowTitle(HWND hWnd) {
        int length = GetWindowTextLength(hWnd);
        if (length == 0) return "";

        StringBuilder builder = new StringBuilder(length);
        GetWindowText(hWnd, builder, length + 1);

        return builder.ToString();
    }

  private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

  [DllImport("USER32.DLL")]
  private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

  [DllImport("USER32.DLL")]
  private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

  [DllImport("USER32.DLL")]
  private static extern int GetWindowTextLength(HWND hWnd);

  [DllImport("USER32.DLL")]
  private static extern bool IsWindowVisible(HWND hWnd);

  //[DllImport("USER32.DLL")]
  //private static extern bool HasOwnerWindow(HWND hWnd);

  [DllImport("USER32.DLL")]
  private static extern IntPtr GetShellWindow();
  [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_MAXIMIZE = 3;
    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;
    public const int GCL_HICONSM = -34;
    public const int GCL_HICON = -14;
    
    public const int ICON_SMALL = 0;
    public const int ICON_BIG = 1;
    public const int ICON_SMALL2 = 2;
    
    public const int WM_GETICON = 0x7F;
    
    public static IntPtr GetClassLongPtr(IntPtr hWnd, int nIndex)
    {
    if (IntPtr.Size > 4)
        return GetClassLongPtr64(hWnd, nIndex);
    else
        return new IntPtr(GetClassLongPtr32(hWnd, nIndex));
    }
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", EntryPoint = "GetClassLong")]
    public static extern uint GetClassLongPtr32(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    public static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    
    const int WM_CLOSE = 0x0010;

    public ImageSource? GetAppIcon(IntPtr hwnd)
    {
        IntPtr iconHandle = SendMessage(hwnd,WM_GETICON,ICON_BIG,0);
        if(iconHandle == IntPtr.Zero)
            iconHandle = SendMessage(hwnd,WM_GETICON,ICON_SMALL,0);
    
        if(iconHandle == IntPtr.Zero)
            iconHandle = SendMessage(hwnd,WM_GETICON,ICON_SMALL2,0);
    
        if (iconHandle == IntPtr.Zero)
            iconHandle = GetClassLongPtr(hwnd, GCL_HICON);
        if (iconHandle == IntPtr.Zero)
            iconHandle = GetClassLongPtr(hwnd, GCL_HICONSM);
    
        if(iconHandle == IntPtr.Zero)
            return null;
        try {
            ImageSource icn = Imaging.CreateBitmapSourceFromHIcon(
                        iconHandle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
            return icn;
        }catch {return null;}
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    private struct WINDOWPLACEMENT {
        public int length;
        public int flags;
        public int showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public System.Drawing.Rectangle rcNormalPosition;
    }
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow (IntPtr hWnd);
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

    List<IntPtr> wins = new();
    static Dictionary<IntPtr,List<object>> appics = new();
    static Dictionary<Button,dockButton> dockbtn = new();
    Grid cnt = new() {Background = Brushes.Black};
    Image bgr = new();
    HWND hwnd = 0;
    HWND fgw = 0;

    StackPanel mbar = new() {Orientation = Orientation.Horizontal};
    StackPanel dockitems = new() { Orientation = Orientation.Horizontal};
    StackPanel dockiconsleft = new() { Orientation = Orientation.Horizontal};
    StackPanel dockiconsright = new() { Orientation = Orientation.Horizontal};
    public StackPanel runningapps = new() { Orientation = Orientation.Horizontal};
    static double iconsize = 48;
    //string smp = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
    public Grid mtc = new();
    Border appsmenu = new();
    
    string appfolderpath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrangemiumDock");
    string settingspath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrangemiumDock\\settings.json");
    Border apsb = new() {Background = new SolidColorBrush(Color.FromArgb(150,0,0,0)),HorizontalAlignment = HorizontalAlignment.Center,ClipToBounds = true};
    static ContextMenu mainmenu = new();
    ScrollViewer sw = new() {};
    Dictionary<string,dockButton> groupexeicon = new();

    static SolidColorBrush activecolor = new SolidColorBrush();
    bool dockhovered = false;
    double cpadd = 0;
    Border? rapsep;
    RECT? foregroundrct = null;
    public appsdrawerWindow? apdw = null;
    

    public Window? blr = null;
    public bool focusblur = true;
    public MainWindow()
    {
        
        InitializeComponent();
        if (!Directory.Exists(appfolderpath)) {
            Directory.CreateDirectory(appfolderpath);
        }
        if (!Directory.Exists(appfolderpath + "\\icons")) {
            Directory.CreateDirectory(appfolderpath + "\\icons");
        }
        if (!File.Exists(settingspath)) {
            App.settings = new();
            App.settings.iconBlacklist.Add("TextInputHost.exe");
            App.settings.dockItems.Add(new App.iconDataType() {name = "Apps...", target = "AppsDrawer"});
            File.WriteAllText(settingspath,JsonConvert.SerializeObject(App.settings));
        }else {
            var s = JsonConvert.DeserializeObject<App.settingsDataType>(File.ReadAllText(settingspath));
            if (s == null) {
                throw new Exception("Settings is null!!!");
            }
            App.settings = s;
        }
        

        ShowInTaskbar = false;
        //iconsize = System.Windows.SystemParameters.PrimaryScreenHeight - System.Windows.SystemParameters.WorkArea.Height;
        hwnd = new WindowInteropHelper(this).Handle;
        bool actagain = true;
        void blrf() {
            if (actagain && blr != null && blr.IsVisible && focusblur && apdw == null) {
                blr.Activate();
                Topmost = false;
                Topmost = true;
                
                Topmost = App.settings.topmost;
                actagain = false;
            }
            if (actagain) focusblur = true;
        }
        Loaded += (e,a) => {
            hwnd = new WindowInteropHelper(this).Handle;
            repos();
            appbar();
            if (App.settings.blurDock == "Area") {
                EnableBlur(this);
            }else if (App.settings.blurDock == "DockOnly") {
                blr = new();
                blr.WindowStyle = WindowStyle.None;
                blr.ShowInTaskbar = false;
                blr.AllowsTransparency = true;
                blr.Background = Brushes.Transparent;
                
                
                blr.Activated += (e,a) => {blrf();};
                blr.Loaded += (e,a) => {
                    EnableBlur(blr);
                    WindowInteropHelper wndHelper = new WindowInteropHelper(blr);

                    int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

                    exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                    SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
                    blr.Show();
                    blrf();
                };
                blr.Show();
            }
        };
        MenuItem setitm = new() {Header = "Settings..."};
        mainmenu.Items.Add(setitm);
        setitm.Click += (e,a) => {
            new SettingsWindow(App.settings).ShowDialog();
            loadsettings();
            savesettings();
        };
        MenuItem deditm = new() {Header = "Edit..."};
        mainmenu.Items.Add(deditm);
        deditm.Click += (e,a) => {
            new dockeditWindow(App.settings.dockItems).ShowDialog();
            loadsettings();
            savesettings();
        };
        MenuItem qitm = new() {Header = "Quit"};
        mainmenu.Items.Add(qitm);
        qitm.Click += (e,a) => {
            savesettings();
            Application.Current.Shutdown();
        };
        Closing += (e,a) => {savesettings();};

        mtc.MouseEnter += (e,a) => {
            dockhovered = true;
        };
        mtc.MouseLeave += (e,a) => {
            dockhovered = false;
        };

        KeyDown += (s,e) => {
            if (e.Key == Key.S) {
                new SettingsWindow(App.settings).ShowDialog();
                loadsettings();
                savesettings();
            }
        };

        apsb.AllowDrop = true;
        apsb.Drop += (a,e) => {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files) {
                    App.iconDataType ico = new() {target = file, name = Path.GetFileName(file).Split(".")[0]};
                    try {
                        string filePath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OrangemiumDock\\icons\\" + file.Replace(":","+").Replace("\\","+").Replace("/","+") + ".png");
                        var image = App.GetIcon(file,App.IconSize.Large,App.ItemState.Undefined);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(image));
                            encoder.Save(fileStream);
                        }
                        ico.icon = filePath;
                    }catch{}
                    App.settings.dockItems.Add(ico);
                }
                loadsettings();
                savesettings();
            }
        };
        
        dockitems.Children.Add(dockiconsleft);
        dockitems.Children.Add(runningapps);
        dockitems.Children.Add(dockiconsright);
        
        sw.Content = dockitems;
        apsb.Child = sw;
        
        mbar.Children.Add(apsb);
        mtc.Children.Add(mbar);
        
        
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Content = mtc;
        //tb.Topmost = true;
        HWND tbh = 0;
        
        Deactivated += (e,a) => actagain = true;
        Activated += (e,a) => {
            blrf();
        };

        loadsettings();
        repos();

        if (App.settings.automaticSeparatorAtRunningApps) rapsep = createseparator();

        DispatcherTimer AnimationTicker = new() {Interval = TimeSpan.FromMilliseconds(1)};
        DispatcherTimer dt = new() {Interval = TimeSpan.FromMilliseconds(App.settings.tickerInterval)};
        dt.Tick += (e,a) => {
            HWND fg = GetForegroundWindow();
            if (fg != tbh) {
                fgw = fg;
            }
            refreshtasklist();
            var win = GetForegroundWindow();
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            GetWindowPlacement(fgw, ref placement);
            if (wins.Contains(win) && placement.showCmd != 2) {
                RECT aa;
                GetWindowRect(new HandleRef(this, win), out aa);
                foregroundrct = aa;
                //Console.WriteLine(aa.Bottom);
                //Console.WriteLine(SystemParameters.FullPrimaryScreenHeight);
                //Console.WriteLine(aa.Top);
                if (aa.Bottom >= SystemParameters.FullPrimaryScreenHeight && aa.Top == 0) {
                    Hide(); //hide in fullscreen apps/games.
                    if (blr != null) blr.Hide();
                    AnimationTicker.Stop();
                }else {
                    AnimationTicker.Start();
                    if (blr != null && apdw == null) blr.Show();
                    Show();
                }
            }else {
                foregroundrct = null;
                AnimationTicker.Start();
                if (blr != null && apdw == null) blr.Show();
                Show();
            }
            if (App.settings.autohide == "Off" || apdw != null) {
                mtc.Background = null;
                cpadd = 0;
                apsb.Opacity = 1;
            }else {
                mtc.Background = new SolidColorBrush(Color.FromArgb(1,0,0,0));
            }
            
            repos();
            
        };
        dt.Start();
        
        
        AnimationTicker.Tick += (e,a) => {
            if ((App.settings.autohide == "On" || App.settings.autohide == "Smart") && apdw == null) {
                double target = 0;
                
                if (App.settings.autohide == "On") {
                    if (!dockhovered) {
                        target = App.settings.iconSize - 1;
                    }
                }
                if (App.settings.autohide == "Smart") {
                    target = 0;
                    
                    //var win = GetForegroundWindow();
                    //if (wins.Contains(win)) {
                        
                        
                        if(foregroundrct != null)
                        {
                            var rect = (RECT)foregroundrct;
                            if (App.settings.dockPosition == "Bottom") {
                                if (rect.Bottom > Top - cpadd) {
                                    target = App.settings.iconSize - 1;
                                    //Console.WriteLine(win);
                                }
                            }else if (App.settings.dockPosition == "Top") {
                                if (rect.Top < Top + Height + cpadd) {
                                    target = App.settings.iconSize - 1;
                                    //Console.WriteLine(win);
                                }
                            }else if (App.settings.dockPosition == "Right") {
                                if (rect.Right > Left - cpadd) {
                                    target = App.settings.iconSize - 1;
                                    //Console.WriteLine(win);
                                }
                            }else if (App.settings.dockPosition == "Left") {
                                if (rect.Left < Left + Width + cpadd) {
                                    target = App.settings.iconSize - 1;
                                    //Console.WriteLine(win);
                                }
                            }
                        }
                    //}
                }
                if (dockhovered) {
                    target = 0;
                }
                cpadd += (target - cpadd) / App.settings.animationSpeed;
                apsb.Opacity = 1 - (cpadd / App.settings.iconSize);
                repos();
            }
            
            foreach (object x in dockitems.Children) {
                if (x is StackPanel) {
                    var sp = (StackPanel)x;
                    int docksize = 0;
                    double currentsize;
                    if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                        currentsize = sp.Width;
                    }else {
                        currentsize = sp.Height;
                    }
                    foreach (object y in ((StackPanel)x).Children) {
                        if (y is Button) {
                            var btnc = (Button)y;
                            dockButton btn = dockbtn[(Button)y];
                            if (btnc.Visibility != Visibility.Collapsed) {
                                if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                                    docksize += (int)(btnc.Width + btnc.Margin.Left + btnc.Margin.Right);
                                }else {
                                    docksize += (int)(btnc.Height + btnc.Margin.Top + btnc.Margin.Bottom);
                                }
                            }
                            //docksize += App.settings.iconSize;
                            if (Math.Ceiling(currentsize) < docksize || docksize < Math.Ceiling(currentsize)) {
                                if (docksize > Math.Ceiling(currentsize)) {
                                    double diff = docksize - currentsize;
                                    if (diff > 0) {
                                        double size = (App.settings.iconSize - diff) / 2;
                                        if (size > 0 && size < App.settings.iconSize / 2) {
                                            btn.ico.Width = size;
                                            btn.ico.Height = size;
                                            btn.ico.Opacity = size / (App.settings.iconSize / 2);
                                        }else {
                                            if (btn.hwnds.Count == 0 || ((StackPanel)x) == dockiconsleft || ((StackPanel)x) == dockiconsright) {
                                                btn.ico.Width = iconsize / 2;
                                                btn.ico.Height = iconsize / 2;
                                            }
                                            
                                        }
                                    }
                                }else {
                                    if (btn.hwnds.Count == 0 || ((StackPanel)x) == dockiconsleft || ((StackPanel)x) == dockiconsright) {
                                        btn.ico.Width = iconsize / 2;
                                        btn.ico.Height = iconsize / 2;
                                    }
                                    
                                    btn.ico.Opacity = 1;
                                }
                            }
                        }else if (y is Border) {
                            //docksize += 7;
                            var grd = (Border)y;
                            if (grd.Visibility != Visibility.Collapsed) {
                                if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                                    docksize += (int)(grd.Width + grd.Margin.Left + grd.Margin.Right);
                                }else {
                                    docksize += (int)(grd.Height + grd.Margin.Top + grd.Margin.Bottom);
                                }
                            }
                        }
                    }
                    currentsize += (docksize - currentsize) / App.settings.animationSpeed;
                    if (Math.Ceiling(currentsize) == docksize) {
                        currentsize = docksize;
                    }
                    
                    if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                        try{sp.Width = currentsize;}catch{sp.Width = 1;}
                    }else {
                        try{sp.Height = currentsize;}catch{sp.Height = 1;}
                    }
                }
            }
            
            try {
                if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                    //sw.Width = docksize;
                    //apsb.Width = currentsize;
                    //mbar.Height = iconsize;
                    mbar.Width = Double.NaN;
                }else if (App.settings.dockPosition == "Right" || App.settings.dockPosition == "Left") {
                    //sw.Height = docksize;
                    //apsb.Height = currentsize;
                    //mbar.Width = iconsize;
                    mbar.Height = Double.NaN;
                }
            }catch {}
            if (blr != null) {
                try {
                    blr.Topmost = Topmost;
                    blr.Height = this.Height;
                    blr.Width = dockitems.ActualWidth;
                    Point position = apsb.PointToScreen(new Point(0d, 0d));
                    blr.Top = Top;
                    blr.Left = position.X;
                }catch {}
            }
        };
        AnimationTicker.Start();
    }
    internal void EnableBlur(Window win)
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

    
    bool appbarregistered = true;

    void repos() {
        if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
            double extra = 0;
            if (App.settings.docktransformY < 0) {
                extra = App.settings.docktransformY;
            }
            if (App.settings.dockPosition == "Bottom") {
                if (!App.settings.registerAsAppBar) Top = System.Windows.SystemParameters.WorkArea.Top + System.Windows.SystemParameters.WorkArea.Height - iconsize + extra + cpadd - (apsb.BorderThickness.Top + apsb.BorderThickness.Bottom);//+ App.settings.docktransformY;
            }
            if (App.settings.dockPosition == "Top") {
                if (!App.settings.registerAsAppBar) Top = System.Windows.SystemParameters.WorkArea.Top + extra - cpadd;//+ App.settings.docktransformY;
            }
            if (!App.settings.registerAsAppBar) Left = System.Windows.SystemParameters.WorkArea.Left;
            if (!App.settings.registerAsAppBar) Width = System.Windows.SystemParameters.WorkArea.Width;
            Height = iconsize + Math.Abs(App.settings.docktransformY) + apsb.BorderThickness.Top + apsb.BorderThickness.Bottom;
            dockitems.Orientation = Orientation.Horizontal;
            dockiconsleft.Orientation = Orientation.Horizontal;
            dockiconsright.Orientation = Orientation.Horizontal;
            runningapps.Orientation = Orientation.Horizontal;
            if (App.settings.docktransformY > 0) {
                mbar.VerticalAlignment = VerticalAlignment.Bottom;
            }else {
                mbar.VerticalAlignment = VerticalAlignment.Top;
            }
            
            mbar.HorizontalAlignment = HorizontalAlignment.Center;
            sw.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto; 
            sw.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            if (App.settings.docktransformX > 0) {
                apsb.Margin = new Thickness(App.settings.docktransformX,0,0,0);
            }else {
                apsb.Margin = new Thickness(0,0,Math.Abs(App.settings.docktransformX),0);
            }
            
        }else if (App.settings.dockPosition == "Right" || App.settings.dockPosition == "Left") {
            double extra = 0;
            if (App.settings.docktransformX < 0) {
                extra = App.settings.docktransformX;
            }
            if (App.settings.dockPosition == "Right") {
                if (!App.settings.registerAsAppBar) Left = System.Windows.SystemParameters.WorkArea.Width + System.Windows.SystemParameters.WorkArea.Left - iconsize + extra + cpadd - (apsb.BorderThickness.Left + apsb.BorderThickness.Right);// + App.settings.docktransformX;
            }
            if (App.settings.dockPosition == "Left") {
                if (!App.settings.registerAsAppBar) Left = System.Windows.SystemParameters.WorkArea.Left + extra - cpadd; //+ App.settings.docktransformX;
            }
            if (!App.settings.registerAsAppBar) Top = System.Windows.SystemParameters.WorkArea.Top;
            if (!App.settings.registerAsAppBar) Height = System.Windows.SystemParameters.WorkArea.Height;
            Width = iconsize + Math.Abs(App.settings.docktransformX) + apsb.BorderThickness.Left + apsb.BorderThickness.Right;
            dockitems.Orientation = Orientation.Vertical;
            dockiconsleft.Orientation = Orientation.Vertical;
            dockiconsright.Orientation = Orientation.Vertical;
            runningapps.Orientation = Orientation.Vertical;
            if (App.settings.docktransformX > 0) {
                mbar.HorizontalAlignment = HorizontalAlignment.Right;
            }else {
                mbar.HorizontalAlignment = HorizontalAlignment.Left;
            }
            mbar.VerticalAlignment = VerticalAlignment.Center;
            
            sw.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled; 
            sw.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            if (App.settings.docktransformY > 0) {
                apsb.Margin = new Thickness(0,App.settings.docktransformY,0,0);
            }else {
                apsb.Margin = new Thickness(0,0,0,Math.Abs(App.settings.docktransformY));
            }
        }
        if (!appbarregistered) {
            appbar();
        }
    }
    void appbar() {
        try {
            if (App.settings.registerAsAppBar) {
                //WindowStyle = WindowStyle.SingleBorderWindow;
                if (App.settings.dockPosition == "Bottom") {
                    AppBarFunctions.SetAppBar(this, ABEdge.Bottom);
                }
                if (App.settings.dockPosition == "Top") {
                    AppBarFunctions.SetAppBar(this, ABEdge.Top);
                }
                if (App.settings.dockPosition == "Left") {
                    AppBarFunctions.SetAppBar(this, ABEdge.Left);
                }
                if (App.settings.dockPosition == "Right") {
                    AppBarFunctions.SetAppBar(this, ABEdge.Right);
                }
            }else {
                //AppBarFunctions.SetAppBar(this, ABEdge.None);
                WindowStyle = WindowStyle.None;
            }
            appbarregistered = true;
        }catch (Exception e) {
            MessageBox.Show(e.ToString());
        }
    }
    Border createseparator() {
        Border sp = new();
        sp.Background = new SolidColorBrush(App.getColor(App.styles["separatorColor"].ToString()) ?? Color.FromRgb(255,255,255));
        if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
            sp.Width = 1;
            //sp.Height = iconsize - 6;
        }else if (App.settings.dockPosition == "Right" || App.settings.dockPosition == "Left") {
            sp.Height = 1;
            //sp.Width = iconsize - 6;
        }
        sp.Margin = new Thickness(3);
        if (App.styles.ContainsKey("separator")) {
            //Console.WriteLine("apply separator style...");
            applyStyle(sp, (JObject)App.styles["separator"]);
        }
        return sp;
    }

    void loadsettings() {
        foreach (object x in dockitems.Children) {
            if (x is StackPanel) {
                var sp = (StackPanel)x;
                sp.Width = double.NaN;
                sp.Height = double.NaN;
            }
        }

        try {
            var s = JsonConvert.DeserializeObject<Dictionary<string, Object>>(File.ReadAllText(App.settings.stylesPath));
            if (s != null) {
                App.styles = s;
                
            }
        }catch {

        }
        
        if (!App.styles.ContainsKey("separatorColor")) {
            App.styles["separatorColor"] = "#FFFFFF";
        }
        if (!App.styles.ContainsKey("activeAppColor")) {
            App.styles["activeAppColor"] = "ACCENT";
        }
        if (!App.styles.ContainsKey("dockButtonStyleToUse")) {
            App.styles["dockButtonStyleToUse"] = "asbs";
        }
        if (!App.styles.ContainsKey("submenuButtonStyleToUse")) {
            App.styles["submenuButtonStyleToUse"] = "asbs";
        }
        if (!App.styles.ContainsKey("submenuForeground")) {
            App.styles["submenuForeground"] = "#000000";
        }

        iconsize = App.settings.iconSize;
        Topmost = App.settings.topmost;
        if (App.styles.ContainsKey("dockBorder")) {
            applyStyle(apsb, (JObject)App.styles["dockBorder"]);
        }
        activecolor = new SolidColorBrush(App.getColor(App.styles["activeAppColor"].ToString()) ?? Color.FromRgb(0,0,0));
        foreach (object x in dockitems.Children) {
            if (x is StackPanel) {
                var sp = (StackPanel)x;
                if (App.settings.dockPosition == "Top" || App.settings.dockPosition == "Bottom") {
                    sp.Width = iconsize;
                }else {
                    sp.Height = iconsize;
                }
            }
        }
        appics.Clear();
        wins.Clear();
        groupexeicon.Clear();
        dockbtn.Clear();
        runningapps.Children.Clear();
        dockiconsleft.Children.Clear();
        dockiconsright.Children.Clear();
        //currentsize = App.settings.iconSize;
        StackPanel cnt = dockiconsleft;
        foreach (App.iconDataType ico in App.settings.dockItems) {
            if (ico.target == "EndSideStart") {
                cnt = dockiconsright;
            }else if (ico.target == "Separator") {
               cnt.Children.Add(createseparator()); 
            }else {
                dockButton btn = new(this);
                
                cnt.Children.Add(btn.btn);
                btn.updatedata(ico.name, false);
                btn.btn.Click += (e,a) => {
                    try {
                        if (ico.target == "AppsDrawer") {
                            if (apdw != null) {
                                apdw.Close();
                                apdw = null;
                            }else {
                                focusblur = false;
                                apdw = new appsdrawerWindow(this);
                                apdw.Show();
                                focusblur = false;
                            }
                        }else {
                            if (apdw != null) {
                                apdw.Close();
                                apdw = null;
                            }
                            Process.Start(new ProcessStartInfo() {
                                FileName = ico.target,
                                UseShellExecute = true
                            });
                        }
                    }catch (Exception ex) {MessageBox.Show(ex.ToString(),"Error",MessageBoxButton.OK,MessageBoxImage.Error);}
                    
                };
                try {
                    BitmapImage bitmap = new(new Uri(ico.icon));
                    btn.updateicon(bitmap);
                }catch {}
                btn.ctx.Items.Add(new Separator());
                MenuItem edititm = new() {Header = "Edit..."};
                edititm.Click += (e,a) => {
                    iconeditWindow ic = new(ico);
                    ic.ShowDialog();
                    loadsettings();
                    savesettings();
                };
                btn.ctx.Items.Add(edititm);
            }
        }
        //appbar();
    }
    void savesettings() {
        File.WriteAllText(settingspath,JsonConvert.SerializeObject(App.settings));
    }


    async void refreshtasklist() {
        if (App.settings.automaticSeparatorAtRunningApps && rapsep == null) rapsep = createseparator();
        IDictionary<HWND,string> windows = GetOpenWindows();
        List<object> editedButtons = new();
        foreach(KeyValuePair<IntPtr, string> window in windows)
        {
            if (hwnd != window.Key) {
                UInt32 pid;
                GetWindowThreadProcessId(window.Key,out pid);
                Process prc = Process.GetProcessById((int)pid);
                bool include = true;
                bool processResponding = false;
                if (!wins.Contains(window.Key)) {
                    try {
                        var task = Task.Factory.StartNew(() => prc.Responding);

                        processResponding = await task.WaitAsync(TimeSpan.FromSeconds(1)) && task.Result;
                        if (processResponding) {
                            if (pid == Process.GetCurrentProcess().Id) include = false;
                            var module = prc.MainModule;
                            if (module != null) {
                                if (App.settings.iconBlacklist.Contains(module.FileName) || App.settings.iconBlacklist.Contains(Path.GetFileName(module.FileName))) {
                                    include = false;
                                }
                            }
                        }else {}
                    }catch {}
                }else {include = false;}
                if (include && !appics.ContainsKey(window.Key)) {
                    
                        
                    void createicon(string key = "", string path = "") {
                        if (rapsep != null && rapsep.Parent != runningapps && App.settings.automaticSeparatorAtRunningApps) {
                            runningapps.Children.Add(rapsep);
                        }
                        dockButton btn = new(this,window.Key);
                        
                        btn.ctx.Items.Add(new Separator());
                        MenuItem miwins = new() {Header = "Windows..."};
                        btn.ctx.Items.Add(miwins);
                        miwins.Click += (e,a) => {
                            btn.spinnerpopup.IsOpen = true;
                        };
                        runningapps.Children.Add(btn.btn);
                        wins.Add(window.Key);
                        
                        if (key != "") {
                            btn.key = key;
                            groupexeicon[key] = btn;
                            dockInnerButton insbtn = new(btn);
                            insbtn.updatedata("New Instance...",false);
                            btn.spinner.Children.Add(insbtn.btn);
                            insbtn.btn.Click += (e,a) => {
                                Process.Start(path);
                            };
                        }
                        dockInnerButton btni = new(btn,window.Key);
                        btn.spinner.Children.Add(btni.btn);
                        appics[window.Key] = new List<object>() {btni,btn};
                    }
                    try {
                        var module = prc.MainModule;
                        if (module == null) throw new Exception();
                        string Key = App.settings.groupRunningApps == "Executable" ? module.FileName : App.settings.groupRunningApps == "Instance" ? prc.Id.ToString() : "";
                        if ((App.settings.groupRunningApps == "Executable" || App.settings.groupRunningApps == "Instance") == false || groupexeicon.ContainsKey(Key) == false) {
                            createicon(Key,module.FileName);
                        }else {
                            dockButton btn = groupexeicon[Key];
                            if (!btn.hwnds.Contains(window.Key)) {
                                btn.hwnds.Add(window.Key);
                                dockInnerButton btni = new(btn,window.Key);
                                if (appics.ContainsKey(window.Key)) {
                                    appics[window.Key].Add(btni);
                                }else {
                                    appics[window.Key] = new List<object>() {btn,btni};
                                }
                                wins.Add(window.Key);
                                btn.spinner.Children.Add(btni.btn);
                                Console.WriteLine("New window, " + window.Value);
                            }
                        }
                        
                    }catch {createicon();}
                    
                
                    
                }
                if (appics.ContainsKey(window.Key)) {
                    var l = appics[window.Key];
                    WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                    GetWindowPlacement(fgw, ref placement);
                    foreach (object x in l) {
                        if (x is dockButton) {
                            var btn = (dockButton)x;
                            Console.WriteLine("dock button");
                            if (btn.hwnds.Contains(fgw) && windows.ContainsKey(fgw)) {
                                Console.WriteLine("Foreground!! " + windows[fgw]);
                                btn.hd = fgw;
                                btn.updatedata(windows[fgw], placement.showCmd != 2);
                                if (!btn.uwpapp) {
                                    try {
                                        var mdl = prc.MainModule;
                                        if (mdl != null) {
                                            var ico = GetAppIcon(fgw);
                                            if (ico == null) {
                                                var app = App.AppxPackage.FromWindow(fgw);
                                                if (app != null) {
                                                    var path = app.FindHighestScaleQualifiedImagePath(app.ResourceId);
                                                    ico = new BitmapImage(new Uri(path));
                                                    btn.uwpapp = true;
                                                }else {
                                                    //MessageBox.Show("its null");
                                                }
                                            }
                                            if (ico == null) {
                                                ico = App.GetIcon(mdl.FileName,App.IconSize.Large, App.ItemState.Undefined);
                                            }
                                            if (ico != null) btn.updateicon(ico);
                                        }
                                    }catch {}
                                }
                            }else {
                                btn.hd = window.Key;
                                btn.updatedata(window.Value, (btn.hwnds.Contains(fgw) && placement.showCmd != 2));
                                try {
                                    var mdl = prc.MainModule;
                                    if (mdl != null) {
                                        var ico = GetAppIcon(window.Key) ?? App.GetIcon(mdl.FileName,App.IconSize.Large, App.ItemState.Undefined);
                                        if (ico != null) btn.updateicon(ico);
                                    }
                                }catch {}
                            }
                            
                        }else if (x is dockInnerButton) { // && !editedButtons.Contains(x)
                            Console.WriteLine("dock inner button");
                            var btn = (dockInnerButton)x;
                            btn.updatedata(window.Value, (fgw == window.Key && placement.showCmd != 2));
                            var ico = GetAppIcon(window.Key);
                            if (ico != null) btn.updateicon(ico);
                        }
                        editedButtons.Add(x);
                    }
                }else {Console.WriteLine("App icon not found!!!");}
            }
        }
        List<HWND> rmdw = new();
        foreach (HWND ptr in wins) {
            if (!windows.ContainsKey(ptr)) {
                rmdw.Add(ptr);
            }
        }
        foreach (HWND ptr in rmdw) {
            wins.Remove(ptr);
            var x = findfirstdockinnerbutton(appics[ptr]);
            if (x != null) {
                //var btn = x;
                //btn.hwnds.Remove(ptr);
                //if (btn.hwnds.Count == 0) {
                //    dockitems.Children.Remove(btn.btn);
                //}
                
                var btn = x;
                btn.parentdb.spinner.Children.Remove(btn.btn);
                btn.parentdb.hwnds.Remove(ptr);
                if (btn.parentdb.hwnds.Count == 0) {
                    runningapps.Children.Remove(btn.parentdb.btn);
                    groupexeicon.Remove(btn.parentdb.key);
                }
                appics.Remove(ptr);
            //}else {
            //    var btn = findfirstdockinnerbutton(appics[ptr]);
            //    btn.parentdb.spinner.Children.Remove(btn.btn);
            //    btn.parentdb.hwnds.Remove(ptr);
            //    if (btn.parentdb.hwnds.Count == 0) {
            //        dockitems.Children.Remove(btn.btn);
            //    }
            }
            
        }
        if (wins.Count == 0) {
            if (rapsep != null && rapsep.Parent == runningapps) {
                runningapps.Children.Remove(rapsep);
            }
        }
    }

    static void applyStyle(UIElement t, JObject style) {
        if (style.ContainsKey("visibility")) {
            switch ((style["visibility"] ?? "").ToString()) {
                case "collapsed":
                    t.Visibility = Visibility.Collapsed;
                    break;
                case "hidden":
                    t.Visibility = Visibility.Hidden;
                    break;
                default:
                    t.Visibility = Visibility.Visible;
                    break;
            }
        }
        
        if (t is Control) {
            var target = (Control)t;
            if (style.ContainsKey("background")) {
                //var clr = App.getColor((style["background"] ?? "").ToString() ?? "");
                //if (clr != null)
                target.Background = App.getBrush(style["background"]);
            }
            if (style.ContainsKey("borderThickness")) {
                var thick = App.getThick((style["borderThickness"] ?? "").ToString() ?? "");
                if (thick != null)
                target.BorderThickness = (Thickness)thick;
            }
            if (style.ContainsKey("borderBrush")) {
                //var clr = App.getColor((style["borderBrush"] ?? "").ToString() ?? "");
                //if (clr != null)
                target.BorderBrush = App.getBrush(style["borderBrush"]);
            }
            if (style.ContainsKey("margin")) {
                var thick = App.getThick((style["margin"] ?? "").ToString() ?? "");
                if (thick != null)
                target.Margin = (Thickness)thick;
            }
            if (style.ContainsKey("padding")) {
                var thick = App.getThick((style["padding"] ?? "").ToString() ?? "");
                if (thick != null)
                target.Padding = (Thickness)thick;
            }
            if (style.ContainsKey("width")) {
                if (style["width"] != null) {
                    var dob = App.getDouble(style["width"].ToString());
                    if (dob != null) target.Width = (double)dob;
                }
            }
            if (style.ContainsKey("height")) {
                if (style["height"] != null) {
                    var dob = App.getDouble(style["height"].ToString());
                    if (dob != null) target.Height = (double)dob;
                }
            }
            if (style.ContainsKey("maxWidth")) {
                if (style["maxWidth"] != null) {
                    target.MaxWidth = (int)style["maxWidth"];
                }
            }
            if (style.ContainsKey("maxHeight")) {
                if (style["maxHeight"] != null) {
                    target.MaxHeight = (int)style["maxHeight"];
                }
            }
            if (style.ContainsKey("minWidth")) {
                if (style["minWidth"] != null) {
                    target.MinWidth = (int)style["minWidth"];
                }
            }
            if (style.ContainsKey("minHeight")) {
                if (style["minHeight"] != null) {
                    target.MinHeight = (int)style["minHeight"];
                }
            }
        }
        if (t is Border) {
            var target = (Border)t;
            if (style.ContainsKey("background")) {
                //var clr = App.getColor((style["background"] ?? "").ToString() ?? "");
                //if (clr != null)
                target.Background = App.getBrush(style["background"]);
            }
            if (style.ContainsKey("borderThickness")) {
                var thick = App.getThick((style["borderThickness"] ?? "").ToString() ?? "");
                if (thick != null)
                target.BorderThickness = (Thickness)thick;
            }
            if (style.ContainsKey("borderBrush")) {
                //var clr = App.getColor((style["borderBrush"] ?? "").ToString() ?? "");
                //if (clr != null)
                target.BorderBrush = App.getBrush(style["borderBrush"]);
            }
            if (style.ContainsKey("margin")) {
                var thick = App.getThick((style["margin"] ?? "").ToString() ?? "");
                if (thick != null)
                target.Margin = (Thickness)thick;
            }
            if (style.ContainsKey("padding")) {
                var thick = App.getThick((style["padding"] ?? "").ToString() ?? "");
                if (thick != null)
                target.Padding = (Thickness)thick;
            }
            if (style.ContainsKey("cornerRadius")) {
                var radius = App.getCornerRadius((style["cornerRadius"] ?? "").ToString() ?? "");
                if (radius != null)
                target.CornerRadius = (CornerRadius)radius;
            }
            if (style.ContainsKey("width")) {
                if (style["width"] != null) {
                    var dob = App.getDouble(style["width"].ToString());
                    if (dob != null) target.Width = (double)dob;
                }
            }
            if (style.ContainsKey("height")) {
                if (style["height"] != null) {
                    var dob = App.getDouble(style["height"].ToString());
                    if (dob != null) target.Height = (double)dob;
                }
            }
            if (style.ContainsKey("maxWidth")) {
                if (style["maxWidth"] != null) {
                    target.MaxWidth = (int)style["maxWidth"];
                }
            }
            if (style.ContainsKey("maxHeight")) {
                if (style["maxHeight"] != null) {
                    target.MaxHeight = (int)style["maxHeight"];
                }
            }
            if (style.ContainsKey("minWidth")) {
                if (style["minWidth"] != null) {
                    target.MinWidth = (int)style["minWidth"];
                }
            }
            if (style.ContainsKey("minHeight")) {
                if (style["minHeight"] != null) {
                    target.MinHeight = (int)style["minHeight"];
                }
            }
        }
    }

    dockButton? findfirstdockbutton(List<object> l) {
        foreach (object i in l) {
            if (i is dockButton) {
                return (dockButton)i;
            }
        }
        return null;
    }
    dockInnerButton? findfirstdockinnerbutton(List<object> l) {
        foreach (object i in l) {
            if (i is dockInnerButton) {
                return (dockInnerButton)i;
            }
        }
        return null;
    }

    public class dockButton {
        ToolTip btntip = new();
        public ContextMenu ctx = new();
        public Button btn = new() {BorderThickness = new Thickness(0),Background = Brushes.Transparent, Width = iconsize,Height = iconsize};
        public Image ico = new() {Width = iconsize / 2, Height = iconsize / 2,Stretch = Stretch.Uniform};
        public Panel spinner;
        public ScrollViewer spinnerscroll = new() {VerticalScrollBarVisibility = ScrollBarVisibility.Auto};
        public Popup spinnerpopup = new();
        bool activ = false;
        public List<HWND> hwnds = new();
        public string key = "";
        public HWND hd = 0;
        public bool uwpapp = false;
        public void updatedata(string title, bool active = false) {
            btntip.Content = title;
            activ = active;
            if (active) {
                btn.Background = activecolor;
                ico.Width = (iconsize / 2) + 4;
                ico.Height = (iconsize / 2) + 4;
            }else {
                btn.Background = Brushes.Transparent;
                ico.Width = iconsize / 2;
                ico.Height = iconsize / 2;
            }
        }
        public void updateicon(ImageSource imgs) {
            ico.Source = imgs;
        }
        public dockButton(MainWindow w,HWND hdd = 0) {
            if (App.settings.useIconsInSubmenus) {
                spinner = new WrapPanel() {MaxWidth=320};
            }else {
                spinner = new StackPanel() {MaxWidth=320};
            }
            hd = hdd;
            spinnerscroll.Content = spinner;
            Border spinnercont = new()
            {
                Background = new SolidColorBrush(Color.FromRgb(255,255,255)),
                CornerRadius = new CornerRadius(8),
                Child = spinnerscroll
            };
            if (App.styles.ContainsKey("submenuBorder")) {
                applyStyle(spinnercont, (JObject)App.styles["submenuBorder"]);
            }
            spinnerpopup.Child = spinnercont;
            spinnerpopup.StaysOpen = false;
            spinnerpopup.Margin = new Thickness(iconsize);
            spinnerpopup.AllowsTransparency = true;
            MenuItem mmi = new() {Header = "OrangemiumDock..."};
            ctx.Items.Add(mmi);
            mmi.Click += (e,a) => {
                mainmenu.IsOpen = true;
            };
            Grid bg = new();
            bg.Children.Add(ico);
            bg.Children.Add(spinnerpopup);
            btn.Content = bg;

            dockbtn[btn] = this;
            btn.ContextMenu = ctx;
            
            btn.ToolTip = btntip;
            try{btn.Style = (Style)Application.Current.Resources[App.styles["dockButtonStyleToUse"].ToString()];}catch{}
            if (hd != 0) {
                btn.Click += (e,a) => {
                    if (hwnds.Count > 1) {
                        spinnerpopup.IsOpen = true;
                    }else {
                        if (activ) {
                            ShowWindow(hd, SW_MINIMIZE);
                        }else {
                            ShowWindow(hd, SW_MINIMIZE);
                            ShowWindow(hd, SW_RESTORE);
                            SetForegroundWindow(hd);
                        }
                    }
                    
                };
            }
            hwnds.Add(hd);
            if (App.styles.ContainsKey("dockButton")) {
                applyStyle(btn, (JObject)App.styles["dockButton"]);
            }
        }
    }
    public class dockInnerButton {
        public dockButton parentdb;
        ToolTip btntip = new();
        public Button btn = new() {BorderThickness = new Thickness(0),Background = Brushes.Transparent, Height = iconsize};
        public Image ico = new() {Width = iconsize / 2, Height = iconsize / 2,Margin = new Thickness(iconsize / 8),Stretch = Stretch.Uniform,VerticalAlignment = VerticalAlignment.Center};
        public TextBlock name = new() {VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(12), Foreground = new SolidColorBrush(App.getColor(App.styles["submenuForeground"].ToString()) ?? Color.FromRgb(255,255,255)),TextTrimming = TextTrimming.CharacterEllipsis};
        bool activ = false;
        public void updatedata(string title, bool active = false) {
            name.Text = title;
            btntip.Content = title;
            activ = active;
            if (active) {
                btn.Background = activecolor;
            }else {
                btn.Background = Brushes.Transparent;
            }
        }
        public void updateicon(ImageSource imgs) {
            ico.Source = imgs;
        }
        public dockInnerButton(dockButton parent,HWND hd = 0) {
            parentdb = parent;
            DockPanel sp = new() {HorizontalAlignment = HorizontalAlignment.Left};
            sp.Children.Add(ico);
            if (App.settings.useIconsInSubmenus) {
                btn.Width = App.settings.iconSize;
                btn.Height = App.settings.iconSize;
                btn.ToolTip = btntip;
            }else {
                sp.Children.Add(name);
            }
            btn.Content = sp;
            try {btn.Style = (Style)Application.Current.Resources[App.styles["submenuButtonStyleToUse"].ToString()];}catch {}
            if (hd != 0) {
                btn.MouseDown += (a,e) => {
                    if (e.ChangedButton == MouseButton.Middle && e.ButtonState == MouseButtonState.Pressed) {
                        SendMessage(hd, WM_CLOSE, 0, 0);
                    }
                };
                btn.Click += (e,a) => {
                    if (activ) {
                        ShowWindow(hd, SW_MINIMIZE);
                    }else {
                        ShowWindow(hd, SW_MINIMIZE);
                        ShowWindow(hd, SW_RESTORE);
                        SetForegroundWindow(hd);
                    }
                };
            }
            if (App.styles.ContainsKey("dockInnerButton")) {
                applyStyle(btn, (JObject)App.styles["dockInnerButton"]);
            }
        }
    }
}