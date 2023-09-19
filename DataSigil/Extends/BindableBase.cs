using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataSigil.Extends;

public class BindableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value)) { return false; }
        field = value;
        //プロパティ変更を通知
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)); return true;
    }
}