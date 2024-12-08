using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using HWND = System.IntPtr;
using System.Windows.Media.Animation;
using Newtonsoft.Json.Linq;

using Control = System.Windows.Controls.Control;
using Button = System.Windows.Controls.Button;
using ToolTip = System.Windows.Controls.ToolTip;
using Image = System.Windows.Controls.Image;
using Panel = System.Windows.Controls.Panel;
using Orientation = System.Windows.Controls.Orientation;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Brush = System.Windows.Media.Brush;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using VerticalAlignment = System.Windows.VerticalAlignment;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
using DataFormats = System.Windows.DataFormats;
using MessageBox = System.Windows.MessageBox;
using Label = System.Windows.Controls.Label;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;

namespace OrangemiumDock;

/// <summary>
/// desktop manager that prolly not gonna make
/// </summary>
public partial class desktopManager : Window
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    
    const int WM_CLOSE = 0x0010;

    [DllImport("USER32.DLL")]
    private static extern IntPtr GetShellWindow();
    public desktopManager() {
        Topmost = true;
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        if (Process.GetProcessesByName("explorer").Length == 0) {
            Process.Start("explorer").WaitForInputIdle();
        }
        //SendMessage(GetShellWindow(), WM_CLOSE, 0, 0);
        Topmost = false;
        Grid content = new();
        Grid appcontent = new();
        Image bg = new();
        bg.Source = new BitmapImage(new Uri(App.GetPathWallpaper()));
        bg.Stretch = Stretch.UniformToFill;

        MainWindow dock = new();
        dock.Show();
        content.Children.Add(bg);
        content.Children.Add(appcontent);

        Content = content;
    }
}