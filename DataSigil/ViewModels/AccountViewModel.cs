using System.Collections.ObjectModel;
using System.Net.WebSockets;
using CatSdk.Crypto;
using CatSdk.CryptoTypes;
using CatSdk.Symbol;
using CatSdk.Utils;
using CommunityToolkit.Maui.Alerts;
using DataSigil.Models;
using DataSigil.Scripts;
using DataSigil.Services;
using DataSigil.Views;
using DataSigil.Views.Actions;
using Newtonsoft.Json;

namespace DataSigil.ViewModels;

public class AccountViewModel
{
    private static SymbolService SymbolService => App.ServiceProvider.GetRequiredService<SymbolService>();
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();
    
    public static AccountModel Account
    {
        get => DataBaseServices.Account;
        set
        {
            if (DataBaseServices.Account != value)
            {
                DataBaseServices.Account = value;
            }
        }
    }
    
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
    
    public static ObservableCollection<PartialModel> PartialModels
    {
        get => DataBaseServices.PartialModels;
        set
        {
            if (DataBaseServices.PartialModels != value)
            {
                DataBaseServices.PartialModels = value;
            }
        }
    }
    
    public static async Task CreateAccount(string userName, string password, INavigation Navigation, Func<Task<string>> func = null)
    {
        var privateKey = PrivateKey.Random();
        var keyPair = new KeyPair(privateKey);
        var publicKey = keyPair.PublicKey;
        var address = SymbolService.Facade.Network.PublicKeyToAddress(publicKey);

        Account.Username = userName;
        Account.PrivateKey = Converter.BytesToHex(privateKey.bytes);
        Account.PublicKey = Converter.BytesToHex(publicKey.bytes);
        Account.Address = address.ToString();
        
        var masterPrivateKey = PrivateKey.Random();
        var masterKeyPair = new KeyPair(masterPrivateKey);
        var masterPublicKey = masterKeyPair.PublicKey;
        var masterAddress = SymbolService.Facade.Network.PublicKeyToAddress(masterPublicKey);

        Account.MasterPrivateKey = Converter.BytesToHex(masterPrivateKey.bytes);
        Account.MasterPublicKey = Converter.BytesToHex(masterPublicKey.bytes);
        Account.MasterAddress = masterAddress.ToString();
 
        var encryptedPrivateKey = Crypto.EncryptString(Account.PrivateKey, password, Account.Address);
        await SecureStorage.SetAsync("EncryptedPrivateKey", encryptedPrivateKey);
        await SecureStorage.SetAsync("Username", Account.Username);
        await SecureStorage.SetAsync("Address", Account.Address);
            
        var encryptedMasterPrivateKey = Crypto.EncryptString(Account.MasterPrivateKey, password, Account.MasterAddress);
        await SecureStorage.SetAsync("EncryptedMasterPrivateKey", encryptedMasterPrivateKey);
        await SecureStorage.SetAsync("MasterAddress", Account.MasterAddress);
        
        Console.WriteLine(@"PrivateKey: " + Account.PrivateKey);

        func?.Invoke();
        SetWebsockets();
        
        var res = await ObserveAccountOverAmount(1000000);
        if (res)
        {
            await SymbolService.GetAccountInfo(Account.PublicKey);
            SymbolService.SetXymAmount();
            
            var aggregateTransaction = SymbolService.CreateAccountTransaction();
            await Navigation.PopModalAsync();
            
            async Task Func()
            {
                var (hash, _) = await SymbolService.AnnounceForMultisig(aggregateTransaction);

                await Navigation.PopModalAsync();
                var loadingPage = new LoadingPage("トランザクションを送信中...");
                await Navigation.PushModalAsync(loadingPage);

                var announceTask = Toast.Make("Announced Transaction: " + hash).Show(new CancellationTokenSource().Token);
                var websocketTask = ListenerService.SetupWebsocket(SymbolService.Node, Account.Address, hash, async () =>
                {
                    var toast = Toast.Make("Confirmed Transaction");
                    await toast.Show(new CancellationTokenSource().Token);
                });
                
                await Task.WhenAll(announceTask, websocketTask);
                await Navigation.PopModalAsync();
                if (Application.Current != null) Application.Current.MainPage = new AppShell();
            }
            await PopupAction.DisplayPopup(new ConfirmTransactionFee(aggregateTransaction, "アカウント作成", Func));
        }
    }

    public static async Task<bool> ReadAccount(string password)
    {
        try
        {
            var encryptedPrivateKey = await SecureStorage.GetAsync("EncryptedPrivateKey");
            var address = await SecureStorage.GetAsync("Address");
            var userName = await SecureStorage.GetAsync("Username");
            var privateKey = Crypto.DecryptString(encryptedPrivateKey, password, address);
            var publicKey = new KeyPair(new PrivateKey(privateKey)).PublicKey.ToString();
            
            Account.Username = userName;
            Account.PrivateKey = privateKey;
            Account.PublicKey = publicKey;
            Account.Address = address;
            
            var encryptedMasterPrivateKey = await SecureStorage.GetAsync("EncryptedMasterPrivateKey");
            var masterAddress = await SecureStorage.GetAsync("MasterAddress");
            var masterPrivateKey = Crypto.DecryptString(encryptedMasterPrivateKey, password, masterAddress);

            Account.MasterPrivateKey = masterPrivateKey;
            var masterPublicKey = new KeyPair(new PrivateKey(masterPrivateKey)).PublicKey.ToString();
            Account.MasterPublicKey = masterPublicKey;
            Account.MasterAddress = masterAddress;
            
            Console.WriteLine(JsonConvert.SerializeObject(Account));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return false;
        }
    }

    public async static Task SetupLogined()
    {
        await SymbolService.GetRegisterdDatasFromApi();
        await SymbolService.GetInputAllDatas();
        await SymbolService.GetApplyedPartialTransactions();
        SymbolService.SetXymAmount();
        SetWebsockets();
    }

    private static void SetWebsockets()
    {
        Console.WriteLine(@"SetWebsockets");
        SymbolService.FuncList.Add(
            "SetXymAmount", 
            async (tx) =>
            {
                await Task.Delay(1000);
                Console.WriteLine(@"SetXymAmount");
                await SymbolService.GetAccountInfo(Account.PublicKey);
                SymbolService.SetXymAmount();
            });
        
        SymbolService.SetupWebSocketConfirmed();
        foreach (var mosaicId in from viewModelRegisterdDataModel in RegisterdDataModels where viewModelRegisterdDataModel.isOwner select viewModelRegisterdDataModel.mosaicId)
        {
            Console.WriteLine(mosaicId);
            SymbolService.FuncListForMaster.Add(
                $"InputNewData_{mosaicId}",
                async (_tx) =>
                {
                    if((string)_tx["transaction"]?["signerPublicKey"] == Account.PublicKey) return;
                    Console.WriteLine($"OWNER_MOSAIC: {mosaicId}");
                    if ((string) _tx["transaction"]?["mosaics"]?[0]?["id"] == mosaicId)
                    {
                        var toast = Toast.Make($"Added New Data: {mosaicId}");
                        await toast.Show(new CancellationTokenSource().Token);
                        await InputDatasViewModel.SetInputDataWithPageNation(mosaicId);
                    }
                });
        }
        SymbolService.SetupWebSocketConfirmedForMaster();

        SymbolService.BondedFuncList.Add(
            "AddedBonded", 
            async (tx)=>
            {
                var hasNewPartial = await SymbolService.GetApplyedPartialTransaction((string)tx["meta"]?["hash"]);
                if (hasNewPartial)
                {
                    await Toast.Make("New approval request received.").Show(new CancellationTokenSource().Token);   
                }
            });
        SymbolService.SetupWebSocketPartial();
    }
    
    public static async Task<bool> ObserveAccountOverAmount(ulong amount)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var websocket = new ListenerService(SymbolService.Node, new ClientWebSocket());
        await websocket.Open();
        Console.WriteLine(Account.Address);
        Task.Run(async () =>
        {
            await websocket.Confirmed(Account.Address, async (tx) =>
            {
                var accApi = new SymbolRestClient.Api.AccountRoutesApi(SymbolService.Node);
                var acc = await accApi.GetAccountInfoAsync(Account.Address);
                var m = acc.Account.Mosaics.Find(mosaic => mosaic.Id == SymbolService.CurrentNetoworkMosaicId);
                if (ulong.Parse(m.Amount) < amount) return;
                websocket.Close();
                tcs.SetResult(true);
                await Task.Yield();
            });
        });
        return await tcs.Task;
    }
    
    public static void SetChangedRegisterdDataModelsFunction(Func<Task> func)
    {
        DataBaseServices.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(RegisterdDataModels))
            {
                func?.Invoke();
            }
        };
    }
}