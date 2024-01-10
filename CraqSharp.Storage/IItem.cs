namespace CraqSharp.Storage;

public interface IItem<TKey, TValue>
{
    public TKey Key { get; set; }
    public TValue Value { get; set; }
    public ulong Version { get; set; }
    public bool Committed { get; set; }
}