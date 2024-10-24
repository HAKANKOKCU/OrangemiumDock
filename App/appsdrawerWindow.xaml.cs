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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace OrangemiumDock;

/// <summary>
/// Interaction logic for appsdrawerWindow.xaml
/// </summary>
public partial class appsdrawerWindow : Window
{
    App.settingsDataType settings;
    Dictionary<string,Image> iconload = new();
    Thread iconloader;
    public appsdrawerWindow(MainWindow window)
    {
        bool running = true;
        iconloader = new Thread(() => {
            while (running) {
                foreach (var kvp in iconload) {
                    var path = kvp.Key;
                    var img = kvp.Value;
                    var ico = App.GetIcon(path,App.IconSize.Large,App.ItemState.Undefined);
                    if (ico != null) {
                        ico.Freeze();
                        Application.Current.Dispatcher.Invoke(new Action(() => {img.Source = ico;}));
                    }
                }
                iconload.Clear();
                Thread.Sleep(100);
            }
        });
        InitializeComponent();
        settings = App.settings;
        if (App.settings.appsDrawerTheme == "Dark") {
            Background = new SolidColorBrush(Color.FromArgb(App.settings.appsMenuAlpha,0,0,0));
        }else {
            Background = new SolidColorBrush(Color.FromArgb(App.settings.appsMenuAlpha,255,255,255));
            mtb.CaretBrush = Brushes.Black;
            mtb.Foreground = Brushes.Black;
            tb.Background = Brushes.White;
        }
        if (window.blr != null) window.blr.Hide();
        window.Content = new Grid();
        if (App.settings.dockPosition == "Bottom") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Center;
            window.mtc.VerticalAlignment = VerticalAlignment.Bottom;
            wp.Margin = new Thickness(0,42,0,App.settings.iconSize);
            mdp.Children.Add(window.mtc);
        }else if (App.settings.dockPosition == "Left") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Left;
            window.mtc.VerticalAlignment = VerticalAlignment.Center;
            wp.Margin = new Thickness(App.settings.iconSize,42,0,0);
            mdp.Children.Add(window.mtc);
        }else if (App.settings.dockPosition == "Right") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Right;
            window.mtc.VerticalAlignment = VerticalAlignment.Center;
            wp.Margin = new Thickness(0,42,0,0);
            sw.Margin = new Thickness(0,0,App.settings.iconSize,0);
            mdp.Children.Add(window.mtc);
        }else {
            wp.Margin = new Thickness(0,42,0,0);
        }
        
       
        Closing += (e,a) => {
            running = false;
            try {
                mdp.Children.Remove(window.mtc);
                window.Content = window.mtc;
                window.mtc.HorizontalAlignment = HorizontalAlignment.Stretch;
                window.mtc.VerticalAlignment = VerticalAlignment.Stretch;
            }catch {}
            window.apdw = null;
            if (window.blr != null) window.blr.Show();
        };
        KeyDown += (a,e) => {
            if (e.Key == Key.Escape) {
                Close();
            }
            if (e.Key == Key.F11) {
                WindowState = WindowState.Normal;
                try {
                    mdp.Children.Remove(window.mtc);
                    window.Content = window.mtc;
                    window.mtc.HorizontalAlignment = HorizontalAlignment.Stretch;
                    window.mtc.VerticalAlignment = VerticalAlignment.Stretch;
                }catch {}
                if (window.blr != null) window.blr.Show();
                mdp.Margin = new Thickness(0);
            }
        };
        
        Loaded += (e,a) => {Activate();if (App.settings.enableAppsDrawerBlur) App.EnableBlur(this);mtb.Focus();Deactivated += (e,a) => {try {Close();}catch{}};};
        dirs = Directory.GetDirectories(smpc).ToList();
        foreach (string d in Directory.GetDirectories(smp)) {
            dirs.Add(d);
        }
        
        dirs.Insert(0, smpc);
        dirs.Insert(0, smp);
        reloadlist();
        mtb.TextChanged += (e,a) => {reloadlist();};
        iconloader.Start();
    }

    List<string> dirs;

    string smp = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    string smpc = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);

    void reloadlist(string? filter = null) {
        sw.ScrollToHome();
        if (filter == null) filter = mtb.Text;
        wp.Children.Clear();
        Dictionary<string,WrapPanel> wpas = new();
        Dictionary<string,StackPanel> sps = new();
        foreach (string dir in dirs) {
            try {
                string[] fils = Directory.GetFiles(dir);
                WrapPanel wpa;
                StackPanel sp;
                if (wpas.ContainsKey(Path.GetFileName(dir).ToLower())) {
                    wpa = wpas[Path.GetFileName(dir).ToLower()];
                    sp = sps[Path.GetFileName(dir).ToLower()];
                }else {
                    sp = new() {Orientation = Orientation.Vertical};
                    Label titlelabel = new() {FontSize = 16, Content = Path.GetFileName(dir),Foreground = settings.appsDrawerTheme == "Dark" ? Brushes.White : Brushes.Black};
                    wpa = new();
                    sp.Children.Add(titlelabel);
                    sp.Children.Add(wpa);
                    wpas[Path.GetFileName(dir).ToLower()] = wpa;
                    sps[Path.GetFileName(dir).ToLower()] = sp;
                }
                
                foreach (string file in fils) {
                    string extension = Path.GetExtension(file).ToLower();
                    string name = Path.GetFileName(file).Replace(extension,"").ToLower();
                    if ((extension == ".lnk" || extension == ".exe") && name.Contains(filter.ToLower()) && ((!name.Contains("uninstall") && !name.Contains("readme")) || name.Contains("tool"))) {
                        Button btn = new() {HorizontalContentAlignment = HorizontalAlignment.Stretch};
                        if (App.settings.appsDrawerItemStyle == "Grid") {
                            btn.Width = 124;
                            btn.Height = 124;
                        }else {
                            btn.Width = 296;
                            btn.Height = 64;
                        }
                        
                        btn.Style = (Style)App.Current.Resources[settings.appsDrawerTheme == "Dark" ? "OBtnLight" : "OBtn"];
                        btn.Background = Brushes.Transparent;
                        Panel btns;
                        Image img = new() {HorizontalAlignment = App.settings.appsDrawerItemStyle == "Grid" ? HorizontalAlignment.Center : HorizontalAlignment.Left,VerticalAlignment = VerticalAlignment.Center};
                        if (App.settings.appsDrawerItemStyle == "Grid") {
                            btns = new StackPanel() {HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,Orientation = App.settings.appsDrawerItemStyle == "Grid" ? Orientation.Vertical : Orientation.Horizontal};
                            img.Margin = new Thickness(8);
                            img.Width = 40;
                            img.Height = 40;
                        }else {
                            btns = new DockPanel();
                            //btns.Width = 296;
                            img.Margin = new Thickness(8,0,8,0);
                            img.Width = 28;
                            img.Height = 28;
                        }
                        var path = file + ""; //remove reference
                        
                        if (App.iconcache.ContainsKey(path.ToLower())) {
                            img.Source = App.iconcache[path.ToLower()];
                        }else {
                            iconload[path] = img;
                        }

                        btns.Children.Add(img);
                        TextBlock lbl = new() {Foreground = settings.appsDrawerTheme == "Dark" ? Brushes.White : Brushes.Black, TextAlignment = TextAlignment.Center,HorizontalAlignment = App.settings.appsDrawerItemStyle == "Grid" ? HorizontalAlignment.Center : HorizontalAlignment.Left, Text = Path.GetFileName(file).Replace(extension,""), TextWrapping = TextWrapping.Wrap,VerticalAlignment = VerticalAlignment.Center};
                        if (App.settings.appsDrawerItemStyle == "Grid") {

                        }else {
                            //lbl.MaxWidth = 288;
                            //lbl.Margin = new Thickness(40,0,8,0);;
                        }
                        btns.Children.Add(lbl);
                        btn.Content = btns;
                        wpa.Children.Add(btn);
                        btn.Click += (e,a) => {
                            Process.Start(new ProcessStartInfo() {
                                FileName = file,
                                UseShellExecute = true
                            });
                            Close();
                        };
                        if (!wp.Children.Contains(sp)) {
                            wp.Children.Add(sp);
                        }
                    }
                }
                
            }catch (Exception e) {Console.WriteLine(e);}
        }
        //if (!iconloader.IsAlive) iconloader.Start();
    }
}