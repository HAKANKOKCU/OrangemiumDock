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

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for shutdownpopup.xaml
/// </summary>
public partial class shutdownPopup : Window
{
    JObject style = new();
    public shutdownPopup()
    {
        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;
        ShowInTaskbar = false;
        Topmost = true;
        if (App.styles.ContainsKey("shutdowndialog")) {
            style = (JObject)App.styles["shutdowndialog"];
        }
        if (style.ContainsKey("background")) {
            Background = App.getBrush(style["background"]);
        }else {
            Background = new SolidColorBrush(Color.FromArgb(180,0,0,0));
        }
        AllowsTransparency = true;
        KeyDown += (a,e) => {
            if (e.Key == Key.Escape) {
                Close();
            }
        };
        DockPanel mc = new() {Margin = new Thickness(8)};
        StackPanel soptions = new() {Orientation = Orientation.Horizontal,HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center};
        soptions.Children.Add(sitem("Shutdown","s /hybrid /t 0"));
        soptions.Children.Add(sitem("Restart","r /t 0"));
        soptions.Children.Add(sitem("Logout","l"));
        soptions.Children.Add(sitem("Hibernate","h"));
        mc.Children.Add(soptions);
        Content = mc;

        Loaded += (e,a) => {
            Activate();Deactivated += (e,a) => {try {Close();}catch{}};
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
            //MessageBox.Show("/" + action);
            Process.Start("shutdown","/" + action);
        };
        return itm;
    }
    void darknessEffect() {
        Close();
        if (App.settings.animationSpeed == 0) return;
        var win = new Window();
        win.ShowInTaskbar = false;
        win.Background = Brushes.Black;
        win.WindowStyle = WindowStyle.None;
        win.WindowState = WindowState.Maximized;
        win.Topmost = true;
        win.AllowsTransparency = true;
        win.Opacity = 0;
        win.Show();
        DoubleAnimation opacit = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5 * (App.settings.animationSpeed / 5)));
        opacit.EasingFunction = App.eio;
        opacit.Completed += (e,a) => {
            Thread.Sleep(250);
            win.Close();
        };
        win.BeginAnimation(Window.OpacityProperty, opacit);
    }
}