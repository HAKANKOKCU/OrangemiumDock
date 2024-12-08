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
/// Interaction logic for shutdownpopup.xaml
/// </summary>
public partial class shutdownPopup : Window
{
    JObject style = new();
    public shutdownPopup()
    {
        Grid mg = new();
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ShowInTaskbar = false;
        Topmost = true;
        if (App.styles.ContainsKey("shutdowndialog")) {
            style = (JObject)App.styles["shutdowndialog"];
        }
        
        Background = Brushes.Transparent;
        Border? bg = null;
        if (App.settings.animationSpeed != 0) {
            bg = new() {CornerRadius = new CornerRadius(24),MaxWidth=24,MaxHeight=24,HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch};
            Opacity = 0;
            mg.Children.Insert(0,bg);
            if (style.ContainsKey("background")) {
                bg.Background = App.getBrush(style["background"]);
            }else {
                bg.Background = new SolidColorBrush(Color.FromArgb(200,0,0,0));
            }
        }else {
            if (style.ContainsKey("background")) {
                Background = App.getBrush(style["background"]);
            }else {
                Background = new SolidColorBrush(Color.FromArgb(200,0,0,0));
            }
        }
        
        AllowsTransparency = true;
        KeyDown += (a,e) => {
            if (e.Key == Key.Escape) {
                Close();
            }
            if (e.Key == Key.F1) {
                darknessEffect();
            }
        };
        DockPanel mc = new() {Margin = new Thickness(8)};
        StackPanel soptions = new() {Orientation = Orientation.Horizontal,HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center};
        soptions.Children.Add(sitem("Shutdown","s /hybrid /t 0"));
        soptions.Children.Add(sitem("Restart","r /t 0"));
        soptions.Children.Add(sitem("Logout","l"));
        soptions.Children.Add(sitem("Hibernate","h"));
        mc.Children.Add(soptions);
        mg.Children.Add(mc);
        Content = mg;

        bool preventclose = true;
        Closing += (e,a) => {
            App.DisableBlur(this);
            if (preventclose) {
                if (App.settings.animationSpeed != 0) {
                    a.Cancel = true;
                    preventclose = false;
                    bg.CornerRadius = new CornerRadius(24);
                    DoubleAnimation opacit = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5 * (App.settings.animationSpeed / 5)));
                    opacit.EasingFunction = App.eio;
                    opacit.Completed += (e,a) => Close();
                    BeginAnimation(Window.OpacityProperty, opacit);
                    DoubleAnimation sizw = new DoubleAnimation(Width, 0, TimeSpan.FromSeconds(0.5 * (App.settings.animationSpeed / 5)));
                    sizw.EasingFunction = App.eio;
                    bg.BeginAnimation(Border.MaxWidthProperty, sizw);
                    bg.BeginAnimation(Border.MaxHeightProperty, sizw);
                }
            }else {

            }
            
        };

        Loaded += (e,a) => {
            Activate();Deactivated += (e,a) => {try {Close();}catch{}};
            if (App.settings.animationSpeed != 0) {
                DoubleAnimation opacit = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5 * (App.settings.animationSpeed / 5)));
                opacit.Completed += (e,a) => {
                    bg.CornerRadius = new CornerRadius(0);
                    App.EnableBlur(this);
                };
                opacit.EasingFunction = App.eio;
                BeginAnimation(Window.OpacityProperty, opacit);
                DoubleAnimation sizw = new DoubleAnimation(0, Width, TimeSpan.FromSeconds(0.5 * (App.settings.animationSpeed / 5)));
                sizw.EasingFunction = App.eio;
                bg.BeginAnimation(Border.MaxWidthProperty, sizw);
                bg.BeginAnimation(Border.MaxHeightProperty, sizw);
            }
        };
    }

    Button sitem(string text,string action) {
        Button itm = new() {HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch};
        itm.Style = (Style)App.Current.Resources["OBtnLight"];
        itm.Background = Brushes.Transparent;
        itm.Margin = new Thickness(8);
        itm.Foreground = Brushes.White;
        itm.Content = text;
        itm.Click += (e,a) => {
            darknessEffect();
            Process.Start(new ProcessStartInfo() {
                FileName = "shutdown",
                Arguments = "/" + action,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        };
        return itm;
    }
    void darknessEffect() {
        if (App.settings.animationSpeed == 0) return;
        var win = new Window();
        win.Loaded += (e,a) => {
            Close();
            App.EnableBlur(win);
        };
        win.ShowInTaskbar = false;
        win.Background = Brushes.Black;
        win.WindowStyle = WindowStyle.None;
        win.WindowState = WindowState.Maximized;
        win.Topmost = true;
        win.AllowsTransparency = true;
        win.Opacity = 0;
        win.Show();
        DoubleAnimation opacit = new DoubleAnimation(0.7843137254901961, 1, TimeSpan.FromSeconds(0.2 * (App.settings.animationSpeed / 5)));
        opacit.EasingFunction = App.eio;
        opacit.Completed += (e,a) => {
            Thread.Sleep(500);
            win.Close();
        };
        win.BeginAnimation(Window.OpacityProperty, opacit);
    }
}