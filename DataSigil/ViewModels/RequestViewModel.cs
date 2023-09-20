using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using DataSigil.Models;
using DataSigil.Scripts;
using DataSigil.Services;
using DataSigil.Views.Actions;

namespace DataSigil.ViewModels;

public class RequestViewModel
{
    public static SymbolService SymbolService => App.ServiceProvider.GetRequiredService<SymbolService>();
    public static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();

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

    public static void SetChangedPartialDatasFunction(Action func)
    {
        DataBaseServices.PartialModels.CollectionChanged += (sender, e) =>
        {
            Console.WriteLine("PartialModels Changed");
            func?.Invoke();
        };
    }
    
    public static async Task<string> Request(string mosaicID, INavigation Navigation, Action action)
    {
        var applyDataTransaction = await SymbolService.CreateApplyDataTransaction(mosaicID);
        var aggregateHashAndpayload = await SymbolService.SignByAdmin(applyDataTransaction);
        var hashTransaction = SymbolService.CreateHashLockTransaction(aggregateHashAndpayload.hash);
        var _hash = "";
        async Task Func()
        {
            var (hash, _) = await SymbolService.AnnounceWithSignature(hashTransaction);
            _hash = hash;
            var closeTask = Navigation.PopModalAsync();
            var announceTask = Toast.Make("Announced HashLock Transaction: " + hash).Show(new CancellationTokenSource().Token);
            Console.WriteLine("Waiting HashLock Transaction: " + hash);
            var websocketTask = ListenerService.SetupWebsocket(SymbolService.Node, DataBaseServices.Account.Address, hash, async () =>
            {
                var toast = Toast.Make("Confirmed HashLock Transaction");
                await toast.Show(new CancellationTokenSource().Token);
                
                await Task.Delay(3000);
                var (hashAggregateBonded, _) = await SymbolService.AnnounceWithSignatureBonded(applyDataTransaction);
                var announceBondedTask = Toast.Make("Announced AggregateBonded Transaction: " + hashAggregateBonded).Show(new CancellationTokenSource().Token);
                Console.WriteLine("Waiting AggregateBonded Transaction: " + hashAggregateBonded);
                var websocketBondedTask = ListenerService.SetupWebsocket(SymbolService.Node, DataBaseServices.Account.Address, hashAggregateBonded, async () =>
                {
                    var toastBonded = Toast.Make("Confirmed AggregateBonded Transaction");
                    await toastBonded.Show(new CancellationTokenSource().Token);
                });
                
                Console.WriteLine(DataBaseServices.Account.Address);
                var partialRemoved = ListenerService.SetupWebsocketPartialRemoved(SymbolService.Node, DataBaseServices.Account.Address, hashAggregateBonded, async () =>
                {
                    var toastPartialRemoved = Toast.Make("Confirmed PartialRemoved Transaction");
                    await toastPartialRemoved.Show(new CancellationTokenSource().Token);
                    await SymbolService.GetRegisterdDatasFromApi(mosaicID);
                });
                await Task.WhenAll(closeTask, announceBondedTask, websocketBondedTask, partialRemoved);
            });
            await Task.WhenAll(closeTask, announceTask, websocketTask);
            action?.Invoke();
        }
        var result = await PopupAction.DisplayPopup(new ConfirmTransactionFee(hashTransaction, "申請", Func, applyDataTransaction.Fee.Value + 10000000));
        if (result != "OK") return result;
        await Task.Delay(1000);
        await SymbolService.CheckTransactionStatus(_hash);
        return result;
    }

    public async static Task Approve(string hash)
    {
        await SymbolService.AnnounceWithSignCosignature(hash);
        var h = DataBaseServices.PartialModels.ToList().Find(p => p.Hash == hash);
        DataBaseServices.PartialModels.Remove(h);
        await Toast.Make("Announced Cosignature").Show(new CancellationTokenSource().Token);
    }
}