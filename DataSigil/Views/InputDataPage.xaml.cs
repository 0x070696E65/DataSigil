using System.Collections.ObjectModel;
using DataSigil.Models;
using DataSigil.ViewModels;

namespace DataSigil.Views;

public partial class InputDataPage : ContentPage
{
    private readonly ObservableCollection<InputDataFieldItem> InputDataFieldItems = new ObservableCollection<InputDataFieldItem>();
    private RegisterdDataModel inputData;

    public InputDataPage()
    {
        InitializeComponent();
        inputDataItemListView.ItemsSource = InputDataFieldItems;
        Init();
        InputDatasViewModel.SetChangedRegisterdDataModelsFunction(Init);
    }
    
    private void Init()
    {
        var mosaicIds = InputDatasViewModel.GetMosaicIds();
        if(mosaicIds.Count == 0)
        {
            return;
        }
        SelectMosaicId.ItemsSource = mosaicIds.Keys.ToList();
        SelectMosaicId.SelectedIndex = 0;
    }
    
    private async void OnInputNewData(object sender, EventArgs e)
    {
        try
        {
            var result = await InputDatasViewModel.AnnounceInputData(inputData.title, inputData.mosaicId, inputData.isEncrypt, InputDataFieldItems, inputData.masterPublicKey, Navigation);
            if (result == "OK")
            {
                OnMosaicIdChanged(SelectMosaicId, EventArgs.Empty);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "OK");
        }
    }
    
    private void OnMosaicIdChanged(object sender, EventArgs e)
    {
        InputDataFieldItems.Clear();
        var index = SelectMosaicId.SelectedIndex == -1 ? 0 :SelectMosaicId.SelectedIndex ;
        var selectedTitle = SelectMosaicId.ItemsSource[index] as string;
        inputData = InputDatasViewModel.GetRegisterdDataModelByTitle(selectedTitle);
        foreach (var newItem in inputData.data.Select(d => new InputDataFieldItem { DataTitle = d[0].ToString(), DataContent = "", DataMaxSize = int.Parse(d[2].ToString() ?? string.Empty)}))
        {
            InputDataFieldItems.Add(newItem);
        }
    }
}

public class InputDataFieldItem
{
    public string DataTitle { get; set; }
    public string DataContent { get; set; }
    public int DataMaxSize { get; set; }
}
