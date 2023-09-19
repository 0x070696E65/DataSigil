using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using DataSigil.Handlers;
using DataSigil.Models;
using DataSigil.Services;
using DataSigil.Views;
using DataSigil.Views.Actions;

namespace DataSigil.ViewModels;

public class RegisterDataViewModel
{
    private static SymbolService SymbolService => App.ServiceProvider.GetRequiredService<SymbolService>();
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();

    public static bool IsExistTitle(string title)
    {
        return DataBaseServices.RegisterdDataModels.ToList().Exists(d => d.title == title);
    }

    public static string GetJsonFromDatas(string title, ObservableCollection<RegisterDataFieldItem> FieldItems, bool isEncrypt)
    {
        var jsonStr = $"{{\n" +
                      $"\t\"DataSigil\": {{\n" +
                      $"\t\t\"title\": \"{title}\",\n" + 
                      $"\t\t\"masterPublicKey\": \"{DataBaseServices.Account.MasterPublicKey}\",\n" + 
                      $"\t\t\"data\": [\n";
        var _isEncrypto = isEncrypt ? "true" : "false";
        
        foreach (var d in FieldItems)
        {
            var t = d.DataType == "英数字のみ" ? 0 : 1;
            jsonStr += $"\t\t\t[\"{d.DataTitle}\", {t}, {d.DataSize}],\n";
        }
        
        if (FieldItems.Count > 0)
        {
            jsonStr = jsonStr.Remove(jsonStr.Length - 2);
            jsonStr += "\n";
        }

        jsonStr += "\t\t],\n" +
                   $"\t\t\"isEncrypt\": {_isEncrypto}\n" +
                   "\t}\n"
                   + "}";
        return jsonStr;
    }

    public static async Task<string> AnnouceNewRegisterData(string json)
    {
        var rentalFee = ulong.Parse(await GetMosaicRentalFee());
        var (tx, mosaicId) = SymbolService.CreateRegistDataTransaction(json, rentalFee);
        var _hash = "";
        async Task Func()
        {
            var (hash,_) = await SymbolService.AnnounceWithSignature(tx);
            _hash = hash;
            SymbolService.FuncList.Add(
                $"RegisteredNewTable_{hash}", 
                async (_tx) =>
                {
                    if ((string) _tx["meta"]?["hash"] == hash)
                    {
                        var toast = Toast.Make("Confirmed Transaction");
                        await toast.Show(new CancellationTokenSource().Token);
                        await SymbolService.GetRegisterdDatasFromApi(mosaicId);
                        SymbolService.DeleteFunc($"RegisteredNewTable_{hash}");
                    }
                });
            SymbolService.FuncListForMaster.Add(
                $"InputNewData_{mosaicId}",
                async (_tx) =>
                {
                    Console.WriteLine($"OWNER_MOSAIC: {mosaicId}");
                    if ((string) _tx["transaction"]?["mosaics"]?[0]?["id"] == mosaicId)
                    {
                        var toast = Toast.Make($"Added New Data: {mosaicId}");
                        await toast.Show(new CancellationTokenSource().Token);
                        await Task.Delay(2000);
                        await InputDatasViewModel.SetInputDataWithPageNation(mosaicId);
                    }
                });
            Console.WriteLine(@"hash: "+hash);
            await Toast.Make("Announced Transaction: " + hash).Show(new CancellationTokenSource().Token);
        }
        var result = await PopupAction.DisplayPopup(new ConfirmTransactionFee(tx, "データ入力", Func,rentalFee));
        if (result != "OK") return result;
        await Task.Delay(1000);
        await SymbolService.CheckTransactionStatus(_hash);
        return result;
    }

    public static async Task<string> GetMosaicRentalFee()
    {
        return await SymbolService.GetMosaicRentalFee();
    }
    
    private int ValidateDatas(List<RegisterDataFieldItem> FieldItems)
    {
        var size = 0;
        foreach (var fieldItem in FieldItems)
        {
            if (fieldItem.DataType == "英数字のみ")
            {
                size += int.Parse(fieldItem.DataSize) * 2;
            }
            else
            {
                size += int.Parse(fieldItem.DataSize) * 3;
            }
        }
        return size;
    }
}