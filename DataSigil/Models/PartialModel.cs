namespace DataSigil.Models;

public class PartialModel
{
    public string Hash { get; set; }
    public string MosaicID { get; set; }
    public string RecipientAddress { get; set; }

    public PartialModel(string Hash, string MosaicID, string RecipientAddress)
    {
        this.Hash = Hash;
        this.MosaicID = MosaicID;
        this.RecipientAddress = RecipientAddress;
    }
}