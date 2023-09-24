namespace DataSigil.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(string text = "Loading...")
    {
        InitializeComponent();
        LoadingText.Text = text;
    }
}