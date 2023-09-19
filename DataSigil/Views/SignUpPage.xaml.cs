using DataSigil.ViewModels;
using DataSigil.Views.Actions;
namespace DataSigil.Views;

public partial class SignUpPage : ContentPage
{
    public SignUpPage()
    {
        InitializeComponent();
    }
    
    private async void CreateAccountButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            await AccountViewModel.CreateAccount(UserName.Text, Password.Text, Navigation, async () => await PopupAction.DisplayPopup(new ObserveAccountPopup(AccountViewModel.Account.Address)));   
        } catch (Exception error)
        {
            Console.WriteLine(error.Message);
            Console.WriteLine(@$"StackTrace: {error.StackTrace}");
            await DisplayAlert("Error", error.Message, "OK");
            throw new Exception(error.Message);
        }
    }
    
    private void ToLoginPage(object sender, EventArgs e)
    {
        if (Application.Current != null) Application.Current.MainPage = new NavigationPage(new LoginPage());
    }
}