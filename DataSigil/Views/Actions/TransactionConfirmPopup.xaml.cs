using System.Windows.Input;
using DataSigil.Controls;

namespace DataSigil.Views.Actions;

public partial class TransactionConfirmPopup : ContentView
{
    public static readonly BindableProperty ColorProperty =
        BindableProperty.Create(nameof(Color), typeof(Color), typeof(TransactionConfirmPopup), new Color(0, 0, 0));

    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TransactionConfirmPopup), string.Empty);

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TransactionConfirmPopup()
    {
        InitializeComponent();
    }
    
    private async void OnClose(object sender, EventArgs e)
    {
        await PopupAction.ClosePopup();
    }
}