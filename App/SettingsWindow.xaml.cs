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
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{

    public SettingsWindow(App.settingsDataType settings)
    {
        InitializeComponent();
        iconSizeTB.Text = settings.iconSize.ToString();
        iconSizeTB.TextChanged += (a,e) => {
            try {
                settings.iconSize = int.Parse(iconSizeTB.Text);
            }catch {}
        };
        anisTB.Text = settings.animationSpeed.ToString();
        anisTB.TextChanged += (a,e) => {
            try {
                settings.animationSpeed = double.Parse(anisTB.Text);
            }catch {}
        };
        switch (settings.dockPosition) {
            case "Bottom":
                dockpos.SelectedIndex = 0;
                break;
            case "Top":
                dockpos.SelectedIndex = 1;
                break;
            case "Right":
                dockpos.SelectedIndex = 2;
                break;
            case "Left":
                dockpos.SelectedIndex = 3;
                break;
        }
        dockpos.SelectionChanged += (e,a) => {
            var cbi = (ComboBoxItem)dockpos.SelectedItem;
            settings.dockPosition = cbi.Content.ToString();
        };
        switch (settings.groupRunningApps) {
            case "Executable":
                ruapps.SelectedIndex = 0;
                break;
            case "Instance":
                ruapps.SelectedIndex = 1;
                break;
            default:
                ruapps.SelectedIndex = 2;
                break;
        }
        ruapps.SelectionChanged += (e,a) => {
            switch (ruapps.SelectedIndex) {
                case 0:
                    settings.groupRunningApps = "Executable";
                    break;
                case 1:
                    settings.groupRunningApps = "Instance";
                    break;
                case 2:
                    settings.groupRunningApps = "Off";
                    break;
            }
        };
        dockcolorTB.Text = settings.dockColor;
        dockcolorTB.TextChanged += (a,e) => {
            settings.dockColor = dockcolorTB.Text;
        };
        dockradiusTB.Text = settings.dockBorderRadius;
        dockradiusTB.TextChanged += (a,e) => {
            settings.dockBorderRadius = dockradiusTB.Text;
        };
        sepcolorTB.Text = settings.separatorColor;
        sepcolorTB.TextChanged += (a,e) => {
            settings.separatorColor = sepcolorTB.Text;
        };
        tickinvTB.Text = settings.tickerInterval.ToString();
        tickinvTB.TextChanged += (a,e) => {
            try {
                settings.tickerInterval = int.Parse(tickinvTB.Text);
            }catch {}
        };
        actcolorTB.Text = settings.activeAppColor;
        actcolorTB.TextChanged += (a,e) => {
            settings.activeAppColor = actcolorTB.Text;
        };
        topmostCB.IsChecked = settings.topmost;
        topmostCB.Checked += (e,a) => {settings.topmost = true;};
        topmostCB.Unchecked += (e,a) => {settings.topmost = false;};
        dtxTB.Text = settings.docktransformX.ToString();
        dtxTB.TextChanged += (a,e) => {
            try {
                settings.docktransformX = double.Parse(dtxTB.Text);
            }catch {}
        };
        dtyTB.Text = settings.docktransformY.ToString();
        dtyTB.TextChanged += (a,e) => {
            try {
                settings.docktransformY = double.Parse(dtyTB.Text);
            }catch {}
        };
        switch (settings.autohide) {
            case "Smart":
                auhid.SelectedIndex = 0;
                break;
            case "On":
                auhid.SelectedIndex = 1;
                break;
            default:
                auhid.SelectedIndex = 2;
                break;
        }
        auhid.SelectionChanged += (e,a) => {
            switch (auhid.SelectedIndex) {
                case 0:
                    settings.autohide = "Smart";
                    break;
                case 1:
                    settings.autohide = "On";
                    break;
                case 2:
                    settings.autohide = "Off";
                    break;
            }
        };
        adbb.IsChecked = settings.enableAppsDrawerBlur;
        adbb.Checked += (e,a) => {settings.enableAppsDrawerBlur = true;};
        adbb.Unchecked += (e,a) => {settings.enableAppsDrawerBlur = false;};
        dbs.Text = settings.dockButtonStyleToUse;
        dbs.TextChanged += (a,e) => {
            settings.dockButtonStyleToUse = dbs.Text;
        };
        dbsub.Text = settings.submenuButtonStyleToUse;
        dbsub.TextChanged += (a,e) => {
            settings.submenuButtonStyleToUse = dbsub.Text;
        };
        uis.IsChecked = settings.useIconsInSubmenus;
        uis.Checked += (e,a) => {settings.useIconsInSubmenus = true;};
        uis.Unchecked += (e,a) => {settings.useIconsInSubmenus = false;};
        adwt.IsChecked = settings.appsDrawerTheme == "Light";
        adwt.Checked += (e,a) => {settings.appsDrawerTheme = "Light";};
        adwt.Unchecked += (e,a) => {settings.appsDrawerTheme = "Dark";};
        udat.IsChecked = settings.registerAsAppBar;
        udat.Checked += (e,a) => {settings.registerAsAppBar = true;};
        udat.Unchecked += (e,a) => {settings.registerAsAppBar = false;};
    }
}