using CatSdk.CryptoTypes;
using CatSdk.Facade;
using CatSdk.Symbol;

namespace DataSigil.Models;

public class AccountModel
{
    public string Username { get; set; }
    public string PrivateKey { get; set; }
    public string PublicKey { get; set; }
    public string Address { get; set; }
    public string MasterPrivateKey { get; set; }
    public string MasterPublicKey { get; set; }
    public string MasterAddress { get; set; }
    public ulong XymAmount { get; set; }
}