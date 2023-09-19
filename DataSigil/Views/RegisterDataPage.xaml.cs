using System.Collections.ObjectModel;
using DataSigil.Models;
using DataSigil.ViewModels;

namespace DataSigil.Views;

public partial class RegisterDataPage : ContentPage
{
    private const int cellHeight = 65;
    private readonly ObservableCollection<RegisterDataFieldItem> FieldItems = new ObservableCollection<RegisterDataFieldItem>();

    public RegisterDataPage()
    {
        InitializeComponent();
        itemListView.ItemsSource = FieldItems;
        var newItem = new RegisterDataFieldItem { DataTitle = "", DataType = "英数字のみ", DataSize = "10"};
        FieldItems.Add(newItem);
    }
    
    private async void FixDataButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (RegisterDataViewModel.IsExistTitle(Title.Text))
            {
                throw new Exception("同じタイトルがすでに存在します");
            }
            //SizeCheck();
            var json = RegisterDataViewModel.GetJsonFromDatas(Title.Text, FieldItems, IsEncrypt.IsChecked);
            var result = await RegisterDataViewModel.AnnouceNewRegisterData(json);
            if (result == "OK")
            {
                Title.Text = "";
                FieldItems.Clear();
                var newItem = new RegisterDataFieldItem { DataTitle = "", DataType = "英数字のみ", DataSize = "10"};
                FieldItems.Add(newItem);
            }
        }
        catch (Exception error)
        {
            await DisplayAlert("Error", error.Message, "OK");
        }
    }
    
    private void AddColumnButtonClicked(object sender, EventArgs e)
    {
        var newItem = new RegisterDataFieldItem { DataTitle = "", DataType = "英数字のみ", DataSize = "10"};
        FieldItems.Add(newItem);
        itemListView.SelectedItem = newItem;
        MainTable.HeightRequest += cellHeight;
    }
    
    private void DataTypePickerSelectedIndexChanged(object sender, EventArgs e)
    {
        var picker = (Picker)sender;
        var selectedItem = (string)picker.SelectedItem;
        var f = (RegisterDataFieldItem)picker.BindingContext;
        f.DataType = selectedItem;
    }
    
    private void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button {CommandParameter: RegisterDataFieldItem fieldItem})
        {
            FieldItems.Remove(fieldItem);
            MainTable.HeightRequest -= cellHeight;
        }
    }
    
    private void OnNumberEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.NewTextValue)) return;
        var newText = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        if (newText == e.NewTextValue) return;
        var entry = (Entry)sender;
        var fieldItem = (RegisterDataFieldItem)entry.BindingContext;
        fieldItem.DataSize = newText;
    }

    /*private void SizeCheck()
    {
    }*/
}
