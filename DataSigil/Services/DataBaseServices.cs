using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using DataSigil.Extends;
using DataSigil.Models;

namespace DataSigil.Services;

public class DataBaseServices: ObservableObject
{
    private AccountModel account { get; set; } = new AccountModel();
    public AccountModel Account
    {
        get => account;
        set
        {
            if (account != value)
            {
                account = value;
                OnPropertyChanged(nameof(Account));
            }
        }
    }
    
    public ulong XymAmount
    {
        get => account.XymAmount;
        set
        {
            if (account.XymAmount != value)
            {
                account.XymAmount = value;
                OnPropertyChanged(nameof(XymAmount));
            }
        }
    }
    
    private ObservableCollection<RegisterdDataModel> registerdDataModels { get; set; } = new ObservableCollection<RegisterdDataModel>();
    public ObservableCollection<RegisterdDataModel> RegisterdDataModels
    {
        get => registerdDataModels;
        set
        {
            if (registerdDataModels != value)
            {
                registerdDataModels = value;
                OnPropertyChanged(nameof(RegisterdDataModels));
            }
        }
    }
    private ObservableCollection<PartialModel> partialModels { get; set; } = new ObservableCollection<PartialModel>();
    public ObservableCollection<PartialModel> PartialModels
    {
        get => partialModels;
        set
        {
            if (partialModels != value)
            {
                partialModels = value;
                OnPropertyChanged(nameof(PartialModels));
            }
        }
    }
    
    private ObservableDictionary<string, List<InputDataWithPageNation>> allInputDataWithPageNation { get; set; } = new ObservableDictionary<string, List<InputDataWithPageNation>>();
    public ObservableDictionary<string, List<InputDataWithPageNation>> AllInputDataWithPageNation
    {
        get => allInputDataWithPageNation;
        set
        {
            if (allInputDataWithPageNation != value)
            {
                allInputDataWithPageNation = value;
                OnPropertyChanged(nameof(AllInputDataWithPageNation));
            }
        }
    }
}