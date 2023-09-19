using System.Globalization;
using CommunityToolkit.Mvvm.Messaging;
using DataSigil.Messages;
using DataSigil.Resources.Translations;

namespace DataSigil.AppSettings;

public class AppSettings
{
    private readonly AppTheme currentTheme;
    
    private bool enableRTL = false;

    private bool isDarkTheme = false;

    private int selectedPrimaryColor;

    private ThemePalette selectedPrimaryColorItem;
    public LocalizationResourceManager LocalizationResourceManager { get; }

    private bool isGridView;

    private string menuBackground;

    static AppSettings()
    {
        Instance = new AppSettings();
    }

    private AppSettings()
    {
        LocalizationResourceManager = LocalizationResourceManager.Instance;
        this.IsGridView = true;
        this.currentTheme = Application.Current.RequestedTheme;
        this.selectedPrimaryColor = this.currentTheme == AppTheme.Light ? 0 : 1;  //ThemeUtil: ApplyColorSet1 by default for LightTheme, ApplyColorSet2 by default for DarkTheme
    }

    public static AppSettings Instance { get; }

    public bool IsSafeAreaEnabled { get; set; }

    public double SafeAreaHeight { get; set; }

    public static bool IsFirstLaunching
    {
        get => Preferences.Get(nameof(IsFirstLaunching), true);
        set => Preferences.Set(nameof(IsFirstLaunching), value);
    }

    /// <summary>
    /// Change Layout direction 
    /// EnableRTL value = true => load Arabic resource;
    /// EnableRTL value = false => load current culture resource;  
    /// Send culture changed message;
    /// </summary>
    public bool EnableRTL
    {
        get => this.enableRTL;
        set
        {
            if (value == this.enableRTL)
            {
                return;
            }

            this.enableRTL = value;
            Application.Current.MainPage.FlowDirection =
                this.enableRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            //CultureInfo ci = this.EnableRTL ? new CultureInfo("ar-AE") : Thread.CurrentThread.CurrentCulture;
            //LocalizationResourceManager.Instance.SetCulture(ci);
            //WeakReferenceMessenger.Default.Send(new CultureChangeMessage("ar-AE"));
        }
    }

    public bool IsDarkTheme
    {
        get => this.isDarkTheme;
        set
        {
            if (this.isDarkTheme == value)
            {
                return;
            }

            this.isDarkTheme = value;
            if (this.isDarkTheme)
            {
                // Dark Theme
                Application.Current.Resources.ApplyDarkTheme();
            }
            else
            {
                // Light Theme
                Application.Current.Resources.ApplyLightTheme();
            }
        }
    }

    public bool IsGridView
    {
        get => this.isGridView;
        set
        {
            if (this.isGridView == value)
            {
                return;
            }

            this.isGridView = value;
        }
    }

    public string MenuBackground
    {
        get => this.menuBackground;
        set
        {
            if (this.menuBackground == value)
            {
                return;
            }
            this.menuBackground = value;
        }
    }

    public ThemePalette SelectedPrimaryColorItem 
    {
        get => this.selectedPrimaryColorItem;
        set
        {
            if (this.selectedPrimaryColorItem == value)
            {
                return;
            }

            this.selectedPrimaryColorItem = value;
        }
    }

    public int SelectedPrimaryColor
    {
        get => this.selectedPrimaryColor;
        set
        {
            if (this.selectedPrimaryColor == value)
            {
                return;
            }

            this.selectedPrimaryColor = value;
            ThemeUtil.ApplyColorSet(this.selectedPrimaryColor);
        }
    }
}

public class ThemePalette
{
    public int Index { get; set; }
    public Color Color { get; set; }
}
