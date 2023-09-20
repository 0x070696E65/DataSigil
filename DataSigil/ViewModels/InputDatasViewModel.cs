using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using DataSigil.Extends;
using DataSigil.Handlers;
using DataSigil.Models;
using DataSigil.Services;
using DataSigil.Views;
using DataSigil.Views.Actions;
using Newtonsoft.Json;

namespace DataSigil.ViewModels;

public class InputDatasViewModel
{
    private static SymbolService SymbolService => App.ServiceProvider.GetRequiredService<SymbolService>();
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();

    public static readonly string PublicKey = DataBaseServices.Account.PublicKey;
    public static readonly string MasterPublicKey = DataBaseServices.Account.MasterPublicKey;
    
    public static ObservableCollection<RegisterdDataModel> RegisterdDataModels
    {
        get => DataBaseServices.RegisterdDataModels;
        set
        {
            if (DataBaseServices.RegisterdDataModels != value)
            {
                DataBaseServices.RegisterdDataModels = value;
            }
        }
    }
    
    public static ObservableDictionary<string, List<InputDataWithPageNation>> AllInputDataWithPageNation
    {
        get => DataBaseServices.AllInputDataWithPageNation;
        set
        {
            if (DataBaseServices.AllInputDataWithPageNation != value)
            {
                DataBaseServices.AllInputDataWithPageNation = value;
            }
        }
    }
    
    public static void SetChangedRegisterdDataModelsFunction(Action func)
    {
        DataBaseServices.RegisterdDataModels.CollectionChanged += (sender, e) =>
        {
            func?.Invoke();
        };
    }

    public static void SetChangedInputDatasFunction(Action func)
    {
        DataBaseServices.AllInputDataWithPageNation.CollectionChanged += (sender, e) =>
        {
            func?.Invoke();
        };
    }

    public static Dictionary<string, string> GetMosaicIds()
    {
        return RegisterdDataModels.ToDictionary(registerdDataModel => registerdDataModel.title, registerdDataModel => registerdDataModel.mosaicId);
    }
    
    public static RegisterdDataModel GetRegisterdDataModelByTitle(string title)
    {
        return RegisterdDataModels.ToList().Find(r => r.title == title);
    }
    
    public static bool GetIsEncryptByMosaicId(string mosaicId)
    {
        return RegisterdDataModels.ToList().Find(r => r.mosaicId == mosaicId).isEncrypt;
    }

    public static async Task<string> Decrypt(string encrypted, string publicKey, bool isMine)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var masterPrivateKeyStr = await SecureStorage.GetAsync("MasterPrivateKey");
        var priv = isMine ? privateKeyStr : masterPrivateKeyStr;
        return SymbolService.Decrypt(encrypted, priv, publicKey);
    }

    public static async Task<string> AnnounceInputData(string title, string mosaicId, bool isEncrypt, ObservableCollection<InputDataFieldItem> InputDataFieldItems, string masterPublicKey, INavigation Navigation)
    {
        var data = new Dictionary<string, object>()
            {
                {"Title", title},
                {"MosaicId", mosaicId},
                {"UserName", DataBaseServices.Account.Username}
            };
            
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var dic = isEncrypt ? 
                InputDataFieldItems.ToDictionary(inputDataFieldItem => inputDataFieldItem.DataTitle, inputDataFieldItem => SymbolService.Encrypt(inputDataFieldItem.DataContent, privateKeyStr, masterPublicKey)) : 
                InputDataFieldItems.ToDictionary(inputDataFieldItem => inputDataFieldItem.DataTitle, inputDataFieldItem => inputDataFieldItem.DataContent);
            data.Add("Items", dic);
            var tx = SymbolService.CreateInputDataTransaction(JsonConvert.SerializeObject(data), mosaicId, masterPublicKey, isEncrypt);

            var _hash = "";
            async Task Func()
            {
                var (hash,_) = await SymbolService.AnnounceWithSignature(tx);
                _hash = hash;
                SymbolService.FuncList.Add(
                    $"InputNewData_{hash}", 
                    async (_tx) =>
                    {
                        if ((string) _tx["meta"]?["hash"] == hash)
                        {
                            var toast = Toast.Make("Confirmed Transaction");
                            await toast.Show(new CancellationTokenSource().Token);
                            Console.WriteLine($@"Confirmed Transaction: {hash}");
                            await SetInputDataWithPageNation(mosaicId);
                            SymbolService.DeleteFunc($"InputNewData_{hash}");
                        }
                    });
                
                await Navigation.PopModalAsync();
                Console.WriteLine($"input new data hash: {hash}");
                await Toast.Make("Announced Transaction: " + hash).Show(new CancellationTokenSource().Token);

                foreach (var inputDataFieldItem in InputDataFieldItems)
                {
                    inputDataFieldItem.DataContent = "";
                }
            }
            var result = await PopupAction.DisplayPopup(new ConfirmTransactionFee(tx, "データ入力", Func));
            if (result != "OK") return result;
        
            await Task.Delay(1000);
            await SymbolService.CheckTransactionStatus(_hash);
            return result;
    }
    
    public static async Task SetInputDataWithPageNation(string mosaicId)
    {
        var d = RegisterdDataModels.ToList().Find(m => m.mosaicId == mosaicId);
        await SetInputDataWithPageNation((mosaicId, d.isOwner));
    }
    
    public static async Task SetInputDataWithPageNation((string, bool) mosaicId)
    {
        try
        {
            var list = new List<InputDataWithPageNation>();
            var count = 1;
            while (true)
            {
                var inputData = await SymbolService.GetInputData(mosaicId.Item1, mosaicId.Item2, 20, count);
                if (inputData == null) break;
                list.Add(inputData);
                count++;
            }
            DataBaseServices.AllInputDataWithPageNation[mosaicId.Item1] = list;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}
