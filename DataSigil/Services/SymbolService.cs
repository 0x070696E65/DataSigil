using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using CatSdk.Crypto;
using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;
using CatSdk.Symbol.Factory;
using CatSdk.Utils;
using DataSigil.Models;
using DataSigil.Scripts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SymbolRestClient.Model;
using Cosignature = CatSdk.Symbol.Cosignature;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NETWORK = CatSdk.Symbol.Network;
using UnresolvedMosaic = CatSdk.Symbol.UnresolvedMosaic;

namespace DataSigil.Services;

public class SymbolService
{
    private static DataBaseServices DataBaseServices => App.ServiceProvider.GetRequiredService<DataBaseServices>();

    public string Node { get; private set; }
    public SymbolFacade Facade { get; private set; }
    private NetworkType NetworkType { get; set; }

    public const string CurrentNetoworkMosaicId = "72C0212E67A08BCE";

    private readonly ulong Key = IdGenerator.GenerateUlongKey("DSIGIL");

    private static AccountModel Account
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
    
    private static ObservableCollection<RegisterdDataModel> RegisterdDataModels
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
    
    public void Init(string node, NetworkType networkType)
    {
        Node = node;
        NetworkType = networkType;
        Facade = new SymbolFacade(NetworkType == NetworkType.TESTNET ? NETWORK.TestNet : NETWORK.MainNet);
        Account = DataBaseServices.Account;
        RegisterdDataModels = DataBaseServices.RegisterdDataModels;
    }
    
    public async Task<AggregateCompleteTransactionV2> CreateAccountTransaction()
    {
        var privateKey = await SecureStorage.GetAsync("PrivateKey");
        var masterPrivateKey = await SecureStorage.GetAsync("MasterPrivateKey");
        
        var masterKeyPair = new KeyPair(new PrivateKey(masterPrivateKey));
        var userKeyPair = new KeyPair(new PrivateKey(privateKey));
        var userAddress = Facade.Network.PublicKeyToAddress(userKeyPair.PublicKey);
        
        var multisigTransaction = new EmbeddedMultisigAccountModificationTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterKeyPair.PublicKey,
            MinApprovalDelta = 1,
            MinRemovalDelta = 1,
            AddressAdditions = new[] {new UnresolvedAddress(userAddress.bytes)}
        };
        var accountAddressRestrictionTransaction = new EmbeddedAccountAddressRestrictionTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterKeyPair.PublicKey,
            RestrictionFlags = new AccountRestrictionFlags((ushort) new[] {AccountRestrictionFlags.ADDRESS}.ToList()
                .Select(flag => (int) flag.Value).Sum()),
            RestrictionAdditions = new[] {new UnresolvedAddress(userAddress.bytes)}
        };
        var innerTransactions = new IBaseTransaction[] { multisigTransaction, accountAddressRestrictionTransaction };
        var merkleHash = SymbolFacade.HashEmbeddedTransactions(innerTransactions);
        var aggregateTx = new AggregateCompleteTransactionV2()
        {
            Network = NetworkType,
            SignerPublicKey = userKeyPair.PublicKey,
            Deadline = new Timestamp(Facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp),
            Transactions = innerTransactions,
            TransactionsHash = merkleHash,
        };
        TransactionHelper.SetMaxFee(aggregateTx, 100, 1);
        return aggregateTx;
    }
    
    public (AggregateCompleteTransactionV2 tx, string mosaicId) CreateRegistDataTransaction(string value, ulong rentalFee)
    {
        var masterPublicKey = new PublicKey(Converter.HexToBytes(Account.MasterPublicKey));
        var adminPublicKey = new PublicKey(Converter.HexToBytes(Account.PublicKey));
        var adminAddress = Facade.Network.PublicKeyToAddress(adminPublicKey);
        var masterAddress = Facade.Network.PublicKeyToAddress(masterPublicKey);
        
        var nonce = BitConverter.ToUInt32(Crypto.RandomBytes(8), 0);
        var mosaicId = IdGenerator.GenerateMosaicId(masterAddress, nonce);

        const bool supplyMutable = true;
        const bool transferable = true;
        const bool restrictable = true;
        const bool revokable = true;

        var mosaicFeeTransferTx = new EmbeddedTransferTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = adminPublicKey,
            RecipientAddress = new UnresolvedAddress(masterAddress.bytes),
            Mosaics = new UnresolvedMosaic[]
            {
                new ()
                {
                    MosaicId = new UnresolvedMosaicId(Convert.ToUInt64(CurrentNetoworkMosaicId, 16)),
                    Amount = new Amount(rentalFee),
                }
            },
        };
        var mosaicDefTx = new EmbeddedMosaicDefinitionTransactionV1()
        {
            Network = NetworkType,
            Nonce = new MosaicNonce(nonce),
            SignerPublicKey = masterPublicKey,
            Id = new MosaicId(mosaicId),
            Duration = new BlockDuration(),
            Divisibility = 0,
            Flags = new MosaicFlags(Converter.CreateMosaicFlags(supplyMutable, transferable, restrictable, revokable)),
        };
        var mosaicChangeTx = new EmbeddedMosaicSupplyChangeTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            MosaicId = new UnresolvedMosaicId(mosaicId),
            Action = MosaicSupplyChangeAction.INCREASE,
            Delta = new Amount(1000000),
        };
        var valueBytes = Converter.Utf8ToBytes(value);
        var mosaicMetadataTransaction = new EmbeddedMosaicMetadataTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            TargetAddress = new UnresolvedAddress(masterAddress.bytes),
            TargetMosaicId = new UnresolvedMosaicId(mosaicId),
            ScopedMetadataKey = Key,
            Value = valueBytes,
            ValueSizeDelta = (byte) valueBytes.Length
        };
        var mosaicGlobalResTx = new EmbeddedMosaicGlobalRestrictionTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            MosaicId = new UnresolvedMosaicId(mosaicId),
            RestrictionKey = Key,
            NewRestrictionValue = 1,
            NewRestrictionType = MosaicRestrictionType.EQ,
        };
        
        var mosaicAddressResTx1 = new EmbeddedMosaicAddressRestrictionTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            MosaicId = new UnresolvedMosaicId(mosaicId),
            RestrictionKey = Key,
            TargetAddress = new UnresolvedAddress(masterAddress.bytes),
            NewRestrictionValue = 1,
            PreviousRestrictionValue = 0xFFFFFFFFFFFFFFFF
        };
        
        var mosaicAddressResTx2 = new EmbeddedMosaicAddressRestrictionTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            MosaicId = new UnresolvedMosaicId(mosaicId),
            RestrictionKey = Key,
            TargetAddress = new UnresolvedAddress(adminAddress.bytes),
            NewRestrictionValue = 1,
            PreviousRestrictionValue = 0xFFFFFFFFFFFFFFFF
        };

        var mosaicTransferTransaction = new EmbeddedTransferTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            RecipientAddress = new UnresolvedAddress(adminAddress.bytes),
            Mosaics = new UnresolvedMosaic[]
            {
                new()
                {
                    MosaicId = new UnresolvedMosaicId(mosaicId),
                    Amount = new Amount(1)
                }
            },
        };

        var innerTransactions = new IBaseTransaction[]
        {
            mosaicFeeTransferTx, mosaicDefTx, mosaicChangeTx, mosaicMetadataTransaction, mosaicGlobalResTx,
            mosaicAddressResTx1, mosaicAddressResTx2, mosaicTransferTransaction
        };
        var merkleHash = SymbolFacade.HashEmbeddedTransactions(innerTransactions);

        var aggregateTx = new AggregateCompleteTransactionV2()
        {
            Network = NetworkType,
            SignerPublicKey = adminPublicKey,
            Transactions = innerTransactions,
            TransactionsHash = merkleHash,
            Deadline = new Timestamp(Facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp)
        };
        TransactionHelper.SetMaxFee(aggregateTx, 100);
        return (aggregateTx, mosaicId.ToString("X16"));
    }
    
    public TransferTransactionV1 CreateInputDataTransaction(string data, string mosaicId, string masterPublicKey, bool isEncrypt = false)
    {
        var headerByte = isEncrypt ? new byte[] {1} : new byte[] {0};
        var messageBytes = Converter.Utf8ToBytes(data);
        var masterAddress = Facade.Network.PublicKeyToAddress(new PublicKey(Converter.HexToBytes(masterPublicKey)));
        
        var transferTransaction = new TransferTransactionV1()
        {
            Network = NetworkType,
            RecipientAddress = new UnresolvedAddress(masterAddress.bytes),
            SignerPublicKey = new PublicKey(Converter.HexToBytes(Account.PublicKey)),
            Deadline = new Timestamp(Facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp),
            Mosaics = new UnresolvedMosaic[]
            {
                new ()
                {
                    MosaicId = new UnresolvedMosaicId(ulong.Parse(mosaicId, System.Globalization.NumberStyles.HexNumber)),
                    Amount = new Amount()
                }
            },
            Message = CombineByteArrays(headerByte, messageBytes),
        };
        TransactionHelper.SetMaxFee(transferTransaction, 100);
        return transferTransaction;
    }

    public async Task<AggregateBondedTransactionV2> CreateApplyDataTransaction(string mosaicId)
    {
        var mosaicClient = new SymbolRestClient.Api.MosaicRoutesApi(Node);
        var mosaic = await mosaicClient.GetMosaicAsync(mosaicId);
        var recipientAddress = new SymbolAddress(Converter.HexToBytes(mosaic.Mosaic.OwnerAddress));
        var address = Facade.Network.PublicKeyToAddress(Converter.HexToBytes(Account.PublicKey));

        var accountClient = new SymbolRestClient.Api.AccountRoutesApi(Node);
        var acc = await accountClient.GetAccountInfoAsync(recipientAddress.ToString());
        var masterPublicKey = new PublicKey(Converter.HexToBytes(acc.Account.PublicKey));
        var masterAddress = Facade.Network.PublicKeyToAddress(masterPublicKey).ToString();
        
        var json = await GetDataFromApi(Node, $"/restrictions/account/{masterAddress}");
        var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
        var values  = jsonObject["accountRestrictions"]["restrictions"][0]["values"];
        var hasRestrictionAddress = false;
        foreach (var jToken in (JArray) values)
        {
            var a = new SymbolAddress(Converter.HexToBytes((string)jToken ?? string.Empty));
            if (address.ToString() == a.ToString())
            {
                hasRestrictionAddress = true;
            } 
        }
        
        var mosaicAddressResTx = new EmbeddedMosaicAddressRestrictionTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = masterPublicKey,
            MosaicId = new UnresolvedMosaicId(Convert.ToUInt64(mosaicId, 16)),
            RestrictionKey = Key,
            TargetAddress = new UnresolvedAddress(address.bytes),
            NewRestrictionValue = 1,
            PreviousRestrictionValue = 0xFFFFFFFFFFFFFFFF
        };
        var dummyTransaction = new EmbeddedTransferTransactionV1()
        {
            Network = NetworkType,
            RecipientAddress = new UnresolvedAddress(recipientAddress.bytes),
            SignerPublicKey = new PublicKey(Converter.HexToBytes(Account.PublicKey)),
            Message = Converter.Utf8ToBytes("ApplyData"),
        };
        var sendMosaicTransaction = new EmbeddedTransferTransactionV1()
        {
            Network = NetworkType,
            RecipientAddress = new UnresolvedAddress(address.bytes),
            SignerPublicKey = masterPublicKey,
            Mosaics = new UnresolvedMosaic[]
            {
                new ()
                {
                    MosaicId = new UnresolvedMosaicId(Convert.ToUInt64(mosaicId, 16)),
                    Amount = new Amount(1)
                }
            }
        };

        var innerTransactions = new List<IBaseTransaction> { mosaicAddressResTx, dummyTransaction, sendMosaicTransaction};
        
        if (!hasRestrictionAddress)
        {
            var accountAddressRestrictionTransactionIncomming = new EmbeddedAccountAddressRestrictionTransactionV1()
            {
                Network = NetworkType,
                SignerPublicKey = masterPublicKey,
                RestrictionFlags = new AccountRestrictionFlags((ushort) new[] {AccountRestrictionFlags.ADDRESS}.ToList()
                    .Select(flag => (int) flag.Value).Sum()),
                RestrictionAdditions = new[] {new UnresolvedAddress(address.bytes)}
            };
            innerTransactions.Insert(0, accountAddressRestrictionTransactionIncomming);
        }

        var merkleHash = SymbolFacade.HashEmbeddedTransactions(innerTransactions.ToArray());
        var aggregateTx = new AggregateBondedTransactionV2()
        {
            Network = NetworkType,
            SignerPublicKey = new PublicKey(Converter.HexToBytes(Account.PublicKey)),
            Deadline = new Timestamp(Facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(48).Timestamp),
            Transactions = innerTransactions.ToArray(),
            TransactionsHash = merkleHash,
        };
        TransactionHelper.SetMaxFee(aggregateTx, 100, 1);

        return aggregateTx;
    }
    
    public HashLockTransactionV1 CreateHashLockTransaction(Hash256 hash)
    {
        var hashLockTx = new HashLockTransactionV1()
        {
            Network = NetworkType,
            SignerPublicKey = new PublicKey(Converter.HexToBytes(Account.PublicKey)),
            Mosaic = new UnresolvedMosaic() //10xym固定値
            {
                MosaicId = new UnresolvedMosaicId(Convert.ToUInt64(CurrentNetoworkMosaicId, 16)),
                Amount = new Amount(10 * 1000000)
            },
            Duration = new BlockDuration(480), // ロック有効期限
            Hash = hash, // このハッシュ値を登録
            Deadline = new Timestamp(Facade.Network.FromDatetime<NetworkTimestamp>(DateTime.UtcNow).AddHours(2).Timestamp),
        };
        TransactionHelper.SetMaxFee(hashLockTx, 100);
        return hashLockTx;
    }
    
    private static byte[] CombineByteArrays(byte[] firstArray, byte[] secondArray)
    {
        var combinedArray = new byte[firstArray.Length + secondArray.Length];
        Buffer.BlockCopy(firstArray, 0, combinedArray, 0, firstArray.Length);
        Buffer.BlockCopy(secondArray, 0, combinedArray, firstArray.Length, secondArray.Length);
        return combinedArray;
    }
    
    private async Task<string> Announce(string payload)
    {
        using var client = new HttpClient();
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response =  client.PutAsync(Node + "/transactions", content).Result;
        return await response.Content.ReadAsStringAsync();
    }
    
    private async Task<string> AnnounceBonded(string payload)
    {
        using var client = new HttpClient();
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response =  client.PutAsync(Node + "/transactions/partial", content).Result;
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<(string hash, string message)> AnnounceWithSignature(ITransaction transaction)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var privateKey = new PrivateKey(privateKeyStr);
        var keyPair = new KeyPair(privateKey);
        var signature = Facade.SignTransaction(keyPair, transaction);
        var payload = TransactionsFactory.AttachSignature(transaction, signature);
        var hash = Facade.HashTransaction(transaction);
        var message = await Announce(payload);
        return (hash.ToString(), message);
    }
    
    public async Task<(string hash, string message)> AnnounceWithSignatureBonded(ITransaction transaction)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var privateKey = new PrivateKey(privateKeyStr);
        var keyPair = new KeyPair(privateKey);
        var signature = Facade.SignTransaction(keyPair, transaction);
        var payload = TransactionsFactory.AttachSignature(transaction, signature);
        var hash = Facade.HashTransaction(transaction);
        var message = await AnnounceBonded(payload);
        return (hash.ToString(), message);
    }

    public async Task<(Hash256 hash, string payload)> SignByAdmin(ITransaction transaction)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var privateKey = new PrivateKey(privateKeyStr);
        var keyPair = new KeyPair(privateKey);
        var signature = Facade.SignTransaction(keyPair, transaction);
        var payload = TransactionsFactory.AttachSignature(transaction, signature);
        var hash = Facade.HashTransaction(transaction);
        return (hash, payload);
    }

    public async Task<string> AnnounceWithSignCosignature(string hash)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var keyPair = new KeyPair(new PrivateKey(privateKeyStr));
        var data = new Dictionary<string, string>()
        {
            {"parentHash", hash},
            {"signature", keyPair.Sign(hash).ToString()},
            {"signerPublicKey", keyPair.PublicKey.ToString()},
            {"version", "0"}
        };
        var json = JsonSerializer.Serialize(data);
        return await AnnounceCosignature(json);
    }
    
    private async Task<string> AnnounceCosignature(string data)
    {
        using var client = new HttpClient();
        var content = new StringContent(data, Encoding.UTF8, "application/json");
        var response =  client.PutAsync(Node + "/transactions/cosignature", content).Result;
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<(string hash, string message)> AnnounceForMultisig(AggregateCompleteTransactionV2 transaction)
    {
        var privateKeyStr = await SecureStorage.GetAsync("PrivateKey");
        var masterPrivateKeyStr = await SecureStorage.GetAsync("MasterPrivateKey");
        var keyPair = new KeyPair(new PrivateKey(privateKeyStr));
        var signature = Facade.SignTransaction(keyPair, transaction);
        TransactionsFactory.AttachSignature(transaction, signature);
        var hash = Facade.HashTransaction(transaction);

        var masterKeyPair = new KeyPair(new PrivateKey(masterPrivateKeyStr));
        var cosignature = new Cosignature
        {
            Signature = masterKeyPair.Sign(hash.bytes),
            SignerPublicKey = masterKeyPair.PublicKey
        };
        transaction.Cosignatures = new[] {cosignature};
        var payload = TransactionsFactory.CreatePayload(transaction);
        Console.WriteLine(payload);
        var message = await Announce(payload);
        return (hash.ToString(), message);
    }

    public async Task CheckTransactionStatus(string hash)
    {
        var jsonStr = await GetDataFromApi(Node, $"/transactionStatus/{hash}");
        var json = JsonNode.Parse(jsonStr);
        if (json != null)
        {
            var code = (string)json["code"];
            if (code != "Success")
            {
                throw new Exception(code);
            }
        }
    }

    public async Task<string> GetMosaicRentalFee()
    {
        var jsonStr = await GetDataFromApi(Node, "/network/fees/rental");
        var json = JsonNode.Parse(jsonStr);
        if (json != null) return (string) json["effectiveMosaicRentalFee"];
        throw new Exception("effectiveMosaicRentalFee is null");
    }
    
    private static async Task<string> GetDataFromApi(string _node, string _param)
    {
        var url = $"{_node}{_param}";
        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode) {
                return await response.Content.ReadAsStringAsync();
            }
            throw new Exception($"Error: {response.StatusCode}");
        }
        catch (Exception ex) {
            throw new Exception(ex.Message);
        }
    }

    public async Task GetAccountInfo(string address)
    {
        var accountApi = new SymbolRestClient.Api.AccountRoutesApi(Node);
        accountInfo = await accountApi.GetAccountInfoAsync(address);
    }

    public void SetXymAmount()
    {
        DataBaseServices.XymAmount = ulong.Parse(accountInfo.Account.Mosaics.Find(m => CurrentNetoworkMosaicId == m.Id.ToString()).Amount);
    }

    public async Task GetResisterdDataByMosaicId(string mosaicId)
    {
        var json = await GetDataFromApi(Node,$"/metadata?&targetId={mosaicId}");
        var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
        foreach (var meta in jsonObject.data)
        {
            try
            {
                var rootObject = JsonConvert.DeserializeObject<RootRegisterdDataModel>(Converter.HexToUtf8((string) meta.metadataEntry.value));
                rootObject.DataSigil.mosaicId = mosaicId;
                rootObject.DataSigil.isOwner = rootObject.DataSigil.masterPublicKey == Account.MasterPublicKey;
                RegisterdDataModels.Add(rootObject.DataSigil);
            }
            catch
            {
                // 何もしない
            }
        }
    }
    private AccountInfoDTO accountInfo { get; set; }
    public async Task GetRegisterdDatasFromApi(string mosaicId = null)
    {
        if (mosaicId != null)
        {
            if (!RegisterdDataModels.ToList().Exists((m) => m.mosaicId == mosaicId))
            {
                var json = await GetDataFromApi(Node,$"/metadata?&targetId={mosaicId}");
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                foreach (var meta in jsonObject.data)
                {
                    try
                    {
                        var rootObject = JsonConvert.DeserializeObject<RootRegisterdDataModel>(Converter.HexToUtf8((string) meta.metadataEntry.value));
                        rootObject.DataSigil.mosaicId = mosaicId;
                        rootObject.DataSigil.isOwner = rootObject.DataSigil.masterPublicKey == Account.MasterPublicKey;
                        RegisterdDataModels.Add(rootObject.DataSigil);
                    }
                    catch
                    {
                        // 何もしない
                    }
                }
            }
        }
        else
        {
            await GetAccountInfo(Account.PublicKey);
            var registerdDataModels = new ObservableCollection<RegisterdDataModel>();
            foreach (var t in accountInfo.Account.Mosaics.Where(t => t.Id != CurrentNetoworkMosaicId))
            {
                var json = await GetDataFromApi(Node,$"/metadata?&targetId={t.Id}");
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                foreach (var meta in jsonObject.data)
                {
                    try
                    {
                        var rootObject = JsonConvert.DeserializeObject<RootRegisterdDataModel>(Converter.HexToUtf8((string) meta.metadataEntry.value));
                        rootObject.DataSigil.mosaicId = t.Id;
                        rootObject.DataSigil.isOwner = rootObject.DataSigil.masterPublicKey == Account.MasterPublicKey;
                        registerdDataModels.Add(rootObject.DataSigil);
                    }
                    catch
                    {
                        // 何もしない
                    }
                }
            }
            RegisterdDataModels = registerdDataModels;
        }
    }

    public async Task GetInputAllDatas()
    {
        foreach (var registerdDataModel in DataBaseServices.RegisterdDataModels)
        {
            var count = 1;

            var list = new List<InputDataWithPageNation>();
            while (true)
            {
                var inputData = await GetInputData(registerdDataModel.mosaicId, registerdDataModel.isOwner, 20, count);
                if (inputData == null) break;
                list.Add(inputData);
                count++;
            }
            if (DataBaseServices.AllInputDataWithPageNation.ContainsKey(registerdDataModel.mosaicId))
            {
                DataBaseServices.AllInputDataWithPageNation.Remove(registerdDataModel.mosaicId);
            }
            DataBaseServices.AllInputDataWithPageNation.Add(registerdDataModel.mosaicId, list);
        }
    }
    
    public async Task<InputDataWithPageNation> GetInputData(string mosaicId, bool isAdmin, int pageSize = 100, int pageNumber = 1, string order = "desc")
    {
        var param = isAdmin ? $"/transactions/confirmed?transferMosaicId={mosaicId}&pageSize={pageSize}&pageNumber={pageNumber}&order={order}" 
            : $"/transactions/confirmed?signerPublicKey={Account.PublicKey}&transferMosaicId={mosaicId}&pageSize={pageSize}&pageNumber={pageNumber}&order={order}";
        var json = await GetDataFromApi(Node, param);
        var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
        if (jsonObject.data.Count == 0)
        {
            return null;
        }
        var pageNation = new PageNation(
            (int)jsonObject.pagination.pageNumber,
            (int)jsonObject.pagination.pageSize);
        var datas = new List<InputDataModel>();
        try
        {
            foreach (var d in jsonObject.data)
            {
                var byteArray = Converter.HexToBytes((string) d.transaction.message);
                var str = Encoding.UTF8.GetString(byteArray, 1, byteArray.Length - 1);
                
                var innerData = JsonConvert.DeserializeObject<InnerData>(str);
                if (d.transaction.type != 16724) continue;
                if (Facade.Network.epocTime != null)
                    datas.Add(new InputDataModel(
                            Facade.Network.epocTime.Value.AddMilliseconds(
                                long.Parse((string) d.meta.timestamp)),
                            (string) d.transaction.signerPublicKey,
                            innerData));
            }
        }
        catch
        {
            // 何もしない
        }
        return new InputDataWithPageNation(datas, pageNation);
    }

    public static string Encrypt(string data, string privateKey, string publicKey)
    {
        return Crypto.Encode(privateKey, publicKey, data);
    }
    
    public static string Decrypt(string decrypted, string privateKey, string publicKey)
    {
        return Crypto.Decode(privateKey, publicKey, decrypted);
    }
    
    private async Task<List<(string Hash, AggregateTransactionExtendedDTO AggregateTransactionExtendedDTO)>> GetAggregateTransactionExtendedDTOAndHash()
    {
        var result = new List<(string, AggregateTransactionExtendedDTO)>();
        var client = new SymbolRestClient.Api.TransactionRoutesApi(Node);
        var count = 1;
        var transactionInfoDTOs = new List<TransactionInfoDTO>();
        while (true)
        {
            var partialPage = await client.SearchPartialTransactionsAsync(Account.Address, null, null, null, null, null, null,
                null, null, null, null, null, count);
            if (partialPage.Data.Count == 0)
            {
                break;
            }
            transactionInfoDTOs = transactionInfoDTOs.Concat(partialPage.Data).ToList();
            count++;
        }

        foreach (var TransactionMeta in transactionInfoDTOs.Select(t => (TransactionMetaDTO) t.Meta.ActualInstance))
        {
            var partial = await client.GetPartialTransactionAsync(TransactionMeta.Hash);
            var aggregateTransactionExtendedDTO = (AggregateTransactionExtendedDTO) partial.Transaction.ActualInstance;
            result.Add((TransactionMeta.Hash, aggregateTransactionExtendedDTO));
        }
        return result;
    }

    public async Task<bool> GetApplyedPartialTransaction(string hash)
    {
        var client = new SymbolRestClient.Api.TransactionRoutesApi(Node);
        var partial = await client.GetPartialTransactionAsync(hash);
        var aggregateTransactionExtendedDTO = (AggregateTransactionExtendedDTO) partial.Transaction.ActualInstance;
        var counter = aggregateTransactionExtendedDTO.Transactions.Count == 4 ? 1 : 0;
        var t1 = (EmbeddedMosaicAddressRestrictionTransactionDTO) aggregateTransactionExtendedDTO.Transactions[counter].Transaction.ActualInstance;
        var t2 = (EmbeddedTransferTransactionDTO) aggregateTransactionExtendedDTO.Transactions[counter + 1].Transaction.ActualInstance;
        var t3 = (EmbeddedTransferTransactionDTO) aggregateTransactionExtendedDTO.Transactions[counter + 2].Transaction.ActualInstance;
        
        var address = Facade.Network
            .PublicKeyToAddress(aggregateTransactionExtendedDTO.SignerPublicKey).ToString();
        var targetAddress = new SymbolAddress(Converter.HexToBytes(t1.TargetAddress));
        if (address != targetAddress.ToString())
        {
            Console.WriteLine($@"address != targetAddress.ToString()");
            return false;
        }

        if (t2.Mosaics.Count != 0)
        {
            Console.WriteLine(@"t2.Mosaics.Count != 0");
            return false;
        }

        if (t3.Mosaics.Count != 1)
        {
            Console.WriteLine(@"t3.Mosaics.Count != 1");
            return false;
        }
        
        foreach (var unresolvedMosaic in t3.Mosaics)
        {
            if (unresolvedMosaic.Id == CurrentNetoworkMosaicId)
            {
                Console.WriteLine($@"unresolvedMosaic.Id != {CurrentNetoworkMosaicId}");
                return false;
            }

            if (unresolvedMosaic.Amount != "1")
            {
                Console.WriteLine($@"unresolvedMosaic.Amount != 1");
                return false;
            }
        }

        DataBaseServices.PartialModels.Add(new PartialModel(hash, t3.Mosaics[0].Id, address));
        return true;
    }

    public async Task GetApplyedPartialTransactions()
    {
        var hashAndAggs = await GetAggregateTransactionExtendedDTOAndHash();
        var result = new ObservableCollection<PartialModel>();
        if (hashAndAggs.Count == 0)
        {
            DataBaseServices.PartialModels = result;
        }
        else
        {
            try
            {
                foreach (var hashAndAgg in hashAndAggs)
                {
                    var counter = hashAndAgg.AggregateTransactionExtendedDTO.Transactions.Count == 4 ? 1 : 0;
                    var t1 = (EmbeddedMosaicAddressRestrictionTransactionDTO) hashAndAgg.AggregateTransactionExtendedDTO.Transactions[counter].Transaction.ActualInstance;
                    var t2 = (EmbeddedTransferTransactionDTO) hashAndAgg.AggregateTransactionExtendedDTO.Transactions[counter + 1].Transaction.ActualInstance;
                    var t3 = (EmbeddedTransferTransactionDTO) hashAndAgg.AggregateTransactionExtendedDTO.Transactions[counter + 2].Transaction.ActualInstance;
                    
                    var address = Facade.Network
                        .PublicKeyToAddress(hashAndAgg.AggregateTransactionExtendedDTO.SignerPublicKey).ToString();
                    var targetAddress = new SymbolAddress(Converter.HexToBytes(t1.TargetAddress));
                    if (address != targetAddress.ToString())
                    {
                        Console.WriteLine($@"address != targetAddress.ToString()");
                        break;
                    }

                    if (t2.Mosaics.Count != 0)
                    {
                        Console.WriteLine(@"t2.Mosaics.Count != 0");
                        break;
                    }

                    if (t3.Mosaics.Count != 1)
                    {
                        Console.WriteLine(@"t3.Mosaics.Count != 1");
                        break;
                    }
                    
                    foreach (var unresolvedMosaic in t3.Mosaics)
                    {
                        if (unresolvedMosaic.Id == CurrentNetoworkMosaicId)
                        {
                            Console.WriteLine($@"unresolvedMosaic.Id != {CurrentNetoworkMosaicId}");
                            break;
                        }

                        if (unresolvedMosaic.Amount != "1")
                        {
                            Console.WriteLine($@"unresolvedMosaic.Amount != 1");
                            break;
                        }
                    }
                    result.Add(new PartialModel(hashAndAgg.Hash, t3.Mosaics[0].Id, address));
                }
                DataBaseServices.PartialModels = result;
            }
            catch (Exception e)
            {
                Console.WriteLine($@"ERROR: {e.Message}");
            }   
        }
    }

    private ListenerService websocket;
    private ListenerService websocketForMaster;
    private ListenerService websocketBoded;
    
    public readonly Dictionary<string, Func<JsonNode, Task>> FuncList = new Dictionary<string, Func<JsonNode, Task>>();
    public void DeleteFunc(string key)
    {
        FuncList.Remove(key);
    }
    
    public readonly Dictionary<string, Func<JsonNode, Task>> BondedFuncList = new Dictionary<string, Func<JsonNode, Task>>();
    public void BondedDeleteFunc(string key)
    {
        BondedFuncList.Remove(key);
    }
    
    public readonly Dictionary<string, Func<JsonNode, Task>> FuncListForMaster = new Dictionary<string, Func<JsonNode, Task>>();
    public void DeleteAsOwnerFunc(string key)
    {
        FuncList.Remove(key);
    }
    
    public async void SetupWebSocketConfirmedForMaster()
    {
        websocketForMaster = new ListenerService(Node, new ClientWebSocket());
        await websocketForMaster.Open();
        await websocketForMaster.Confirmed(Account.MasterAddress, (tx) =>
        {
            Console.WriteLine("RECIEVED");
            foreach (var funcKeyValuePair in FuncListForMaster)
            {
                funcKeyValuePair.Value?.Invoke(tx);
            }
        });
    }
    
    public async void SetupWebSocketConfirmed()
    {
        websocket = new ListenerService(Node, new ClientWebSocket());
        await websocket.Open();
        await websocket.Confirmed(Account.Address, (tx) =>
        {
            foreach (var funcKeyValuePair in FuncList)
            {
                funcKeyValuePair.Value?.Invoke(tx);
            }
        });
    }
    
    public async void SetupWebSocketPartial()
    {
        websocketBoded = new ListenerService(Node, new ClientWebSocket());
        await websocketBoded.Open();
        await websocketBoded.AggregateBondedAdded(Account.MasterAddress, (tx) =>
        {
            foreach (var funcKeyValuePair in BondedFuncList)
            {
                funcKeyValuePair.Value?.Invoke(tx);
            }
        });
    }
}