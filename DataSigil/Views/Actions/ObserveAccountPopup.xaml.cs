
using System.Windows.Input;
using DataSigil.Controls;

namespace DataSigil.Views.Actions;

public partial class ObserveAccountPopup : BasePopupPage
{
	public ObserveAccountPopup(string address)
	{
		InitializeComponent();
		Account.Text = address;

		StartBlinkingAnimation();
	}
    
	private async void StartBlinkingAnimation()
	{
		while (true)
		{
			// 徐々にOpacityを下げる
			for (var opacity = 1.0; opacity >= 0.0; opacity -= 0.1)
			{
				blinkingLabel.Opacity = opacity;
				await Task.Delay(100); // 100ms待機
			}

			// 徐々にOpacityを上げる
			for (var opacity = 0.0; opacity <= 1.0; opacity += 0.1)
			{
				blinkingLabel.Opacity = opacity;
				await Task.Delay(100); // 100ms待機
			}
		}
	}
	
	public new ICommand PopModelCommand => new Command(async () =>
	{
		//if (IsCloseOnBackgroundClick)
			//await PopupAction.ClosePopup();
	});

    private async void GoBack_Clicked(object sender, EventArgs e)
    {
        await PopupAction.ClosePopup();
    }
}