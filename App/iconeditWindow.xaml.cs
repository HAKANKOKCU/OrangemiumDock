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
/// Interaction logic for iconeditWindow.xaml
/// </summary>
public partial class iconeditWindow : Window
{

    public iconeditWindow(App.iconDataType icon)
    {
        InitializeComponent();
        Title = icon.name;
        
        nameTB.Text = icon.name;
        nameTB.TextChanged += (a,e) => {
            icon.name = nameTB.Text;
        };
        targetTB.Text = icon.target;
        targetTB.TextChanged += (a,e) => {
            icon.target = targetTB.Text;
        };
        iconTB.Text = icon.icon;
        iconTB.TextChanged += (a,e) => {
            icon.icon = iconTB.Text;
            licon(icon);
        };
        parametersTB.Text = icon.parameters;
        parametersTB.TextChanged += (a,e) => {
            icon.parameters = parametersTB.Text;
        };
        licon(icon);
    }
    void licon(App.iconDataType icon) {
        try {
            BitmapImage bitmap = new(new Uri(icon.icon));
            Icon = bitmap;
        }catch {}
    }
}