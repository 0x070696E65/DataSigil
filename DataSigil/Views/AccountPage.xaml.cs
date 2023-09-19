using DataSigil.Handlers;
using DataSigil.Services;
using DataSigil.ViewModels;

namespace DataSigil.Views;

public partial class AccountPage : ContentPage
{
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();
    public AccountPage()
    {
        InitializeComponent();
        BindingContext = DataBaseServices;
        PublicKey.Text = DataBaseServices.Account.PublicKey;
        Address.Text = DataBaseServices.Account.Address;
        UserName.Text = DataBaseServices.Account.Username;
        AccountViewModel.SetChangedRegisterdDataModelsFunction(SetMosaicId);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();
        await SetMosaicId();
    }

    private async Task SetMosaicId()
    {
        await Task.Delay(1000);
        MosaicPicker.ItemsSource = DataBaseServices.RegisterdDataModels.Select(m => m.title).ToList();
        MosaicPicker.SelectedIndex = 0;
    }

    private async void OnShowPrivateKey(object sender, EventArgs e)
    {
        var privateKeyEntry = this.FindByName<BorderlessEntry>("PrivateKey");
        privateKeyEntry.Text = DataBaseServices.Account.PrivateKey;
        await Task.Delay(10000);
        privateKeyEntry.Text = "******************************************************";
    }

    private void MosaicIdSelectChanged(object sender, EventArgs e)
    {
        var picker = (BorderlessPicker)sender;
        var selectedItem = picker.SelectedItem?.ToString();
        if (!string.IsNullOrEmpty(selectedItem))
        {
            var dataModel = DataBaseServices.RegisterdDataModels.ToList().Find(m => m.title == selectedItem);
            MosaicId.Text = dataModel?.mosaicId;
        }
    }
}