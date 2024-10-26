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
public partial class dockeditWindow : Window
{
    List<App.iconDataType> list;

    public dockeditWindow(List<App.iconDataType> dockitems)
    {
        InitializeComponent();
        list = dockitems;
        makelist();
        upbtn.Click += (e,a) => {
            try {
                var cache = list[lb.SelectedIndex - 1];
                list[lb.SelectedIndex - 1] = list[lb.SelectedIndex];
                list[lb.SelectedIndex] = cache;
                int pos = lb.SelectedIndex;
                makelist();
                lb.SelectedIndex = pos - 1;
            }catch {}
        };
        downbtn.Click += (e,a) => {
            try {
                var cache = list[lb.SelectedIndex + 1];
                list[lb.SelectedIndex + 1] = list[lb.SelectedIndex];
                list[lb.SelectedIndex] = cache;
                int pos = lb.SelectedIndex;
                makelist();
                lb.SelectedIndex = pos + 1;
            }catch {}
        };
        deletebtn.Click += (e,a) => {
            try {
                list.RemoveAt(lb.SelectedIndex);
                makelist();
            }catch {}
        };
        editbtn.Click += (e,a) => {
            if (lb.SelectedIndex < 0) {return;}
            iconeditWindow ic = new(list[lb.SelectedIndex]);
            ic.ShowDialog();
            makelist();
        };
        lb.Drop += (s,e) => {
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
                    list.Add(ico);
                    if (files.Length == 1) {
                        iconeditWindow ic = new(ico);
                        ic.ShowDialog();
                    }
                }
                makelist();
            }
        };
        rdcbtn.Click += (e,a) => {
            list.Add(new App.iconDataType() {target="EndSideStart"});
            makelist();
        };
        sepbtn.Click += (e,a) => {
            list.Add(new App.iconDataType() {target="Separator"});
            makelist();
        };
    }
    void makelist() {
        lb.Items.Clear();
        rdcbtn.IsEnabled = true;
        foreach (App.iconDataType i in list) {
            if (i.target == "Separator" || i.target == "EndSideStart") {
                if (i.target == "EndSideStart") {
                    rdcbtn.IsEnabled = false;
                }
                lb.Items.Add(i.target);
            }else {
                lb.Items.Add(i.name);
            }
        }
    }
}