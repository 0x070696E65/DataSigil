using DataSigil.Controls;

namespace DataSigil.Views.Actions;

public partial class ActionWarningPopup : BasePopupPage
{
    public ActionWarningPopup(string text)
    {
        InitializeComponent();
        WarningText.Text = text;
    }

    private async void GoBack_Clicked(object sender, EventArgs e)
    {
        await PopupAction.ClosePopup();
    }
}