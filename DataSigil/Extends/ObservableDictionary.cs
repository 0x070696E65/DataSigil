using System.Collections.Specialized;

namespace DataSigil.Extends;

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    public event EventHandler<NotifyDictionaryChangedEventArgs<TKey, TValue>> CollectionChanged;

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
        OnCollectionChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyCollectionChangedAction.Add, key, value));
    }

    public new bool Remove(TKey key)
    {
        if (base.Remove(key))
        {
            OnCollectionChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyCollectionChangedAction.Remove, key));
            return true;
        }
        return false;
    }

    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            base[key] = value;
            OnCollectionChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyCollectionChangedAction.Replace, key, value));
        }
    }

    protected virtual void OnCollectionChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> e)
    {
        CollectionChanged?.Invoke(this, e);
    }
}

public class NotifyDictionaryChangedEventArgs<TKey, TValue> : EventArgs
{
    public NotifyCollectionChangedAction Action { get; private set; }
    public TKey Key { get; private set; }
    public TValue Value { get; private set; }

    public NotifyDictionaryChangedEventArgs(NotifyCollectionChangedAction action, TKey key, TValue value = default(TValue))
    {
        Action = action;
        Key = key;
        Value = value;
    }
}