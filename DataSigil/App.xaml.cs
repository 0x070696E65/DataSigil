using System.Globalization;
using CatSdk.Symbol;
using DataSigil.Handlers;
using DataSigil.Models;
using DataSigil.Resources.Translations;
using DataSigil.Services;
using DataSigil.ViewModels;
using DataSigil.Views;
using NETWORK = CatSdk.Symbol.Network;
namespace DataSigil;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }
    public App()
    {
        InitializeComponent();
        ServiceProvider = ConfigureServices();
        
        LocalizationResourceManager.Instance.SetCulture(new CultureInfo("ja-JP"));
        
        var SymbolService = ServiceProvider.GetRequiredService<SymbolService>();
        SymbolService.Init("https://mikun-testnet.tk:3001", NetworkType.TESTNET);
        
        if (Current != null) Current.UserAppTheme = AppTheme.Light;
        
        #region Handlers

        //Borderless entry
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntry), (handler, view) =>
        {
            if (view is BorderlessEntry)
            {
#if __ANDROID__
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif __IOS__
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif __WINDOWS__
                handler.PlatformView.FontWeight = Microsoft.UI.Text.FontWeights.Thin;
                handler.PlatformView.TextBox.BorderThickness = new Thickness(0);
#endif
            }
        });

        //Borderless editor
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEditor), (handler, view) =>
        {
            if (view is BorderlessEditor)
            {
#if __ANDROID__
                handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#elif __IOS__
                    handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif __WINDOWS__
                handler.PlatformView.FontWeight = Microsoft.UI.Text.FontWeights.Thin;
                handler.PlatformView.BorderThickness = new Thickness(0);
#endif
            }
        });

        #endregion
        
        if (AppSettings.AppSettings.IsFirstLaunching)
        {
            AppSettings.AppSettings.IsFirstLaunching = false;
            MainPage = new NavigationPage(new SignUpPage());
        }
        else
        {
            //AppSettings.AppSettings.IsFirstLaunching = false;
            MainPage = new NavigationPage(new LoginPage());
        }
    }
    
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DataBaseServices>();
        services.AddSingleton<AccountModel>();
        services.AddSingleton<AccountViewModel>();
        services.AddSingleton<InputDatasViewModel>();
        services.AddSingleton<SymbolService>();
        return services.BuildServiceProvider();
    }
}