using DataSigil.Extends;
using DataSigil.Services;
using Newtonsoft.Json.Linq;

namespace DataSigil.Models;

public class InputDataWithPageNation
{
    public List<InputDataModel> Data { get; set; }
    public PageNation PageNation { get; set; }

    public InputDataWithPageNation(List<InputDataModel> _data, PageNation _pageNation)
    {
        Data = _data;
        PageNation = _pageNation;
    }
}

public class InputDataModel
{
    public DateTime DateTime { get; set; }
    public string SignerPublicKey { get; set; }
    public InnerData Data { get; set; }

    public InputDataModel(DateTime _date, string _signerPublicKey, InnerData _data)
    {
        DateTime = _date;
        SignerPublicKey = _signerPublicKey;
        Data = _data;
    }
}

public class PageNation
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public PageNation(int _pageNumber, int _pageSize)
    {
        PageNumber = _pageNumber;
        PageSize = _pageSize;
    }
}

public class InnerData
{
    public string MosaicId { get; set; }
    public string Title { get; set; }
    public string UserName { get; set; }
    public JObject Items { get; set; }
}