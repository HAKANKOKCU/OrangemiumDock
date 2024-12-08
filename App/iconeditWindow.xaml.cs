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