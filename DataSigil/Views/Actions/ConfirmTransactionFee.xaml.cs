using System.Windows.Input;
using CatSdk.Symbol;
using DataSigil.Controls;
using DataSigil.Models;
using DataSigil.Services;
using DataSigil.ViewModels;

namespace DataSigil.Views.Actions;

public partial class ConfirmTransactionFee : BasePopupPage
{
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();
    private static AccountViewModel accountViewModel => App.ServiceProvider.GetRequiredService<AccountViewModel>();
    private readonly Func<Task> function;
    public ConfirmTransactionFee(ITransaction transaction, string name, Func<Task> func = null, ulong addtionalFee = 0)
    {
        InitializeComponent();
        var totalFee = transaction.Fee.Value + addtionalFee;
        TransactionFee.Text = (double)totalFee / 1000000 + "XYM";
        TransactionName.Text = name;
        function = func;
        if (DataBaseServices.Account.XymAmount < totalFee)
        {
            ExcuteTransactionButton.IsEnabled = false;
            TransactionFee.Text += "\n 残高不足です";
        }
    }

    private async void OnClickTransactionAnnounce(object sender, EventArgs e)
    {
        function?.Invoke();
        returnResultTask.SetResult("OK");
        await PopupAction.ClosePopup();
    }
}
