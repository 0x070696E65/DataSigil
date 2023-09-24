using DataSigil.ViewModels;
namespace DataSigil.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }
    
    private async void ReadAccountButton_Clicked(object sender, EventArgs e)
    {
        var loadingPage = new LoadingPage("アカウントを読み込み中...");
        
        await Navigation.PushModalAsync(loadingPage);
        try
        {
            if (await AccountViewModel.ReadAccount(Password.Text))
            {
                await AccountViewModel.SetupLogined();
                if (Application.Current != null) Application.Current.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("Error", "パスワードが違います", "OK");
            }
        } 
        catch (Exception error)
        {
            Console.WriteLine(error.Message);
            await DisplayAlert("Error", error.Message, "OK");
            throw new Exception(error.Message);
        }
        await Navigation.PopModalAsync();
    }
    
    private void ToSignupPage(object sender, EventArgs e)
    {
        if (Application.Current != null) Application.Current.MainPage = new NavigationPage(new SignUpPage());
    }
}