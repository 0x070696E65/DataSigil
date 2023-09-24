namespace DataSigil.Models;

public class RegisterdDataModel
{
    public string title;
    public string mosaicId;
    public string masterPublicKey;
    public bool isOwner;
    public List<List<object>> data { get; set; }
    public bool isEncrypt { get; set; }
}

public class RootRegisterdDataModel
{
    public RegisterdDataModel DataSigil { get; set; }
}

public class RegisterDataFieldItem
{
    public string DataTitle { get; set; }
    public string DataType { get; set; }
    public string DataSize { get; set; }
}