using System.Collections.ObjectModel;
using DataSigil.ViewModels;

namespace DataSigil.Views;

public partial class RequestPage : ContentPage
{
    private ObservableCollection<PartialFieldItem> PartialFieldItems { get; } = new ObservableCollection<PartialFieldItem>();
    
    public RequestPage()
    {
        InitializeComponent();
        PartialFieldItemsView.ItemsSource = PartialFieldItems;
        Init();
        RequestViewModel.SetChangedPartialDatasFunction(Init);
    }
    
    private void Init()
    {
        PartialFieldItems.Clear();
        foreach (var item in 
                 RequestViewModel.PartialModels.Select(partialModel => new PartialFieldItem()
                 {
                     Hash = partialModel.Hash,
                     MosaicID = partialModel.MosaicID,
                     Address = partialModel.RecipientAddress
                 }))
        {
            PartialFieldItems.Add(item);
        }
    }

    private async void OnRequestButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (MosaicID.Text == "")
            {
                await DisplayAlert("Error", "MosaicIDが入力されていません", "OK");
                return;
            }

            var result = await RequestViewModel.Request(MosaicID.Text, Navigation, () => { MosaicID.Text = ""; });
            if (result == "OK")
            {
                MosaicID.Text = "";
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "OK");
        }
    }
    
    private async void OnApproveButtonClicked(object sender, EventArgs e)
    {
        try
        {
            var button = (Button) sender;
            var viewCell = (ViewCell) button.Parent.Parent;
            var item = (PartialFieldItem) viewCell.BindingContext;
            var hash = item.Hash;
            var isApprove = await DisplayAlert("確認", "承認しますか？", "OK", "NO");
            if (isApprove)
            {
                await RequestViewModel.Approve(hash);
            }
        }
        catch (Exception error)
        {
            Console.WriteLine(error.StackTrace);
            await DisplayAlert("Error", error.Message, "OK");
        }
    }
}

public class PartialFieldItem
{
    public string Hash { get; set; }
    public string MosaicID { get; set; }
    public string Address { get; set; }
}