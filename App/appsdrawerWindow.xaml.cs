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
    List<Thread> threads = new();
    Dictionary<string,BitmapSource> iconcache = new();
    public appsdrawerWindow(MainWindow window)
    {
        InitializeComponent();
        settings = window.settings;
        if (window.settings.appsDrawerTheme == "Dark") {
            Background = new SolidColorBrush(Color.FromArgb(120,0,0,0));
        }else {
            Background = new SolidColorBrush(Color.FromArgb(120,255,255,255));
            mtb.CaretBrush = Brushes.Black;
            mtb.Foreground = Brushes.Black;
            tb.Background = Brushes.White;
        }
        
        window.Content = new Grid();
        if (window.settings.dockPosition == "Bottom") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Center;
            window.mtc.VerticalAlignment = VerticalAlignment.Bottom;
            wp.Margin = new Thickness(0,42,0,window.settings.iconSize);
            mdp.Children.Add(window.mtc);
        }else if (window.settings.dockPosition == "Left") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Left;
            window.mtc.VerticalAlignment = VerticalAlignment.Center;
            wp.Margin = new Thickness(window.settings.iconSize,42,0,0);
            mdp.Children.Add(window.mtc);
        }else if (window.settings.dockPosition == "Right") {
            window.mtc.HorizontalAlignment = HorizontalAlignment.Right;
            window.mtc.VerticalAlignment = VerticalAlignment.Center;
            wp.Margin = new Thickness(0,42,0,0);
            sw.Margin = new Thickness(0,0,window.settings.iconSize,0);
            mdp.Children.Add(window.mtc);
        }
        
       
        Closing += (e,a) => {
            threads.Clear();
            try {
                mdp.Children.Remove(window.mtc);
                window.Content = window.mtc;
                window.mtc.HorizontalAlignment = HorizontalAlignment.Stretch;
                window.mtc.VerticalAlignment = VerticalAlignment.Stretch;
            }catch {}
            window.apdw = null;
            
        };
        KeyDown += (a,e) => {
            if (e.Key == Key.Escape) {
                Close();
            }
        };
        Loaded += (e,a) => {Activate();mtb.Focus();Deactivated += (e,a) => {try {Close();}catch{}};};
        reloadlist();
        mtb.TextChanged += (e,a) => {reloadlist();};
    }

    string smp = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);

    void startnextthread() {
        if (threads.Count > 0) {
            Thread th = threads[0];
            threads.RemoveAt(0);
            if (!th.IsAlive) th.Start();
        }
    }
    void reloadlist(string? filter = null) {
        threads.Clear();
        if (filter == null) filter = mtb.Text;
        wp.Children.Clear();
        List<string> dirs = Directory.GetDirectories(smp).ToList();
        dirs.Insert(0, smp);
        foreach (string dir in dirs) {
            try {
                string[] fils = Directory.GetFiles(dir);
                StackPanel sp = new() {Orientation = Orientation.Vertical};
                Label titlelabel = new() {FontSize = 16, Content = Path.GetFileName(dir),Foreground = settings.appsDrawerTheme == "Dark" ? Brushes.White : Brushes.Black};
                WrapPanel wpa = new();
                sp.Children.Add(titlelabel);
                sp.Children.Add(wpa);
                
                bool add = true;
                foreach (string file in fils) {
                    string extension = Path.GetExtension(file).ToLower();
                    if ((extension == ".lnk" || extension == ".exe") && Path.GetFileName(file).Replace(extension,"").ToLower().Contains(filter.ToLower())) {
                        Button btn = new() {Width = 124, Height = 124};
                        btn.Style = (Style)App.Current.Resources[settings.appsDrawerTheme == "Dark" ? "OBtnLight" : "OBtn"];
                        btn.Background = Brushes.Transparent;
                        StackPanel btns = new() {HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,Orientation = Orientation.Vertical};
                        Image img = new() {HorizontalAlignment = HorizontalAlignment.Center,Width = 40, Height = 40,Margin = new Thickness(8)};
                        var path = file + ""; //remove reference
                        if (iconcache.ContainsKey(path)) {
                            img.Source = iconcache[path];
                        }else {
                            threads.Add(new Thread(() => {
                                var ico = App.GetIcon(path,App.IconSize.Large,App.ItemState.Undefined);
                                if (ico != null) {
                                    ico.Freeze();
                                    iconcache.Add(path,ico);
                                    Application.Current.Dispatcher.Invoke(new Action(() => {img.Source = ico;}));
                                }
                                startnextthread();
                            }));
                        }
                        btns.Children.Add(img);
                        TextBlock lbl = new() {Foreground = settings.appsDrawerTheme == "Dark" ? Brushes.White : Brushes.Black, TextAlignment = TextAlignment.Center,HorizontalAlignment = HorizontalAlignment.Center, Text = Path.GetFileName(file).Replace(extension,""), TextWrapping = TextWrapping.Wrap};
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
                        if (add) {
                            add = false;
                            wp.Children.Add(sp);
                        }
                    }
                }
                
            }catch {}
        }
        startnextthread();
    }
}